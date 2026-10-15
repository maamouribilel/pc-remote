using System.Diagnostics;
using Microsoft.Win32;
using PcController.Host;

namespace PcController.Windows;

// "Restart to update" for the installer copy only. Runs the verified Inno Setup installer silently while
// UpdateService holds its file locked. The window has already paused control and released input (and
// stopped if that failed); the installer's PrepareToInstall closes this companion with --exit and
// /relaunch=1 reopens it.
public sealed class WindowsUpdateInstaller : IUpdateInstaller
{
    public string Platform => WindowsUpdateRules.Platform;
    public string Kind => WindowsUpdateRules.Kind;
    public string DownloadDirectory { get; }
    private WindowsUpdateInstaller(string directory) => DownloadDirectory = directory;

    public static WindowsUpdateInstaller? ForInstalledCopy(string dataDirectory)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(WindowsUpdateRules.UninstallKey);
            return WindowsUpdateRules.IsInstalledCopy(key?.GetValue("InstallLocation") as string, key?.GetValue("UninstallString") as string, AppContext.BaseDirectory)
                ? new(Path.Combine(dataDirectory, "updates")) : null;
        }
        catch (Exception e) when (e is System.Security.SecurityException or IOException or UnauthorizedAccessException) { return null; }
    }

    public Task<int> Launch(VerifiedDownload download)
    {
        var start = new ProcessStartInfo(download.Path, WindowsUpdateRules.InstallerArguments(Path.Combine(DownloadDirectory, "install.log")))
        { UseShellExecute = false, WorkingDirectory = DownloadDirectory };
        var process = Process.Start(start) ?? throw new InvalidOperationException("The installer did not start.");
        return Wait(process);

        static async Task<int> Wait(Process process)
        {
            using (process) { await process.WaitForExitAsync().ConfigureAwait(false); return process.ExitCode; }
        }
    }
}
