using System.ComponentModel;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using PcController.Core;

namespace PcController.Host;

public sealed partial class CompanionServer
{
    private DateTimeOffset nextLaunch;
    private object? PublicPower() => Power.Pending is { } pending ? new { action = pending.Action, executeAt = pending.ExecuteAt } : null;
    private void MapPcEndpoints()
    {
        app.MapGet("/api/pc", (HttpContext context) => Authenticate(context) == null ? Error("unauthenticated", 401) : Results.Json(new
        {
            apps = Launcher.List().Select(a => new { id = a.Id, name = a.Name, profile = a.Profile, available = Capabilities.Launcher && Launcher.Available(a) }),
            volume = Capabilities.Volume ? PcActions.GetVolume() : new VolumeState(false), power = PublicPower(), powerMessage = PowerMessage
        }));
        app.MapPost("/api/pc/launch", async Task<IResult> (HttpContext context) => await PcRequest(context, ["session", "id"], (body, deviceId) =>
        {
            if (!Capabilities.Launcher) return Error("unsupported_action", 409);
            if (!StringField(body, "id", out var id) || id.Length != 32) return Error("invalid_app", 400);
            var entry = Launcher.Find(id);
            if (entry == null) return Error("invalid_app", 400);
            if (DateTimeOffset.UtcNow < nextLaunch) return Error("launch_rate_limit", 429);
            nextLaunch = DateTimeOffset.UtcNow.AddSeconds(2);
            Input.ReleaseAll();
            var result = PcActions.Launch(entry);
            MediaSettings.Save(entry.Profile);
            return Results.Json(new { name = entry.Name, focused = result.Focused, profile = entry.Profile });
        }));
        app.MapPost("/api/pc/volume", async Task<IResult> (HttpContext context) => await PcRequest(context, ["session", "level"], (body, deviceId) =>
        {
            if (!Capabilities.Volume) return Error("unsupported_action", 409);
            var value = body.GetProperty("level");
            if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var level) || level is < 0 or > 100) return Error("invalid_volume", 400);
            var volume = PcActions.SetVolume(level);
            return volume.Available ? Results.Json(new { volume }) : Error("audio_unavailable", 503);
        }));
        app.MapPost("/api/pc/power/prepare", async Task<IResult> (HttpContext context) => await PcRequest(context, ["session", "action"], (body, deviceId) =>
        {
            if (!StringField(body, "action", out var action) || !PowerQueue.Supported(action)) return Error("invalid_power_action", 400);
            if (!Capabilities.PowerActions.Contains(action)) return Error("unsupported_action", 409);
            Input.ReleaseAll();
            var value = Power.Prepare(action, deviceId, body.GetProperty("session").GetString()!);
            PowerMessage = "";
            return Results.Json(new { token = value.Token, action, expires = value.Expires, delaySeconds = 30 });
        }));
        app.MapPost("/api/pc/power/confirm", async Task<IResult> (HttpContext context) => await PcRequest(context, ["session", "token"], (body, deviceId) =>
        {
            if (!StringField(body, "token", out var token) || token.Length != 64) return Error("confirmation_expired", 400);
            Input.ReleaseAll();
            Power.Confirm(token, deviceId, body.GetProperty("session").GetString()!);
            return Results.Json(new { power = PublicPower() });
        }));
        app.MapPost("/api/pc/power/cancel", async Task<IResult> (HttpContext context) => await PcRequest(context, ["session"], (_, deviceId) =>
        {
            Power.Cancel(); PowerMessage = "Power action cancelled.";
            return Results.Json(new { cancelled = true });
        }));
    }
    private static bool StringField(JsonElement body, string key, out string value)
    {
        var field = body.GetProperty(key); value = field.ValueKind == JsonValueKind.String ? field.GetString()! : "";
        return field.ValueKind == JsonValueKind.String;
    }
    private async Task<IResult> PcRequest(HttpContext context, string[] fields, Func<JsonElement, string, IResult> execute)
    {
        var device = Authenticate(context);
        if (device == null) return Error("unauthenticated", 401);
        using var body = await JsonBody(context, fields);
        if (body == null || !StringField(body.RootElement, "session", out var session) || session.Length != 32) return Error("invalid_request", 400);
        IResult result = Error("controller_busy_or_paused", 409);
        try
        {
            Sessions.ExecuteDevice(device.Id, session, () => result = Authenticate(context)?.Id == device.Id ? execute(body.RootElement, device.Id) : Error("unauthenticated", 401));
            return result;
        }
        catch (InputFailureException) { InputProblem = true; Sessions.Pause(true); return Error("input_failed", 503); }
        catch (InvalidOperationException exception) when (exception.Message is "power_pending" or "confirmation_expired") { return Error(exception.Message, 409); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or Win32Exception) { return Error("pc_action_failed", 503); }
    }
    public void ScheduleLocalPower(string action)
    {
        if (!Capabilities.PowerActions.Contains(action)) throw new InvalidOperationException("unsupported_action");
        Input.ReleaseAll();
        var prepared = Power.Prepare(action, "local", "local");
        Power.Confirm(prepared.Token, "local", "local"); PowerMessage = "";
    }
    public void CancelPower() { Power.Cancel(); PowerMessage = "Power action cancelled."; }
    private void CheckPower()
    {
        var pending = Power.Pending;
        if (pending == null || pending.ExecuteAt > DateTimeOffset.UtcNow) return;
        PendingPower? due = null;
        void Take()
        {
            if (pending.Session != "local" && !Devices.List().Any(d => d.Id == pending.Device && d.Expires > DateTimeOffset.UtcNow)) { Power.Cancel(); return; }
            due = Power.TakeDue(pending.Device, pending.Session);
            if (due == null) return;
            try
            {
                if (!Capabilities.PowerActions.Contains(due.Action)) { due = null; PowerMessage = "Power action unavailable."; return; }
                Input.ReleaseAll();
            }
            catch (InputFailureException) { due = null; InputProblem = true; Sessions.Pause(true); PowerMessage = "Power cancelled because held input could not be released."; }
        }
        if (pending.Session == "local") Take();
        else if (!Sessions.ExecuteDevice(pending.Device, pending.Session, Take)) Power.Cancel();
        if (due == null) return;
        // Sleep may block until resume. Never hold the session/input lock across it.
        try { PowerMessage = "Power request sent to the operating system. Save any work if asked."; PcActions.Power(due.Action); }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        { PowerMessage = "The operating system could not complete the power request. Check the PC and your account permissions."; }
    }
}
