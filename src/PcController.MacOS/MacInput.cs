using PcController.Core;
namespace PcController.MacOS;

// One tracker survives listener replacement. Track before down; forget only after up submission.
public sealed class MacInput : IMouseInput, IKeyboardInput, IMediaInput
{
    private readonly object gate = new();
    private readonly HashSet<string> buttons = [];
    private readonly Dictionary<ushort, ulong> keys = [];
    private ushort[]? packet;
    private readonly HashSet<int> systemKeys = [];
    private double scrollX, scrollY;
    private readonly int targetPid;
    private readonly IMacEvents events;
    public MacInput(int disposableTargetPid = 0) { MacNative.RequireUser(); targetPid = disposableTargetPid; events = new NativeMacEvents(); }
    internal MacInput(IMacEvents events, int disposableTargetPid = 0) { this.events = events; targetPid = disposableTargetPid; }
    public static bool ShortcutsAvailable => new[] { 'c', 'v', 'a', 'z' }.All(c => MacNative.pc_command_key(c) >= 0);
    private ulong Flags => keys.Values.Aggregate(0UL, (a, b) => a | b);
    private static void Submitted(int result)
    { if (result == 0) throw new InputFailureException("macOS input submission failed. Check Accessibility, Secure Input, session and target focus; cleanup must succeed before Resume."); }
    private static (ushort Code, ulong Flag) Map(string key) => key switch
    {
        "Enter" => (36, 0), "Tab" => (48, 0), "Backspace" => (51, 0), "Escape" => (53, 0),
        "Windows" => (55, 1UL << 20), "Shift" => (56, 1UL << 17), "Alt" => (58, 1UL << 19), "Control" => (59, 1UL << 18),
        "Home" => (115, 0), "End" => (119, 0), "PageUp" => (116, 0), "PageDown" => (121, 0),
        "ArrowLeft" => (123, 0), "ArrowRight" => (124, 0), "ArrowDown" => (125, 0), "ArrowUp" => (126, 0),
        _ => throw new InputFailureException("Unsupported macOS key.")
    };
    public void Move(int dx, int dy)
    { lock (gate) Submitted(events.Move(dx, dy, buttons.Contains("left") ? 1 : buttons.Contains("right") ? 2 : 0, Flags, targetPid)); }
    public void Scroll(int dx, int dy)
    {
        lock (gate)
        {
            // v1 normalized detents -> Quartz pixel units; preserve small deltas.
            scrollX += dx * 12.0 / InputSemantics.ScrollUnitsPerDetent;
            scrollY += dy * 12.0 / InputSemantics.ScrollUnitsPerDetent;
            var x = (int)scrollX; var y = (int)scrollY;
            if (x != 0 || y != 0) Submitted(events.Scroll(x, y, Flags, targetPid));
            scrollX -= x; scrollY -= y;
        }
    }
    public void Button(string button, bool down) { lock (gate) SendButton(button, down, 1); }
    private void SendButton(string button, bool down, int count)
    {
        if (button is not ("left" or "right")) throw new InputFailureException();
        if (down && buttons.Contains(button)) return;
        if (!down && !buttons.Contains(button)) return;
        if (down) buttons.Add(button);
        Submitted(events.Button(button == "right" ? 1 : 0, down ? 1 : 0, count, Flags, targetPid));
        if (!down) buttons.Remove(button);
    }
    public void Click(string button, bool twice)
    {
        lock (gate)
        {
            if (buttons.Contains(button)) throw new InputFailureException();
            for (var count = 1; count <= (twice ? 2 : 1); count++) { SendButton(button, true, count); SendButton(button, false, count); }
        }
    }
    private void SendKey(ushort code, ulong flag, bool down)
    {
        var repeat = down && keys.ContainsKey(code);
        if (repeat && flag != 0) return;
        if (!down && !keys.ContainsKey(code)) return;
        if (down) keys[code] = flag;
        var flags = down ? Flags : keys.Where(k => k.Key != code).Aggregate(0UL, (a, k) => a | k.Value);
        Submitted(events.Key(code, down ? 1 : 0, repeat ? 1 : 0, flags, targetPid));
        if (!down) keys.Remove(code);
    }
    public void Key(string key, bool down) { lock (gate) { var value = Map(key); SendKey(value.Code, value.Flag, down); } }
    public void Press(string key)
    { lock (gate) { var value = Map(key); if (keys.ContainsKey(value.Code)) throw new InputFailureException(); SendKey(value.Code, value.Flag, true); SendKey(value.Code, value.Flag, false); } }
    public void Text(string text)
    {
        lock (gate)
        {
            if (keys.Count != 0 || packet != null) throw new InputFailureException();
            foreach (var rune in text.Replace("\r\n", "\n").Replace('\r', '\n').EnumerateRunes())
            {
                if (rune.Value is 10 or 9) { Press(rune.Value == 10 ? "Enter" : "Tab"); continue; }
                packet = rune.ToString().Select(c => (ushort)c).ToArray();
                Submitted(events.Unicode(packet, packet.Length, 1, targetPid));
                Submitted(events.Unicode(packet, packet.Length, 0, targetPid));
                packet = null;
            }
        }
    }
    public void Shortcut(string action)
    {
        lock (gate)
        {
            if (keys.Count != 0 || packet != null) throw new InputFailureException();
            var character = action switch { "copy" => 'c', "paste" => 'v', "selectAll" => 'a', "undo" => 'z', "switchApp" => '\t', _ => throw new InputFailureException() };
            var code = character == '\t' ? 48 : events.CommandKey(character);
            if (code < 0) throw new InputFailureException("Current keyboard layout cannot resolve this Command shortcut. Select a Unicode keyboard layout and retry.");
            SendKey(55, 1UL << 20, true); SendKey((ushort)code, 0, true);
            SendKey((ushort)code, 0, false); SendKey(55, 1UL << 20, false);
        }
    }
    public void Media(string profile, string action)
    {
        lock (gate)
        {
            if (!MediaProfiles.Supports(profile, action) || buttons.Count != 0 || keys.Count != 0 || packet != null || systemKeys.Count != 0) throw new InputFailureException();
            if (profile == "General")
            {
                if (action is "volumeUp" or "volumeDown" or "mute") { events.GeneralVolume(action); return; }
                var code = action switch { "playPause" => 16, "next" => 17, "previous" => 18, _ => throw new InputFailureException() };
                systemKeys.Add(code); Submitted(events.SystemKey(code, 1, targetPid));
                Submitted(events.SystemKey(code, 0, targetPid)); systemKeys.Remove(code); return;
            }
            if (!events.PlayerFocus(profile)) throw new InputFailureException("Focus the matching player/browser locally before media control. Browser tab/player focus is still required.");
            var chord = PcController.Host.DesktopMediaBindings.Chord("macos", profile, action);
            var command = chord.Contains("Windows");
            var mapped = chord.Select(key => key.Length == 1
                ? (Code: (ushort)ResolveMedia(key[0], command), Flag: 0UL) : Map(key)).ToArray();
            foreach (var key in mapped) SendKey(key.Code, key.Flag, true);
            foreach (var key in mapped.Reverse()) SendKey(key.Code, key.Flag, false);
        }
    }
    private int ResolveMedia(char character, bool command)
    {
        var code = character == ' ' ? 49 : command ? events.CommandKey(character) : events.LayoutKey(character);
        return code >= 0 ? code : throw new InputFailureException("Current layout cannot resolve player shortcut.");
    }
    public void Chord(IReadOnlyList<string> chord)
    {
        lock (gate)
        {
            if (keys.Count != 0 || packet != null) throw new InputFailureException();
            // Only allow the public navigation/modifier contract here. Logical shortcuts resolve layouts above.
            var mapped = chord.Select(Map).ToArray();
            foreach (var value in mapped) SendKey(value.Code, value.Flag, true);
            foreach (var value in mapped.Reverse()) SendKey(value.Code, value.Flag, false);
        }
    }
    public void ReleaseAll()
    {
        lock (gate)
        {
            bool failed = false;
            void Try(Action release) { try { release(); } catch (InputFailureException) { failed = true; } }
            foreach (var code in systemKeys.ToArray()) Try(() => { Submitted(events.SystemKey(code, 0, targetPid)); systemKeys.Remove(code); });
            foreach (var button in buttons.ToArray()) Try(() => SendButton(button, false, 1));
            if (packet is { } pending) Try(() => { Submitted(events.Unicode(pending, pending.Length, 0, targetPid)); packet = null; });
            foreach (var key in keys.OrderBy(k => k.Value == 0 ? 0 : 1).ToArray()) Try(() => SendKey(key.Key, key.Value, false));
            scrollX = scrollY = 0;
            if (failed) throw new InputFailureException("macOS could not release all tracked input. Restore Accessibility and the session, then Release input before Resume.");
        }
    }
}
