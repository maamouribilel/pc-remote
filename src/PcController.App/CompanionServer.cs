using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PcController.Core;
using PcController.Windows;

namespace PcController.App;

public sealed partial class CompanionServer : IAsyncDisposable
{
    public const int Port = 8443;
    public NetworkSnapshot Network { get; }
    public LocalCertificates Certificates { get; }
    public string ConnectionAddress => $"https://{Network.PrivateAddresses.FirstOrDefault()?.ToString() ?? "localhost"}:{Port}";
    private readonly WebApplication app;
    private readonly CancellationTokenSource shutdown = new();
    private readonly SemaphoreSlim sockets = new(4, 4);
    public InputController Input { get; }
    public PointerSettings PointerSettings { get; }
    public MediaSettings MediaSettings { get; }
    public bool InputProblem { get; private set; }
    public LauncherSettings Launcher { get; }
    public IPcActions PcActions { get; }
    public PowerQueue Power { get; } = new();
    public string PowerMessage { get; private set; } = "";
    private readonly Timer powerTimer;

    public CompanionServer(string? dataDirectory = null, IMouseInput? mouseInput = null, bool loopbackOnly = false, NetworkSnapshot? network = null, IPcActions? pcActions = null)
    {
        Network = loopbackOnly ? new NetworkSnapshot([], [], "Loopback test fixture") : network ?? NetworkSnapshot.Read();
        var addresses = Network.PrivateAddresses.Append(IPAddress.Loopback).ToArray();
        var certificateDirectory = Path.Combine(dataDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PcController"), "certificates");
        Certificates = new LocalCertificates(certificateDirectory, addresses);
        Devices = new ProtectedDevices(certificateDirectory);
        Pairing = new PairingService(Devices);
        Pairing.IssueChallenge();
        PointerSettings = new PointerSettings(certificateDirectory);
        MediaSettings = new MediaSettings(certificateDirectory);
        Launcher = new LauncherSettings(certificateDirectory);
        PcActions = pcActions ?? new WindowsPcActions();
        Input = new InputController(mouseInput ?? new WindowsInput(), PointerSettings.Value);
        Sessions.ReleaseInput += () => { try { Input.ReleaseAll(); } catch (InputFailureException) { InputProblem = true; throw; } };
        Input.Failed += () => { InputProblem = true; Sessions.Pause(true); };
        Input.TimedOut += Sessions.EndCurrent;
        Sessions.Changed += () => Power.KeepSession(Sessions.ActiveSessionId);
        var webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        if (!File.Exists(Path.Combine(webRoot, "index.html"))) throw new IOException("Mobile assets are missing.");
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [], WebRootPath = webRoot, ContentRootPath = AppContext.BaseDirectory });
        builder.Logging.ClearProviders(); // No URLs, payloads, cookies or exception data logged.
        var hosts = addresses.Select(a => $"{a}:{Port}").Append($"localhost:{Port}").ToHashSet(StringComparer.Ordinal);
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;
            options.Limits.MaxRequestBodySize = DiagnosticProtocol.MaxBytes;
            foreach (var address in addresses) options.Listen(address, Port, listen => listen.UseHttps(tls =>
            {
                tls.ServerCertificate = Certificates.Server;
                tls.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
            }));
        });
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ => RateLimitPartition.GetFixedWindowLimiter("global", _ =>
                new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        app = builder.Build();
        app.Use(async (context, next) =>
        {
            if (context.Request.Host.Value is not { } host || !hosts.Contains(host)) { context.Response.StatusCode = 400; return; }
            if (context.Request.Method is not ("GET" or "HEAD" or "OPTIONS") && !ValidOrigin(context))
            { context.Response.StatusCode = 403; return; }
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Content-Security-Policy"] = $"default-src 'self'; script-src 'self'; style-src 'self'; connect-src 'self' wss://{host}; img-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            await next(context);
        });
        app.UseRateLimiter();
        app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(10), KeepAliveTimeout = TimeSpan.FromSeconds(10) });
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.MapGet("/health", () => Results.Json(new { ready = true, protocol = 1, stage = "mobile", inputEnabled = true }));
        MapPairingEndpoints();
        MapPcEndpoints();
        // Chromium can use HTTP/2 CONNECT for WebSockets; HTTP/1.1 uses GET.
        app.MapMethods("/diagnostics/ws", ["GET", "CONNECT"], async (HttpContext context) =>
        {
            // This unauthenticated endpoint can only reply to diagnostic pings, never inject input.
            var expected = $"https://{context.Request.Host.Value}";
            if (context.Request.Headers.Origin.Count != 1 || context.Request.Headers.Origin[0] != expected)
            { context.Response.StatusCode = 403; return; }
            if (!context.WebSockets.IsWebSocketRequest) { context.Response.StatusCode = 400; return; }
            if (!await sockets.WaitAsync(0, shutdown.Token)) { context.Response.StatusCode = 429; return; }
            try
            {
                using var socket = await context.WebSockets.AcceptWebSocketAsync();
                await RunDiagnostics(socket, context.RequestAborted);
            }
            catch (Exception exception) when (exception is WebSocketException or OperationCanceledException or IOException) { }
            finally { sockets.Release(); }
        });
        powerTimer = new Timer(_ => CheckPower(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }
    public Task StartAsync() => app.StartAsync(shutdown.Token);
    private async Task RunDiagnostics(WebSocket socket, CancellationToken disconnected)
    {
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(disconnected, shutdown.Token);
        var session = Guid.NewGuid().ToString("N");
        var bytes = new byte[DiagnosticProtocol.MaxBytes + 1];
        long previous = 0;
        var window = DateTime.UtcNow;
        var count = 0;
        while (socket.State == WebSocketState.Open && !lifetime.IsCancellationRequested)
        {
            lifetime.CancelAfter(TimeSpan.FromSeconds(15));
            var received = 0;
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(bytes, received, bytes.Length - received), lifetime.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                { await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "disconnected", lifetime.Token); return; }
                received += result.Count;
                if (result.MessageType != WebSocketMessageType.Text || received > DiagnosticProtocol.MaxBytes)
                { await socket.CloseOutputAsync(WebSocketCloseStatus.InvalidPayloadData, "invalid_message", lifetime.Token); return; }
            } while (!result.EndOfMessage);
            if (DateTime.UtcNow - window >= TimeSpan.FromSeconds(1)) { window = DateTime.UtcNow; count = 0; }
            if (++count > 4)
            { await socket.CloseOutputAsync(WebSocketCloseStatus.PolicyViolation, "rate_limit", lifetime.Token); return; }
            if (!DiagnosticProtocol.TryPing(bytes.AsMemory(0, received), previous, out var sequence))
            { await socket.CloseOutputAsync(WebSocketCloseStatus.InvalidPayloadData, "invalid_message", lifetime.Token); return; }
            previous = sequence;
            var response = JsonSerializer.SerializeToUtf8Bytes(new { v = 1, seq = sequence, type = "pong", session, payload = new { } });
            await socket.SendAsync(response, WebSocketMessageType.Text, true, lifetime.Token);
        }
    }
    public async ValueTask DisposeAsync()
    {
        Power.Cancel();
        await powerTimer.DisposeAsync();
        Sessions.Shutdown();
        await shutdown.CancelAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try { await app.StopAsync(timeout.Token).ConfigureAwait(false); } catch (OperationCanceledException) { }
        await app.DisposeAsync().ConfigureAwait(false);
        try { Input.Dispose(); } catch (InputFailureException) { InputProblem = true; }
        Certificates.Dispose();
        shutdown.Dispose();
        sockets.Dispose();
    }
}
