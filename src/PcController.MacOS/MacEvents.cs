using PcController.Core;
namespace PcController.MacOS;
// Internal seam for fault/cleanup tests. Production always creates NativeMacEvents.
internal interface IMacEvents
{
    int Move(int x, int y, int drag, ulong flags, int guard);
    int Scroll(int x, int y, ulong flags, int guard);
    int Button(int right, int down, int count, ulong flags, int guard);
    int Key(ushort key, int down, int repeat, ulong flags, int guard);
    int Unicode(ushort[] text, int length, int down, int guard);
    int CommandKey(ushort character);
    int LayoutKey(ushort character) => CommandKey(character);
    int SystemKey(int code, int down, int guard) => 0;
    bool PlayerFocus(string profile) => false;
    void GeneralVolume(string action) => throw new InputFailureException();
}
internal sealed class NativeMacEvents : IMacEvents
{
    public int Move(int x, int y, int drag, ulong flags, int guard) => MacNative.pc_move(x, y, drag, flags, guard);
    public int Scroll(int x, int y, ulong flags, int guard) => MacNative.pc_scroll(x, y, flags, guard);
    public int Button(int right, int down, int count, ulong flags, int guard) => MacNative.pc_button(right, down, count, flags, guard);
    public int Key(ushort key, int down, int repeat, ulong flags, int guard) => MacNative.pc_key(key, down, repeat, flags, guard);
    public int Unicode(ushort[] text, int length, int down, int guard) => MacNative.pc_unicode(text, length, down, guard);
    public int LayoutKey(ushort character) => MacNative.pc_layout_key(character);
    public int SystemKey(int code, int down, int guard) => MacNative.pc_system_key(code, down, guard);
    public bool PlayerFocus(string profile) => MacNative.pc_player_focus(profile) != 0;
    public void GeneralVolume(string action) => new MacPcActions().GeneralVolume(action);
    public int CommandKey(ushort character) => MacNative.pc_command_key(character);
}
