using System.IO;
using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using PcController.Core;

namespace PcController.Host;

public sealed partial class CompanionServer
{
    private const string DeviceCookie = "__Host-pc-device";
    private const string PairCookie = "__Host-pc-pair";
    private const string LeaseCookie = "__Host-pc-lease";
    public IDeviceRepository Devices { get; }
    public PairingService Pairing { get; }
    public SessionOwnership Sessions { get; } = new();
    public Func<PairingRequest, string, Task<bool>>? ApproveDeviceAsync { get; set; }
    public string? PairingAddress => Pairing.CurrentChallenge is { } c ? $"{ConnectionAddress}/#pair={c.Value}" : null;
    public void RevokeDevice(string id)
    {
        try { Devices.Revoke(id); }
        catch { Sessions.Pause(true); throw; }
        finally { Sessions.Revoke(id); }
    }
    private static bool ValidOrigin(HttpContext context) => context.Request.Headers.Origin.Count == 1 &&
        context.Request.Headers.Origin[0] == $"https://{context.Request.Host.Value}";
    private static CookieOptions Cookie(DateTimeOffset expires) => new()
    { Secure = true, HttpOnly = true, SameSite = SameSiteMode.Strict, Path = "/", Expires = expires, IsEssential = true };
    private static IResult Error(string code, int status) => Results.Json(new { error = code }, statusCode: status);
    private DeviceRecord? Authenticate(HttpContext context) => Pairing.Authenticate(context.Request.Cookies[DeviceCookie] ?? "");
    private static async Task<JsonDocument?> JsonBody(HttpContext context, string[] fields)
    {
        if (!string.Equals(context.Request.ContentType?.Split(';')[0].Trim(), "application/json", StringComparison.OrdinalIgnoreCase)) return null;
        try
        {
            // Independently bounded for chunked bodies as well as Content-Length.
            var bytes = new byte[1025]; var length = 0;
            while (length < bytes.Length)
            {
                var read = await context.Request.Body.ReadAsync(bytes.AsMemory(length), context.RequestAborted);
                if (read == 0) break;
                length += read;
            }
            if (length > 1024) return null;
            var document = JsonDocument.Parse(bytes.AsMemory(0, length), new JsonDocumentOptions { MaxDepth = 4 });
            if (document.RootElement.ValueKind != JsonValueKind.Object) { document.Dispose(); return null; }
            var names = document.RootElement.EnumerateObject().Select(p => p.Name).ToArray();
            if (names.Length != fields.Length || names.Distinct().Count() != fields.Length || names.Except(fields).Any())
            { document.Dispose(); return null; }
            return document;
        }
        catch (Exception exception) when (exception is JsonException or IOException) { return null; }
    }
    private void MapPairingEndpoints()
    {
        app.MapGet("/api/status", (HttpContext context) =>
        {
            var device = Authenticate(context);
            return Results.Json(new { authenticated = device != null, deviceName = device?.Name,
                paused = Sessions.Paused, busy = Sessions.OwnerDeviceId != null, protocol = 1, inputEnabled = Capabilities.InputEnabled, inputProblem = InputProblem, mediaProfile = MediaSettings.Profile, capabilities = device == null ? null : Capabilities, readiness = device == null ? null : Readiness });
        });
        app.MapPost("/api/pair/request", async (HttpContext context) =>
        {
            using var body = await JsonBody(context, ["challenge", "name"]);
            if (body == null || body.RootElement.GetProperty("challenge").ValueKind != JsonValueKind.String ||
                body.RootElement.GetProperty("name").ValueKind != JsonValueKind.String) return Error("invalid_request", 400);
            try
            {
                var request = Pairing.Request(body.RootElement.GetProperty("challenge").GetString()!, body.RootElement.GetProperty("name").GetString()!);
                context.Response.Cookies.Append(PairCookie, request.Ticket, Cookie(request.Expires));
                var address = context.Connection.RemoteIpAddress?.ToString() ?? "unknown address";
                _ = DecideOnPc(request, address);
                return Results.Json(new { state = "pending", expires = request.Expires }, statusCode: 202);
            }
            catch (InvalidOperationException exception)
            { return Error(exception.Message, exception.Message == "pair_rate_limit" ? 429 : 400); }
        });
        // Poll is POST because approval consumes its ticket and issues authentication.
        app.MapPost("/api/pair/poll", (HttpContext context) =>
        {
            var result = Pairing.Claim(context.Request.Cookies[PairCookie] ?? "");
            if (result.State == "approved")
            {
                context.Response.Cookies.Append(DeviceCookie, result.Credential!, Cookie(result.Device!.Expires));
                context.Response.Cookies.Delete(PairCookie, Cookie(DateTimeOffset.UtcNow));
            }
            return Results.Json(new { state = result.State }); // Never send the reusable token to JS.
        });
        app.MapPost("/api/session/claim", async (HttpContext context) =>
        {
            var device = Authenticate(context);
            if (device == null) return Error("unauthenticated", 401);
            using var body = await JsonBody(context, ["takeover"]);
            if (body == null || body.RootElement.GetProperty("takeover").ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return Error("invalid_request", 400);
            try
            {
                var authorized = Sessions.ReserveAuthorized(device.Id, body.RootElement.GetProperty("takeover").GetBoolean(),
                    () => !shutdown.IsCancellationRequested && Authenticate(context)?.Id == device.Id);
                if (authorized == null) return Error("unauthenticated", 401);
                var reservation = authorized.Value;
                context.Response.Cookies.Append(LeaseCookie, reservation.Ticket, Cookie(DateTimeOffset.UtcNow.AddSeconds(30)));
                return Results.Json(new { session = reservation.Session.Id });
            }
            catch (InvalidOperationException exception) { return Error(exception.Message, exception.Message == "paused" ? 403 : 409); }
        });
        app.MapPost("/api/logout", (HttpContext context) =>
        {
            var device = Authenticate(context);
            if (device != null) RevokeDevice(device.Id);
            context.Response.Cookies.Delete(DeviceCookie, Cookie(DateTimeOffset.UtcNow));
            return Results.Json(new { state = "unpaired" });
        });
        app.MapPost("/api/settings/media", async (HttpContext context) =>
        {
            var device = Authenticate(context);
            if (device == null) return Error("unauthenticated", 401);
            var session = Sessions.ActiveSessionId;
            if (session == null) return Error("controller_busy_or_paused", 409);
            using var body = await JsonBody(context, ["profile"]);
            if (body == null || body.RootElement.GetProperty("profile").ValueKind != JsonValueKind.String) return Error("invalid_request", 400);
            var profile = body.RootElement.GetProperty("profile").GetString()!;
            if (!MediaProfiles.Actions.ContainsKey(profile)) return Error("invalid_profile", 400);
            try
            {
                IResult result = Error("controller_busy_or_paused", 409);
                // Body reads can outlive a pause, revoke, or reconnect. Authorize the
                // original live session again while holding the ownership lock.
                Sessions.ExecuteDevice(device.Id, session, () =>
                {
                    if (Authenticate(context)?.Id != device.Id) { result = Error("unauthenticated", 401); return; }
                    if (!Capabilities.MediaActions.TryGetValue(profile, out var actions) || actions.Count == 0) { result = Error("unsupported_action", 409); return; }
                    MediaSettings.Save(profile); result = Results.Json(new { profile });
                });
                return result;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { return Error("settings_failed", 503); }
        });
        app.MapMethods("/control/ws", ["GET", "CONNECT"], async (HttpContext context) =>
        {
            if (!ValidOrigin(context)) { context.Response.StatusCode = 403; return; }
            var device = Authenticate(context);
            if (device == null) { context.Response.StatusCode = 401; return; }
            if (!context.WebSockets.IsWebSocketRequest) { context.Response.StatusCode = 400; return; }
            var session = Sessions.Connect(device.Id, context.Request.Cookies[LeaseCookie] ?? "");
            if (session == null) { context.Response.StatusCode = 409; return; }
            try
            {
                using var socket = await context.WebSockets.AcceptWebSocketAsync();
                await RunControl(socket, session, context.Request.Cookies[DeviceCookie]!, context.RequestAborted);
            }
            catch (Exception exception) when (exception is WebSocketException or OperationCanceledException or IOException) { }
            finally { Sessions.End(session); }
        });
    }
    private async Task DecideOnPc(PairingRequest request, string address)
    {
        var allow = false;
        try
        {
            if (ApproveDeviceAsync != null)
                allow = await ApproveDeviceAsync(request, address).WaitAsync(TimeSpan.FromMinutes(2), shutdown.Token);
        }
        catch (Exception) { /* Any UI/lifetime failure denies approval. No request data is logged. */ }
        Pairing.Decide(request.Ticket, allow);
    }
    private static async Task SendControlReply(WebSocket socket, object reply, CancellationToken token)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(reply, WireJson);
        if (bytes.Length > ControlProtocol.MaxReplyBytes)
        {
            await socket.CloseOutputAsync(WebSocketCloseStatus.InternalServerError, "reply_unavailable", token);
            return;
        }
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, token);
    }
    private async Task RunControl(WebSocket socket, ControlSession session, string credential, CancellationToken disconnected)
    {
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(disconnected, shutdown.Token, session.Stop.Token);
        var buffer = new byte[1025]; long previous = 0;
        var window = DateTime.UtcNow; var count = 0;
        while (socket.State == WebSocketState.Open && !lifetime.IsCancellationRequested)
        {
            lifetime.CancelAfter(TimeSpan.FromSeconds(15));
            var length = 0;
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer, length, buffer.Length - length), lifetime.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                { await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "disconnected", lifetime.Token); return; }
                length += result.Count;
                if (length > 1024 || result.MessageType != WebSocketMessageType.Text)
                { await socket.CloseOutputAsync(WebSocketCloseStatus.InvalidPayloadData, "invalid_message", lifetime.Token); return; }
            } while (!result.EndOfMessage);
            if (Pairing.Authenticate(credential) == null) return;
            if (DateTime.UtcNow - window >= TimeSpan.FromSeconds(1)) { window = DateTime.UtcNow; count = 0; }
            if (++count > 180) { await socket.CloseOutputAsync(WebSocketCloseStatus.PolicyViolation, "rate_limit", lifetime.Token); return; }
            if (!ControlProtocol.TryParse(buffer.AsMemory(0, length), previous, out var command))
            { await socket.CloseOutputAsync(WebSocketCloseStatus.InvalidPayloadData, "invalid_message", lifetime.Token); return; }
            previous = command!.Sequence;
            try { if (!Sessions.Execute(session, () => { Input.Execute(command); if (command.Type != "ping") InputProblem = false; })) return; }
            catch (InputFailureException)
            {
                InputProblem = true; Sessions.Pause(true);
                using var close = CancellationTokenSource.CreateLinkedTokenSource(disconnected, shutdown.Token);
                close.CancelAfter(TimeSpan.FromSeconds(5));
                await socket.CloseOutputAsync(WebSocketCloseStatus.PolicyViolation, "input_failed", close.Token); return;
            }
            if (command.Type == "ping")
                await SendControlReply(socket, new { v = 1, seq = previous, type = "pong", session = session.Id, payload = new { capabilities = Capabilities, readiness = Readiness, mediaProfile = MediaSettings.Profile, volume = Capabilities.Volume ? PcActions.GetVolume() : new VolumeState(false), power = PublicPower(), powerMessage = PowerMessage } }, lifetime.Token);
            else if (command.Type == "text.insert")
                await SendControlReply(socket, new { v = 1, seq = previous, type = "ack", session = session.Id, payload = new { } }, lifetime.Token);
        }
    }
}
