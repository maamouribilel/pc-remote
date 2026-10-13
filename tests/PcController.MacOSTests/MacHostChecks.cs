using System.Net;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
namespace PcController.MacOS;
internal static class MacHostChecks
{
    public static async Task Run(string directory, MacStorage storage, string expectedRoot, Action<bool, string> check)
    {
        var factory = new MacHostFactory(directory, storage, new MacInput());
        var snapshot = new PcController.Core.NetworkSnapshot([], [], "real Keychain loopback fixture");
        var server = factory.Create(snapshot);
        try
        {
            await server.StartAsync();
            bool Validate(X509Certificate? leaf, SslPolicyErrors errors)
            {
                if (leaf == null || (errors & ~SslPolicyErrors.RemoteCertificateChainErrors) != 0) return false;
                using var cert = new X509Certificate2(leaf);
                using var chain = new X509Chain(); chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(server.Certificates.Root); chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                return chain.Build(cert);
            }
            using var client = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, leaf, _, errors) => Validate(leaf, errors) }) { BaseAddress = new Uri("https://localhost:8443") };
            using var response = await client.GetAsync("/health"); check(response.IsSuccessStatusCode && server.Certificates.Fingerprint == expectedRoot, "actual Mac Keychain host HTTPS startup retains existing root");
            using var socket = new ClientWebSocket(); socket.Options.RemoteCertificateValidationCallback = (_, leaf, _, errors) => Validate(leaf, errors);
            socket.Options.SetRequestHeader("Origin", "https://localhost:8443");
            await socket.ConnectAsync(new Uri("wss://localhost:8443/diagnostics/ws"), CancellationToken.None);
            await socket.SendAsync(Encoding.UTF8.GetBytes("{\"v\":1,\"seq\":1,\"type\":\"ping\",\"payload\":{}}"), WebSocketMessageType.Text, true, CancellationToken.None);
            var bytes = new byte[4096]; using var deadline = new CancellationTokenSource(5000);
            var result = await socket.ReceiveAsync(bytes.AsMemory(), deadline.Token);
            using var pong = JsonDocument.Parse(bytes.AsMemory(0, result.Count));
            check(pong.RootElement.GetProperty("type").GetString() == "pong", "actual Mac Keychain host trusted WSS diagnostics");
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "test_done", deadline.Token);
            check(server.Sessions.Paused && server.Sessions.OwnerDeviceId == null, "production Mac composition starts paused without an input owner");
            await server.DisposeAsync();
            server = factory.Create(snapshot); await server.StartAsync();
            check(server.Certificates.Fingerprint == expectedRoot && server.Sessions.Paused, "actual host restart keeps per-install root and safety pause");
        }
        finally { await server.DisposeAsync(); }
    }
}
