using System.Diagnostics;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

internal static class DesktopChecks
{
    internal static async Task Run(Action<bool, string> check, string executable)
    {
        using var root = X509CertificateLoader.LoadCertificateFromFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PcController", "certificates", "pc-remote-root.cer"));
        using var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (_, certificate, _, errors) =>
        {
            if (certificate == null || (errors & (SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateNotAvailable)) != 0) return false;
            using var chain = new X509Chain(); chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(root); chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.ApplicationPolicy.Add(new("1.3.6.1.5.5.7.3.1")); return chain.Build(certificate);
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        async Task<bool> Paused() { using var response = JsonDocument.Parse(await client.GetStringAsync("https://localhost:8443/api/status")); return response.RootElement.GetProperty("paused").GetBoolean(); }
        async Task Command(string argument)
        {
            using var child = Process.Start(new ProcessStartInfo(executable, argument) { UseShellExecute = false, CreateNoWindow = true })!;
            await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            check(child.ExitCode == 0, "running companion accepts local action " + argument);
        }
        var current = Process.GetProcessesByName("PcController.App").Single();
        var originallyPaused = await Paused();
        try
        {
            await Command("");
            check(Process.GetProcessesByName("PcController.App").Length == 1 && !current.HasExited, "second launch opens existing app without another server");
            await Command("--pause"); check(await Paused(), "real companion pause immediately reflected over verified HTTPS");
            await Command("--resume"); check(!await Paused(), "real companion resumes from local integration command");
            current.Refresh(); check(current.CloseMainWindow(), "setup close message delivered"); await Task.Delay(300);
            check(!current.HasExited && (await client.GetAsync("https://localhost:8443/health")).IsSuccessStatusCode, "closing setup keeps real tray server running");
            await Command(""); current.Refresh(); check(current.MainWindowHandle != 0, "Open restores hidden setup window");
        }
        finally { if (!current.HasExited) await Command(originallyPaused ? "--pause" : "--resume"); }
        await Command("--exit");
        check(current.HasExited, "Exit helper waits for primary teardown before installer may replace files");
        check(current.HasExited, "Exit shuts down real companion gracefully"); current.Dispose();
    }
}
