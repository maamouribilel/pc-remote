using System.Runtime.InteropServices;
using Avalonia;
using PcController.MacOS;
using PcController.Linux;
using PcController.Core;
using PcController.Host;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Styling;
using Avalonia.Platform;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Threading;

namespace PcController.Desktop;
public class DesktopApp : Application
{
    private MainWindow? shellWindow;
    public DesktopApp()
    {
        Name = "PC Remote";
        // macOS reads the application menu during platform initialization; set it
        // before its exporter is created, rather than replacing it at runtime.
        var menu = new NativeMenu();
        var open = new NativeMenuItem("Open PC Remote"); open.Click += (_, _) => shellWindow?.Open(); menu.Items.Add(open);
        var preferences = new NativeMenuItem("Preferences…") { Gesture = new KeyGesture(Key.OemComma, KeyModifiers.Meta) };
        preferences.Click += (_, _) => shellWindow?.ShowPreferences(); menu.Items.Add(preferences);
        NativeMenu.SetMenu(this, menu);
    }
    public override void Initialize() => InitializeTheme(this);
    public static void InitializeTheme(Application application)
    {
        application.Name = "PC Remote";
        application.RequestedThemeVariant = ThemeVariant.Dark;
        application.Styles.Add(new FluentTheme());
        application.Styles.Add(new StyleInclude(new Uri("avares://PcController.Desktop/")) { Source = new Uri("avares://PcController.Desktop/Theme.axaml") });
    }
    protected virtual DesktopController CreateController()
    {
        if (OperatingSystem.IsLinux())
        {
            try
            {
                var linux = new LinuxHostFactory();
                return new("linux", new TrustedNetworks(), linux.Create, environment: new LinuxDesktopEnvironment(linux), startup: new LinuxDesktopStartup());
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or DllNotFoundException or InvalidOperationException)
            { return new("linux", new TrustedNetworks(), setupMessage: "Linux setup unavailable: " + e.Message); }
        }
        try
        {
            var factory = new MacHostFactory();
            return new("macos", new TrustedNetworks(), factory.Create, environment: new MacDesktopEnvironment(factory), startup: new MacDesktopStartup());
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or DllNotFoundException or InvalidOperationException)
        { return new("macos", new TrustedNetworks(), setupMessage: "macOS setup unavailable: " + e.Message); }
    }
    // Update checks keep their own plain preferences beside the platform data, so they also work
    // when secure storage is locked or host setup failed. Test shells return null (no network).
    protected virtual UpdateService? CreateUpdates()
    {
        try { return UpdateService.Create(OperatingSystem.IsMacOS() ? MacHostFactory.DefaultDirectory : LinuxHostFactory.DefaultDirectory); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or System.Security.Cryptography.CryptographicException) { return null; }
    }
    protected virtual string WindowTitle => $"PC Remote {typeof(DesktopApp).Assembly.GetName().Version?.ToString(3)} · experimental preview";
    protected virtual void ShutdownCleanup() { }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var controller = CreateController();
            var signals = new List<PosixSignalRegistration>();
            var updates = CreateUpdates();
            var window = new MainWindow(controller, updates) { Title = WindowTitle };
            shellWindow = window; desktop.MainWindow = window;
            // kill, Ctrl+C and terminal hangup request the same graceful Exit on macOS and Linux
            // (macOS previously ignored SIGTERM). Failed input cleanup keeps the window open.
            foreach (var signal in new[] { PosixSignal.SIGTERM, PosixSignal.SIGINT, PosixSignal.SIGHUP })
                signals.Add(PosixSignalRegistration.Create(signal, context => { context.Cancel = true; Dispatcher.UIThread.Post(window.RequestExit); }));
            window.ExitRequested += async () =>
            {
                try { await controller.DisposeAsync(); }
                catch (InputFailureException) { window.CancelExit(); return; }
                // Input was released before any later disposal fault; exiting is still safe.
                catch (Exception) { }
                desktop.Shutdown();
            };
            // macOS asks synchronously for logout, restart, shutdown and Quit. Cancelling makes
            // macOS report that PC Remote stopped the logout, so clean up now and cancel only
            // when input cleanup fails. Marking the window as exiting first lets its Closing pass.
            desktop.ShutdownRequested += (_, e) =>
            {
                window.PrepareExit();
                try { Task.Run(async () => await controller.DisposeAsync()).GetAwaiter().GetResult(); }
                catch (InputFailureException) { e.Cancel = true; window.CancelExit(); }
                catch (Exception) { }
            };
            // The window always remains reachable. Hide-on-close is opt-in only after
            // a person verifies the icon; Linux can expose a tray API without a visible host.
            var menu = new NativeMenu();
            NativeMenuItem Item(string title, Action action) { var item = new NativeMenuItem(title); item.Click += (_, _) => action(); menu.Items.Add(item); return item; }
            Item("Open PC Remote", window.Open);
            var updateItem = Item("Update available…", window.Open); updateItem.IsVisible = false;
            var pauseItem = Item("Pause / Resume control", controller.PauseResume);
            var releaseItem = Item("Release input", controller.ReleaseInput);
            void UpdateMenu() => Dispatcher.UIThread.Post(() => { pauseItem.IsEnabled = releaseItem.IsEnabled = controller.Server != null; });
            pauseItem.IsEnabled = releaseItem.IsEnabled = controller.Server != null;
            controller.Changed += UpdateMenu;
            void UpdateNotice() => Dispatcher.UIThread.Post(() =>
            {
                var notice = updates?.Notice;
                updateItem.IsVisible = notice != null;
                if (notice != null) updateItem.Header = (notice.Security ? "Security update: " : "") + $"PC Remote {notice.Version} available…";
            });
            if (updates != null) { updates.Changed += UpdateNotice; UpdateNotice(); updates.Start(); }
            Item("Exit PC Remote", window.RequestExit);
            var tray = new TrayIcon { IsVisible = true, ToolTipText = "PC Remote", Menu = menu,
                Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://PcController.Desktop/Assets/tray.ico"))) };
            tray.Clicked += (_, _) => window.Open();
            TrayIcon.SetIcons(this, new TrayIcons { tray });
            desktop.Exit += (_, _) => { foreach (var signal in signals) signal.Dispose(); controller.Changed -= UpdateMenu; tray.Dispose();
                if (updates != null) { updates.Changed -= UpdateNotice; updates.Dispose(); }
                // The window is gone; a cleanup fault here cannot be retried and must not throw from the lifetime event.
                try { Task.Run(async () => await controller.DisposeAsync()).GetAwaiter().GetResult(); } catch (Exception) { }
                ShutdownCleanup(); };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
