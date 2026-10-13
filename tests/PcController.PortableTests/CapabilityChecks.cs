using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using PcController.Core;

internal static class CapabilityChecks
{
    internal static async Task Run(Action<bool, string> check)
    {
        check(InputSemantics.Modifier("Windows") == LogicalModifier.Meta && InputSemantics.ScrollUnitsPerDetent == 120,
            "v1 Meta alias and normalized scroll units are explicit");
        var emptyMedia = PlatformCapabilities.Unavailable("test-empty-media") with
        { MediaActions = new Dictionary<string, IReadOnlySet<string>> { ["General"] = new HashSet<string>() } };
        check(!emptyMedia.InputEnabled && !emptyMedia.Supports(new(1, "media.action", Action: "mute", Profile: "General")),
            "empty media capability cannot advertise input or authorize a media action");
        var directory = Path.Combine(Path.GetTempPath(), "PcCapabilities-" + Guid.NewGuid().ToString("N"));
        try
        {
            var recording = new RecordingMouse(); var actions = new RecordingPcActions();
            var capabilities = PlatformCapabilities.Unavailable("test-unavailable");
            using (var input = new InputController(recording, new(), startWatchdog: false, capabilities: capabilities))
            {
                bool failed = false;
                try { input.Execute(new(1, "pointer.move", 10, 10)); } catch (InputFailureException) { failed = true; }
                check(failed && recording.Events.SequenceEqual(["release"]), "unavailable input is rejected with cleanup before adapter invocation");
            }
            await using var server = PortableFixture.Create(directory, recording, actions, capabilities: capabilities);
            await server.StartAsync();
            bool Trust(X509Certificate2? leaf, SslPolicyErrors errors)
            {
                if (leaf == null || (errors & (SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateNotAvailable)) != 0) return false;
                using var chain = new X509Chain();
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(server.Certificates.Root);
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.ApplicationPolicy.Add(new("1.3.6.1.5.5.7.3.1"));
                return chain.Build(leaf);
            }
            const string origin = "https://localhost:8443";
            var cookies = new CookieContainer();
            using var handler = new HttpClientHandler { CookieContainer = cookies, ServerCertificateCustomValidationCallback = (_, c, _, e) => Trust(c, e) };
            using var client = new HttpClient(handler) { BaseAddress = new(origin) };
            using (var health = JsonDocument.Parse(await client.GetStringAsync("/health")))
                check(!health.RootElement.GetProperty("inputEnabled").GetBoolean(), "health never advertises unavailable native input");
            var request = server.Pairing.Request(server.Pairing.CurrentChallenge!.Value, "Capabilities fixture");
            server.Pairing.Decide(request.Ticket, true); var claim = server.Pairing.Claim(request.Ticket);
            cookies.Add(new Uri(origin), new Cookie("__Host-pc-device", claim.Credential!, "/") { Secure = true, HttpOnly = true });
            using (var status = JsonDocument.Parse(await client.GetStringAsync("/api/status")))
                check(status.RootElement.GetProperty("capabilities").GetProperty("platform").GetString() == "test-unavailable", "authenticated additive capabilities identify the platform");
            var reservation = server.Sessions.Reserve(claim.Device!.Id, false);
            server.Sessions.Connect(claim.Device.Id, reservation.Ticket);
            foreach (var (path, body) in new (string, object)[]
            {
                ("/api/pc/volume", new { session = reservation.Session.Id, level = 20 }),
                ("/api/pc/launch", new { session = reservation.Session.Id, id = new string('a', 32) }),
                ("/api/pc/power/prepare", new { session = reservation.Session.Id, action = "sleep" }),
                ("/api/settings/media", new { profile = "VLC" })
            })
            {
                using var message = new HttpRequestMessage(HttpMethod.Post, path) { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };
                message.Headers.Add("Origin", origin);
                using var response = await client.SendAsync(message);
                check(response.StatusCode == HttpStatusCode.Conflict && (await response.Content.ReadAsStringAsync()).Contains("unsupported_action"), "backend blocks unavailable capability: " + path);
            }
            check(actions.Events.Length == 0 && server.Power.Pending == null, "capability rejection does not invoke native PC actions or schedule power");
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
