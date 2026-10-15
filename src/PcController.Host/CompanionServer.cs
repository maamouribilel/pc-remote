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

namespace PcController.Host;

public sealed partial class CompanionServer : IAsyncDisposable
{
    public const int Port = 8443;
    private static readonly JsonSerializerOptions WireJson = new(JsonSerializerDefaults.Web);
    public NetworkSnapshot Network { get; }
    public ILocalCertificates Certificates { get; }
    public string ConnectionAddress => $"https://{Network.PrivateAddresses.FirstOrDefault()?.ToString() ?? "localhost"}:{Port}";
    private readonly WebApplication app;
    private readonly CancellationTokenSource shutdown = new();
    private readonly SemaphoreSlim sockets = new(4, 4);
    public InputController Input { get; }
    public PointerSettings PointerSettings { get; }
    public MediaSettings MediaSettings { get; }
    public bool InputProblem { get; private set; }
    public ILauncherSettings Launcher { get; }
    public IPcActions PcActions { get; }
    public PowerQueue Power { get; } = new();
    public string PowerMessage { get; private set; } = "";
    private readonly Timer powerTimer;

    public PlatformCapabilities Capabilities { get; }
    public HostReadiness Readiness { get; }
    public CompanionServer(HostServices services)
    {
        var webRoot = services.WebRoot;
        if (!File.Exists(Path.Combine(webRoot, "index.html"))) throw new IOException("Mobile assets are missing.");
        if ((services.Capabilities.Keyboard || services.Capabilities.Text || services.Capabilities.Shortcuts) && services.Input is not IKeyboardInput ||
            services.Capabilities.MediaActions.Values.Any(actions => actions.Count > 0) && services.Input is not IMediaInput)
            throw new ArgumentException("Input adapter does not implement the advertised capabilities.", nameof(services));
        Network = services.Network;
        var addresses = Network.PrivateAddresses.Append(IPAddress.Loopback).Distinct().ToArray();
        Certificates = services.Certificates;
        if (!Certificates.Server.MatchesHostname("localhost", false, false) || addresses.Any(a => !Certificates.Server.MatchesHostname(a.ToString(), false, false)))
            throw new ArgumentException("Server certificate SANs do not cover the exact listener bindings.", nameof(services));
        Devices = services.Devices;
        Pairing = new PairingService(Devices);
        Pairing.IssueChallenge();
        PointerSettings = services.PointerSettings;
        MediaSettings = services.MediaSettings;
        Launcher = services.Launcher;
        PcActions = services.PcActions;
        Capabilities = services.Capabilities;
        Readiness = services.Readiness ?? new("ready", Capabilities.InputEnabled ? "ready" : "unavailable", Network.PrivateAddresses.Length > 0 ? "approved" : "loopback", "");
        Input = new InputController(services.Input, PointerSettings.Value, capabilities: Capabilities);
        Sessions.ReleaseInput += () => { try { Input.ReleaseAll(); } catch (InputFailureException) { InputProblem = true; throw; } };
        Input.Failed += () => { InputProblem = true; Sessions.Pause(true); };
        Input.TimedOut += Sessions.EndCurrent;
        Sessions.Changed += () => Power.KeepSession(Sessions.ActiveSessionId);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [], WebRootPath = webRoot, ContentRootPath = AppContext.BaseDirectory });
        builder.Logging.ClearProviders(); // No URLs, payloads, cookies or exception data logged.
        var hosts = addresses.Select(a => $"{a}:{Port}").Append($"localhost:{Port}").ToHashSet(StringComparer.Ordinal);
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;
            options.Limits.MaxRequestBodySize = DiagnosticProtocol.MaxBytes;
            options.Limits.MaxConcurrentConnections = 64;
            options.Limits.MaxConcurrentUpgradedConnections = 8;
            foreach (var address in addresses) options.Listen(address, Port, listen => listen.UseHttps(tls =>
            {
                tls.ServerCertificate = Certificates.Server;
                tls.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
            }));
        });
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                // Authenticate the cookie, not a caller-supplied identifier. Unpaired
                // peers cannot consume a device's budget, even from the same address.
                var device = Authenticate(context);
                var path = context.Request.Path;
                var safety = device != null && context.Request.Method == "POST" &&
                    (path == "/api/pc/power/cancel" || path == "/api/logout");
                var key = device == null ? $"unpaired:{context.Connection.RemoteIpAddress}" : $"device:{device.Id}:{(safety ? "safety" : "normal")}";
                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                { PermitLimit = safety ? 30 : 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
            });
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await context.HttpContext.Response.WriteAsJsonAsync(new { error = "rate_limit" }, cancellationToken: token);
            };
        });
        app = builder.Build();
        app.Use(async (context, next) =>
        {
            if (context.Request.Host.Value is not { } host || !hosts.Contains(host)) { context.Response.StatusCode = 400; return; }
            if (context.Request.Method is not ("GET" or "HEAD" or "OPTIONS") && !ValidOrigin(context))
            { context.Response.StatusCode = 403; return; }
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            // No Permissions-Policy header: the remote's Air pointer reads devicemotion at the top level. If one is
            // ever added it must keep accelerometer and gyroscope for 'self' (tests/remote/air.spec.ts checks this).
            context.Response.Headers["Content-Security-Policy"] = $"default-src 'self'; script-src 'self'; style-src 'self'; connect-src 'self' wss://{host}; img-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            await next(context);
        });
        app.UseRateLimiter();
        app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(10), KeepAliveTimeout = TimeSpan.FromSeconds(10) });
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.MapGet("/health", () => Results.Json(new { ready = true, protocol = 1, stage = "mobile", inputEnabled = Capabilities.InputEnabled }));
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
        Pairing.Close();
        Sessions.Shutdown();
        Power.Cancel();
        await powerTimer.DisposeAsync().ConfigureAwait(false);
        await shutdown.CancelAsync().ConfigureAwait(false);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try { await app.StopAsync(timeout.Token).ConfigureAwait(false); } catch (OperationCanceledException) { }
        await app.DisposeAsync().ConfigureAwait(false);
        try { Input.Dispose(); } catch (InputFailureException) { InputProblem = true; }
        Certificates.Dispose();
        shutdown.Dispose();
        sockets.Dispose();
    }
}
