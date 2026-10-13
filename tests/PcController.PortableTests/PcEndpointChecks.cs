using System.Net;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using PcController.Host;

internal static class PcEndpointChecks
{
    internal static async Task Run(Action<bool,string> check)
    {
        var directory = Path.Combine(Path.GetTempPath(),"PcFeatures-" + Guid.NewGuid().ToString("N"));
        try
        {
            var pc = new RecordingPcActions(); var input = new RecordingMouse();
            await using var server = PortableFixture.Create(directory, input, pc);
            foreach (var entry in server.Launcher.List()) server.Launcher.Remove(entry.Id);
            server.Launcher.Add(PortableFixture.TestExecutable(directory),"Notes","General");
            await server.StartAsync();
            const string origin = "https://localhost:8443";
            bool Trust(X509Certificate? certificate, SslPolicyErrors errors)
            {
                if (certificate == null || (errors & (SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateNotAvailable)) != 0) return false;
                using var chain = new X509Chain(); using var leaf = new X509Certificate2(certificate);
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust; chain.ChainPolicy.CustomTrustStore.Add(server.Certificates.Root);
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck; chain.ChainPolicy.ApplicationPolicy.Add(new("1.3.6.1.5.5.7.3.1"));
                return chain.Build(leaf);
            }
            var cookies = new CookieContainer();
            using var handler = new HttpClientHandler { CookieContainer = cookies, ServerCertificateCustomValidationCallback = (_,c,_,e) => Trust(c,e) };
            using var http = new HttpClient(handler) { BaseAddress = new(origin), Timeout = TimeSpan.FromSeconds(5) };
            async Task<HttpResponseMessage> Post(string path, object body, string? suppliedOrigin = origin)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post,path) { Content = new StringContent(JsonSerializer.Serialize(body),Encoding.UTF8,"application/json") };
                if (suppliedOrigin != null) request.Headers.Add("Origin",suppliedOrigin);
                return await http.SendAsync(request);
            }
            async Task<JsonDocument> Json(HttpResponseMessage response) => JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            check((await http.GetAsync("/api/pc")).StatusCode == HttpStatusCode.Unauthorized,"unpaired browser cannot read launcher or volume");
            var unpaired = await Post("/api/pc/volume",new { session = new string('a',32), level = 20 });
            check(unpaired.StatusCode == HttpStatusCode.Unauthorized,"unpaired browser cannot change PC volume (status " + (int)unpaired.StatusCode + ")");
            var pairing = server.Pairing.Request(server.Pairing.CurrentChallenge!.Value,"Feature tests"); server.Pairing.Decide(pairing.Ticket,true);
            var claim = server.Pairing.Claim(pairing.Ticket);
            cookies.Add(new Uri(origin),new Cookie("__Host-pc-device",claim.Credential!,"/") { Secure = true, HttpOnly = true });
            using var catalog = await Json(await http.GetAsync("/api/pc"));
            var appId = catalog.RootElement.GetProperty("apps")[0].GetProperty("id").GetString()!;
            check(!catalog.RootElement.GetRawText().Contains("test-player.exe",StringComparison.OrdinalIgnoreCase),"phone receives app names and IDs without executable paths");
            check((await Post("/api/pc/launch",new { session = new string('a',32), id = appId })).StatusCode == HttpStatusCode.Conflict,"paired browser needs a live controller session to launch apps");
            using var reservation = await Json(await Post("/api/session/claim",new { takeover = false }));
            var session = reservation.RootElement.GetProperty("session").GetString()!;
            using var socket = new ClientWebSocket(); socket.Options.Cookies = cookies; socket.Options.SetRequestHeader("Origin",origin);
            socket.Options.RemoteCertificateValidationCallback = (_,c,_,e) => Trust(c,e);
            await socket.ConnectAsync(new("wss://localhost:8443/control/ws"),CancellationToken.None);
            using var heartbeatStop = new CancellationTokenSource();
            var heartbeat = Task.Run(async () =>
            {
                long seq = 0;
                try
                {
                    while (!heartbeatStop.IsCancellationRequested)
                    {
                        await socket.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { v = 1, seq = ++seq, type = "ping", payload = new { } })),WebSocketMessageType.Text,true,heartbeatStop.Token);
                        using var reply = await SocketReplies.Json(socket, heartbeatStop.Token);
                        await Task.Delay(1000,heartbeatStop.Token);
                    }
                }
                catch (Exception exception) when (exception is OperationCanceledException or WebSocketException) { }
            });
            try
            {
                check((await Post("/api/pc/volume",new { session, level = 20 },"https://foreign.invalid")).StatusCode == HttpStatusCode.Forbidden,"PC actions require exact origin");
                check((await Post("/api/pc/volume",new { session = new string('b',32), level = 20 })).StatusCode == HttpStatusCode.Conflict,"stale controller cannot mutate PC state");
                foreach (var level in new object[] { -1,101,2.5,"50" })
                    check((await Post("/api/pc/volume",new { session, level })).StatusCode == HttpStatusCode.BadRequest,"volume rejects non-integer or out-of-range values");
                check((await Post("/api/pc/volume",new { session, level = 55, extra = true })).StatusCode == HttpStatusCode.BadRequest,"PC endpoints reject extra fields");
                check((await Post("/api/pc/volume",new { session, level = 55 })).IsSuccessStatusCode && pc.GetVolume().Level == 55,"authenticated exact volume reaches the PC adapter");
                check((await Post("/api/pc/launch",new { session, id = new string('b',32) })).StatusCode == HttpStatusCode.BadRequest,"phone cannot launch an unconfigured executable");
                check((await Post("/api/pc/launch",new { session, id = appId })).IsSuccessStatusCode && pc.Events.Contains("launch:Notes"),"configured launcher app reaches the PC adapter");
                check((await Post("/api/pc/launch",new { session, id = appId })).StatusCode == HttpStatusCode.TooManyRequests,"rapid app launch is bounded");
                check((await Post("/api/pc/power/prepare",new { session, action = "format" })).StatusCode == HttpStatusCode.BadRequest,"power endpoint rejects arbitrary commands");
                foreach (var action in new[] { "restart","shutdown" })
                {
                    using var prepared = await Json(await Post("/api/pc/power/prepare",new { session, action }));
                    var token = prepared.RootElement.GetProperty("token").GetString()!;
                    check(server.Power.Pending == null,"power preparation has no power side effect");
                    check((await Post("/api/pc/power/confirm",new { session, token })).IsSuccessStatusCode && server.Power.Pending?.Action == action,"explicit confirmation schedules " + action);
                    check((await Post("/api/pc/power/confirm",new { session, token })).StatusCode == HttpStatusCode.Conflict,"power confirmation replay rejected over HTTPS");
                    check((await Post("/api/pc/power/cancel",new { session })).IsSuccessStatusCode && server.Power.Pending == null,"phone cancels countdown before execution");
                }
                using var sleep = await Json(await Post("/api/pc/power/prepare",new { session, action = "sleep" }));
                await Post("/api/pc/power/confirm",new { session, token = sleep.RootElement.GetProperty("token").GetString() });
                check(!pc.Events.Any(e => e.StartsWith("power:")),"countdown has no immediate power side effect");
                var until = DateTimeOffset.UtcNow.AddSeconds(33);
                while (!pc.Events.Contains("power:sleep") && DateTimeOffset.UtcNow < until) await Task.Delay(250);
                check(pc.Events.Count(e => e == "power:sleep") == 1 && server.Power.Pending == null,"actual scheduler invokes the recording power adapter once after countdown");
                using var disconnect = await Json(await Post("/api/pc/power/prepare",new { session, action = "shutdown" }));
                await Post("/api/pc/power/confirm",new { session, token = disconnect.RootElement.GetProperty("token").GetString() });
                server.Sessions.Pause(true);
                check(server.Power.Pending == null,"PC pause cancels pending phone power");
                check((await Post("/api/pc/volume",new { session, level = 10 })).StatusCode == HttpStatusCode.Conflict,"PC pause blocks volume changes");
                check(!pc.Events.Contains("power:shutdown") && !pc.Events.Contains("power:restart"),"cancelled shutdown and restart never reach adapter");
            }
            finally { heartbeatStop.Cancel(); socket.Abort(); await heartbeat; }
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory,true); }
    }
}
