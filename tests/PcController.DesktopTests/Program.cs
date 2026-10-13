using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PcController.Core;
using PcController.Desktop;

internal static class Program
{
    private static int passed;
    internal static void Check(bool ok, string message) { if (!ok) throw new Exception("FAIL: " + message); Console.WriteLine("PASS: " + message); passed++; }
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--show-fixture")
        { AppBuilder.Configure<FixtureApp>().UsePlatformDetect().StartWithClassicDesktopLifetime(args); return; }
        if (args.Length == 1 && args[0] == "--native-startup-check")
        { AppBuilder.Configure<NativeStartupCheckApp>().UsePlatformDetect().StartWithClassicDesktopLifetime(args); return; }
        DesktopChecks.Run(Check).GetAwaiter().GetResult();
        AppBuilder.Configure<DesktopApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions()).SetupWithoutStarting();
        using var test = new UiChecks(); test.Run(Check);
        Console.WriteLine($"{passed} desktop checks passed.");
    }
}
internal sealed class UiChecks : IDisposable
{
    private readonly DesktopController controller = new("macos", new TrustedNetworks(() => []), observeNetworks: false);
    private MainWindow? window;
    internal void Run(Action<bool, string> check)
    {
        check(NativeMenu.GetMenu(Application.Current!)!.Items.OfType<NativeMenuItem>().Any(i => i.Header == "Open PC Remote"), "application menu is configured before native initialization");
        window = new MainWindow(controller); window.Show(); Dispatcher.UIThread.RunJobs();
        Button Find(Window owner, string id) => owner.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == id);
        check(window.IsVisible && window.Title!.Contains("development"), "headless shell opens with development identity");
        check(!Find(window, "new-pairing").IsEnabled && !Find(window, "release").IsEnabled && !Find(window, "pause").IsEnabled, "setup-required shell disables unavailable host actions");
        check(!Find(window, "approve-network").IsEnabled, "network approval requires an explicitly selected observable candidate");
        check(window.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text?.Contains("Keychain") == true), "shell explains secure storage/native setup requirement");
        var allowed = new ApprovalPrompt(new("test", "Disposable browser", DateTimeOffset.UtcNow.AddMinutes(1)), "loopback fixture");
        var approve = new ApprovalWindow(allowed); approve.Show(); Dispatcher.UIThread.RunJobs();
        Find(approve, "approve-browser").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        check(allowed.Decision.GetAwaiter().GetResult(), "actual approval button resolves allow");
        var denied = new ApprovalPrompt(new("test", "Disposable browser", DateTimeOffset.UtcNow.AddMinutes(1)), "loopback fixture");
        var deny = new ApprovalWindow(denied); deny.Show(); Dispatcher.UIThread.RunJobs();
        Find(deny, "deny-browser").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        check(!denied.Decision.GetAwaiter().GetResult(), "actual deny button resolves denial");
        var dismissed = new ApprovalPrompt(new("test", "Disposable browser", DateTimeOffset.UtcNow.AddMinutes(1)), "loopback fixture");
        var dismiss = new ApprovalWindow(dismissed); dismiss.Show(); dismiss.Close();
        check(!dismissed.Decision.GetAwaiter().GetResult(), "closing approval window denies request");
        var expired = new ApprovalPrompt(new("test", "Disposable browser", DateTimeOffset.UtcNow.AddSeconds(-1)), "loopback fixture");
        var expire = new ApprovalWindow(expired); expire.Show(); Dispatcher.UIThread.RunJobs();
        Find(expire, "approve-browser").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        check(!expired.Decision.GetAwaiter().GetResult(), "expired approval cannot be granted by UI click");
        bool exited = false; window.ExitRequested += () => exited = true;
        window.Close(); check(exited, "window fallback closes through graceful Exit when tray has not been verified");
        window.Close();
        var directory = Path.Combine(Path.GetTempPath(), "PcDesktopButtons-" + Guid.NewGuid().ToString("N"));
        var input = new RecordingMouse();
        var fixture = new DesktopController("macos", new TrustedNetworks(() => []), network => PortableFixture.Create(directory, input, network: network, capabilities: PlatformCapabilities.Unavailable("macos")), observeNetworks: false);
        try
        {
            fixture.RefreshAsync().GetAwaiter().GetResult();
            check(fixture.Server != null, "fixture dashboard host starts: " + fixture.Status);
            var dashboard = new MainWindow(fixture); dashboard.Show(); Dispatcher.UIThread.RunJobs(); dashboard.UpdateLayout();
            check(dashboard.GetVisualDescendants().OfType<Image>().Any(i => i.Source != null && i.IsVisible), "fixture dashboard renders actual short-lived pairing QR (visual images=" + string.Join(",", dashboard.GetVisualDescendants().OfType<Image>().Select(i => $"{i.IsVisible}/{i.Source != null}")) + "; active link=" + (fixture.Server!.PairingAddress != null) + ")");
            var before = fixture.Server!.PairingAddress;
            Find(dashboard, "new-pairing").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Dispatcher.UIThread.RunJobs();
            check(fixture.Server.PairingAddress != before, "dashboard creates a fresh manual/QR link");
            Find(dashboard, "pause").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Dispatcher.UIThread.RunJobs();
            check(fixture.Server.Sessions.Paused && Equals(Find(dashboard, "pause").Content, "Resume control"), "dashboard Pause button updates host and accessible Resume label");
            Find(dashboard, "pause").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Dispatcher.UIThread.RunJobs();
            check(!fixture.Server.Sessions.Paused, "dashboard Resume button resumes host");
            Find(dashboard, "release").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(input.Events.Last() == "release", "dashboard Release button cleans input");
            var request = fixture.Server.Pairing.Request(fixture.Server.Pairing.CurrentChallenge!.Value, "UI revoke fixture"); fixture.Server.Pairing.Decide(request.Ticket, true);
            var device = fixture.Server.Devices.List().Single(); Dispatcher.UIThread.RunJobs();
            Find(dashboard, "revoke-" + device.Id).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Dispatcher.UIThread.RunJobs();
            check(fixture.Server.Devices.List().Length == 0, "dashboard Revoke button removes device");
            dashboard.GetVisualDescendants().OfType<Expander>().Single(e => Equals(e.Header, "Preferences")).IsExpanded = true;
            Dispatcher.UIThread.RunJobs(); dashboard.UpdateLayout();
            Find(dashboard, "save-preferences").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(File.Exists(Path.Combine(directory, "certificates", "pointer-settings.json")), "dashboard Save preferences uses host persistence");
            bool requestedExit = false; dashboard.ExitRequested += () => requestedExit = true;
            Find(dashboard, "exit").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); check(requestedExit, "dashboard Exit button requests graceful shutdown"); dashboard.Close();
        }
        finally
        {
            var cleanup = fixture.DisposeAsync().AsTask();
            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (!cleanup.IsCompleted && DateTime.UtcNow < deadline) { Dispatcher.UIThread.RunJobs(); Thread.Sleep(5); }
            check(cleanup.IsCompletedSuccessfully, "dashboard graceful host cleanup completes while UI pumps");
            cleanup.GetAwaiter().GetResult(); Directory.Delete(directory, true);
        }
    }
    public void Dispose() { controller.DisposeAsync().AsTask().GetAwaiter().GetResult(); window?.Close(); }
}
// Only this test executable has a graphical recording fixture entry point.
public sealed class FixtureApp : DesktopApp
{
    protected override DesktopController CreateController() => FixtureRuntime.Create();
    protected override string WindowTitle => "PC Remote · LOOPBACK RECORDING FIXTURE";
    protected override void ShutdownCleanup() => FixtureRuntime.Cleanup();
}
internal static class FixtureRuntime
{
    private static readonly string directory = Path.Combine(Path.GetTempPath(), "PcDesktopUi-" + Guid.NewGuid().ToString("N"));
    internal static DesktopController Create() => new("macos", new TrustedNetworks(() => []), network => PortableFixture.Create(directory, new RecordingMouse(), network: network, capabilities: PlatformCapabilities.Unavailable("macos")), observeNetworks: false);
    internal static void Cleanup() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
}

// Starts the real platform backend and normal setup-required shell, never a recording host.
public sealed class NativeStartupCheckApp : DesktopApp
{
    public override void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            var desktop = (IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
            var window = (MainWindow)desktop.MainWindow!;
            Program.Check(window.IsVisible && window.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text?.Contains("No HTTPS listener") == true), "real platform shell startup with unavailable adapters");
            window.RequestExit();
        };
        timer.Start();
    }
}
