namespace PcController.Windows;

// Pure rules for "Restart to update" (AUTO_UPDATE_PLAN.md Phase 2). Portable tests compile this file directly.
public static class WindowsUpdateRules
{
    // installer/PCRemote.iss AppId ("{{" there escapes one brace); Inno registers its uninstall entry as "<AppId>_is1".
    public const string AppId = "{DDF99789-80D2-481E-9501-9723169F070E}";
    public const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + AppId + "_is1";
    public const string Platform = "win-x64", Kind = "installer";

    // True only for the folder the installer registered, with its uninstaller inside it; a portable ZIP copy keeps notices only.
    public static bool IsInstalledCopy(string? installLocation, string? uninstallString, string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(installLocation) || string.IsNullOrWhiteSpace(uninstallString)) return false;
        try
        {
            static string Folder(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
            var installed = Folder(installLocation);
            if (!string.Equals(installed, Folder(baseDirectory), StringComparison.OrdinalIgnoreCase)) return false;
            // UninstallString is the quoted uninstaller path; Inno numbers it (unins000.exe, unins001.exe, …).
            var uninstaller = uninstallString.Trim().Trim('"');
            return Path.IsPathFullyQualified(uninstaller) && string.Equals(Path.GetDirectoryName(Path.GetFullPath(uninstaller)), installed, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException) { return false; }
    }

    // Silent install; PrepareToInstall closes the running companion with --exit and /relaunch=1 reopens it (PCRemote.iss UpdateRelaunch).
    public static string InstallerArguments(string logPath) => $"/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /relaunch=1 /LOG=\"{logPath}\"";
}
