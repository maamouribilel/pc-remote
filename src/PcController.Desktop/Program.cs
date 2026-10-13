using Avalonia;
namespace PcController.Desktop;
internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (!OperatingSystem.IsMacOS() && !OperatingSystem.IsLinux()) { Console.Error.WriteLine("Use the existing Windows WPF companion. This development shell targets macOS/Linux."); Environment.ExitCode = 1; return; }
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<DesktopApp>().UsePlatformDetect();
}
