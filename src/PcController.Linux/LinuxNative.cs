using System.Runtime.InteropServices;
namespace PcController.Linux;
internal static class LinuxNative
{
    private const string Bridge = "PcControllerLinux";
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate void UnsafeCallback();
    [DllImport(Bridge)] internal static extern int pc_linux_open(int backend, UnsafeCallback callback);
    [DllImport(Bridge)] internal static extern int pc_linux_ready();
    [DllImport(Bridge)] internal static extern int pc_linux_observed();
    [DllImport(Bridge)] internal static extern ulong pc_linux_generation();
    [DllImport(Bridge)] internal static extern int pc_linux_arm(ulong generation);
    [DllImport(Bridge)] internal static extern int pc_linux_consent();
    [DllImport(Bridge)] internal static extern void pc_linux_cancel_consent();
    [DllImport(Bridge)] internal static extern int pc_linux_event(int type, int code, double x, double y, int down);
    [DllImport(Bridge)] internal static extern int pc_linux_resolve(uint symbol);
    [DllImport(Bridge)] internal static extern void pc_linux_close();
    [DllImport(Bridge)] internal static extern int pc_linux_forget_ended_backend();
    [DllImport(Bridge)] internal static extern int pc_linux_secret([MarshalAs(UnmanagedType.LPUTF8Str)] string account, byte[] key, int create);
    [DllImport(Bridge)] internal static extern int pc_linux_owned_path([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int directory);
    [DllImport("libc")] internal static extern uint geteuid();
    [DllImport("libc")] internal static extern uint umask(uint mask);
    [DllImport(Bridge)] internal static extern int pc_linux_feature_ready();
    [DllImport(Bridge)] internal static extern int pc_linux_volume(int mode, int requested, out int level, out int muted);
    [DllImport(Bridge)] internal static extern int pc_linux_media(int action);
    [DllImport(Bridge)] internal static extern int pc_linux_can_power(int action);
    [DllImport(Bridge)] internal static extern int pc_linux_power(int action);
    [DllImport(Bridge)] internal static extern int pc_linux_app_valid([MarshalAs(UnmanagedType.LPUTF8Str)] string path);
    [DllImport(Bridge)] internal static extern int pc_linux_launch([MarshalAs(UnmanagedType.LPUTF8Str)] string path);
    internal static void RequireUser()
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException();
        if (geteuid() == 0) throw new IOException("Run PC Remote as the signed-in desktop user, never root.");
    }
}
public enum LinuxBackend { X11 = 1, WaylandPortal = 2 }
public static class LinuxSession
{
    internal static void RequireBaseline()
    {
        var values = File.ReadAllLines("/etc/os-release").Where(line => line.Contains('='))
            .Select(line => line.Split('=', 2)).ToDictionary(p => p[0], p => p[1].Trim('"'), StringComparer.Ordinal);
        if (values.GetValueOrDefault("ID") != "ubuntu" || values.GetValueOrDefault("VERSION_ID") != "24.04" || RuntimeInformation.ProcessArchitecture != Architecture.X64)
            throw new IOException("Production Linux candidate targets Ubuntu 24.04 x64/GNOME only. Other distributions/architectures need separate review and native acceptance.");
        var desktop = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "";
        if (!desktop.Split(':').Contains("GNOME", StringComparer.OrdinalIgnoreCase)) throw new IOException("The Linux candidate requires a local GNOME desktop session.");
    }
    public static LinuxBackend Detect(string? type, string? wayland, string? display)
    {
        // Never infer arbitrary Wayland control from an XWayland DISPLAY.
        if (type == "wayland" && !string.IsNullOrWhiteSpace(wayland)) return LinuxBackend.WaylandPortal;
        if (type == "x11" && string.IsNullOrWhiteSpace(wayland) && !string.IsNullOrWhiteSpace(display)) return LinuxBackend.X11;
        throw new IOException("No unambiguous local X11/Wayland desktop session. SSH/headless and conflicting session variables are unsupported.");
    }
    public static LinuxBackend Detect() => Detect(Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"), Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"), Environment.GetEnvironmentVariable("DISPLAY"));
}
