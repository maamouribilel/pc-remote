using Avalonia;
using PcController.MacOS;
using PcController.Linux;
namespace PcController.Desktop;
internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.SequenceEqual(new[] { "--package-info" }))
        {
            var assembly = typeof(Program).Assembly;
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { version = assembly.GetName().Version?.ToString(), informationalVersion = assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false).Cast<System.Reflection.AssemblyInformationalVersionAttribute>().Single().InformationalVersion,
                runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription, architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(), bundledRemote = File.Exists(Path.Combine(AppContext.BaseDirectory, "wwwroot", "index.html")) }));
            return;
        }
        if (!OperatingSystem.IsMacOS() && !OperatingSystem.IsLinux()) { Console.Error.WriteLine("Use the existing Windows WPF companion. This development shell targets macOS/Linux."); Environment.ExitCode = 1; return; }
        IDisposable? instance;
        try { instance = OperatingSystem.IsMacOS() ? SingleInstance.TryAcquire() : LinuxSingleInstance.TryAcquire(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or DllNotFoundException) { Console.Error.WriteLine("PC Remote setup unavailable: " + e.Message); Environment.ExitCode = 1; return; }
        using var heldInstance = instance;
        if (instance == null) { Console.Error.WriteLine("PC Remote is already running. Open its menu-bar or taskbar window."); return; }
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<DesktopApp>().UsePlatformDetect();
}
