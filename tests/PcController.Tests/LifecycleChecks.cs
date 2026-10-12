using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32;
using PcController.App;
using PcController.Core;
using PcController.Windows;

internal static class LifecycleChecks
{
    internal static void Local(Action<bool, string> check)
    {
        check(!FirewallRules.Exists(Path.Combine(Path.GetTempPath(), "PcRemote-absent-" + Guid.NewGuid().ToString("N") + ".exe")),
            "real Windows firewall absent-rule lookup is safe without elevation");
        var suffix = Guid.NewGuid().ToString("N");
        using var primary = new UserInstance(suffix);
        using var secondary = new UserInstance(suffix);
        check(primary.IsPrimary && !secondary.IsPrimary, "single same-user instance enforced");
        var actions = new List<LocalAction>();
        var listener = primary.ListenAsync(action => { actions.Add(action); return Task.CompletedTask; });
        foreach (var action in Enum.GetValues<LocalAction>())
            check(secondary.SendAsync(action).GetAwaiter().GetResult(), "same-user local IPC acknowledgement: " + action);
        check(!secondary.SendAsync((LocalAction)255).GetAwaiter().GetResult() && actions.SequenceEqual(Enum.GetValues<LocalAction>()), "IPC accepts only declared local actions");
        primary.Dispose(); listener.GetAwaiter().GetResult();
        // Preserve the user's preference, even if a verification check fails.
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        var original = key.GetValue(SignInStartup.ValueName);
        var kind = original == null ? RegistryValueKind.String : key.GetValueKind(SignInStartup.ValueName);
        try
        {
            SignInStartup.SetEnabled(true, Environment.ProcessPath!);
            check(SignInStartup.Enabled && (string)key.GetValue(SignInStartup.ValueName)! == "\"" + Environment.ProcessPath + "\" --background", "current-user sign-in executable quoted with background option");
            SignInStartup.SetEnabled(false, Environment.ProcessPath!);
            check(!SignInStartup.Enabled, "optional sign-in preference can be removed");
        }
        finally { if (original == null) key.DeleteValue(SignInStartup.ValueName, false); else key.SetValue(SignInStartup.ValueName, original, kind); }
    }
    internal static async Task Network(Action<bool, string> check)
    {
        var directory = Path.Combine(Path.GetTempPath(), "PcLifecycleTests", Guid.NewGuid().ToString("N"));
        var snapshot = new NetworkSnapshot([], [IPAddress.Parse("192.0.2.1")], "No trusted test network; loopback only");
        var input = new RecordingMouse();
        var creates = 0;
        try
        {
            await using var lifetime = new ServerLifecycle(network => { creates++; return new CompanionServer(directory, input, network: network); }, () => snapshot);
            await lifetime.RefreshAsync(); var first = lifetime.Server!;
            var root = first.Certificates.Fingerprint;
            var request = first.Pairing.Request(first.Pairing.CurrentChallenge!.Value, "Lifecycle phone");
            first.Pairing.Decide(request.Ticket, true); var credential = first.Pairing.Claim(request.Ticket).Credential!;
            first.MediaSettings.Save("Stremio"); first.PointerSettings.Save(new(2, true));
            var session = first.Sessions.Reserve(first.Pairing.Authenticate(credential)!.Id, false);
            first.Sessions.Connect(session.Session.DeviceId, session.Ticket);
            first.Sessions.Execute(session.Session, () => first.Input.Execute(new(1, "pointer.button", Button: "left", Action: "down")));
            await lifetime.RefreshAsync(); check(creates == 1 && ReferenceEquals(first, lifetime.Server), "unchanged network does not restart listeners");
            snapshot = snapshot with { AllAddresses = [IPAddress.Parse("192.0.2.2")] };
            await lifetime.RefreshAsync(); var next = lifetime.Server!;
            check(creates == 2 && !ReferenceEquals(first, next) && session.Session.Stop.IsCancellationRequested && input.Events.Contains("release"), "address change cancels ownership and releases input before rebind");
            check(next.Certificates.Fingerprint == root && next.Pairing.Authenticate(credential) != null && next.MediaSettings.Profile == "Stremio" && next.PointerSettings.Value == new PointerPreferences(2, true), "network refresh preserves root, remembered device and settings");
            check(next.Network.PrivateAddresses.Length == 0 && next.ConnectionAddress == "https://localhost:8443", "untrusted addresses are never bound for LAN control");
            using var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (_, certificate, _, errors) =>
            {
                if (certificate == null || (errors & (SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateNotAvailable)) != 0) return false;
                using var chain = new X509Chain(); chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(next.Certificates.Root); chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.ApplicationPolicy.Add(new("1.3.6.1.5.5.7.3.1")); return chain.Build(certificate);
            };
            using var client = new HttpClient(handler);
            check((await client.GetAsync("https://localhost:8443/health")).IsSuccessStatusCode, "replacement listener serves real certificate-validated HTTPS");
            next.Sessions.Pause(true); snapshot = snapshot with { AllAddresses = [] };
            await lifetime.RefreshAsync(); check(lifetime.Server!.Sessions.Paused, "user pause survives network refresh");
            await lifetime.DisposeAsync();
            check(lifetime.Server == null && input.Events.Last() == "release", "graceful lifetime exit releases input and stops server");
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
