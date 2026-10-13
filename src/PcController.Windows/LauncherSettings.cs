namespace PcController.Windows;
public sealed class LauncherSettings(string directory)
    : Host.LauncherSettings(directory, new WindowsLauncherPlatform())
{
    public static bool ValidExecutable(string executable) => new WindowsLauncherPlatform().ValidExecutable(executable);
}
