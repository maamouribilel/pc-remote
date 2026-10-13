using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using PcController.Host;
using PcController.Core;

internal static class SecurityChecks
{
    internal static async Task Run(Action<bool, string> check)
    {
        var directory = Path.Combine(Path.GetTempPath(), "PcSecurity-" + Guid.NewGuid().ToString("N"));
        try
        {
            var input = new RecordingMouse(); var pc = new RecordingPcActions();
            await using var server = PortableFixture.Create(directory, input, pc);
            await server.StartAsync();
            const string origin = "https://localhost:8443";
            bool Trust(X509Certificate? certificate, SslPolicyErrors errors)
            {
                if (certificate == null || (errors & (SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateNotAvailable)) != 0) return false;
                using var leaf = new X509Certificate2(certificate); using var chain = new X509Chain();
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(server.Certificates.Root);
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.ApplicationPolicy.Add(new("1.3.6.1.5.5.7.3.1"));
                return chain.Build(leaf);
            }
            var cookies = new CookieContainer();
            using var pairedHandler = new HttpClientHandler { CookieContainer = cookies, ServerCertificateCustomValidationCallback = (_, c, _, e) => Trust(c, e) };
            using var anonymousHandler = new HttpClientHandler { UseCookies = false, ServerCertificateCustomValidationCallback = (_, c, _, e) => Trust(c, e) };
            using var paired = new HttpClient(pairedHandler) { BaseAddress = new(origin), Timeout = TimeSpan.FromSeconds(8) };
            using var anonymous = new HttpClient(anonymousHandler) { BaseAddress = new(origin), Timeout = TimeSpan.FromSeconds(8) };
            async Task<HttpResponseMessage> Post(HttpClient client, string path, object body, string? suppliedOrigin = origin)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };
                if (suppliedOrigin != null) request.Headers.Add("Origin", suppliedOrigin);
                return await client.SendAsync(request);
            }
            using (var page = await anonymous.GetAsync("/"))
            {
                check(page.IsSuccessStatusCode && page.Headers.GetValues("Content-Security-Policy").Single().Contains("frame-ancestors 'none'"), "browser page denies framing and uses a CSP");
                check(page.Headers.GetValues("X-Content-Type-Options").Single() == "nosniff" && page.Headers.GetValues("Referrer-Policy").Single() == "no-referrer", "browser replies prevent MIME sniffing and referrer leakage");
            }
            foreach (var path in new[] { "/certificates/root.protected", "/devices.protected", "/.env", "/../certificates/server.protected" })
                using (var response = await anonymous.GetAsync(path)) check(response.StatusCode == HttpStatusCode.NotFound, "private file is not served: " + path);
            foreach (var path in new[] { "/api/pc/launch", "/api/pc/volume", "/api/pc/power/prepare", "/api/pc/power/confirm", "/api/pc/power/cancel", "/api/settings/media" })
                using (var response = await Post(anonymous, path, new { })) check(response.StatusCode == HttpStatusCode.Unauthorized, "unpaired mutation denied: " + path);
            foreach (var suppliedOrigin in new string?[] { null, "null", origin + "/", "https://foreign.invalid" })
                using (var response = await Post(anonymous, "/api/pc/power/cancel", new { }, suppliedOrigin)) check(response.StatusCode == HttpStatusCode.Forbidden, "power cancel requires exact browser Origin");

            var request = server.Pairing.Request(server.Pairing.CurrentChallenge!.Value, "Security fixture");
            server.Pairing.Decide(request.Ticket, true); var claim = server.Pairing.Claim(request.Ticket);
            cookies.Add(new Uri(origin), new Cookie("__Host-pc-device", claim.Credential!, "/") { Secure = true, HttpOnly = true });
            var reservation = server.Sessions.Reserve(claim.Device!.Id, false);
            using (var response = await Post(paired, "/api/settings/media", new { profile = "VLC" }))
                check(response.StatusCode == HttpStatusCode.Conflict && server.MediaSettings.Profile == "General", "a reserved but disconnected session cannot change media settings");
            var session = server.Sessions.Connect(claim.Device.Id, reservation.Ticket)!;
            using (var response = await Post(paired, "/api/settings/media", new { profile = "VLC" }))
                check(response.IsSuccessStatusCode && server.MediaSettings.Profile == "VLC", "live authenticated controller can save media profile");

            using (var delayed = new GatedJson("{\"profile\":", "\"Stremio\"}"))
            using (var message = new HttpRequestMessage(HttpMethod.Post, "/api/settings/media") { Content = delayed })
            {
                message.Headers.Add("Origin", origin);
                var responseTask = paired.SendAsync(message);
                await delayed.Started.Task.WaitAsync(TimeSpan.FromSeconds(3));
                await Task.Delay(250); // Let the server await the incomplete chunked JSON body.
                server.Sessions.Pause(true); delayed.Finish.TrySetResult();
                using var response = await responseTask;
                check(response.StatusCode == HttpStatusCode.Conflict && server.MediaSettings.Profile == "VLC", "pause during a request body prevents a late media mutation");
            }
            server.Sessions.Pause(false);
            reservation = server.Sessions.Reserve(claim.Device.Id, false); session = server.Sessions.Connect(claim.Device.Id, reservation.Ticket)!;
            using (var delayed = new GatedJson("{\"profile\":", "\"Stremio\"}"))
            using (var message = new HttpRequestMessage(HttpMethod.Post, "/api/settings/media") { Content = delayed })
            {
                message.Headers.Add("Origin", origin); var responseTask = paired.SendAsync(message);
                await delayed.Started.Task.WaitAsync(TimeSpan.FromSeconds(3)); await Task.Delay(250);
                server.Sessions.End(session);
                reservation = server.Sessions.Reserve(claim.Device.Id, false); session = server.Sessions.Connect(claim.Device.Id, reservation.Ticket)!;
                delayed.Finish.TrySetResult(); using var response = await responseTask;
                check(response.StatusCode == HttpStatusCode.Conflict && server.MediaSettings.Profile == "VLC", "reconnection cannot authorize a previous session's delayed media request");
            }

            if (server.Launcher.List().Length == 0) server.Launcher.Add(PortableFixture.TestExecutable(directory), "Security no-op", "General");
            var configuredId = server.Launcher.List()[0].Id;
            foreach (var route in new[] { "volume", "launch", "power/prepare", "power/confirm", "power/cancel" })
            {
                var nextToken = "";
                var initialEvents = pc.Events.Length;
                using var delayedPc = new GatedJson("{\"session\":", () => JsonSerializer.Serialize(session.Id) + (route switch
                {
                    "volume" => ",\"level\":19}", "launch" => ",\"id\":" + JsonSerializer.Serialize(configuredId) + "}",
                    "power/prepare" => ",\"action\":\"sleep\"}", "power/confirm" => ",\"token\":" + JsonSerializer.Serialize(nextToken) + "}",
                    _ => "}"
                }));
                using var messagePc = new HttpRequestMessage(HttpMethod.Post, "/api/pc/" + route) { Content = delayedPc };
                messagePc.Headers.Add("Origin", origin); var resultPc = paired.SendAsync(messagePc);
                await delayedPc.Started.Task.WaitAsync(TimeSpan.FromSeconds(3)); await Task.Delay(250);
                server.Sessions.End(session); reservation = server.Sessions.Reserve(claim.Device.Id, false); session = server.Sessions.Connect(claim.Device.Id, reservation.Ticket)!;
                if (route is "power/confirm" or "power/cancel")
                {
                    var preparedPc = server.Power.Prepare("sleep", claim.Device.Id, session.Id); nextToken = preparedPc.Token;
                    if (route == "power/cancel") server.Power.Confirm(nextToken, claim.Device.Id, session.Id);
                }
                delayedPc.Finish.TrySetResult(); using var responsePc = await resultPc;
                check(responsePc.StatusCode == HttpStatusCode.Conflict && pc.Events.Length == initialEvents && (route != "power/cancel" || server.Power.Pending != null),
                    "delayed PC body cannot adopt a replacement session or its power ticket: " + route);
                server.Power.Cancel();
            }

            using (var delayed = new GatedJson("{\"profile\":", "\"Stremio\"}"))
            using (var message = new HttpRequestMessage(HttpMethod.Post, "/api/settings/media") { Content = delayed })
            {
                message.Headers.Add("Origin", origin); var responseTask = paired.SendAsync(message);
                await delayed.Started.Task.WaitAsync(TimeSpan.FromSeconds(3)); await Task.Delay(250);
                server.RevokeDevice(claim.Device.Id); delayed.Finish.TrySetResult(); using var response = await responseTask;
                check(response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.Unauthorized && server.MediaSettings.Profile == "VLC", "revocation during a request body prevents a late media mutation");
            }
            request = server.Pairing.Request(server.Pairing.IssueChallenge().Value, "Replacement security fixture");
            server.Pairing.Decide(request.Ticket, true); claim = server.Pairing.Claim(request.Ticket);
            cookies.Add(new Uri(origin), new Cookie("__Host-pc-device", claim.Credential!, "/") { Secure = true, HttpOnly = true });
            reservation = server.Sessions.Reserve(claim.Device!.Id, false); session = server.Sessions.Connect(claim.Device.Id, reservation.Ticket)!;
            using (var delayedClaim = new GatedJson("{\"takeover\":", "true}"))
            using (var message = new HttpRequestMessage(HttpMethod.Post, "/api/session/claim") { Content = delayedClaim })
            {
                message.Headers.Add("Origin", origin); var responseTask = paired.SendAsync(message);
                await delayedClaim.Started.Task.WaitAsync(TimeSpan.FromSeconds(3)); await Task.Delay(250);
                server.RevokeDevice(claim.Device.Id); delayedClaim.Finish.TrySetResult();
                using var response = await responseTask;
                check(response.StatusCode == HttpStatusCode.Unauthorized && server.Sessions.OwnerDeviceId == null, "revocation during session claim body cannot reserve a fresh owner");
            }
            request = server.Pairing.Request(server.Pairing.IssueChallenge().Value, "Safety security fixture");
            server.Pairing.Decide(request.Ticket, true); claim = server.Pairing.Claim(request.Ticket);
            cookies.Add(new Uri(origin), new Cookie("__Host-pc-device", claim.Credential!, "/") { Secure = true, HttpOnly = true });
            reservation = server.Sessions.Reserve(claim.Device!.Id, false); session = server.Sessions.Connect(claim.Device.Id, reservation.Ticket)!;
            var preparation = server.Power.Prepare("sleep", claim.Device.Id, session.Id);
            server.Power.Confirm(preparation.Token, claim.Device.Id, session.Id);
            var rejected = 0;
            for (var i = 0; i < 125; i++)
            {
                using var response = await anonymous.GetAsync("/health");
                if (response.StatusCode == HttpStatusCode.TooManyRequests) rejected++;
            }
            check(rejected > 0, "anonymous HTTP flooding is bounded");
            using (var forged = new HttpRequestMessage(HttpMethod.Get, "/health"))
            {
                forged.Headers.Add("Cookie", "__Host-pc-device=" + new string('a', 64));
                forged.Headers.Add("X-Forwarded-For", "192.0.2.123");
                using var response = await anonymous.SendAsync(forged);
                check(response.StatusCode == HttpStatusCode.TooManyRequests, "forged cookies and forwarded headers cannot create a fresh request budget");
                check(response.Headers.RetryAfter?.Delta == TimeSpan.FromSeconds(60) && (await response.Content.ReadAsStringAsync()).Contains("rate_limit"), "HTTP rate rejection gives a bounded retry hint and JSON error");
            }
            using (var response = await paired.GetAsync("/api/pc"))
                check(response.IsSuccessStatusCode, "anonymous traffic cannot exhaust the paired device's HTTP budget");
            using (var response = await Post(paired, "/api/pc/power/cancel", new { session = session.Id }))
                check(response.IsSuccessStatusCode && server.Power.Pending == null, "paired controller can cancel power after anonymous flooding");

            for (var i = 0; i < 125; i++) { using var response = await paired.GetAsync("/api/pc"); }
            preparation = server.Power.Prepare("sleep", claim.Device.Id, session.Id); server.Power.Confirm(preparation.Token, claim.Device.Id, session.Id);
            using (var response = await Post(paired, "/api/pc/power/cancel", new { session = session.Id }))
                check(response.IsSuccessStatusCode && server.Power.Pending == null, "power cancellation has capacity even after the device's ordinary HTTP budget is exhausted");
            check(pc.Events.All(e => !e.StartsWith("power:")) && input.Events.All(e => e == "release"), "security probes neither execute power nor inject native input");
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    private sealed class GatedJson : HttpContent
    {
        private readonly string prefix;
        private readonly Func<string> suffix;
        public GatedJson(string prefix, string suffix) : this(prefix, () => suffix) { }
        public GatedJson(string prefix, Func<string> suffix)
        {
            this.prefix = prefix; this.suffix = suffix;
            Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Finish { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            await stream.WriteAsync(Encoding.UTF8.GetBytes(prefix)); await stream.FlushAsync(); Started.TrySetResult();
            await Finish.Task.WaitAsync(TimeSpan.FromSeconds(5)); await stream.WriteAsync(Encoding.UTF8.GetBytes(suffix()));
        }
    }
}
