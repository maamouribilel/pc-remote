using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using PcController.Core;

namespace PcController.Windows;

public sealed class WindowsPcActions : IPcActions
{
    public VolumeState GetVolume() => WindowsVolume.Read();
    public VolumeState SetVolume(int level) => WindowsVolume.Set(level);
    public LaunchResult Launch(LauncherApp app)
    {
        if (!LauncherSettings.ValidExecutable(app.Executable) || !File.Exists(app.Executable)) throw new IOException("app_missing");
        foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(app.Executable)))
        {
            using (process)
            {
                try
                {
                    if (!string.Equals(process.MainModule?.FileName, app.Executable, StringComparison.OrdinalIgnoreCase) || process.MainWindowHandle == 0) continue;
                    if (IsIconic(process.MainWindowHandle)) ShowWindowAsync(process.MainWindowHandle, 9);
                    if (SetForegroundWindow(process.MainWindowHandle)) return new(true);
                    FlashWindow(process.MainWindowHandle, true);
                    return new(false); // Windows can refuse foreground activation.
                }
                catch (Exception exception) when (exception is Win32Exception or InvalidOperationException) { }
            }
        }
        using var started = Process.Start(new ProcessStartInfo(app.Executable) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(app.Executable)!, Arguments = "" });
        if (started == null) throw new IOException("launch_failed");
        return new(false);
    }
    public void Power(string action)
    {
        if (!PowerQueue.Supported(action)) throw new ArgumentOutOfRangeException(nameof(action));
        if (!OpenProcessToken(GetCurrentProcess(), 0x0020 | 0x0008, out var token)) throw new Win32Exception(Marshal.GetLastWin32Error());
        using (token)
        {
            if (!LookupPrivilegeValue(null, "SeShutdownPrivilege", out var luid)) throw new Win32Exception(Marshal.GetLastWin32Error());
            var enabled = new TokenPrivileges { Count = 1, Luid = luid, Attributes = 2 };
            if (!AdjustTokenPrivileges(token, false, ref enabled, Marshal.SizeOf<TokenPrivileges>(), out var previous, out _) || Marshal.GetLastWin32Error() != 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            try
            {
                // No force flags: applications can still stop restart/shutdown to save work.
                var result = action == "sleep" ? SetSuspendState(false, false, false) : ExitWindowsEx(action == "restart" ? 2u : 8u, 0x80040000);
                if (!result) throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally { AdjustTokenPrivileges(token, false, ref previous, Marshal.SizeOf<TokenPrivileges>(), out _, out _); }
        }
    }
    [StructLayout(LayoutKind.Sequential)] private struct Luid { public uint Low; public int High; }
    [StructLayout(LayoutKind.Sequential)] private struct TokenPrivileges { public uint Count; public Luid Luid; public uint Attributes; }
    [DllImport("kernel32.dll")] private static extern IntPtr GetCurrentProcess();
    [DllImport("advapi32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool OpenProcessToken(IntPtr process, uint access, out SafeAccessTokenHandle token);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool LookupPrivilegeValue(string? system, string name, out Luid luid);
    [DllImport("advapi32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool AdjustTokenPrivileges(SafeAccessTokenHandle token, [MarshalAs(UnmanagedType.Bool)] bool disableAll, ref TokenPrivileges next, int length, out TokenPrivileges previous, out int returned);
    [DllImport("powrprof.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.U1)] private static extern bool SetSuspendState([MarshalAs(UnmanagedType.U1)] bool hibernate, [MarshalAs(UnmanagedType.U1)] bool force, [MarshalAs(UnmanagedType.U1)] bool disableWake);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ExitWindowsEx(uint flags, uint reason);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ShowWindowAsync(IntPtr window, int command);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool FlashWindow(IntPtr window, [MarshalAs(UnmanagedType.Bool)] bool invert);
}
