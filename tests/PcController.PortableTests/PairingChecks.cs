using System.Net;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using PcController.Host;
using PcController.Core;

internal static class PairingChecks
{
    internal static async Task Run(Action<bool, string> check, bool integration)
    {
        var directory = Path.Combine(Path.GetTempPath(), "PcPairTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var store = new ProtectedDevices(directory, new TestProtectedStorage());
            var now = DateTimeOffset.UtcNow;
            var pairing = new PairingService(store, () => now);
            bool Fails(Action action, string code) { try { action(); return false; } catch (InvalidOperationException e) { return e.Message == code; } }
            var expired = pairing.IssueChallenge(); now = now.AddMinutes(3);
            check(Fails(() => pairing.Request(expired.Value, "Phone"), "pair_expired_or_invalid"), "expired challenge rejected");
            var challenge = pairing.IssueChallenge(); var request = pairing.Request(challenge.Value, "Phone");
            check(pairing.Claim(request.Ticket).State == "pending" && store.List().Length == 0, "new device cannot authenticate before PC approval");
            check(Fails(() => pairing.Request(challenge.Value, "Other"), "pair_expired_or_invalid"), "pairing challenge consumed exactly once");
            pairing.Decide(request.Ticket, false);
            check(pairing.Claim(request.Ticket).State == "denied" && store.List().Length == 0, "PC denial never issues a credential");
            request = pairing.Request(pairing.IssueChallenge().Value, "Remembered phone"); pairing.Decide(request.Ticket, true);
            var claim = pairing.Claim(request.Ticket);
            check(claim.State == "approved" && claim.Credential != null && pairing.Authenticate(claim.Credential) != null, "approved device authenticates");
            check(pairing.Claim(request.Ticket).Credential == null, "approval credential claim cannot replay");
            check(store.List().Single().CredentialHash != claim.Credential && !File.ReadAllText(Path.Combine(directory, "devices.protected")).Contains(claim.Credential!), "device store contains no clear reusable credential");
            var reloaded = new PairingService(new ProtectedDevices(directory, new TestProtectedStorage()), () => now);
            check(reloaded.Authenticate(claim.Credential!) != null, "remembered authentication survives store reload");
            now = now.AddDays(31); check(reloaded.Authenticate(claim.Credential!) == null, "expired device credential rejected"); now = now.AddDays(-31);
            store.Revoke(claim.Device!.Id); check(pairing.Authenticate(claim.Credential!) == null, "revoked device rejected");
            now = now.AddMinutes(1);
            for (var i = 0; i < 5; i++) Fails(() => pairing.Request("", "Phone"), "pair_expired_or_invalid");
            check(Fails(() => pairing.Request(pairing.IssueChallenge().Value, "Phone"), "pair_rate_limit"), "pairing attempts rate limited");
            now = now.AddMinutes(2); request = pairing.Request(pairing.IssueChallenge().Value, "Phone"); now = now.AddMinutes(3); pairing.Decide(request.Ticket, true);
            check(pairing.Claim(request.Ticket).State == "pair_expired" && store.List().Length == 0, "late PC approval cannot resurrect an expired ticket");
            var failing = new PairingService(new FailedDeviceRepository());
            var failedRequest = failing.Request(failing.IssueChallenge().Value, "Storage fixture");
            failing.Decide(failedRequest.Ticket, true);
            check(failing.Claim(failedRequest.Ticket) is { State: "storage_failed", Credential: null }, "protected-store failure cannot approve a browser");
            var sessions = new SessionOwnership(() => now); var releases = 0;
            sessions.ReleaseInput += () => releases++;
            var first = sessions.Reserve("one", false); var connected = sessions.Connect("one", first.Ticket)!;
            check(connected == first.Session && sessions.Connect("one", first.Ticket) == null, "session upgrade ticket is single use");
            check(Fails(() => sessions.Reserve("two", false), "controller_busy"), "second controller requires explicit takeover");
            var second = sessions.Reserve("two", true);
            check(releases == 1 && first.Session.Stop.IsCancellationRequested && !sessions.Execute(first.Session, () => throw new Exception()), "takeover releases and cancels previous owner before replacement");
            sessions.Connect("two", second.Ticket); sessions.Pause(true);
            check(releases == 2 && second.Session.Stop.IsCancellationRequested && Fails(() => sessions.Reserve("one", false), "paused"), "pause immediately cancels ownership and release hook");
            sessions.Pause(false); var pending = sessions.Reserve("one", false); now = now.AddSeconds(31);
            check(sessions.Connect("one", pending.Ticket) == null && releases == 3, "unclaimed session reservation expires");
            var revoked = sessions.Reserve("one", false); sessions.Connect("one", revoked.Ticket); sessions.Revoke("one");
            check(revoked.Session.Stop.IsCancellationRequested && releases == 4, "revocation immediately releases session");
            var exit = sessions.Reserve("two", false); sessions.Connect("two", exit.Ticket); sessions.Shutdown();
            check(exit.Session.Stop.IsCancellationRequested && releases == 5, "shutdown releases owner");
            if (integration) await Integration(check, Path.Combine(directory, "server"));
        }
        finally { Directory.Delete(directory, true); }
    }
    private sealed class FailedDeviceRepository : IDeviceRepository
    {
        public DeviceRecord[] List() => [];
        public void Add(DeviceRecord device) => throw new IOException("Test storage unavailable.");
        public void Revoke(string id) => throw new IOException("Test storage unavailable.");
    }
    private static async Task Integration(Action<bool, string> check, string directory)
    {
        var mouse = new RecordingMouse();
        await using var server = PortableFixture.Create(directory, mouse);
        var decision = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        server.ApproveDeviceAsync = (_, _) => decision.Task; // Test fixture only; production uses the actual WPF approval window.
        await server.StartAsync();
        const string origin = "https://localhost:8443";
        bool Trust(object _, X509Certificate? certificate, X509Chain? __, SslPolicyErrors errors)
        {
            if (certificate == null || (errors & (SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateNotAvailable)) != 0) return false;
            using var chain = new X509Chain(); using var leaf = new X509Certificate2(certificate);
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(server.Certificates.Root);
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.ApplicationPolicy.Add(new System.Security.Cryptography.Oid("1.3.6.1.5.5.7.3.1"));
            return chain.Build(leaf);
        }
        var cookies = new CookieContainer();
        using var handler = new HttpClientHandler { CookieContainer = cookies, ServerCertificateCustomValidationCallback = (r, c, ch, e) => Trust(r, c, ch, e) };
        using var http = new HttpClient(handler) { BaseAddress = new Uri(origin), Timeout = TimeSpan.FromSeconds(5) };
        async Task<HttpResponseMessage> Post(string path, string json, string? suppliedOrigin = origin)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            if (suppliedOrigin != null) request.Headers.TryAddWithoutValidation("Origin", suppliedOrigin);
            return await http.SendAsync(request);
        }
        async Task<JsonDocument> Json(HttpResponseMessage response) => JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var unauth = await Post("/api/session/claim", "{\"takeover\":false}");
        check(unauth.StatusCode == HttpStatusCode.Unauthorized, "real HTTPS unauthenticated session rejected");
        foreach (var badOrigin in new string?[] { null, "null", origin + "/", "https://attacker.invalid" })
            check((await Post("/api/pair/request", "{}", badOrigin)).StatusCode == HttpStatusCode.Forbidden, "HTTP mutation exact Origin enforcement");
        foreach (var json in new[] { "{", "{}", "{\"challenge\":null,\"name\":\"Phone\"}", "{\"challenge\":\"x\",\"challenge\":\"x\",\"name\":\"Phone\"}", new string('x', 1025) })
            check((await Post("/api/pair/request", json)).StatusCode == HttpStatusCode.BadRequest, "bounded malformed pairing HTTP rejected");
        var challenge = server.Pairing.CurrentChallenge!.Value;
        var response = await Post("/api/pair/request", JsonSerializer.Serialize(new { challenge, name = "Integration phone" }));
        check(response.StatusCode == HttpStatusCode.Accepted, "real HTTPS pairing waits for PC decision");
        check(response.Headers.GetValues("Set-Cookie").All(v => v.Contains("secure", StringComparison.OrdinalIgnoreCase) && v.Contains("httponly", StringComparison.OrdinalIgnoreCase) && v.Contains("samesite=strict", StringComparison.OrdinalIgnoreCase) && v.Contains("path=/")), "pair cookie Secure HttpOnly Strict host path");
        using (var pending = await Json(await Post("/api/pair/poll", "{}"))) check(pending.RootElement.GetProperty("state").GetString() == "pending", "poll cannot bypass PC approval");
        check((await Post("/api/pair/request", JsonSerializer.Serialize(new { challenge, name = "Replay" }))).StatusCode == HttpStatusCode.BadRequest, "HTTPS challenge replay rejected");
        decision.SetResult(true);
        // This deadline covers many separate TLS/WebSocket connections. Fresh
        // Windows runners need more than ten seconds for the complete suite.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        while (server.Devices.List().Length == 0) await Task.Delay(20, timeout.Token);
        response = await Post("/api/pair/poll", "{}");
        using (var approved = await Json(response)) check(approved.RootElement.GetProperty("state").GetString() == "approved" && !approved.RootElement.TryGetProperty("credential", out _), "credential issued only in HttpOnly cookie after approval");
        var cookie = response.Headers.GetValues("Set-Cookie").Single(v => v.StartsWith("__Host-pc-device="));
        check(cookie.Contains("secure", StringComparison.OrdinalIgnoreCase) && cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase) && cookie.Contains("samesite=strict", StringComparison.OrdinalIgnoreCase), "remembered cookie flags enforced");
        using (var status = await Json(await http.GetAsync("/api/status"))) check(status.RootElement.GetProperty("authenticated").GetBoolean(), "remembered cookie authenticates real HTTPS");
        ClientWebSocket Socket()
        {
            var socket = new ClientWebSocket(); socket.Options.Cookies = cookies;
            socket.Options.SetRequestHeader("Origin", origin); socket.Options.RemoteCertificateValidationCallback = Trust; return socket;
        }
        async Task Connect(ClientWebSocket socket) => await socket.ConnectAsync(new Uri("wss://localhost:8443/control/ws"), timeout.Token);
        async Task Ping(ClientWebSocket socket)
        {
            await socket.SendAsync(Encoding.UTF8.GetBytes("{\"v\":1,\"seq\":1,\"type\":\"ping\",\"payload\":{}}"), WebSocketMessageType.Text, true, timeout.Token);
            using var pong = await SocketReplies.Json(socket, timeout.Token, chunkSize: 127);
            check(pong.RootElement.GetProperty("payload").GetProperty("capabilities").GetProperty("platform").GetString() == "recording", "fragmented additive capability reply retains camel-case v1 metadata");
            check(pong.RootElement.GetProperty("type").GetString() == "pong" && pong.RootElement.GetProperty("session").GetString() != null, "authenticated real WSS ping/pong with verified certificate");
        }
        check((await Post("/api/session/claim", "{\"takeover\":false}")).IsSuccessStatusCode, "approved device reserves one session");
        using (var socket = Socket())
        {
            await Connect(socket); await Ping(socket);
            check((await Post("/api/session/claim", "{\"takeover\":false}")).StatusCode == HttpStatusCode.Conflict, "real HTTPS ownership refuses implicit takeover");
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", timeout.Token);
        }
        while (server.Sessions.OwnerDeviceId != null) await Task.Delay(20, timeout.Token);
        async Task Fault(byte[] bytes, WebSocketMessageType messageType, string label)
        {
            check((await Post("/api/session/claim", "{\"takeover\":false}")).IsSuccessStatusCode, "authenticated fault check can reserve: " + label);
            using var faulty = Socket(); await Connect(faulty);
            var releases = mouse.Events.Count(e => e == "release");
            var holds = mouse.Events.Count(e => e == "left:True");
            await faulty.SendAsync(Encoding.UTF8.GetBytes("{\"v\":1,\"seq\":1,\"type\":\"pointer.button\",\"payload\":{\"button\":\"left\",\"action\":\"down\"}}"), WebSocketMessageType.Text, true, timeout.Token);
            while (mouse.Events.Count(e => e == "left:True") == holds) await Task.Delay(10, timeout.Token);
            await faulty.SendAsync(bytes, messageType, true, timeout.Token);
            var closed = false;
            try { closed = (await faulty.ReceiveAsync(new byte[2048], timeout.Token)).MessageType == WebSocketMessageType.Close; } catch (WebSocketException) { closed = true; }
            while (server.Sessions.OwnerDeviceId != null) await Task.Delay(10, timeout.Token);
            check(closed && mouse.Events.Count(e => e == "release") > releases, "authenticated malformed input closes with held-button cleanup: " + label);
        }
        await Fault(Encoding.UTF8.GetBytes("{"), WebSocketMessageType.Text, "invalid JSON");
        await Fault(new byte[1025], WebSocketMessageType.Text, "oversized");
        await Fault(new byte[1], WebSocketMessageType.Binary, "binary");
        await Fault(Encoding.UTF8.GetBytes("{\"v\":1,\"seq\":1,\"type\":\"ping\",\"payload\":{}}"), WebSocketMessageType.Text, "sequence replay");
        check((await Post("/api/session/claim", "{\"takeover\":false}")).IsSuccessStatusCode, "authenticated rate check reserves owner");
        using (var limited = Socket())
        {
            await Connect(limited);
            var releases = mouse.Events.Count(e => e == "release");
            var readClose = Task.Run(async () =>
            {
                var buffer = new byte[1024];
                try { while ((await limited.ReceiveAsync(buffer, timeout.Token)).MessageType != WebSocketMessageType.Close) { } }
                catch (WebSocketException) { }
            });
            await limited.SendAsync(Encoding.UTF8.GetBytes("{\"v\":1,\"seq\":1,\"type\":\"pointer.button\",\"payload\":{\"button\":\"left\",\"action\":\"down\"}}"), WebSocketMessageType.Text, true, timeout.Token);
            try
            {
                for (var sequence = 2; sequence <= 240; sequence++)
                    await limited.SendAsync(Encoding.UTF8.GetBytes($"{{\"v\":1,\"seq\":{sequence},\"type\":\"ping\",\"payload\":{{}}}}"), WebSocketMessageType.Text, true, timeout.Token);
            }
            catch (WebSocketException) { }
            await readClose;
            while (server.Sessions.OwnerDeviceId != null) await Task.Delay(10, timeout.Token);
            check(mouse.Events.Count(e => e == "release") > releases && limited.CloseStatusDescription == "rate_limit", "authenticated socket rate excess closes and releases held input");
        }
        check((await Post("/api/session/claim", "{\"takeover\":false}")).IsSuccessStatusCode, "remembered device reconnects after disconnect");
        using (var socket = Socket())
        {
            await Connect(socket); await Ping(socket);
            await socket.SendAsync(Encoding.UTF8.GetBytes("{\"v\":1,\"seq\":2,\"type\":\"pointer.button\",\"payload\":{\"button\":\"left\",\"action\":\"down\"}}"), WebSocketMessageType.Text, true, timeout.Token);
            while (!mouse.Events.Contains("left:True")) await Task.Delay(20, timeout.Token);
            server.RevokeDevice(server.Devices.List().Single().Id);
            bool closed;
            try { closed = (await socket.ReceiveAsync(new byte[1024], timeout.Token)).MessageType == WebSocketMessageType.Close; }
            catch (WebSocketException) { closed = true; }
            check(closed && server.Sessions.OwnerDeviceId == null && mouse.Events.Last() == "release", "revocation immediately closes active real WSS and releases adapter");
        }
        check((await Post("/api/session/claim", "{\"takeover\":false}")).StatusCode == HttpStatusCode.Unauthorized, "revoked remembered browser cannot reconnect");
        check((await http.GetAsync("/health")).IsSuccessStatusCode, "host survives pairing rejection and revocation");
    }
}
