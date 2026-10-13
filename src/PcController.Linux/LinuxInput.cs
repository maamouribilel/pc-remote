using System.Globalization;
using System.Text;
using PcController.Core;
namespace PcController.Linux;

internal interface ILinuxEvents
{
    int Resolve(uint symbol);
    bool Send(int type, int code = 0, double x = 0, double y = 0, bool down = false);
    bool Media(int action) => false;
    void GeneralVolume(string action) => throw new InputFailureException();
}
internal sealed class NativeLinuxEvents : ILinuxEvents
{
    public bool Media(int action) => LinuxNative.pc_linux_media(action) != 0;
    public void GeneralVolume(string action) => new LinuxPcActions().GeneralVolume(action);
    public int Resolve(uint symbol) => LinuxNative.pc_linux_resolve(symbol);
    public bool Send(int type, int code = 0, double x = 0, double y = 0, bool down = false) => LinuxNative.pc_linux_event(type, code, x, y, down ? 1 : 0) != 0;
}
// Tracking belongs to the adapter, surviving HTTP listener replacement and partial failure.
public sealed class LinuxInput : IMouseInput, IKeyboardInput, IMediaInput
{
    private readonly object gate = new();
    private readonly ILinuxEvents events;
    private readonly HashSet<int> keys = [], buttons = [];
    private double scrollX, scrollY;
    public bool GtkTextEnabled { get; internal set; }
    internal void WithExclusiveBackend(Action action) { lock (gate) action(); }
    private readonly Dictionary<string, int> logicalHeld = [];
    private bool composePending;
    public LinuxInput() : this(new NativeLinuxEvents()) { }
    internal LinuxInput(ILinuxEvents events) => this.events = events;
    private void Submit(bool success) { if (!success) throw new InputFailureException("Linux input unavailable. Restore this desktop/portal session, Release input, and deliberately Resume."); }
    private int Resolve(uint symbol)
    { var code = events.Resolve(symbol); return code > 0 ? code : throw new InputFailureException("The current keyboard layout cannot resolve this key without changing layout or keymap."); }
    private static uint Symbol(string key) => key switch
    {
        "Enter" => 0xff0d, "Backspace" => 0xff08, "Escape" => 0xff1b, "Tab" => 0xff09,
        "Control" => 0xffe3, "Alt" => 0xffe9, "Shift" => 0xffe1, "Windows" => 0xffeb,
        "ArrowLeft" => 0xff51, "ArrowUp" => 0xff52, "ArrowRight" => 0xff53, "ArrowDown" => 0xff54,
        "Home" => 0xff50, "End" => 0xff57, "PageUp" => 0xff55, "PageDown" => 0xff56,
        _ => throw new InputFailureException()
    };
    private void SendKey(int code, bool down)
    {
        if (down) keys.Add(code); // Include possibly submitted down events even when transport fails.
        Submit(events.Send(3, code, down: down));
        if (!down) keys.Remove(code);
    }
    private void SendButton(int code, bool down)
    {
        if (down) buttons.Add(code);
        Submit(events.Send(2, code, down: down));
        if (!down) buttons.Remove(code);
    }
    public void Move(int dx, int dy) { lock (gate) Submit(events.Send(1, x: dx, y: dy)); }
    public void Scroll(int dx, int dy)
    {
        lock (gate)
        {
            scrollX += dx / 120.0; scrollY += dy / 120.0;
            var x = (int)scrollX; var y = (int)scrollY;
            // Protocol wheel-positive is up/right; portal/X11 axes are down/right.
            if (x != 0 || y != 0) Submit(events.Send(4, x: x, y: -y));
            scrollX -= x; scrollY -= y;
        }
    }
    public void Button(string button, bool down) { lock (gate) SendButton(button switch { "left" => 1, "right" => 3, _ => throw new InputFailureException() }, down); }
    public void Click(string button, bool twice)
    { lock (gate) { for (var i = 0; i < (twice ? 2 : 1); i++) { if (i != 0) Thread.Sleep(40); Button(button, true); Button(button, false); } } }
    public void Key(string key, bool down)
    {
        lock (gate)
        {
            // Resolve navigation/modifiers to stable tokens; releases never re-resolve a changed layout.
            var code = logicalHeld.TryGetValue(key, out var original) ? original : Resolve(Symbol(key));
            if (!KeyboardCommands.Modifiers.Contains(key))
            {
                // Core owns held/repeat state. Pulse navigation keys so X11/Mutter cannot
                // add independent server auto-repeat on top of the bounded phone repeats.
                if (down) { logicalHeld[key] = code; Tap(code); }
                else logicalHeld.Remove(key);
                return;
            }
            if (down)
            {
                if (logicalHeld.ContainsKey(key)) return;
                logicalHeld[key] = code;
            }
            SendKey(code, down);
            if (!down) logicalHeld.Remove(key);
        }
    }
    public void Press(string key) { lock (gate) { var code = Resolve(Symbol(key)); SendKey(code, true); SendKey(code, false); } }
    private void Tap(int code)
    {
        if ((code & 0x10000) != 0)
        {
            var shift = Resolve(0xffe1); SendKey(shift, true);
            SendKey(code & 0xffff, true); SendKey(code & 0xffff, false); SendKey(shift, false);
        }
        else { SendKey(code, true); SendKey(code, false); }
    }
    private void Batch(uint modifier, uint symbol)
    {
        var mod = Resolve(modifier); var code = Resolve(symbol);
        SendKey(mod, true); Tap(code); SendKey(mod, false);
    }
    public void Shortcut(string action)
    {
        lock (gate)
        {
            if (keys.Count != 0 || logicalHeld.Count != 0 || composePending) throw new InputFailureException();
            var symbol = action switch { "copy" => 'c', "paste" => 'v', "selectAll" => 'a', "undo" => 'z', "switchApp" => 0xff09u, _ => throw new InputFailureException() };
            Batch(action == "switchApp" ? 0xffe9u : 0xffe3u, symbol);
        }
    }
    public void Media(string profile, string action)
    {
        lock (gate)
        {
            if (!MediaProfiles.Supports(profile, action) || buttons.Count != 0 || keys.Count != 0 || logicalHeld.Count != 0 || composePending) throw new InputFailureException();
            if (profile == "General")
            {
                if (action is "volumeUp" or "volumeDown" or "mute") events.GeneralVolume(action);
                else Submit(events.Media(action switch { "playPause" => 0, "next" => 1, "previous" => 2, _ => throw new InputFailureException() }));
                return;
            }
            var chord = PcController.Host.DesktopMediaBindings.Chord("linux", profile, action);
            var codes = chord.Select(key => Resolve(key.Length == 1 ? key[0] : Symbol(key))).ToArray();
            foreach (var code in codes[..^1]) SendKey(code, true);
            Tap(codes[^1]);
            foreach (var code in codes[..^1].Reverse()) SendKey(code, false);
        }
    }
    public void Chord(IReadOnlyList<string> chord)
    {
        lock (gate)
        {
            if (keys.Count != 0 || logicalHeld.Count != 0 || composePending) throw new InputFailureException();
            var codes = chord.Select(k => Resolve(Symbol(k))).ToArray();
            foreach (var code in codes) SendKey(code, true);
            foreach (var code in codes.Reverse()) SendKey(code, false);
        }
    }
    // GNOME/GTK's documented Unicode compose method, never clipboard transfer/keymap mutation.
    // Explicit opt-in is required: non-GTK apps and custom input methods may interpret chords.
    public void Text(string text)
    {
        lock (gate)
        {
            if (!GtkTextEnabled || keys.Count != 0 || logicalHeld.Count != 0 || composePending || text.Length is < 1 or > 256) throw new InputFailureException("Enable GTK Unicode text only for a verified GTK text target.");
            for (int i = 0; i < text.Length; i++)
                if (char.IsSurrogate(text[i]) && (!char.IsHighSurrogate(text[i]) || ++i >= text.Length || !char.IsLowSurrogate(text[i]))) throw new InputFailureException();
            var runes = text.Replace("\r\n", "\n").Replace('\r', '\n').EnumerateRunes().ToArray();
            if (runes.Any(r => Rune.IsControl(r) && r.Value is not (9 or 10))) throw new InputFailureException();
            // Validate the complete required alphabet before any text is emitted.
            var alphabet = runes.Where(r => r.Value is not (9 or 10)).SelectMany(r => r.Value.ToString("x", CultureInfo.InvariantCulture)).Append('u').Distinct().ToDictionary(c => c, c => Resolve(c));
            var control = Resolve(0xffe3); var shift = Resolve(0xffe1); var enter = Resolve(0xff0d); _ = Resolve(0xff1b);
            foreach (var rune in runes)
            {
                if (rune.Value is 9 or 10) { Tap(Resolve(rune.Value == 9 ? 0xff09u : 0xff0du)); continue; }
                composePending = true;
                SendKey(control, true); SendKey(shift, true); Tap(alphabet['u']); SendKey(shift, false); SendKey(control, false);
                foreach (var hex in rune.Value.ToString("x", CultureInfo.InvariantCulture)) Tap(alphabet[hex]);
                Tap(enter); composePending = false;
            }
        }
    }
    internal void ConfirmLocalTextRecovery()
    {
        lock (gate)
        {
            if (keys.Count != 0 || buttons.Count != 0) throw new InputFailureException("First restore the original backend and Release input. Held cleanup is still pending.");
            composePending = false;
        }
    }
    internal void ConfirmEndedBackendRecovery()
    {
        lock (gate)
        {
            if (LinuxNative.pc_linux_forget_ended_backend() == 0) throw new InputFailureException("Original input transport is still live. Restore permission and use Release input.");
            keys.Clear(); buttons.Clear(); logicalHeld.Clear(); composePending = false;
            scrollX = scrollY = 0;
        }
    }
    public void ReleaseAll()
    {
        lock (gate)
        {
            var failed = !events.Send(5); // Retry native X11 wheel-button ups after partial scroll failure.
            foreach (var button in buttons.ToArray()) try { SendButton(button, false); } catch (InputFailureException) { failed = true; }
            foreach (var key in keys.ToArray()) try { SendKey(key, false); } catch (InputFailureException) { failed = true; }
            foreach (var logical in logicalHeld.Where(k => !keys.Contains(k.Value)).Select(k => k.Key).ToArray()) logicalHeld.Remove(logical);
            // Do not inject Escape into a newly focused app during cleanup. Incomplete compose
            // remains a local recovery requirement; restore target focus and cancel locally.
            scrollX = scrollY = 0;
            if (failed || composePending) throw new InputFailureException(composePending ? "Unicode composition interrupted. Cancel it locally in the original GTK target before restarting the companion; control stays paused." : "Linux could not release every tracked key/button. Restore the original backend and retry Release input.");
        }
    }
}
