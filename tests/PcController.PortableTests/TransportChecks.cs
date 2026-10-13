using System.Net;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
internal static class TransportChecks
{
    internal static async Task Run(Action<bool, string> check)
    {
        var directory = Path.Combine(Path.GetTempPath(), "PcTransport-" + Guid.NewGuid().ToString("N"));
        try
        {
            await using var server = PortableFixture.Create(directory, new RecordingMouse());
            await server.StartAsync();
            byte[] Bytes(string value) => Encoding.UTF8.GetBytes(value);
            var valid = "{\"v\":1,\"seq\":1,\"type\":\"ping\",\"payload\":{}}";
    const string targetHost = "localhost";
    var origin = $"https://{targetHost}:8443";
    Console.WriteLine("Transport test address: " + origin);
    var rootPath = Path.Combine(directory, "certificates", "pc-remote-root.cer");
    using var root = X509CertificateLoader.LoadCertificateFromFile(rootPath);
    bool Trust(object sender, X509Certificate? certificate, X509Chain? _, SslPolicyErrors errors)
    {
        if (certificate == null || (errors & (SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateNotAvailable)) != 0) return false;
        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.Add(root);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.1"));
        using var leaf = new X509Certificate2(certificate);
        return chain.Build(leaf);
    }
    using var handler = new HttpClientHandler();
    handler.ServerCertificateCustomValidationCallback = (request, certificate, chain, errors) => Trust(request, certificate, chain, errors);
    using var http = new HttpClient(handler) { BaseAddress = new Uri(origin), Timeout = TimeSpan.FromSeconds(5) };
    var response = await http.GetAsync("/health");
    using var health = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    check(response.IsSuccessStatusCode && health.RootElement.GetProperty("ready").GetBoolean() && health.RootElement.GetProperty("inputEnabled").GetBoolean(), "HTTPS health with chain and hostname verification");
    var page = await http.GetStringAsync("/");
    check(page.Contains("PC Remote") && !page.Contains("https://cdn"), "bundled mobile page served");
    check((await http.GetAsync("/ws")).StatusCode == HttpStatusCode.NotFound, "no input endpoint before pairing");
    // Keep TLS SNI/name validation on localhost while independently changing the HTTP Host.
    using (var tcp = new System.Net.Sockets.TcpClient())
    {
        await tcp.ConnectAsync("localhost", 8443);
        using var tls = new SslStream(tcp.GetStream(), false, Trust);
        await tls.AuthenticateAsClientAsync("localhost");
        check(tls.SslProtocol is System.Security.Authentication.SslProtocols.Tls12 or System.Security.Authentication.SslProtocols.Tls13, "negotiated TLS 1.2 or 1.3");
        await tls.WriteAsync(Bytes("GET /health HTTP/1.1\r\nHost: attacker.invalid:8443\r\nConnection: close\r\n\r\n"));
        var data = new byte[1024];
        var length = await tls.ReadAsync(data);
        check(Encoding.ASCII.GetString(data, 0, length).StartsWith("HTTP/1.1 400"), "reject foreign HTTP Host after valid TLS");
    }
    foreach (var rejectedOrigin in new string?[] { null, "https://attacker.invalid", "null", origin + "/" })
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/diagnostics/ws");
        if (rejectedOrigin != null) request.Headers.TryAddWithoutValidation("Origin", rejectedOrigin);
        check((await http.SendAsync(request)).StatusCode == HttpStatusCode.Forbidden, "reject missing/non-exact Origin");
    }
    ClientWebSocket Socket()
    {
        var socket = new ClientWebSocket();
        socket.Options.SetRequestHeader("Origin", origin);
        socket.Options.RemoteCertificateValidationCallback = Trust;
        return socket;
    }
    async Task Connect(ClientWebSocket socket) => await socket.ConnectAsync(new Uri($"wss://{targetHost}:8443/diagnostics/ws"), CancellationToken.None);
    using var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(20));
    using (var socket = Socket())
    {
        await Connect(socket);
        await socket.SendAsync(Bytes(valid), WebSocketMessageType.Text, true, cancel.Token);
        var buffer = new byte[1024];
        var result = await socket.ReceiveAsync(buffer, cancel.Token);
        using var pong = JsonDocument.Parse(buffer.AsMemory(0, result.Count));
        check(pong.RootElement.GetProperty("type").GetString() == "pong" && pong.RootElement.GetProperty("seq").GetInt64() == 1, "real WSS ping/pong with custom root trust");
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", cancel.Token);
    }
    foreach (var message in new[] { "{", valid.Replace("ping", "pointer.move"), new string('x', 1025) })
    {
        using var socket = Socket();
        await Connect(socket);
        await socket.SendAsync(Bytes(message), WebSocketMessageType.Text, true, cancel.Token);
        var result = await socket.ReceiveAsync(new byte[1024], cancel.Token);
        check(result.MessageType == WebSocketMessageType.Close && socket.CloseStatus == WebSocketCloseStatus.InvalidPayloadData, "malformed/oversized/input frame closed without injection");
    }
    using (var socket = Socket())
    {
        await Connect(socket);
        for (var number = 1; number <= 5; number++)
        {
            await socket.SendAsync(Bytes(valid.Replace("\"seq\":1", $"\"seq\":{number}")), WebSocketMessageType.Text, true, cancel.Token);
            var result = await socket.ReceiveAsync(new byte[1024], cancel.Token);
            if (number == 5) check(result.MessageType == WebSocketMessageType.Close && socket.CloseStatus == WebSocketCloseStatus.PolicyViolation, "diagnostic command rate limit");
        }
    }
    check((await http.GetAsync("/health")).IsSuccessStatusCode, "host survives rejection checks");
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
