using System.Runtime.InteropServices;
using PcController.Core;

namespace PcController.Windows;

public sealed class WindowsInput : IMouseInput, IKeyboardInput
{
    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput { public int X, Y; public uint Data, Flags, Time; public nuint Extra; }
    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput { public ushort Vk, Scan; public uint Flags, Time; public nuint Extra; }
    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion { [FieldOffset(0)] public MouseInput Mouse; [FieldOffset(0)] public KeyboardInput Keyboard; }
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeInput { public uint Type; public InputUnion Value; }
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, [In] NativeInput[] input, int size);
    private readonly object gate = new();
    private readonly HashSet<string> held = [];
    private readonly HashSet<string> heldKeys = [];
    private readonly HashSet<ushort> packets = [];
    public static int NativeSize => Marshal.SizeOf<NativeInput>();
    public WindowsInput()
    {
        if (NativeSize != (IntPtr.Size == 8 ? 40 : 28)) throw new InvalidOperationException("Native INPUT layout mismatch.");
    }
    private static NativeInput Mouse(uint flags, int x = 0, int y = 0, int data = 0) => new()
    { Type = 0, Value = new InputUnion { Mouse = new MouseInput { X = x, Y = y, Data = unchecked((uint)data), Flags = flags } } };
    private static void Insert(params NativeInput[] events)
    { if (events.Length != 0 && SendInput((uint)events.Length, events, NativeSize) != events.Length) throw new InputFailureException(); }
    private static uint Flag(string button, bool down) => button switch
    { "left" => down ? 0x0002u : 0x0004u, "right" => down ? 0x0008u : 0x0010u, _ => throw new ArgumentOutOfRangeException(nameof(button)) };
    public void Move(int dx, int dy) { lock (gate) Insert(Mouse(0x0001, dx, dy)); }
    public void Scroll(int dx, int dy)
    {
        lock (gate)
        {
            var events = new List<NativeInput>();
            if (dy != 0) events.Add(Mouse(0x0800, data: dy));
            if (dx != 0) events.Add(Mouse(0x1000, data: dx));
            Insert(events.ToArray());
        }
    }
    public void Button(string button, bool down)
    {
        lock (gate)
        {
            if (down) held.Add(button); // Conservatively track before potentially partial insertion.
            Insert(Mouse(Flag(button, down)));
            if (!down) held.Remove(button);
        }
    }
    public void Click(string button, bool twice)
    {
        lock (gate)
        {
            if (held.Contains(button)) throw new InputFailureException();
            held.Add(button);
            var down = Mouse(Flag(button, true)); var up = Mouse(Flag(button, false));
            Insert(twice ? [down, up, down, up] : [down, up]); held.Remove(button);
        }
    }
    public void ReleaseAll()
    {
        lock (gate)
        {
            var releases = held.Select(button => Mouse(Flag(button, false)))
                .Concat(heldKeys.OrderBy(key => KeyboardCommands.Modifiers.Contains(key) ? 1 : 0).Select(key => Keyboard(key, false)))
                .Concat(packets.Select(scan => Packet(scan, true))).ToArray();
            Insert(releases); held.Clear(); heldKeys.Clear(); packets.Clear();
        }
    }
    private static (ushort Vk, bool Extended) VirtualKey(string key) => key switch
    {
        "Enter" => (0x0D, false), "Backspace" => (0x08, false), "Escape" => (0x1B, false), "Tab" => (0x09, false),
        "Control" => (0xA2, false), "Alt" => (0xA4, false), "Shift" => (0xA0, false), "Windows" => (0x5B, true),
        "ArrowLeft" => (0x25, true), "ArrowUp" => (0x26, true), "ArrowRight" => (0x27, true), "ArrowDown" => (0x28, true),
        "Home" => (0x24, true), "End" => (0x23, true), "PageUp" => (0x21, true), "PageDown" => (0x22, true),
        "A" => (0x41, false), "C" => (0x43, false), "V" => (0x56, false), "Z" => (0x5A, false),
        "K" => (0x4B, false), "J" => (0x4A, false), "L" => (0x4C, false), "F" => (0x46, false), "M" => (0x4D, false), "N" => (0x4E, false), "P" => (0x50, false), "Space" => (0x20, false),
        "MediaPlayPause" => (0xB3, true), "MediaNext" => (0xB0, true), "MediaPrevious" => (0xB1, true), "VolumeUp" => (0xAF, true), "VolumeDown" => (0xAE, true), "VolumeMute" => (0xAD, true),
        _ => throw new ArgumentOutOfRangeException(nameof(key))
    };
    private static NativeInput Keyboard(string key, bool down)
    {
        var value = VirtualKey(key);
        return new() { Type = 1, Value = new InputUnion { Keyboard = new KeyboardInput { Vk = value.Vk, Flags = (down ? 0u : 2u) | (value.Extended ? 1u : 0u) } } };
    }
    private static NativeInput Packet(ushort character, bool up) => new()
    { Type = 1, Value = new InputUnion { Keyboard = new KeyboardInput { Scan = character, Flags = 4u | (up ? 2u : 0u) } } };
    public void Key(string key, bool down)
    {
        lock (gate) { if (down) heldKeys.Add(key); Insert(Keyboard(key, down)); if (!down) heldKeys.Remove(key); }
    }
    public void Press(string key)
    {
        lock (gate) { if (heldKeys.Contains(key)) throw new InputFailureException(); heldKeys.Add(key); Insert(Keyboard(key, true), Keyboard(key, false)); heldKeys.Remove(key); }
    }
    public void Text(string text)
    {
        lock (gate)
        {
            var events = new List<NativeInput>();
            foreach (var character in text.Replace("\r\n", "\n").Replace('\r', '\n'))
            {
                // VK_PACKET carries each UTF-16 unit, including both units of surrogate pairs.
                if (character == '\n') { heldKeys.Add("Enter"); events.Add(Keyboard("Enter", true)); events.Add(Keyboard("Enter", false)); }
                else { packets.Add(character); events.Add(Packet(character, false)); events.Add(Packet(character, true)); }
            }
            Insert(events.ToArray()); packets.Clear(); heldKeys.Remove("Enter");
        }
    }
    public void Shortcut(string action)
        => Chord(KeyboardCommands.Shortcuts[action]);
    public void Chord(IReadOnlyList<string> keys)
    {
        lock (gate)
        {
            if (heldKeys.Count != 0) throw new InputFailureException();
            foreach (var key in keys) heldKeys.Add(key);
            Insert(keys.Select(key => Keyboard(key, true)).Concat(keys.Reverse().Select(key => Keyboard(key, false))).ToArray());
            heldKeys.Clear();
        }
    }
}
