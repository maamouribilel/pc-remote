using System.Text.Json;

namespace PcController.Core;

public sealed record ControlCommand(long Sequence, string Type, double X = 0, double Y = 0, string Button = "", string Action = "", string Key = "", string Text = "", string Profile = "");
public static class ControlProtocol
{
    public static bool TryParse(ReadOnlyMemory<byte> bytes, long previous, out ControlCommand? command)
    {
        command = null;
        if (bytes.Length == 0 || bytes.Length > 1024) return false;
        try
        {
            using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 4 });
            var root = document.RootElement;
            if (!Fields(root, ["v", "seq", "type", "payload"]) || !root.GetProperty("v").TryGetInt32(out var version) || version != 1 ||
                !root.GetProperty("seq").TryGetInt64(out var sequence) || sequence <= Math.Max(0, previous) || sequence > 9_007_199_254_740_991 ||
                root.GetProperty("type").ValueKind != JsonValueKind.String) return false;
            var type = root.GetProperty("type").GetString()!; var payload = root.GetProperty("payload");
            if (type is "ping" or "input.releaseAll")
            { if (!Fields(payload, [])) return false; command = new(sequence, type); return true; }
            if (type is "pointer.move" or "pointer.scroll")
            {
                var bound = type == "pointer.move" ? 500 : 1200;
                if (!Fields(payload, ["dx", "dy"]) || !Number(payload.GetProperty("dx"), bound, out var x) || !Number(payload.GetProperty("dy"), bound, out var y)) return false;
                command = new(sequence, type, x, y); return true;
            }
            if (type == "pointer.button")
            {
                if (!Fields(payload, ["button", "action"]) || payload.GetProperty("button").ValueKind != JsonValueKind.String || payload.GetProperty("action").ValueKind != JsonValueKind.String) return false;
                var button = payload.GetProperty("button").GetString()!; var action = payload.GetProperty("action").GetString()!;
                if (button is not ("left" or "right") || action is not ("down" or "up" or "click" or "double")) return false;
                command = new(sequence, type, Button: button, Action: action); return true;
            }
            if (type is "key.down" or "key.up" or "key.press")
            {
                if (!Fields(payload, ["key"]) || payload.GetProperty("key").ValueKind != JsonValueKind.String) return false;
                var key = payload.GetProperty("key").GetString()!;
                if (!KeyboardCommands.Keys.Contains(key)) return false;
                command = new(sequence, type, Key: key); return true;
            }
            if (type == "shortcut")
            {
                if (!Fields(payload, ["action"]) || payload.GetProperty("action").ValueKind != JsonValueKind.String) return false;
                var action = payload.GetProperty("action").GetString()!;
                if (!KeyboardCommands.Shortcuts.ContainsKey(action)) return false;
                command = new(sequence, type, Action: action); return true;
            }
            if (type == "text.insert")
            {
                if (!Fields(payload, ["text"]) || payload.GetProperty("text").ValueKind != JsonValueKind.String) return false;
                var text = payload.GetProperty("text").GetString()!;
                if (text.Length is < 1 or > 256 || text.Any(c => char.IsControl(c) && c is not ('\r' or '\n' or '\t'))) return false;
                for (var i = 0; i < text.Length; i++)
                    if (char.IsSurrogate(text[i])) { if (!char.IsHighSurrogate(text[i]) || i + 1 >= text.Length || !char.IsLowSurrogate(text[++i])) return false; }
                command = new(sequence, type, Text: text); return true;
            }
            if (type == "media.action")
            {
                if (!Fields(payload, ["profile", "action"]) || payload.GetProperty("profile").ValueKind != JsonValueKind.String || payload.GetProperty("action").ValueKind != JsonValueKind.String) return false;
                var profile = payload.GetProperty("profile").GetString()!; var action = payload.GetProperty("action").GetString()!;
                if (!MediaProfiles.Supports(profile, action)) return false;
                command = new(sequence, type, Action: action, Profile: profile); return true;
            }
            return false;
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException) { return false; }
    }
    private static bool Number(JsonElement value, double bound, out double number)
    { number = 0; return value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out number) && double.IsFinite(number) && Math.Abs(number) <= bound; }
    private static bool Fields(JsonElement value, string[] fields)
    {
        if (value.ValueKind != JsonValueKind.Object) return false;
        var names = value.EnumerateObject().Select(p => p.Name).ToArray();
        return names.Length == fields.Length && names.Distinct().Count() == fields.Length && !names.Except(fields).Any();
    }
}
public interface IMouseInput
{
    void Move(int dx, int dy);
    void Scroll(int dx, int dy);
    void Button(string button, bool down);
    void Click(string button, bool twice);
    void ReleaseAll();
}
public interface IKeyboardInput
{
    void Key(string key, bool down);
    void Press(string key);
    void Text(string text);
    void Shortcut(string action);
    void Chord(IReadOnlyList<string> keys);
}
public static class KeyboardCommands
{
    public static readonly HashSet<string> Modifiers = ["Control", "Alt", "Shift", "Windows"];
    public static readonly HashSet<string> Keys = ["Enter", "Backspace", "Escape", "Tab", "Control", "Alt", "Shift", "Windows", "ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", "Home", "End", "PageUp", "PageDown"];
    public static readonly IReadOnlyDictionary<string, string[]> Shortcuts = new Dictionary<string, string[]>
    { ["copy"] = ["Control", "C"], ["paste"] = ["Control", "V"], ["selectAll"] = ["Control", "A"], ["undo"] = ["Control", "Z"], ["switchApp"] = ["Alt", "Tab"] };
}
public sealed class InputFailureException : Exception
{
    public InputFailureException() : base("Windows could not insert all input events. Check the focused application's privileges and interactive desktop.") { }
}
public sealed record PointerPreferences(double Sensitivity = 1, bool NaturalScroll = true)
{
    public bool Valid => double.IsFinite(Sensitivity) && Sensitivity >= .25 && Sensitivity <= 4;
}
public sealed class InputController : IDisposable
{
    private readonly object gate = new();
    private readonly IMouseInput input;
    private readonly Func<DateTimeOffset> clock;
    private readonly Timer? watchdog;
    private readonly HashSet<string> held = [];
    private readonly HashSet<string> heldKeys = [];
    private readonly Queue<DateTimeOffset> repeats = new();
    private readonly Queue<DateTimeOffset> mediaActions = new();
    private DateTimeOffset lastText;
    private DateTimeOffset last;
    private PointerPreferences preferences;
    private double residualX, residualY, scrollX, scrollY;
    private bool cleanupPending;
    public event Action? Failed;
    public event Action? TimedOut;
    public InputController(IMouseInput input, PointerPreferences preferences, Func<DateTimeOffset>? clock = null, bool startWatchdog = true)
    {
        this.input = input; this.preferences = preferences.Valid ? preferences : throw new ArgumentOutOfRangeException(nameof(preferences));
        var start = System.Diagnostics.Stopwatch.GetTimestamp(); var startTime = DateTimeOffset.UtcNow;
        this.clock = clock ?? (() => startTime + System.Diagnostics.Stopwatch.GetElapsedTime(start));
        if (startWatchdog) watchdog = new Timer(_ => { try { if (CheckWatchdog()) TimedOut?.Invoke(); } catch (InputFailureException) { Failed?.Invoke(); } }, null, 100, 100);
    }
    public void Configure(PointerPreferences value) { if (!value.Valid) throw new ArgumentOutOfRangeException(nameof(value)); lock (gate) preferences = value; }
    public void Execute(ControlCommand command)
    {
        lock (gate)
        {
            last = clock();
            try
            {
                switch (command.Type)
                {
                    case "ping": break;
                    case "input.releaseAll": ReleaseAll(); break;
                    case "pointer.move":
                        residualX += command.X * preferences.Sensitivity; residualY += command.Y * preferences.Sensitivity;
                        var x = (int)residualX; var y = (int)residualY; residualX -= x; residualY -= y;
                        if (x != 0 || y != 0) input.Move(x, y); break;
                    case "pointer.scroll":
                        var sign = preferences.NaturalScroll ? -1 : 1;
                        // Browser deltas use down/right positive. Wheel positive is up/right; natural flips both axes.
                        scrollX += command.X * sign; scrollY += command.Y * -sign;
                        var sx = (int)scrollX; var sy = (int)scrollY; scrollX -= sx; scrollY -= sy;
                        if (sx != 0 || sy != 0) input.Scroll(sx, sy); break;
                    case "pointer.button":
                        if (command.Action is "click" or "double")
                        { if (held.Contains(command.Button)) throw new InputFailureException(); input.Click(command.Button, command.Action == "double"); }
                        else if (command.Action == "down") { if (held.Add(command.Button)) input.Button(command.Button, true); }
                        else if (held.Contains(command.Button)) { input.Button(command.Button, false); held.Remove(command.Button); }
                        break;
                    case "key.down":
                        if (input is not IKeyboardInput keyboardDown) throw new InputFailureException();
                        if (heldKeys.Contains(command.Key))
                        {
                            if (KeyboardCommands.Modifiers.Contains(command.Key)) break;
                            while (repeats.TryPeek(out var first) && first <= clock().AddSeconds(-1)) repeats.Dequeue();
                            if (repeats.Count >= 10) throw new InputFailureException();
                            repeats.Enqueue(clock());
                        }
                        heldKeys.Add(command.Key); keyboardDown.Key(command.Key, true); break;
                    case "key.up":
                        if (input is not IKeyboardInput keyboardUp) throw new InputFailureException();
                        if (heldKeys.Contains(command.Key)) { keyboardUp.Key(command.Key, false); heldKeys.Remove(command.Key); } break;
                    case "key.press":
                        if (input is not IKeyboardInput keyboardPress || heldKeys.Contains(command.Key)) throw new InputFailureException();
                        keyboardPress.Press(command.Key); break;
                    case "shortcut":
                        if (input is not IKeyboardInput shortcut || heldKeys.Count != 0) throw new InputFailureException();
                        shortcut.Shortcut(command.Action); break;
                    case "text.insert":
                        if (input is not IKeyboardInput textInput || heldKeys.Count != 0 || clock() - lastText < TimeSpan.FromMilliseconds(250)) throw new InputFailureException();
                        lastText = clock(); textInput.Text(command.Text); break;
                    case "media.action":
                        if (input is not IKeyboardInput mediaInput || heldKeys.Count != 0 || !MediaProfiles.Supports(command.Profile, command.Action)) throw new InputFailureException();
                        while (mediaActions.TryPeek(out var firstMedia) && firstMedia <= clock().AddSeconds(-1)) mediaActions.Dequeue();
                        if (mediaActions.Count >= 10) throw new InputFailureException();
                        mediaActions.Enqueue(clock());
                        mediaInput.Chord(MediaProfiles.Mappings[command.Profile][command.Action]); break;
                    default: throw new InvalidOperationException("Unsupported command");
                }
            }
            catch (InputFailureException) { ReleaseAll(); throw; }
        }
    }
    public bool CheckWatchdog()
    {
        lock (gate)
        {
            if ((held.Count == 0 && heldKeys.Count == 0 && !cleanupPending) || clock() - last < TimeSpan.FromMilliseconds(1200)) return false;
            ReleaseAll(); return true;
        }
    }
    public void ReleaseAll()
    {
        lock (gate)
        {
            // Keep held state on failure so the watchdog can retry cleanup.
            cleanupPending = true; input.ReleaseAll(); cleanupPending = false;
            held.Clear(); heldKeys.Clear(); repeats.Clear(); residualX = residualY = scrollX = scrollY = 0;
        }
    }
    public void Dispose() { watchdog?.Dispose(); ReleaseAll(); }
}
