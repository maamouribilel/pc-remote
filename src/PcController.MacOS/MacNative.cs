using System.Runtime.InteropServices;
namespace PcController.MacOS;

internal static class MacNative
{
    private const string Library = "PcControllerMac";
    [DllImport(Library)] internal static extern int pc_diagnostics();
    [DllImport(Library)] internal static extern int pc_permission();
    [DllImport(Library)] internal static extern void pc_onboard();
    [DllImport(Library)] internal static extern int pc_session();
    [DllImport(Library)] internal static extern int pc_frontmost();
    [DllImport(Library)] internal static extern int pc_test_activate(int pid);
    [DllImport(Library)] internal static extern int pc_arm(ulong expected);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate void UnsafeCallback();
    [DllImport(Library)] internal static extern int pc_observe(UnsafeCallback callback);
    [DllImport(Library)] internal static extern void pc_unobserve();
    [DllImport(Library)] internal static extern ulong pc_generation();
    [DllImport(Library)] internal static extern void pc_pointer(out double x, out double y);
    [DllImport(Library)] internal static extern int pc_move(int x, int y, int drag, ulong flags, int guard);
    [DllImport(Library)] internal static extern int pc_button(int right, int down, int count, ulong flags, int guard);
    [DllImport(Library)] internal static extern int pc_scroll(int x, int y, ulong flags, int guard);
    [DllImport(Library)] internal static extern int pc_key(ushort key, int down, int repeat, ulong flags, int guard);
    [DllImport(Library)] internal static extern int pc_unicode([In] ushort[] text, int length, int down, int guard);
    [DllImport(Library)] internal static extern int pc_test_layout_key([MarshalAs(UnmanagedType.LPUTF8Str)] string identifier, ushort character);
    [DllImport(Library)] internal static extern int pc_layout_key(ushort character);
    [DllImport(Library)] internal static extern int pc_command_key(ushort character);
    [DllImport(Library)] internal static extern int pc_secret([MarshalAs(UnmanagedType.LPUTF8Str)] string account, [In, Out] byte[] bytes, int create);
    [DllImport(Library)] internal static extern int pc_secret_authorize([MarshalAs(UnmanagedType.LPUTF8Str)] string account);
    [DllImport(Library)] internal static extern int pc_delete_test_secret([MarshalAs(UnmanagedType.LPUTF8Str)] string account);
    [DllImport(Library)] internal static extern int pc_test_keychain_create([MarshalAs(UnmanagedType.LPUTF8Str)] string path, byte[] password, int length);
    [DllImport(Library)] internal static extern int pc_test_keychain_lock([MarshalAs(UnmanagedType.LPUTF8Str)] string path);
    [DllImport(Library)] internal static extern int pc_test_keychain_delete([MarshalAs(UnmanagedType.LPUTF8Str)] string path);
    [DllImport(Library)] internal static extern int pc_test_secret([MarshalAs(UnmanagedType.LPUTF8Str)] string path, [MarshalAs(UnmanagedType.LPUTF8Str)] string account, [In, Out] byte[] bytes, int create);
    [DllImport("libc")] internal static extern uint geteuid();
    [DllImport("libc")] internal static extern uint umask(uint mask);
    [DllImport(Library)] internal static extern int pc_feature_ready();
    [DllImport(Library)] internal static extern int pc_volume(int mode, int requested, out int level, out int muted);
    [DllImport(Library)] internal static extern int pc_system_key(int code, int down, int guard);
    [DllImport(Library)] internal static extern int pc_player_focus([MarshalAs(UnmanagedType.LPUTF8Str)] string profile);
    [DllImport(Library)] internal static extern int pc_app_valid([MarshalAs(UnmanagedType.LPUTF8Str)] string path);
    [DllImport(Library)] internal static extern int pc_launch([MarshalAs(UnmanagedType.LPUTF8Str)] string path);
    [DllImport(Library)] internal static extern int pc_can_sleep();
    [DllImport(Library)] internal static extern int pc_sleep();
    [DllImport(Library)] internal static extern int pc_login_status();
    [DllImport(Library)] internal static extern int pc_login_set(int enabled);
    public static void RequireUser()
    {
        if (!OperatingSystem.IsMacOS() || geteuid() == 0) throw new InvalidOperationException("PC Remote requires a signed-in macOS user; never run as root.");
    }
}
public static class MacPermissions
{
    public static bool Accessibility => MacNative.pc_permission() != 0;
    public static bool Interactive => MacNative.pc_session() != 0;
    public static void OpenAccessibilitySettings() => MacNative.pc_onboard();
    public const string Recovery = "Open System Settings → Privacy & Security → Accessibility (Device Control and Data Access on macOS 27), add this PC Remote.app and enable it. Quit and reopen the same app. After a rebuild, remove a stale entry and add the candidate again. Control remains paused until permission exists and you choose Resume. Secure Input/password fields temporarily block control.";
}
