using System.Net;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using PcController.Core;
using PcController.Windows;

if (args.Length == 2 && args[0] == "--serve-mouse-fixture") { await MouseBrowserFixture.Run(args[1]); return; }

if (args.Contains("--network"))
{
    var network = WindowsNetworkDiscovery.Read();
    Console.WriteLine(network.Notice);
    Console.WriteLine("Private IPv4: " + string.Join(", ", network.PrivateAddresses.Select(a => a.ToString())));
    Console.WriteLine("All IPv4: " + string.Join(", ", network.AllAddresses.Select(a => a.ToString())));
    return;
}

var passed = 0;
void Check(bool success, string name)
{
    if (!success) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name); passed++;
}
MouseChecks.Run(Check);
KeyboardChecks.Run(Check);
MediaChecks.Run(Check);
PcActionChecks.Run(Check);
if (args.Contains("--pc-native-smoke")) WindowsPcChecks.Native(Check);
WindowsLifecycleChecks.Local(Check);
WindowsPcChecks.Launcher(Check);
Check(WindowsInput.NativeSize == (IntPtr.Size == 8 ? 40 : 28), "native INPUT size agrees with Windows ABI");
if (args.Contains("--lifecycle")) await LifecycleChecks.Network(Check);
if (args.Contains("--pc-features")) await PcEndpointChecks.Run(Check);
if (args.Contains("--security")) await SecurityChecks.Run(Check);
var desktopArgument = Array.IndexOf(args, "--desktop");
if (desktopArgument >= 0) await DesktopChecks.Run(Check, args[desktopArgument + 1]);
if (args.Contains("--mouse-smoke")) await MouseSmoke.Run(Check);
if (args.Contains("--keyboard-smoke")) await KeyboardSmoke.Run(Check);
await PairingChecks.Run(Check, args.Contains("--pairing-integration"));
byte[] Bytes(string value) => Encoding.UTF8.GetBytes(value);
var valid = "{\"v\":1,\"seq\":1,\"type\":\"ping\",\"payload\":{}}";
Check(DiagnosticProtocol.TryPing(Bytes(valid), 0, out var seq) && seq == 1, "versioned ping");
foreach (var invalid in new[] { "{", "null", "[]", valid.Replace("ping", "key.down"), valid.Replace("\"v\":1", "\"v\":2"),
    valid.Replace("\"seq\":1", "\"seq\":-1"), valid.Replace("\"seq\":1", "\"seq\":1.5"),
    valid.Replace("\"seq\":1", "\"seq\":9007199254740992"), valid.Replace("\"payload\":{}", "\"payload\":{\"text\":\"x\"}"),
    valid.Replace("\"v\":1", "\"v\":1,\"v\":1"), valid.Replace("\"v\":1", "\"v\":null") })
    Check(!DiagnosticProtocol.TryPing(Bytes(invalid), 0, out _), "reject malformed/unsupported ping");
Check(!DiagnosticProtocol.TryPing(Bytes(valid), 1, out _), "reject sequence replay");
Check(!DiagnosticProtocol.TryPing(new byte[1025], 0, out _), "message bound");
var secret = RandomNumberGenerator.GetBytes(32);
var protectedBytes = UserProtection.Protect(secret);
Check(!protectedBytes.SequenceEqual(secret) && UserProtection.Unprotect(protectedBytes).SequenceEqual(secret), "DPAPI round trip");
CryptographicOperations.ZeroMemory(secret);

var testDirectory = Path.Combine(Path.GetTempPath(), "PcControllerTests", Guid.NewGuid().ToString("N"));
using (var first = new LocalCertificates(testDirectory, [IPAddress.Loopback]))
{
    Check(first.Renewed && first.Root.HasPrivateKey && first.Server.HasPrivateKey, "initial certificate generation");
    Check(first.Server.Extensions["2.5.29.35"]?.RawData.SequenceEqual(X509AuthorityKeyIdentifierExtension.CreateFromCertificate(first.Root, true, false).RawData) == true, "server authority key identifier matches the trusted root");
    Check(first.Server.Extensions["2.5.29.14"] != null, "server certificate has a subject key identifier for strict validators");
    // Write the pre-extraction DPAPI JSON format directly; adapter refactoring must read it unchanged.
    var migrationCredential = PairingService.RandomToken();
    var legacyDevice = new DeviceRecord(Guid.NewGuid().ToString("N"), "Migration fixture", PairingService.Hash(migrationCredential), DateTimeOffset.UtcNow.AddDays(1));
    var devicePath = Path.Combine(testDirectory, "devices.protected");
    File.WriteAllBytes(devicePath, UserProtection.Protect(JsonSerializer.SerializeToUtf8Bytes(new[] { legacyDevice })));
    var protectedRoot = File.ReadAllBytes(Path.Combine(testDirectory, "root.protected"));
    var protectedDevices = File.ReadAllBytes(devicePath);
    using var next = new LocalCertificates(testDirectory, [IPAddress.Loopback]);
    Check(!next.Renewed && next.Fingerprint == first.Fingerprint && next.Server.Thumbprint == first.Server.Thumbprint, "protected persistence");
    // Model an existing 0.2.0 installation whose valid leaf omitted AKI/SKI.
    // Matching addresses/expiry must not suppress its safe leaf-only migration.
    using (var legacyKey = RSA.Create(2048))
    {
        var legacyRequest = new CertificateRequest("CN=PC Remote", legacyKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        legacyRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        legacyRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        legacyRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new("1.3.6.1.5.5.7.3.1") }, false));
        var legacySan = new SubjectAlternativeNameBuilder(); legacySan.AddDnsName("localhost"); legacySan.AddIpAddress(IPAddress.Loopback);
        legacyRequest.CertificateExtensions.Add(legacySan.Build());
        using var legacyPublic = legacyRequest.Create(first.Root, DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(90), RandomNumberGenerator.GetBytes(16));
        using var legacy = legacyPublic.CopyWithPrivateKey(legacyKey); var bytes = legacy.Export(X509ContentType.Pfx);
        try { File.WriteAllBytes(Path.Combine(testDirectory, "server.protected"), UserProtection.Protect(bytes)); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
        using var migrated = new LocalCertificates(testDirectory, [IPAddress.Loopback]);
        Check(migrated.Renewed && migrated.Fingerprint == first.Fingerprint && migrated.Server.Thumbprint != legacy.Thumbprint && migrated.Server.Extensions["2.5.29.35"] != null,
            "legacy server certificate renews without replacing the trusted root");
        var migratedDevices = new ProtectedDevices(testDirectory);
        Check(migratedDevices.List().Single() == legacyDevice && new PairingService(migratedDevices).Authenticate(migrationCredential) != null,
            "pre-extraction DPAPI device data still authenticates after leaf migration");
        Check(File.ReadAllBytes(Path.Combine(testDirectory, "root.protected")).SequenceEqual(protectedRoot) && File.ReadAllBytes(devicePath).SequenceEqual(protectedDevices),
            "leaf migration leaves original protected root/device bytes untouched");
    }
    using var changed = new LocalCertificates(testDirectory, [IPAddress.Loopback, IPAddress.Parse("192.0.2.10")]);
    Check(changed.Renewed && changed.Fingerprint == first.Fingerprint && changed.Server.Thumbprint != first.Server.Thumbprint, "SAN regeneration preserves root");
    var profile = XDocument.Load(first.ProfilePath);
    using var publicRoot = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(profile.Descendants("data").Single().Value));
    Check(!publicRoot.HasPrivateKey && publicRoot.Thumbprint == first.Root.Thumbprint, "export contains public root only");
    using var chain = new X509Chain();
    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
    chain.ChainPolicy.CustomTrustStore.Add(first.Root);
    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
    chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.1"));
    Check(chain.Build(first.Server), "leaf chain and server EKU validate against installation root");
}
// Clean up only the unique, explicitly created test directory.
Directory.Delete(testDirectory, true);

if (args.Contains("--integration"))
{
    var targetHost = args.Contains("--lan")
        ? WindowsNetworkDiscovery.Read().PrivateAddresses.FirstOrDefault()?.ToString() ?? throw new InvalidOperationException("No Private IPv4 address available for LAN tests.")
        : "localhost";
    var origin = $"https://{targetHost}:8443";
    Console.WriteLine("Transport test address: " + origin);
    var rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PcController", "certificates", "pc-remote-root.cer");
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
    Check(response.IsSuccessStatusCode && health.RootElement.GetProperty("ready").GetBoolean() && health.RootElement.GetProperty("inputEnabled").GetBoolean(), "HTTPS health with chain and hostname verification");
    var page = await http.GetStringAsync("/");
    Check(page.Contains("PC Remote") && !page.Contains("https://cdn"), "bundled mobile page served");
    Check((await http.GetAsync("/ws")).StatusCode == HttpStatusCode.NotFound, "no input endpoint before pairing");
    // Keep TLS SNI/name validation on localhost while independently changing the HTTP Host.
    using (var tcp = new System.Net.Sockets.TcpClient())
    {
        await tcp.ConnectAsync("localhost", 8443);
        using var tls = new SslStream(tcp.GetStream(), false, Trust);
        await tls.AuthenticateAsClientAsync("localhost");
        Check(tls.SslProtocol is System.Security.Authentication.SslProtocols.Tls12 or System.Security.Authentication.SslProtocols.Tls13, "negotiated TLS 1.2 or 1.3");
        await tls.WriteAsync(Bytes("GET /health HTTP/1.1\r\nHost: attacker.invalid:8443\r\nConnection: close\r\n\r\n"));
        var data = new byte[1024];
        var length = await tls.ReadAsync(data);
        Check(Encoding.ASCII.GetString(data, 0, length).StartsWith("HTTP/1.1 400"), "reject foreign HTTP Host after valid TLS");
    }
    foreach (var rejectedOrigin in new string?[] { null, "https://attacker.invalid", "null", origin + "/" })
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/diagnostics/ws");
        if (rejectedOrigin != null) request.Headers.TryAddWithoutValidation("Origin", rejectedOrigin);
        Check((await http.SendAsync(request)).StatusCode == HttpStatusCode.Forbidden, "reject missing/non-exact Origin");
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
        Check(pong.RootElement.GetProperty("type").GetString() == "pong" && pong.RootElement.GetProperty("seq").GetInt64() == 1, "real WSS ping/pong with custom root trust");
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", cancel.Token);
    }
    foreach (var message in new[] { "{", valid.Replace("ping", "pointer.move"), new string('x', 1025) })
    {
        using var socket = Socket();
        await Connect(socket);
        await socket.SendAsync(Bytes(message), WebSocketMessageType.Text, true, cancel.Token);
        var result = await socket.ReceiveAsync(new byte[1024], cancel.Token);
        Check(result.MessageType == WebSocketMessageType.Close && socket.CloseStatus == WebSocketCloseStatus.InvalidPayloadData, "malformed/oversized/input frame closed without injection");
    }
    using (var socket = Socket())
    {
        await Connect(socket);
        for (var number = 1; number <= 5; number++)
        {
            await socket.SendAsync(Bytes(valid.Replace("\"seq\":1", $"\"seq\":{number}")), WebSocketMessageType.Text, true, cancel.Token);
            var result = await socket.ReceiveAsync(new byte[1024], cancel.Token);
            if (number == 5) Check(result.MessageType == WebSocketMessageType.Close && socket.CloseStatus == WebSocketCloseStatus.PolicyViolation, "diagnostic command rate limit");
        }
    }
    Check((await http.GetAsync("/health")).IsSuccessStatusCode, "host survives rejection checks");
}
Console.WriteLine($"{passed} checks passed.");
