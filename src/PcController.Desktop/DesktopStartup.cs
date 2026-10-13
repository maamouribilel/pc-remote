using PcController.MacOS;
using PcController.Linux;
namespace PcController.Desktop;
public interface IDesktopStartup
{
    bool Enabled { get; }
    string Message { get; }
    void Set(bool enabled);
}
internal sealed class MacDesktopStartup : IDesktopStartup
{
    public bool Enabled => MacLoginStartup.Status == 1;
    public string Message => MacLoginStartup.Status == 2
        ? "Login registration needs local approval in System Settings → General → Login Items."
        : "Login preference saved. Next sign-in starts paused on loopback; approve the network and Resume locally.";
    public void Set(bool enabled) => MacLoginStartup.Set(enabled);
}
internal sealed class LinuxDesktopStartup : IDesktopStartup
{
    private readonly LinuxLoginStartup startup = new();
    public bool Enabled => startup.Enabled;
    public string Message => "GNOME autostart preference saved for this extracted executable. Next sign-in starts paused; Wayland needs fresh consent and LAN needs approval.";
    public void Set(bool enabled) => startup.Set(enabled);
}
