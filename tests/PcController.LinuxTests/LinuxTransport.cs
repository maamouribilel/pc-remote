using System.Diagnostics;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using PcController.Core;
using PcController.Host;
using PcController.Linux;
namespace PcController.LinuxTests;
internal static class LinuxTransport
{
    internal static async Task Run(string directory, LinuxStorage storage, LocalCertificates certificates)
    {
        // Test-project-owned recording input, real GNOME-backed files; loopback only, paused.
        await using var server = new CompanionServer(new(new([], [], "isolated real GNOME keyring storage fixture"), certificates,
            new ProtectedDevices(directory, storage), new PointerSettings(directory), new MediaSettings(directory), new Catalog(), new NoInput(), new Actions(),
            PlatformCapabilities.Unavailable("linux"), Path.Combine(AppContext.BaseDirectory, "wwwroot")));
        server.Sessions.Pause(true);
        await server.StartAsync();
        bool Validate(X509Certificate? leaf, SslPolicyErrors errors)
        {
            if (leaf == null || (errors & ~SslPolicyErrors.RemoteCertificateChainErrors) != 0) return false;
            using var cert = new X509Certificate2(leaf); using var chain = new X509Chain();
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(certificates.Root); chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            return chain.Build(cert);
        }
        using var client = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, leaf, _, errors) => Validate(leaf, errors) });
        Program.Check((await client.GetAsync("https://localhost:8443/health")).IsSuccessStatusCode, "real GNOME stored certificate chain/name validated HTTPS");
        using var socket = new ClientWebSocket(); socket.Options.RemoteCertificateValidationCallback = (_, leaf, _, errors) => Validate(leaf, errors);
        socket.Options.SetRequestHeader("Origin", "https://localhost:8443");
        using var deadline = new CancellationTokenSource(5000);
        await socket.ConnectAsync(new("wss://localhost:8443/diagnostics/ws"), deadline.Token);
        await socket.SendAsync(Encoding.UTF8.GetBytes("{\"v\":1,\"seq\":1,\"type\":\"ping\",\"payload\":{}}"), WebSocketMessageType.Text, true, deadline.Token);
        var bytes = new byte[4096]; var result = await socket.ReceiveAsync(bytes.AsMemory(), deadline.Token);
        Program.Check(Encoding.UTF8.GetString(bytes, 0, result.Count).Contains("pong", StringComparison.Ordinal), "real GNOME stored certificate trusted WSS diagnostic pong");
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "test_done", deadline.Token);
        if (Environment.GetEnvironmentVariable("PC_REMOTE_LINUX_STRICT_PYTHON") is { } python)
        {
            using var strict = Process.Start(new ProcessStartInfo(python) { ArgumentList = { Environment.GetEnvironmentVariable("PC_REMOTE_LINUX_TLS_SCRIPT")!, Path.Combine(directory, "certificates/pc-remote-root.cer") } })!;
            await strict.WaitForExitAsync(); Program.Check(strict.ExitCode == 0, "Python 3.13 strict Linux TLS positives and negatives");
        }
        Program.Check(server.Sessions.Paused && server.Sessions.OwnerDeviceId == null, "storage transport fixture remains paused");
    }
    private sealed class NoInput : IMouseInput
    {
        public void Move(int dx, int dy) => throw new InputFailureException();
        public void Scroll(int dx, int dy) => throw new InputFailureException();
        public void Button(string b, bool d) => throw new InputFailureException();
        public void Click(string b, bool t) => throw new InputFailureException();
        public void ReleaseAll() { }
    }
    private sealed class Catalog : ILauncherSettings
    {
        public LauncherApp[] List() => []; public LauncherApp? Find(string id) => null; public bool Available(LauncherApp app) => false;
        public void Add(string e, string n, string p) => throw new NotSupportedException(); public void Remove(string id) => throw new NotSupportedException();
    }
    private sealed class Actions : IPcActions
    {
        public VolumeState GetVolume() => new(false); public VolumeState SetVolume(int v) => throw new NotSupportedException();
        public LaunchResult Launch(LauncherApp app) => throw new NotSupportedException(); public void Power(string a) => throw new NotSupportedException();
    }
}
