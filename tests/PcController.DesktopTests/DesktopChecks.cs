using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using PcController.Core;
using PcController.Desktop;
using PcController.Host;

internal static class DesktopChecks
{
    internal static async Task Run(Action<bool, string> check)
    {
        var candidate = new NetworkCandidate("fixture", "Test interface", "network-a", [IPAddress.Parse("192.168.40.10")]);
        var candidates = new[] { candidate };
        var networks = new TrustedNetworks(() => candidates);
        check(networks.Read().PrivateAddresses.Length == 0, "private/candidate address is never automatically trusted");
        check(networks.Approve(candidate) && networks.Read().PrivateAddresses.SequenceEqual(candidate.Addresses), "explicit network approval selects exact bindings");
        candidates = [candidate with { Identity = "network-b" }];
        check(networks.Read().PrivateAddresses.Length == 0 && !networks.Approve(candidate), "same-IP identity change and stale selection require new approval");
        networks.Approve(candidates[0]); networks.Invalidate();
        check(networks.Read().PrivateAddresses.Length == 0, "connectivity event removes session approval even with unchanged identity");
        await using (var broken = new DesktopController("macos", new TrustedNetworks(() => []), _ => throw new IOException("Port 8443 is in use (fixture)."), observeNetworks: false, environment: new FakeEnvironment()))
        {
            await broken.RefreshAsync(); await broken.RefreshAsync();
            check(broken.Server == null && broken.Message.Contains("Port 8443 is in use (fixture)."), "host setup failure stays visible across readiness refreshes");
        }
        await EnvironmentChecks(check);
        await NetworkTrustChecks(check);
        await using (var blocked = new DesktopController("macos", networks, observeNetworks: false))
        { await blocked.ApproveNetworkAsync(candidates[0]); check(blocked.Server == null && blocked.Message.Contains("Keychain"), "normal startup cannot replace secure storage with recording/plaintext services"); }
        var now = DateTimeOffset.UtcNow;
        var expiry = new ApprovalPrompt(new("ticket", "Test", now.AddSeconds(1)), "loopback", () => now);
        now = now.AddSeconds(2); expiry.Expire(); check(!await expiry.Decision, "approval expires to denial");
        var directory = Path.Combine(Path.GetTempPath(), "PcDesktopChecks-" + Guid.NewGuid().ToString("N"));
        try
        {
            bool mismatchRejected = false;
            try { await using var mismatch = PortableFixture.Create(directory, new RecordingMouse(), network: new([IPAddress.Parse("127.0.0.2")], [], "Mismatched SAN fixture")); }
            catch (ArgumentException) { mismatchRejected = true; }
            check(mismatchRejected, "host rejects certificate SANs that disagree with exact listener binding");
            var input = new RecordingMouse();
            var loopback = new TrustedNetworks(() => []);
            await using var controller = new DesktopController("macos", loopback, network => PortableFixture.Create(directory, input,
                network: network, capabilities: PlatformCapabilities.Unavailable("macos")), observeNetworks: false);
            await controller.RefreshAsync(); var server = controller.Server!;
            check(server != null && server.Network.PrivateAddresses.Length == 0 && server.ConnectionAddress == "https://localhost:8443", "shell connects explicit fixture factory with aligned loopback address");
            check(!server!.Capabilities.InputEnabled, "fixture shell reports native input unavailable");
            using var handler = new HttpClientHandler { CookieContainer = new CookieContainer(), ServerCertificateCustomValidationCallback = (_, leaf, _, errors) =>
            {
                if (leaf == null || (errors & ~SslPolicyErrors.RemoteCertificateChainErrors) != 0) return false;
                using var chain = new X509Chain(); chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(server.Certificates.Root); chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                return chain.Build(leaf);
            } };
            using var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:8443") };
            async Task<JsonElement> Post(string route, object body)
            {
                using var message = new HttpRequestMessage(HttpMethod.Post, route) { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };
                message.Headers.Add("Origin", "https://localhost:8443");
                using var response = await client.SendAsync(message); using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); return document.RootElement.Clone();
            }
            ApprovalPrompt? pending = null;
            controller.ApprovalRequested += p => pending = p;
            async Task Request()
            {
                pending = null; controller.NewPairing();
                var result = await Post("/api/pair/request", new { challenge = server.Pairing.CurrentChallenge!.Value, name = "Shell test browser" });
                check(result.GetProperty("state").GetString() == "pending" && pending != null, "HTTPS pairing routes request to local shell approval");
            }
            async Task<string?> Poll()
            {
                for (var i = 0; i < 30; i++) { var state = (await Post("/api/pair/poll", new { })).GetProperty("state").GetString(); if (state != "pending") return state; await Task.Delay(20); }
                throw new Exception("Approval not completed");
            }
            await Request(); pending!.Decide(false); check(await Poll() == "denied" && server.Devices.List().Length == 0, "shell denial does not store authorization");
            var blockedWrite = Path.Combine(directory, "certificates", "devices.protected.tmp");
            Directory.CreateDirectory(blockedWrite);
            await Request(); pending!.Decide(true); check(await Poll() == "storage_failed" && server.Devices.List().Length == 0, "shell approval fails closed when protected device persistence fails");
            Directory.Delete(blockedWrite);
            await Request(); pending!.Decide(true); check(await Poll() == "approved" && server.Devices.List().Length == 1, "shell approval persists and HTTPS poll issues cookie");
            check(server.PairingAddress == null, "consumed QR link is removed");
            var device = server.Devices.List().Single();
            var reservation = server.Sessions.Reserve(device.Id, false); server.Sessions.Connect(device.Id, reservation.Ticket);
            check(controller.Status == "Controller connected", "shell controller status follows host ownership");
            controller.PauseResume(); check(server.Sessions.Paused && server.Sessions.OwnerDeviceId == null && input.Events.Contains("release"), "Pause cancels ownership and cleans tracked input");
            controller.PauseResume(); check(!server.Sessions.Paused, "Resume restores hosting without advertising unavailable input");
            input.FailNext = true; controller.PauseResume();
            check(server.Sessions.Paused, "failed local cleanup cannot accidentally Resume control");
            controller.PauseResume(); check(!server.Sessions.Paused, "explicit Resume retries successful cleanup");
            controller.ReleaseInput(); check(input.Events.Last() == "release", "shell Release input calls cleanup");
            controller.SavePreferences(new(1.5, false), "VLC"); check(server.PointerSettings.Value == new PointerPreferences(1.5, false) && server.MediaSettings.Profile == "VLC", "shell preferences persist through shared host settings");
            controller.Revoke(device.Id); check(server.Devices.List().Length == 0, "shell revocation removes remembered authorization");
            var root = server.Certificates.Fingerprint;
            await Request(); var replacedPrompt = pending!;
            await controller.ConnectivityChangedAsync();
            check(!await replacedPrompt.Decision && controller.Server != server && controller.Server!.Certificates.Fingerprint == root, "connectivity replacement denies prompts and preserves trusted root");
            check(controller.Server!.Sessions.Paused, "network replacement retains safety pause until explicit Resume");
            server = controller.Server!;
            await Request(); var exitPrompt = pending!;
            await controller.DisposeAsync();
            check(!await exitPrompt.Decision && controller.Server == null, "Exit denies pending approval and disposes host/listeners");
            server.Pairing.Decide(exitPrompt.Request.Ticket, true);
            check(server.Devices.List().Length == 0, "late approval after shutdown cannot persist a device");
            bool closed = false; try { server.Pairing.IssueChallenge(); } catch (InvalidOperationException) { closed = true; }
            check(closed, "shutdown pairing service refuses fresh challenges");
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
    private static async Task EnvironmentChecks(Action<bool, string> check)
    {
        var directory = Path.Combine(Path.GetTempPath(), "PcDesktopEnvironment-" + Guid.NewGuid().ToString("N"));
        var environment = new FakeEnvironment();
        try
        {
            await using var controller = new DesktopController("macos", new TrustedNetworks(() => []), snapshot =>
            {
                var server = PortableFixture.Create(directory, new RecordingMouse(), network: snapshot, capabilities: environment.State.InputReady ? PlatformCapabilities.ProtocolV1 : PlatformCapabilities.Unavailable("macos"));
                server.Sessions.Pause(true); return server;
            }, observeNetworks: false, environment: environment);
            await controller.RefreshAsync(); var first = controller.Server!; var root = first.Certificates.Fingerprint;
            check(first.Sessions.Paused, "lifecycle preserves platform startup pause");
            controller.PauseResume(); check(!first.Sessions.Paused && environment.Armed == 1, "Resume validates and arms the current native environment");
            first.Sessions.Pause(true); environment.FailArm = true; controller.PauseResume();
            check(first.Sessions.Paused, "native generation change racing Resume cannot enable ownership"); environment.FailArm = false;
            environment.NetworkEvent = false; environment.NotifyUnsafe(); environment.NetworkEvent = true;
            check(first.Sessions.Paused, "pause-only native lifecycle callback (Space/display) still pauses synchronously before async refresh");
            environment.State = new(true, false, true, "Permission lost"); await controller.RefreshAsync();
            check(first != controller.Server && controller.Server!.Sessions.Paused && !controller.InputReady && !controller.Server.Capabilities.InputEnabled, "permission loss replaces host paused without native input capability");
            // PauseResume schedules an un-awaited refresh that may replace the host; let it settle first.
            controller.PauseResume(); await controller.RefreshAsync(); check(controller.Server?.Sessions.Paused == true, "missing permission cannot Resume");
            environment.State = new(true, true, false, "Keychain locked"); await controller.RefreshAsync();
            check(controller.Server == null && controller.Message == "Keychain locked", "locked storage stops all listeners with actionable state");
            check(!controller.StorageReady && controller.HasStorageAuthorization && environment.Authorizations == 0, "storage failure offers explicit local Keychain consent without requesting it automatically");
            environment.DenyAuthorization = true; await controller.AuthorizeStorageAsync();
            check(controller.Server == null && environment.Authorizations == 1 && controller.Message.Contains("not granted"), "denied OS storage consent keeps listeners stopped and resets nothing");
            environment.DenyAuthorization = false; environment.State = new(true, true, true, "Storage restored"); await controller.AuthorizeStorageAsync();
            check(environment.Authorizations == 2 && controller.StorageReady && controller.Server!.Certificates.Fingerprint == root && controller.Server.Sessions.Paused, "approved OS storage consent rechecks storage, retains root and stays paused");
            environment.State = new(true, true, true, "Storage restored"); await controller.RefreshAsync();
            check(controller.Server!.Certificates.Fingerprint == root && controller.Server.Sessions.Paused, "storage recovery retains root and requires deliberate Resume");
            environment.State = new(true, false, true, "Accessibility required"); await controller.RefreshAsync();
            var unready = controller.Server!;
            // Permission granted, then Resume before the periodic refresh sees it: Resume's own
            // Inspect consumes the change, so its refresh must still rebuild the capabilities.
            environment.State = new(true, true, true, "Accessibility granted");
            controller.PauseResume(); await controller.RefreshAsync();
            check(!unready.Capabilities.InputEnabled && controller.Server != unready && controller.Server!.Capabilities.InputEnabled && controller.Server.Sessions.Paused,
                "Resume right after a permission grant rebuilds the host with ready capabilities and stays paused");
            environment.FailCleanup = true;
            try { await controller.DisposeAsync(); throw new Exception("Exit accepted failed cleanup"); }
            catch (InputFailureException) { check(controller.Server != null && controller.Server.Sessions.Paused, "failed native cleanup keeps graceful Exit open and paused"); }
            environment.FailCleanup = false;
            await controller.DisposeAsync(); var repeated = controller.DisposeAsync();
            check(repeated.IsCompletedSuccessfully && controller.Server == null, "Exit can be retried after failed cleanup; later Exit/OS shutdown disposals reuse the completed cleanup");
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
    private static async Task NetworkTrustChecks(Action<bool, string> check)
    {
        var candidate = new NetworkCandidate("fixture", "Test Wi-Fi", "network-a", [IPAddress.Parse("192.168.40.10")]);
        var networks = new TrustedNetworks(() => [candidate]);
        var environment = new FakeEnvironment();
        await using var controller = new DesktopController("macos", networks, observeNetworks: false, environment: environment);
        await controller.RefreshAsync(); await controller.ApproveNetworkAsync(candidate);
        bool Approved() => networks.Read().PrivateAddresses.SequenceEqual(candidate.Addresses);
        check(Approved(), "explicit approval selects the LAN interface");
        environment.NetworkEvent = false; environment.State = new(true, true, true, "Space changed") { NetworkChanged = false };
        environment.NotifyUnsafe(); await controller.RefreshAsync();
        check(Approved(), "Space/full-screen or display transition pauses but keeps the session's LAN approval");
        environment.State = new(true, false, true, "Secure Input active") { NetworkChanged = false }; await controller.RefreshAsync();
        check(Approved() && !controller.InputReady, "Secure Input/permission readiness change keeps LAN approval");
        environment.State = new(true, true, true, "Ready") { NetworkChanged = false }; await controller.RefreshAsync();
        environment.NetworkEvent = true; environment.NotifyUnsafe();
        check(!Approved(), "network, sleep or session-switch event revokes LAN approval synchronously");
        await controller.RefreshAsync(); await controller.ApproveNetworkAsync(candidate); check(Approved(), "LAN can be approved again explicitly");
        environment.NetworkEvent = false; environment.State = new(true, true, true, "Woke on a network") { NetworkChanged = true }; await controller.RefreshAsync();
        check(!Approved(), "network change found at inspection revokes LAN approval");
        environment.State = new(true, true, false, "Keychain locked") { NetworkChanged = false }; await controller.ApproveNetworkAsync(candidate);
        check(!Approved(), "storage failure still revokes LAN approval");
        var legacy = new TrustedNetworks(() => [candidate]); var plain = new DefaultEnvironment();
        await using var linux = new DesktopController("linux", legacy, observeNetworks: false, environment: plain);
        await linux.RefreshAsync(); legacy.Approve(candidate); plain.Notify();
        check(legacy.Read().PrivateAddresses.Length == 0, "environments without the distinction keep revoking LAN approval on every unsafe event");
    }
    private sealed class DefaultEnvironment : IDesktopEnvironment
    {
        public event Action? UnsafeChanged;
        public void Notify() => UnsafeChanged?.Invoke();
        public DesktopEnvironmentState Inspect() => new(false, true, true, "Ready");
        public void Arm(DesktopEnvironmentState inspected) { }
        public void ReleaseInput() { }
        public void Onboard() { }
        public void Dispose() { }
    }
    internal sealed class FakeEnvironment : IDesktopEnvironment
    {
        public DesktopEnvironmentState State = new(false, true, true, "Ready");
        public bool NetworkEvent = true;
        public bool NetworkChangedSinceInspect => NetworkEvent;
        public int Armed, Authorizations;
        public bool FailCleanup, FailArm, DenyAuthorization;
        public bool CanAuthorizeStorage => true;
        public void AuthorizeStorage() { Authorizations++; if (DenyAuthorization) throw new IOException("Keychain access was not granted (fixture)."); }
        public event Action? UnsafeChanged;
        public void NotifyUnsafe() => UnsafeChanged?.Invoke();
        public DesktopEnvironmentState Inspect() { var value = State; State = State with { Changed = false, NetworkChanged = false }; return value; }
        public void Arm(DesktopEnvironmentState inspected) { if (FailArm) throw new InputFailureException(); Armed++; }
        public void ReleaseInput() { if (FailCleanup) throw new InputFailureException(); }
        public void Onboard() { }
        public void Dispose() { }
    }

}
