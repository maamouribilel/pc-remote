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
        check(window.IsVisible && window.Title == "PC Remote", "headless shell opens with the product identity");
        check(!Find(window, "new-pairing").IsEnabled && !Find(window, "release").IsEnabled && !Find(window, "pause").IsEnabled, "setup-required shell disables unavailable host actions");
        var featurePreferences = window.GetVisualDescendants().OfType<Expander>().Single(e => Equals(e.Header, "Preferences"));
        featurePreferences.IsExpanded = true; Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
        T FindControl<T>(Window owner, string id) where T : Control => owner.GetVisualDescendants().OfType<T>().Single(c => AutomationProperties.GetAutomationId(c) == id);
        check(!Find(window, "add-app").IsEnabled && !FindControl<Slider>(window, "system-volume").IsEnabled && !Find(window, "save-startup").IsEnabled && new[] { "sleep", "restart", "shutdown" }.All(a => !Find(window, "power-" + a).IsEnabled), "uncomposed shell disables feature and login integrations");
        featurePreferences.IsExpanded = false; Dispatcher.UIThread.RunJobs();
        check(!Find(window, "approve-network").IsEnabled, "network approval requires an explicitly selected observable candidate");
        check(!window.GetVisualDescendants().OfType<Button>().Any(b => AutomationProperties.GetAutomationId(b) == "authorize-keychain"), "Keychain consent is offered only by a real Mac storage environment");
        check(window.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text?.Contains("Keychain") == true), "shell explains secure storage/native setup requirement");
        Expander Section(Window owner, string header) => owner.GetVisualDescendants().OfType<Expander>().Single(e => Equals(e.Header, header));
        check(!Section(window, "Permissions & platform setup").IsExpanded, "permissions section is not opened just because no host is running yet");
        // The real Mac composition adds Keychain consent and permission onboarding; build it with a platform environment.
        var environment = new DesktopChecks.FakeEnvironment();
        var macController = new DesktopController("macos", new TrustedNetworks(() => []), observeNetworks: false, environment: environment);
        var macWindow = new MainWindow(macController); macWindow.Show(); Dispatcher.UIThread.RunJobs(); macWindow.UpdateLayout();
        check(!Find(macWindow, "banner-permission").IsVisible && !Section(macWindow, "Permissions & platform setup").IsExpanded, "banner action hidden and permissions section closed while no permission problem is known");
        environment.State = new(true, false, true, "Control is paused: Secure Input (fixture).") { PermissionReady = true, CapabilitiesChanged = false, NetworkChanged = false };
        macController.RefreshAsync().GetAwaiter().GetResult(); Dispatcher.UIThread.RunJobs(); macWindow.UpdateLayout();
        check(!Find(macWindow, "banner-permission").IsVisible && !Section(macWindow, "Permissions & platform setup").IsExpanded && macWindow.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Control is paused: Secure Input (fixture)."),
            "a Secure Input/login-screen pause shows its own message without the permission action or opening the permissions section");
        environment.State = new(true, false, true, "Accessibility is required for pointer and keyboard control.");
        macController.RefreshAsync().GetAwaiter().GetResult(); Dispatcher.UIThread.RunJobs(); macWindow.UpdateLayout();
        check(Find(macWindow, "banner-permission").IsVisible && macWindow.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Accessibility is required for pointer and keyboard control."), "missing input permission shows the readiness message with its permission action");
        check(Section(macWindow, "Permissions & platform setup").IsExpanded && Find(macWindow, "authorize-keychain") != null && Find(macWindow, "accessibility") != null, "a real missing permission opens the permissions section with Keychain consent and permission onboarding");
        macWindow.Close(); macWindow.Close(); macController.DisposeAsync().AsTask().GetAwaiter().GetResult();
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
        // One observable but unapproved interface (documentation address): the host stays loopback-only.
        var lanCandidate = new NetworkCandidate("fixture", "en-test", "fixture-network", [System.Net.IPAddress.Parse("192.0.2.10")]);
        var fixture = new DesktopController("macos", new TrustedNetworks(() => [lanCandidate]), network => PortableFixture.Create(directory, input, network: network, capabilities: PlatformCapabilities.Unavailable("macos")), observeNetworks: false);
        try
        {
            check(fixture.Message == "Starting secure setup…", "a composed host shows a starting message, not the no-host setup text, before its first refresh");
            var early = new MainWindow(fixture); early.Show(); Dispatcher.UIThread.RunJobs();
            check(early.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "No paired devices."), "device list shows the unavailable state before the host starts");
            fixture.RefreshAsync().GetAwaiter().GetResult();
            check(fixture.Server != null, "fixture dashboard host starts: " + fixture.Status);
            Dispatcher.UIThread.RunJobs(); early.UpdateLayout();
            check(early.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "No paired devices yet. Create a pairing link to pair your phone."), "device list empty state updates once the host is ready");
            early.Close(); early.Close();
            var dashboard = new MainWindow(fixture); dashboard.Show(); Dispatcher.UIThread.RunJobs(); dashboard.UpdateLayout();
            check(dashboard.GetVisualDescendants().OfType<Image>().Any(i => AutomationProperties.GetAutomationId(i) == "pairing-qr" && i.Source != null && i.IsVisible), "fixture dashboard renders actual short-lived pairing QR (active link=" + (fixture.Server!.PairingAddress != null) + ")");
            check(dashboard.GetVisualDescendants().OfType<TextBlock>().Any(t => AutomationProperties.GetAutomationId(t) == "address-scope" && t.Text!.Contains("Only this computer") && t.Text.Contains("Choose and approve en-test") && t.Text.Contains("https://192.0.2.10:8443")), "loopback-only address previews the phone address of the only interface before approval");
            check(fixture.Server!.Network.PrivateAddresses.Length == 0 && fixture.Server.ConnectionAddress == "https://localhost:8443", "the preview grants nothing: the host stays loopback-only until explicit approval");
            var before = fixture.Server!.PairingAddress;
            Find(dashboard, "new-pairing").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Dispatcher.UIThread.RunJobs();
            check(fixture.Server.PairingAddress != before, "dashboard creates a fresh manual/QR link");
            Find(dashboard, "pause").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Dispatcher.UIThread.RunJobs();
            check(fixture.Server.Sessions.Paused && Equals(Find(dashboard, "pause").Content, "Resume control"), "dashboard Pause button updates host and accessible Resume label");
            Find(dashboard, "pause").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Dispatcher.UIThread.RunJobs();
            check(!fixture.Server.Sessions.Paused, "dashboard Resume button resumes host");
            var addressBox = dashboard.GetVisualDescendants().OfType<TextBox>().Single(t => AutomationProperties.GetAutomationId(t) == "address");
            // Any controller change re-renders the dashboard; it must not reset the person's selection.
            addressBox.SelectAll(); var selection = addressBox.SelectionEnd; fixture.NewPairing(); Dispatcher.UIThread.RunJobs(); dashboard.UpdateLayout();
            check(addressBox.SelectionEnd == selection && selection > 0 && Find(dashboard, "copy-address") != null, "address refresh keeps a selection and offers Copy");
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
    protected override DesktopController CreateController() => new(OperatingSystem.IsMacOS() ? "macos" : "linux", new TrustedNetworks());
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
            if (OperatingSystem.IsMacOS())
            {
                // Inside a running NSApplication, macOS Text Input Source APIs trap off the main
                // queue and never return; host setup and phone shortcuts query them from workers.
                var onMain = PcController.MacOS.MacInput.ShortcutsAvailable;
                var worker = Task.Run(() => PcController.MacOS.MacInput.ShortcutsAvailable);
                Program.Check(worker.Wait(TimeSpan.FromSeconds(5)) && worker.Result == onMain, "native app: keyboard layout lookup from a worker thread returns the main-thread result without trapping");
            }
            window.RequestExit();
        };
        timer.Start();
    }
}
