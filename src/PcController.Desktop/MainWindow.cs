using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PcController.Core;
using PcController.Host;
using QRCoder;
using Path = Avalonia.Controls.Shapes.Path;

namespace PcController.Desktop;

// Layout mirrors the Windows companion: header with status and Settings, readiness and update
// banners, a two-column dashboard (pairing QR/address | control, trusted network, paired devices),
// then expandable setup and preferences. Automation IDs are stable; UI checks and assistive
// technology rely on them.
public sealed class MainWindow : Window
{
    private static readonly string[] Profiles = ["General", "YouTube", "VLC", "Stremio"];
    private readonly DesktopController controller;
    private readonly UpdateService? updates;
    private readonly bool mac;
    private readonly TextBlock status = Text("Setup required", "caption"), readiness = Text(""), networkNote = Text("", "caption"), pairingNote = Text("", "caption");
    private readonly TextBlock ownerStatus = Text("No active controller", "secondary"), networkState = Text("", "secondary"), footer = Text("", "caption");
    private readonly Ellipse statusDot = Dot(), ownerDot = Dot(), networkDot = Dot(), readinessDot = Dot();
    private readonly Border readinessBanner = new() { Classes = { "notice" } };
    // Update banner: the one-time automatic-check question, then signed release notices.
    private readonly Border updateBanner = new() { Classes = { "notice" }, IsVisible = false };
    private readonly TextBlock updateTitle = Text("", "subheading"), updateText = Text("", "secondary"), updateStatus = Text("", "caption");
    private readonly StackPanel consentButtons = new() { Orientation = Orientation.Horizontal, Spacing = 10 }, noticeButtons = new() { Orientation = Orientation.Horizontal, Spacing = 10 };
    private readonly CheckBox updateAutomatic = new() { Content = "Check GitHub for new versions automatically (once a day)" };
    private Button? updateCheckNow;
    private bool renderingUpdates;
    private readonly TextBox address = new() { IsReadOnly = true }, pairing = new() { IsReadOnly = true, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock addressScope = Text("", "caption");
    private readonly Image qr = new() { Stretch = Stretch.Uniform };
    private readonly TextBlock qrPlaceholder = new() { Foreground = new SolidColorBrush(Color.Parse("#536171")), TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, FontSize = 14 };
    private readonly ComboBox networks = new() { HorizontalAlignment = HorizontalAlignment.Stretch, PlaceholderText = "Choose your Wi-Fi or Ethernet interface" };
    private readonly StackPanel launcher = new() { Spacing = 8 };
    private readonly Slider volume = new() { Minimum = 0, Maximum = 100, TickFrequency = 1, IsSnapToTickEnabled = true, SmallChange = 1, LargeChange = 10 };
    private readonly TextBlock volumeValue = Text("—", "accent"), audioState = Text("", "caption"), powerState = Text("", "caption");
    private readonly CheckBox startup = new() { Content = "Start PC Remote at sign-in (starts paused; LAN approval stays session-only)" };
    private readonly Button addApp, cancelPower, startupSave, settings, blockNetwork;
    private readonly Button? authorizeStorage, permissionBanner;
    private readonly Dictionary<string, Button> powerButtons = [];
    private readonly StackPanel devices = new() { Spacing = 10 };
    private readonly Button newPair, pause, release, save, export, approveNetwork;
    private readonly TextBox fingerprint = new() { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, Classes = { "mono" } };
    private readonly Slider sensitivity = new() { Minimum = .25, Maximum = 4, TickFrequency = .25, IsSnapToTickEnabled = true, SmallChange = .25, LargeChange = .5, Value = 1 };
    private readonly TextBlock sensitivityValue = Text("1.00×", "accent");
    private readonly CheckBox natural = new() { Content = "Natural scrolling (content follows your fingers)" };
    private readonly ComboBox profile = new() { ItemsSource = Profiles, SelectedIndex = 0, MinWidth = 220 };
    private readonly CheckBox hide;
    private readonly DispatcherTimer timer;
    private readonly Expander preferencesExpander, certificateExpander, permissionsExpander;
    private readonly Grid dashboard, preferenceGrid;
    private readonly Control pairingCard, controlCard, networkCard, devicesCard;
    private readonly Control[] preferenceSections;
    private readonly ScrollViewer scroll;
    private string? renderedPair;
    private Bitmap? qrBitmap;
    private bool exiting, refreshing, updatingVolume, volumeEdited, narrow, certificateShown, permissionsShown;
    private VolumeState lastVolume = new(false);
    private PcController.Host.CompanionServer? renderedServer;
    public event Action? ExitRequested;
    public MainWindow(DesktopController controller, UpdateService? updates = null)
    {
        this.controller = controller; this.updates = updates; mac = controller.Platform == "macos";
        Title = "PC Remote"; Width = 1020; Height = 860; MinWidth = 600; MinHeight = 520;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://PcController.Desktop/Assets/tray.ico")));
        hide = new() { Content = mac ? "Keep PC Remote running in the menu bar when this window closes" : "Keep PC Remote running when this window closes (it stays in the taskbar)" };

        // Header: brand, live status and Settings, like the Windows companion.
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,*,Auto"), Margin = new Thickness(0, 0, 0, 18) };
        header.Children.Add(new Image { Source = new Bitmap(AssetLoader.Open(new Uri("avares://PcController.Desktop/Assets/logo.png"))), Width = 44, Height = 44, Margin = new Thickness(0, 0, 16, 0) });
        var brand = new TextBlock { Text = "PC Remote", FontSize = 28, FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(brand, 1); header.Children.Add(brand);
        statusDot.Margin = new Thickness(22, 0, 8, 0); Grid.SetColumn(statusDot, 2); header.Children.Add(statusDot);
        status.VerticalAlignment = VerticalAlignment.Center; status.TextWrapping = TextWrapping.NoWrap; status.TextTrimming = TextTrimming.CharacterEllipsis; status.Margin = new Thickness(0, 0, 12, 0);
        SetAccessibleName(status, "Connection status", "status"); Grid.SetColumn(status, 3); header.Children.Add(status);
        settings = IconButton(Icons.Gear, "Settings", TogglePreferences, "settings"); Grid.SetColumn(settings, 4); header.Children.Add(settings);

        // Readiness banner: platform permission/storage state and transient results.
        var bannerRow = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        readinessDot.Margin = new Thickness(0, 5, 12, 0); readinessDot.VerticalAlignment = VerticalAlignment.Top; bannerRow.Children.Add(readinessDot);
        Grid.SetColumn(readiness, 1); readiness.VerticalAlignment = VerticalAlignment.Center; bannerRow.Children.Add(readiness);
        SetAccessibleName(readiness, "Readiness", "readiness");
        if (controller.HasPermissionOnboarding)
        {
            permissionBanner = Button(mac ? "Open Accessibility settings" : "Request Remote Desktop consent", controller.OpenPermissionSettings, "banner-permission");
            permissionBanner.Classes.Add("compact"); permissionBanner.Margin = new Thickness(16, 0, 0, 0); permissionBanner.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(permissionBanner, 2); bannerRow.Children.Add(permissionBanner);
        }
        readinessBanner.Child = bannerRow; readinessBanner.Margin = new Thickness(0, 0, 0, 16);

        // Update banner (shown only with an update service): question first, notices afterwards.
        // Text above the actions keeps the banner readable at the 600-pixel minimum width.
        var updateRow = new StackPanel { Spacing = 4 };
        updateRow.Children.Add(updateTitle); updateRow.Children.Add(updateText);
        SetAccessibleName(updateTitle, "Update", "update-title"); SetAccessibleName(updateText, "Update details", "update-text");
        var on = Button("Check automatically", () => updates?.SetAutomatic(true), "update-consent-on"); on.Classes.Add("primary");
        consentButtons.Children.Add(Button("Don't check", () => updates?.SetAutomatic(false), "update-consent-off")); consentButtons.Children.Add(on);
        var view = Button("View release", () => _ = OpenRelease(), "update-view"); view.Classes.Add("primary");
        noticeButtons.Children.Add(Button("Later", () => updates?.Later(), "update-later"));
        noticeButtons.Children.Add(Button("Skip this version", () => updates?.Skip(), "update-skip")); noticeButtons.Children.Add(view);
        var updateActions = new Panel { HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0), Children = { consentButtons, noticeButtons } };
        updateRow.Children.Add(updateActions);
        updateBanner.Child = updateRow; AutomationProperties.SetAutomationId(updateBanner, "update-banner");

        // Pair your phone.
        var pair = new StackPanel();
        pair.Children.Add(Text("Pair your phone", "title"));
        pair.Children.Add(Spaced(Text("Scan with your phone's camera, open the link in its browser, then approve the request on this computer.", "secondary"), 6, 0));
        var qrWell = new Border { Width = 230, Height = 230, Background = Brushes.White, CornerRadius = new CornerRadius(14), Padding = new Thickness(12), Margin = new Thickness(0, 22, 0, 14), HorizontalAlignment = HorizontalAlignment.Center };
        RenderOptions.SetBitmapInterpolationMode(qr, BitmapInterpolationMode.None);
        AutomationProperties.SetName(qr, "Short-lived pairing QR code"); AutomationProperties.SetAutomationId(qr, "pairing-qr");
        qrWell.Child = new Grid { Children = { qrPlaceholder, qr } };
        pair.Children.Add(qrWell);
        pairingNote.HorizontalAlignment = HorizontalAlignment.Center; pairingNote.TextAlignment = TextAlignment.Center; pairingNote.MinHeight = 20;
        pair.Children.Add(Spaced(pairingNote, 0, 14));
        newPair = Button("Create new pairing link", controller.NewPairing, "new-pairing"); newPair.Classes.Add("primary"); newPair.HorizontalAlignment = HorizontalAlignment.Stretch; newPair.MinHeight = 50;
        pair.Children.Add(newPair);
        pair.Children.Add(Spaced(Text("Connection address", "label"), 22, 8));
        SetAccessibleName(address, "Connection address", "address");
        pair.Children.Add(WithCopy(address, () => controller.Server?.ConnectionAddress, "copy-address", "Copy connection address"));
        SetAccessibleName(addressScope, "Connection scope", "address-scope");
        pair.Children.Add(Spaced(addressScope, 8, 0));
        pairingCard = Card(pair);

        // Control.
        var control = new StackPanel();
        control.Children.Add(Text("Control", "title"));
        control.Children.Add(Spaced(StatusRow(ownerDot, ownerStatus), 8, 18));
        SetAccessibleName(ownerStatus, "Controller status", "owner-status");
        pause = Button("Pause control", controller.PauseResume, "pause"); pause.HorizontalAlignment = HorizontalAlignment.Stretch; pause.MinHeight = 50;
        release = Button("Release input", controller.ReleaseInput, "release"); release.HorizontalAlignment = HorizontalAlignment.Stretch; release.MinHeight = 50;
        var controlButtons = new Grid { ColumnDefinitions = new ColumnDefinitions("3*,2*"), ColumnSpacing = 10 };
        controlButtons.Children.Add(pause); Grid.SetColumn(release, 1); controlButtons.Children.Add(release);
        control.Children.Add(controlButtons);
        controlCard = Card(control);

        // Trusted network: macOS/Linux require an explicit, session-only LAN approval.
        var network = new StackPanel();
        network.Children.Add(Text("Trusted network", "title"));
        network.Children.Add(Spaced(StatusRow(networkDot, networkState), 8, 6));
        SetAccessibleName(networkState, "Trusted network state", "network-state");
        network.Children.Add(Spaced(Text("Approve only a network you trust. Approval lasts for this app session; network changes, sleep or switching users require approval again.", "caption"), 0, 14));
        SetAccessibleName(networks, "Trusted network interface", "networks");
        network.Children.Add(networks);
        approveNetwork = Button("Approve network…", () => _ = ApproveNetwork(), "approve-network"); approveNetwork.HorizontalAlignment = HorizontalAlignment.Stretch;
        blockNetwork = Button("Block LAN", () => _ = controller.BlockLanAsync(), "block-network"); blockNetwork.HorizontalAlignment = HorizontalAlignment.Stretch;
        // Re-render (deferred) so the address previews the selected interface before approval.
        networks.SelectionChanged += (_, _) => { approveNetwork.IsEnabled = networks.SelectedItem is NetworkCandidate; Dispatcher.UIThread.Post(Render); };
        var networkButtons = new Grid { ColumnDefinitions = new ColumnDefinitions("3*,2*"), ColumnSpacing = 10, Margin = new Thickness(0, 10, 0, 0) };
        networkButtons.Children.Add(approveNetwork); Grid.SetColumn(blockNetwork, 1); networkButtons.Children.Add(blockNetwork);
        network.Children.Add(networkButtons);
        network.Children.Add(Spaced(networkNote, 10, 0));
        networkCard = Card(network);

        // Paired devices.
        var paired = new StackPanel();
        paired.Children.Add(Text("Paired devices", "title"));
        paired.Children.Add(Spaced(devices, 14, 12));
        paired.Children.Add(Text("Approve only your own browser. Revoke a lost device here to cancel its controller session.", "caption"));
        devicesCard = Card(paired);

        dashboard = new Grid { ColumnSpacing = 16, RowSpacing = 16 };

        // Certificate & phone setup.
        var certificate = new StackPanel { Spacing = 10 };
        certificate.Children.Add(Step("1", "Save the public trust profile, then AirDrop or email it to your phone. It is an iPhone/iPad profile; other devices import pc-remote-root.cer from the same folder. It contains no private key."));
        export = Button("Save trust profile…", () => _ = ExportProfile(), "export-profile"); export.HorizontalAlignment = HorizontalAlignment.Left;
        certificate.Children.Add(Indented(export));
        certificate.Children.Add(Step("2", "Install the profile, then enable full trust in Settings → General → About → Certificate Trust Settings. Before trusting it, confirm this SHA-256 fingerprint matches the profile."));
        SetAccessibleName(fingerprint, "Certificate SHA-256 fingerprint", "fingerprint");
        certificate.Children.Add(Indented(WithCopy(fingerprint, () => controller.Server is { } server ? Grouped(server.Certificates.Fingerprint) : null, "copy-fingerprint", "Copy certificate fingerprint")));
        certificate.Children.Add(Step("3", "Scan the pairing QR, or paste this complete link into the phone's browser. Enter a device name, request pairing, then approve it here. Each browser, including the Home Screen app, may need its own approval."));
        SetAccessibleName(pairing, "Manual pairing link", "pairing-link");
        certificate.Children.Add(Indented(WithCopy(pairing, () => controller.Server?.PairingAddress, "copy-pairing-link", "Copy pairing link")));
        certificateExpander = Section("Certificate & phone setup", certificate);

        // Permissions & platform setup.
        var setup = new StackPanel { Spacing = 12 };
        setup.Children.Add(Text(mac
            ? "Accessibility is required for pointer and keyboard control. Enable this PC Remote.app in System Settings → Privacy & Security → Accessibility, quit and reopen it, then choose Resume. After rebuilding, remove stale entries and add the same app again. Secure Input/password fields block control. Review the selected player before media control. System audio requires a writable output. Restart/shutdown are unavailable on Mac."
            : "Linux requires a local unlocked GNOME session and password-encrypted login keyring. Wayland needs explicit Remote Desktop portal consent; XWayland is not input permission. GTK Unicode text is optional and target-specific. General playback uses MPRIS; player shortcuts require focus. Audio needs PulseAudio/PipeWire-Pulse. Power is gated by logind permission.", "secondary"));
        if (controller.HasPermissionOnboarding)
        {
            var onboarding = Button(mac ? "Open Accessibility settings" : "Request Wayland Remote Desktop consent", controller.OpenPermissionSettings, "accessibility");
            onboarding.HorizontalAlignment = HorizontalAlignment.Left; setup.Children.Add(onboarding);
        }
        if (controller.HasStorageAuthorization)
        {
            authorizeStorage = Button("Allow Keychain access after update…", () => _ = AuthorizeStorage(), "authorize-keychain");
            authorizeStorage.HorizontalAlignment = HorizontalAlignment.Left; setup.Children.Add(authorizeStorage);
            setup.Children.Add(Text("An updated or rebuilt unsigned app has a new code identity. Use this only for the PC Remote.app you installed; macOS asks for your login Keychain password. Deny keeps storage locked; nothing is reset.", "caption"));
        }
        if (controller.HasLinuxTextMode)
        {
            var gtk = new CheckBox { Content = "Enable Unicode text for a verified GTK target only (Ctrl+Shift+U)", IsChecked = controller.LinuxGtkTextEnabled };
            SetAccessibleName(gtk, "GTK Unicode text mode", "linux-gtk-text");
            // A refused change (pending composition, failed cleanup) must not leave the box showing the wrong mode.
            gtk.IsCheckedChanged += (_, _) => { controller.EnableLinuxGtkText(gtk.IsChecked == true); gtk.IsChecked = controller.LinuxGtkTextEnabled; };
            setup.Children.Add(gtk);
            var recovery = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 10, LineSpacing = 10 };
            recovery.Children.Add(Button("Recover ended input session…", () => _ = RecoverEndedBackend(), "linux-backend-recovery"));
            recovery.Children.Add(Button("Recover interrupted GTK text…", () => _ = RecoverGtk(), "linux-gtk-recovery"));
            setup.Children.Add(recovery);
            setup.Children.Add(Text("Verify a disposable GTK editor first. Other apps/IMEs may execute these chords instead. Interrupted composition must be cancelled locally; then confirm recovery here before Resume or Exit.", "caption"));
        }
        permissionsExpander = Section("Permissions & platform setup", setup);

        // Preferences (header text is stable: native Settings/⌘, and UI checks open it by name).
        var pointer = new StackPanel { Spacing = 10 };
        pointer.Children.Add(Text("Pointer & media", "subheading"));
        pointer.Children.Add(LabelRow("Pointer sensitivity", sensitivityValue));
        SetAccessibleName(sensitivity, "Pointer sensitivity", "sensitivity");
        sensitivity.ValueChanged += (_, _) => sensitivityValue.Text = $"{sensitivity.Value:0.00}×";
        pointer.Children.Add(sensitivity); pointer.Children.Add(natural);
        pointer.Children.Add(Spaced(Text("Media profile · focus the matching player on this computer", "label"), 6, 0));
        SetAccessibleName(profile, "Media profile", "profile"); pointer.Children.Add(profile);
        save = Button("Save preferences", () => controller.SavePreferences(new(sensitivity.Value, natural.IsChecked == true), profile.SelectedItem as string ?? "General"), "save-preferences");
        save.HorizontalAlignment = HorizontalAlignment.Left; pointer.Children.Add(Spaced(save, 6, 0));

        var audio = new StackPanel { Spacing = 10 };
        audio.Children.Add(Text("System volume", "subheading"));
        audio.Children.Add(LabelRow(mac ? "Mac output volume" : "System output volume", volumeValue));
        SetAccessibleName(volume, "System output volume", "system-volume");
        volume.ValueChanged += (_, _) => { if (!updatingVolume) { volumeEdited = true; volumeValue.Text = $"{(int)volume.Value}%"; } };
        // Commit like the Windows slider: when the drag, click or key interaction ends, not on every step.
        volume.AddHandler(PointerReleasedEvent, (_, _) => CommitVolume(), RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        volume.AddHandler(PointerCaptureLostEvent, (_, _) => CommitVolume(), RoutingStrategies.Bubble, handledEventsToo: true);
        volume.KeyUp += (_, _) => CommitVolume(); volume.LostFocus += (_, _) => CommitVolume();
        audio.Children.Add(volume); audio.Children.Add(audioState);

        var apps = new StackPanel { Spacing = 10 };
        apps.Children.Add(Text("Phone app launcher", "subheading"));
        apps.Children.Add(Text("Only these apps can be opened from your phone. The phone receives a name and ID, never a local path.", "caption"));
        apps.Children.Add(launcher);
        addApp = Button(mac ? "Add an app…" : "Add a .desktop app…", () => _ = AddApp(), "add-app"); addApp.HorizontalAlignment = HorizontalAlignment.Left; apps.Children.Add(addApp);

        var power = new StackPanel { Spacing = 10 };
        power.Children.Add(Text("Power controls", "subheading"));
        power.Children.Add(Text(mac ? "Sleep runs after a cancellable 30-second countdown. Restart and shut down are unavailable on Mac." : "Each action runs after a cancellable 30-second countdown, and only when logind allows it.", "caption"));
        var powerRow = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 10, LineSpacing = 10 };
        foreach (var (action, label) in new[] { ("sleep", "Sleep…"), ("restart", "Restart…"), ("shutdown", "Shut down…") })
        { var button = Button(label, () => _ = ConfirmPower(action), "power-" + action); powerButtons[action] = button; powerRow.Children.Add(button); }
        power.Children.Add(powerRow);
        cancelPower = Button("Cancel power action", controller.CancelPower, "cancel-power"); cancelPower.HorizontalAlignment = HorizontalAlignment.Left; cancelPower.Classes.Add("danger");
        power.Children.Add(cancelPower); power.Children.Add(powerState);

        var app = new StackPanel { Spacing = 10 };
        app.Children.Add(Text("Startup & window", "subheading"));
        SetAccessibleName(hide, "Keep running when the window closes", "keep-running");
        hide.IsCheckedChanged += (_, _) => RenderFooter();
        app.Children.Add(hide);
        startup.IsChecked = controller.StartupEnabled; startup.IsEnabled = controller.HasStartup;
        SetAccessibleName(startup, "Start at sign-in", "startup"); app.Children.Add(startup);
        startupSave = Button("Save sign-in preference", () => controller.SetStartup(startup.IsChecked == true), "save-startup");
        startupSave.IsEnabled = controller.HasStartup; startupSave.HorizontalAlignment = HorizontalAlignment.Left; app.Children.Add(startupSave);
        var quit = Button("Quit PC Remote", RequestExit, "exit"); quit.Classes.Add("danger"); quit.HorizontalAlignment = HorizontalAlignment.Left;
        app.Children.Add(Spaced(quit, 8, 0));
        app.Children.Add(Text("The menu-bar or tray icon also offers Open, Pause/Resume, Release input and Exit.", "caption"));

        var updateSection = new StackPanel { Spacing = 10 };
        updateSection.Children.Add(Text("Updates", "subheading"));
        updateSection.Children.Add(Text("Checks only ask GitHub whether a newer signed release exists; nothing is downloaded or installed. GitHub sees this computer's IP address; no device, pairing or setting information is sent.", "caption"));
        SetAccessibleName(updateAutomatic, "Check for updates automatically", "update-automatic");
        updateAutomatic.IsCheckedChanged += (_, _) => { if (!renderingUpdates && updates != null && (updateAutomatic.IsChecked == true) != (updates.Preferences.Automatic == true)) updates.SetAutomatic(updateAutomatic.IsChecked == true); };
        updateSection.Children.Add(updateAutomatic);
        updateCheckNow = Button("Check now", () => _ = updates?.CheckNowAsync(), "update-check-now"); updateCheckNow.HorizontalAlignment = HorizontalAlignment.Left;
        updateSection.Children.Add(updateCheckNow);
        SetAccessibleName(updateStatus, "Update status", "update-status"); updateSection.Children.Add(updateStatus);

        preferenceSections = updates == null ? [Group(pointer), Group(apps), Group(audio), Group(power), Group(app)]
            : [Group(pointer), Group(apps), Group(audio), Group(power), Group(updateSection), Group(app)];
        preferenceGrid = new Grid { ColumnSpacing = 16, RowSpacing = 16 };
        foreach (var section in preferenceSections) preferenceGrid.Children.Add(section);
        preferencesExpander = Section("Preferences", preferenceGrid);

        footer.HorizontalAlignment = HorizontalAlignment.Center; footer.TextAlignment = TextAlignment.Center;
        var body = new StackPanel { Spacing = 16 };
        body.Children.Add(dashboard); body.Children.Add(certificateExpander); body.Children.Add(permissionsExpander); body.Children.Add(preferencesExpander);
        var preview = Text("Experimental macOS/Linux preview · Windows remains the primary platform. Native control is reported separately from pairing and transport.", "caption");
        preview.TextAlignment = TextAlignment.Center; body.Children.Add(preview);
        scroll = new ScrollViewer { Content = body, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Padding = new Thickness(0, 0, 10, 0) };
        var root = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*,Auto"), Margin = new Thickness(24, 20, 14, 14) };
        header.Margin = new Thickness(0, 0, 10, 18); readinessBanner.Margin = new Thickness(0, 0, 10, 16); updateBanner.Margin = new Thickness(0, 0, 10, 16);
        root.Children.Add(header);
        Grid.SetRow(readinessBanner, 1); root.Children.Add(readinessBanner);
        Grid.SetRow(updateBanner, 2); root.Children.Add(updateBanner);
        Grid.SetRow(scroll, 3); root.Children.Add(scroll);
        Grid.SetRow(footer, 4); footer.Margin = new Thickness(0, 12, 10, 0); root.Children.Add(footer);
        Content = root;
        ApplyLayout(false);
        SizeChanged += (_, _) => { var next = ClientSize.Width < 880; if (next != narrow) { ApplyLayout(next); Render(); } };

        controller.Changed += Changed; controller.ApprovalRequested += Approval;
        if (updates != null) updates.Changed += Changed;
        Closing += (_, e) =>
        {
            if (exiting) return;
            e.Cancel = true;
            if (hide.IsChecked == true && TrayIcon.GetIcons(Application.Current!)?.Any(t => t.IsVisible) == true)
            {
                if (OperatingSystem.IsLinux()) WindowState = WindowState.Minimized;
                else Hide();
            }
            else RequestExit();
        };
        Closed += (_, _) => { timer?.Stop(); controller.Changed -= Changed; controller.ApprovalRequested -= Approval; if (updates != null) updates.Changed -= Changed; qrBitmap?.Dispose(); };
        timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += async (_, _) =>
        {
            Render(); if (refreshing) return; refreshing = true;
            try
            {
                await controller.RefreshAsync();
                var audioServer = controller.Server;
                try { lastVolume = audioServer?.Capabilities.Volume == true ? await Task.Run(audioServer.PcActions.GetVolume) : new(false); }
                catch (Exception e) when (e is IOException or InvalidOperationException) { lastVolume = new(false); }
                Render();
            }
            // An async timer handler must never crash the app; unknown faults pause control instead.
            catch (Exception e) { controller.ReportFailure(e); }
            finally { refreshing = false; }
        };
        timer.Start(); Opened += async (_, _) => { try { await controller.RefreshAsync(); } catch (Exception e) { controller.ReportFailure(e); } }; Render();
    }
    private void ApplyLayout(bool compact)
    {
        narrow = compact;
        // Wide: pairing on the left, control/network/devices stacked on the right. Narrow: one column.
        dashboard.Children.Clear();
        dashboard.ColumnDefinitions = new ColumnDefinitions(compact ? "*" : "1.08*,*");
        dashboard.RowDefinitions = new RowDefinitions(compact ? "Auto,Auto,Auto,Auto" : "Auto,Auto,*");
        void Place(Control card, int row, int column, int rowSpan = 1)
        { Grid.SetRow(card, row); Grid.SetColumn(card, column); Grid.SetRowSpan(card, rowSpan); dashboard.Children.Add(card); }
        if (compact) { Place(pairingCard, 0, 0); Place(controlCard, 1, 0); Place(networkCard, 2, 0); Place(devicesCard, 3, 0); }
        else { Place(pairingCard, 0, 0, 3); Place(controlCard, 0, 1); Place(networkCard, 1, 1); Place(devicesCard, 2, 1); }
        preferenceGrid.ColumnDefinitions = new ColumnDefinitions(compact ? "*" : "*,*");
        var rows = compact ? preferenceSections.Length : (preferenceSections.Length + 1) / 2;
        preferenceGrid.RowDefinitions = new RowDefinitions(string.Join(",", Enumerable.Repeat("Auto", rows)));
        for (var i = 0; i < preferenceSections.Length; i++)
        {
            var section = preferenceSections[i];
            // An odd last section spans both columns in the wide layout.
            var alone = i == preferenceSections.Length - 1 && preferenceSections.Length % 2 == 1;
            Grid.SetRow(section, compact ? i : i / 2); Grid.SetColumn(section, compact ? 0 : i % 2); Grid.SetColumnSpan(section, !compact && alone ? 2 : 1);
        }
    }
    private async Task AddApp()
    {
        var server = controller.Server; if (server == null) return;
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = mac ? "Choose a local application bundle" : "Choose an installed application entry", AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(mac ? "macOS application" : "Desktop application") { Patterns = [mac ? "*.app" : "*.desktop"], AppleUniformTypeIdentifiers = mac ? ["com.apple.application-bundle"] : null }] });
        var path = files.FirstOrDefault()?.TryGetLocalPath(); if (path == null || controller.Server != server) return;
        var name = new TextBox { Text = System.IO.Path.GetFileNameWithoutExtension(path), MaxLength = 64 };
        var media = new ComboBox { ItemsSource = Profiles, SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
        var fields = new StackPanel { Spacing = 8 };
        fields.Children.Add(Text("Display name", "label")); fields.Children.Add(name);
        fields.Children.Add(Spaced(Text("Media profile", "label"), 6, 0)); fields.Children.Add(media);
        if (await Confirm("Add launcher app", "The phone receives a configured ID and name, never this local path. Opening requests activation; focus and playback still need local review.", "Add app", "confirm-app", "cancel-app", fields)
            && controller.Server == server) controller.AddLauncher(path, name.Text ?? "", media.SelectedItem as string ?? "General");
    }
    private async Task ConfirmPower(string action)
    {
        var server = controller.Server; if (server == null) return;
        controller.ReleaseInput();
        var label = action == "shutdown" ? "Shut down" : action == "restart" ? "Restart" : "Sleep";
        if (await Confirm($"{label} this computer?", $"{label} starts after a cancellable 30-second countdown. Save your work first. OS permissions or inhibitors can refuse it, and once dispatched only the OS can cancel it.", "Start countdown", "confirm-power", "dismiss-power", danger: true)
            && controller.Server == server) controller.SchedulePower(action);
    }
    private async Task ExportProfile()
    {
        if (controller.Server is not { } server) return;
        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save public trust profile (iPhone/iPad)", SuggestedFileName = "pc-remote.mobileconfig",
                FileTypeChoices = [new FilePickerFileType("iPhone/iPad trust profile") { Patterns = ["*.mobileconfig"] }]
            });
            if (file == null || controller.Server != server) return;
            // Only the already-exported public CA profile is copied. No private keys.
            await using var source = File.OpenRead(server.Certificates.ProfilePath);
            await using var target = await file.OpenWriteAsync(); await source.CopyToAsync(target);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { readiness.Text = "Public profile could not be saved. Check the destination and retry."; }
    }
    private void Changed() => Dispatcher.UIThread.Post(Render);
    private async Task OpenRelease()
    {
        // The address comes from a signed manifest and is re-checked against this repository's release pages.
        if (updates?.Notice is not { } release || TopLevel.GetTopLevel(this)?.Launcher is not { } launcher) return;
        try { if (!await launcher.LaunchUriAsync(new Uri(release.ReleaseUrl))) Set(updateText, "Open " + release.ReleaseUrl + " in your browser."); }
        catch (Exception e) when (e is InvalidOperationException or IOException or UriFormatException) { Set(updateText, "Open " + release.ReleaseUrl + " in your browser."); }
    }
    private void RenderUpdates()
    {
        if (updates == null) return;
        renderingUpdates = true;
        try
        {
            var notice = updates.Notice;
            updateBanner.IsVisible = updates.NeedsConsent || notice != null;
            consentButtons.IsVisible = updates.NeedsConsent && notice == null; noticeButtons.IsVisible = notice != null;
            updateBanner.Classes.Set("warning", notice?.Security == true); updateBanner.Classes.Set("ready", notice is { Security: false });
            if (notice != null)
            {
                Set(updateTitle, (notice.Security ? "Security update: " : "") + $"PC Remote {notice.Version} is available");
                Set(updateText, $"{notice.Summary} You have {UpdateService.Display(updates.Current)}. The release page lists the downloads, checksums and update steps for this computer.");
            }
            else if (updates.NeedsConsent)
            {
                Set(updateTitle, "Check for PC Remote updates automatically?");
                Set(updateText, "PC Remote can ask GitHub once a day whether a newer version is published and tell you here. Nothing is downloaded or installed, and no device, pairing or setting information is sent; GitHub sees this computer's IP address. You can change this in Preferences.");
            }
            updateAutomatic.IsChecked = updates.Preferences.Automatic == true;
            if (updateCheckNow != null) updateCheckNow.IsEnabled = !updates.Checking;
            Set(updateStatus, updates.Status);
        }
        finally { renderingUpdates = false; }
    }
    private async Task RecoverEndedBackend()
    {
        if (await Confirm("Verify ended-session cleanup locally", "Use this only after the portal/display connection ended and Release input cannot reach it. Verify on this computer that every held key/button and GTK composition is cleared, using local input as needed. Confirmation retires unreachable tracking; it does not establish successful automatic cleanup. Wayland requires fresh consent; X11 requires app restart. Control remains paused.",
            "I verified all input is released locally", "confirm-backend-recovery", "cancel-backend-recovery")) controller.ConfirmLinuxEndedBackendRecovery();
    }
    private async Task AuthorizeStorage()
    {
        if (await Confirm("Allow existing Keychain storage key", "macOS will show its own Keychain dialog for this PC Remote.app and the existing local.pc-remote.storage.v1 key. Continue only if you installed this app from the official release and placed it in its stable location. Choose Always Allow to keep the current certificate root and remembered devices; Deny keeps storage locked. PC Remote never resets the key, root or devices here.",
            "Continue to macOS dialog", "confirm-keychain", "cancel-keychain")) await controller.AuthorizeStorageAsync();
    }
    private async Task RecoverGtk()
    {
        if (await Confirm("Confirm local text recovery", "Cancel Unicode composition in the original GTK target locally (Escape), restore the original backend and choose Release input. Confirm only after checking the target; control will remain paused.",
            "I cancelled composition locally", "confirm-gtk-recovery", "cancel-gtk-recovery")) controller.ConfirmLinuxTextRecovery();
    }
    private async Task ApproveNetwork()
    {
        if (networks.SelectedItem is not NetworkCandidate candidate) return;
        if (await Confirm("Approve trusted network", $"Trust {candidate} for this app session? Approve only your own trusted LAN: every device on this interface can reach the pairing service.", "Approve for this session", "confirm-network", "cancel-network"))
            await controller.ApproveNetworkAsync(candidate);
    }
    // Consistent confirmation dialog: title, explanation, optional fields, then Cancel and the action.
    private async Task<bool> Confirm(string title, string message, string confirm, string confirmId, string cancelId, Control? extra = null, bool danger = false)
    {
        var dialog = new Window { Title = title, Width = 500, SizeToContent = SizeToContent.Height, CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false };
        var content = new StackPanel { Spacing = 14, Margin = new Thickness(24) };
        content.Children.Add(Text(title, "dialog-title"));
        content.Children.Add(Text(message, "secondary"));
        if (extra != null) content.Children.Add(extra);
        var ok = Button(confirm, () => dialog.Close(true), confirmId); ok.Classes.Add(danger ? "danger-primary" : "primary");
        content.Children.Add(DialogButtons(Button("Cancel", () => dialog.Close(false), cancelId), ok));
        dialog.Content = content;
        dialog.KeyDown += (_, e) => { if (e.Key == Key.Escape) dialog.Close(false); };
        return await dialog.ShowDialog<bool>(this);
    }
    internal static Control DialogButtons(Button cancel, Button confirm)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 6, 0, 0) };
        row.Children.Add(cancel); row.Children.Add(confirm); return row;
    }
    private void Approval(ApprovalPrompt prompt) => Dispatcher.UIThread.Post(async () =>
    {
        if (prompt.Finished || exiting) { prompt.Decide(false); return; }
        Open();
        var dialog = new ApprovalWindow(prompt);
        var decision = dialog.ShowDialog<bool>(this);
        _ = prompt.Decision.ContinueWith(_ => Dispatcher.UIThread.Post(() => { if (dialog.IsVisible) dialog.Close(false); }), TaskScheduler.Default);
        try { prompt.Decide(await decision); } catch { prompt.Decide(false); }
    });
    public void Open() { if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal; Show(); Activate(); }
    public void ShowPreferences() { Open(); controller.ReleaseInput(); preferencesExpander.IsExpanded = true; Dispatcher.UIThread.Post(() => preferencesExpander.BringIntoView(), DispatcherPriority.Background); }
    private void TogglePreferences()
    {
        if (!preferencesExpander.IsExpanded) { ShowPreferences(); return; }
        preferencesExpander.IsExpanded = false; scroll.Offset = default;
    }
    // Marks the window as exiting so Closing no longer hides or cancels; the keep-running
    // choice is kept so a failed Exit (CancelExit) restores the person's preference.
    public void PrepareExit() { exiting = true; timer.Stop(); }
    public void RequestExit() { if (exiting) return; PrepareExit(); ExitRequested?.Invoke(); }
    public void CancelExit() { exiting = false; timer.Start(); Open(); Render(); }
    private void CommitVolume()
    {
        if (!volumeEdited || updatingVolume || !volume.IsEnabled) return;
        volumeEdited = false; controller.SetVolume((int)volume.Value);
    }
    private void RenderFooter() => Set(footer, hide.IsChecked == true
        ? mac ? "Closing this window keeps PC Remote in the menu bar." : "Closing this window keeps PC Remote running in the taskbar."
        : "Closing this window quits PC Remote. To keep it running, turn on “Keep running” in Preferences.");
    private void Render()
    {
        if (exiting) return;
        var server = controller.Server;
        if (server != renderedServer)
        {
            renderedServer = server;
            if (server != null) { sensitivity.Value = server.PointerSettings.Value.Sensitivity; natural.IsChecked = server.PointerSettings.Value.NaturalScroll; profile.SelectedItem = server.MediaSettings.Profile; }
        }
        var lan = server?.Network.PrivateAddresses.Length > 0;
        Set(status, server == null ? "Setup required" : lan ? "Secure LAN ready" : narrow ? "Local only" : "Local only · approve a network for your phone");
        Paint(statusDot, server == null ? "#F5C46B" : lan ? "#5EE8BE" : "#8A95A5");
        var attention = server == null || !controller.InputReady || !controller.StorageReady;
        Set(readiness, controller.Message);
        Paint(readinessDot, attention ? "#F5C46B" : "#5EE8BE");
        readinessBanner.Classes.Set("warning", attention); readinessBanner.Classes.Set("ready", !attention);
        // Onboarding only helps while the permission is missing; a Secure Input/login/inactive pause explains itself.
        if (permissionBanner != null) permissionBanner.IsVisible = !controller.PermissionReady;

        Set(fingerprint, server == null ? "Available after secure certificate setup" : Grouped(server.Certificates.Fingerprint));
        Set(address, server?.ConnectionAddress ?? "Unavailable — secure storage setup required");
        // Before approval, name the interface and the exact address the phone will use (the host
        // binds the approved interface's first address). This previews only; it grants no trust.
        var candidates = controller.Networks.Candidates;
        var suggested = networks.SelectedItem as NetworkCandidate ?? (candidates.Length == 1 ? candidates[0] : null);
        var preview = server != null && suggested?.Addresses.FirstOrDefault() is { } lanAddress ? $"https://{lanAddress}:{PcController.Host.CompanionServer.Port}" : null;
        Set(addressScope, server == null ? "" : lan
            ? "Reachable on the approved network for this session. Network changes, sleep or switching users require approval again."
            : preview != null ? $"Only this computer can connect (localhost). {(networks.SelectedItem is NetworkCandidate ? "Approve" : "Choose and approve")} {suggested!.Name} under Trusted network to use {preview} from your phone."
            : "Only this computer can connect (localhost). To use your phone, choose your Wi-Fi or Ethernet interface under Trusted network and approve it for this session.");
        var nextPair = server?.PairingAddress;
        qr.IsVisible = nextPair != null;
        qrPlaceholder.IsVisible = nextPair == null;
        Set(qrPlaceholder, server == null ? "QR code appears after secure setup completes" : "Create a pairing link\nto show your QR code");
        Set(pairing, nextPair ?? "Create a fresh link when HTTPS is ready.");
        if (nextPair != renderedPair)
        {
            renderedPair = nextPair; qr.Source = null; qrBitmap?.Dispose(); qrBitmap = null;
            if (nextPair != null) { using var data = QRCodeGenerator.GenerateQrCode(nextPair, QRCodeGenerator.ECCLevel.Q); using var png = new PngByteQRCode(data); using var stream = new MemoryStream(png.GetGraphic(6)); qrBitmap = new Bitmap(stream); qr.Source = qrBitmap; }
        }
        Set(pairingNote, server?.Pairing.CurrentChallenge is { } challenge ? $"Link expires in {Math.Max(0, (int)(challenge.Expires - DateTimeOffset.UtcNow).TotalSeconds)} seconds" : server == null ? "" : "Pairing links and pending requests expire after two minutes.");
        newPair.IsEnabled = export.IsEnabled = pause.IsEnabled = release.IsEnabled = save.IsEnabled = server != null;

        var paused = server?.Sessions.Paused == true;
        var records = server?.Devices.List() ?? [];
        var owner = server?.Sessions.OwnerDeviceId;
        var ownerName = owner == null ? null : records.FirstOrDefault(d => d.Id == owner)?.Name ?? "revoked device";
        Set(ownerStatus, server == null ? "Unavailable until secure setup completes" : server.InputProblem && paused ? "Input failed. Control is paused; resolve the cause, then Resume." : paused ? "Control paused · choose Resume when you are ready" :
            ownerName != null ? (server.Sessions.ActiveSessionId == null ? "Connecting: " : "Controlling: ") + ownerName : "No active controller");
        Paint(ownerDot, server == null || paused ? "#F5C46B" : ownerName != null ? "#5EE8BE" : "#8A95A5");
        pause.Content = paused ? "Resume control" : "Pause control";
        pause.Classes.Set("primary", paused);

        sensitivity.IsEnabled = natural.IsEnabled = server?.Capabilities.Pointer == true;
        profile.IsEnabled = server?.Capabilities.MediaActions.Count > 0;
        addApp.IsEnabled = server?.Capabilities.Launcher == true;
        var audio = server?.Capabilities.Volume == true ? lastVolume : new VolumeState(false);
        volume.IsEnabled = audio.Available && !paused;
        if (!volume.IsPointerOver && !volume.IsFocused && !volumeEdited)
        { updatingVolume = true; volume.Value = audio.Level; Set(volumeValue, audio.Available ? $"{audio.Level}%" : "—"); updatingVolume = false; }
        Set(audioState, !audio.Available ? "Writable audio output unavailable" : paused ? "Resume control to change the output volume." : audio.Muted ? "Output is muted. Moving the slider unmutes it." : "System output volume, separate from the player’s own volume.");
        foreach (var (action, button) in powerButtons) button.IsEnabled = server?.Capabilities.PowerActions.Contains(action) == true && !paused;
        cancelPower.IsEnabled = server?.Power.Pending != null;
        Set(powerState, server?.Power.Pending is { } pending ? $"{pending.Action}: {Math.Max(0, (int)Math.Ceiling((pending.ExecuteAt - DateTimeOffset.UtcNow).TotalSeconds))} seconds remaining" : server?.PowerMessage ?? "");
        var apps = server?.Launcher.List() ?? [];
        var appSignature = string.Join("|", apps.Select(a => a.Id + a.Name + a.Profile));
        if (!Equals(launcher.Tag, appSignature))
        {
            launcher.Tag = appSignature; launcher.Children.Clear();
            foreach (var entry in apps) launcher.Children.Add(Row(Icons.App, entry.Name, entry.Profile + " profile", Button("Remove", () => controller.RemoveLauncher(entry.Id), "remove-app-" + entry.Id, "Remove " + entry.Name), false));
            if (apps.Length == 0) launcher.Children.Add(Text("No apps yet. Add one to open it from your phone.", "caption"));
        }

        var selected = networks.SelectedItem as NetworkCandidate;
        if (!((networks.ItemsSource as NetworkCandidate[]) ?? []).Select(c => c.Identity).SequenceEqual(candidates.Select(c => c.Identity)))
        { networks.ItemsSource = candidates; networks.SelectedItem = candidates.FirstOrDefault(c => c.Identity == selected?.Identity); }
        approveNetwork.IsEnabled = networks.SelectedItem is NetworkCandidate;
        approveNetwork.Classes.Set("primary", server != null && !lan);
        blockNetwork.IsEnabled = lan;
        Set(networkState, server == null ? "No listener running" : lan ? "Approved for this session" : "LAN blocked · this computer only");
        Paint(networkDot, lan ? "#5EE8BE" : "#F5C46B");
        Set(networkNote, server == null ? "No listener is running. Network approval alone cannot enable missing secure storage/native adapters."
            : lan ? "Phones on this network can open " + server.ConnectionAddress + "." : candidates.Length == 0 ? "No suitable Wi-Fi or Ethernet interface with an observable network identity was found."
            : preview != null ? $"Approving {suggested!.Name} lets phones on that network open {preview}." : server.Network.Notice);
        if (authorizeStorage != null) authorizeStorage.IsEnabled = !controller.StorageReady;
        // Rebuild only when device content changes to preserve keyboard focus. Host availability is
        // part of the signature so the empty-state text updates when setup completes.
        var signature = (server == null ? "unavailable|" : "ready|") + string.Join("|", records.Select(d => d.Id + d.Name + d.Expires)) + "|" + owner;
        if (!Equals(devices.Tag, signature))
        {
            devices.Tag = signature; devices.Children.Clear();
            foreach (var device in records)
            {
                var controlling = owner == device.Id;
                devices.Children.Add(Row(Icons.Phone, device.Name, (controlling ? "Controlling · " : "") + $"Remembered until {device.Expires.LocalDateTime:d}",
                    Button("Revoke", () => controller.Revoke(device.Id), "revoke-" + device.Id, "Revoke " + device.Name), controlling));
            }
            if (records.Length == 0) devices.Children.Add(Text(server == null ? "No paired devices." : "No paired devices yet. Create a pairing link to pair your phone.", "secondary"));
        }
        // Expand guidance once, when it is first needed, without fighting a person who collapses it.
        if (!certificateShown && server != null && records.Length == 0) { certificateShown = true; certificateExpander.IsExpanded = true; }
        // Only a real permission/storage problem opens the section; a host that is still starting or a transient pause does not.
        if (!permissionsShown && (!controller.PermissionReady || !controller.StorageReady)) { permissionsShown = true; permissionsExpander.IsExpanded = true; }
        RenderUpdates();
        RenderFooter();
    }
    private static string Grouped(string hex) => string.Join(" ", Enumerable.Range(0, hex.Length / 2).Select(i => hex.Substring(i * 2, 2)));
    private static void Set(TextBlock block, string value) { if (block.Text != value) block.Text = value; }
    // Assigning only on change keeps a person's text selection intact across the 1-second refresh.
    private static void Set(TextBox box, string value) { if (box.Text != value) box.Text = value; }
    private static void Paint(Shape shape, string color) { var next = Color.Parse(color); if ((shape.Fill as ISolidColorBrush)?.Color != next) shape.Fill = new SolidColorBrush(next); }
    private static Ellipse Dot() => new() { Width = 10, Height = 10, Fill = new SolidColorBrush(Color.Parse("#8A95A5")), VerticalAlignment = VerticalAlignment.Center };
    private static Border Card(Control content) => new() { Child = content, Classes = { "card" } };
    private static Border Group(Control content) => new() { Child = content, Classes = { "group" } };
    private static T Spaced<T>(T control, double top, double bottom) where T : Control { control.Margin = new Thickness(0, top, 0, bottom); return control; }
    private static Control Indented(Control control) { control.Margin = new Thickness(38, 0, 0, 6); return control; }
    private static Control StatusRow(Ellipse dot, TextBlock text)
    {
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        dot.Margin = new Thickness(0, 0, 10, 0); row.Children.Add(dot);
        Grid.SetColumn(text, 1); text.VerticalAlignment = VerticalAlignment.Center; row.Children.Add(text); return row;
    }
    private static Control LabelRow(string label, TextBlock value)
    {
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        row.Children.Add(Text(label, "label")); Grid.SetColumn(value, 1); row.Children.Add(value); return row;
    }
    private static Control Step(string number, string text)
    {
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        row.Children.Add(new Border { Width = 26, Height = 26, CornerRadius = new CornerRadius(13), Background = new SolidColorBrush(Color.Parse("#23483F")), Margin = new Thickness(0, 0, 12, 0), VerticalAlignment = VerticalAlignment.Top,
            Child = new TextBlock { Text = number, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#5EE8BE")), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } });
        var body = Text(text, "secondary"); Grid.SetColumn(body, 1); row.Children.Add(body); return row;
    }
    private static Control Row(string icon, string title, string detail, Button action, bool active)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        grid.Children.Add(new Path { Data = Geometry.Parse(icon), Stroke = new SolidColorBrush(Color.Parse(active ? "#5EE8BE" : "#A1ADBD")), StrokeThickness = 1.6, Width = 20, Height = 26, Stretch = Stretch.Uniform, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 16, 0) });
        var label = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0), Spacing = 2 };
        label.Children.Add(new TextBlock { Text = title, FontSize = 15, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
        label.Children.Add(Text(detail, "caption"));
        Grid.SetColumn(label, 1); grid.Children.Add(label);
        action.Classes.Add("compact"); action.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(action, 2); grid.Children.Add(action);
        var row = new Border { Child = grid, Classes = { "row" } }; row.Classes.Set("active", active); return row;
    }
    private Control WithCopy(TextBox box, Func<string?> value, string id, string name)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 8 };
        grid.Children.Add(box);
        var copy = IconButton(Icons.Copy, null, () => { }, id, name);
        copy.Click += async (_, _) =>
        {
            try
            {
                if (value() is not { } text || TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard) return;
                await clipboard.SetTextAsync(text);
                if (copy.Content is Path icon) { icon.Data = Geometry.Parse(Icons.Check); DispatcherTimer.RunOnce(() => icon.Data = Geometry.Parse(Icons.Copy), TimeSpan.FromSeconds(1.5)); }
            }
            catch (Exception e) when (e is InvalidOperationException or IOException or NotSupportedException) { readiness.Text = "Could not copy to the clipboard. Select the text and copy it instead."; }
        };
        copy.VerticalAlignment = VerticalAlignment.Top; ToolTip.SetTip(copy, name);
        Grid.SetColumn(copy, 1); grid.Children.Add(copy); return grid;
    }
    private static Expander Section(string header, Control content) => new()
    {
        Header = header, Content = content,
        HeaderTemplate = new FuncDataTemplate<object>((value, _) =>
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14 };
            var text = value as string ?? "";
            row.Children.Add(new Path { Data = Geometry.Parse(text.StartsWith("Certificate") ? Icons.Shield : text.StartsWith("Permissions") ? Icons.Lock : Icons.Sliders), Stroke = new SolidColorBrush(Color.Parse("#A1ADBD")), StrokeThickness = 1.6, Width = 20, Height = 20, Stretch = Stretch.Uniform, VerticalAlignment = VerticalAlignment.Center });
            row.Children.Add(new TextBlock { Text = text, FontSize = 16, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center });
            return row;
        })
    };
    private static Button IconButton(string icon, string? label, Action action, string id, string? name = null)
    {
        var glyph = new Path { Data = Geometry.Parse(icon), Stroke = new SolidColorBrush(Color.Parse("#A1ADBD")), StrokeThickness = 1.5, Width = 18, Height = 18, Stretch = Stretch.Uniform };
        object content = glyph;
        if (label != null) { var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 }; row.Children.Add(glyph); row.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center }); content = row; }
        var button = new Button { Content = content }; button.Classes.Add("icon");
        SetAccessibleName(button, name ?? label ?? id, id); button.Click += (_, _) => action(); return button;
    }
    internal static TextBlock Text(string text, string? style = null)
    { var block = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap }; if (style != null) block.Classes.Add(style); return block; }
    internal static Button Button(string title, Action action, string id, string? name = null)
    { var button = new Button { Content = title }; SetAccessibleName(button, name ?? title, id); button.Click += (_, _) => action(); return button; }
    internal static void SetAccessibleName(Control control, string title, string id) { AutomationProperties.SetName(control, title); AutomationProperties.SetAutomationId(control, id); }
}

// Outline icons in a 24-unit box, matching the Windows companion's stroke style.
internal static class Icons
{
    internal const string Gear = "M9,3 L8.5,5 L6.5,6 L4.5,5.5 L2.5,8.5 L4,10 L4,13 L2.5,14.5 L4.5,17.5 L6.5,17 L8.5,18 L9,20 L13,20 L13.5,18 L15.5,17 L17.5,17.5 L19.5,14.5 L18,13 L18,10 L19.5,8.5 L17.5,5.5 L15.5,6 L13.5,5 L13,3 Z M14,11.5 A3,3 0 1 1 8,11.5 A3,3 0 1 1 14,11.5";
    internal const string Phone = "M5,1 L17,1 Q19,1 19,3 L19,27 Q19,29 17,29 L5,29 Q3,29 3,27 L3,3 Q3,1 5,1 M9,25 L13,25";
    internal const string App = "M4,4 L10,4 L10,10 L4,10 Z M14,4 L20,4 L20,10 L14,10 Z M4,14 L10,14 L10,20 L4,20 Z M14,14 L20,14 L20,20 L14,20 Z";
    internal const string Copy = "M8,8 L19,8 L19,21 L8,21 Z M5,16 L5,3 L15,3";
    internal const string Check = "M4,12 L9,17 L20,6";
    internal const string Shield = "M12,2 L20,5 L20,11 C20,16 16.5,20 12,22 C7.5,20 4,16 4,11 L4,5 Z M8.5,12 L11,14.5 L15.5,9.5";
    internal const string Lock = "M6,11 L18,11 L18,21 L6,21 Z M8.5,11 L8.5,7.5 A3.5,3.5 0 0 1 15.5,7.5 L15.5,11";
    internal const string Sliders = "M4,6 L20,6 M4,12 L20,12 M4,18 L20,18 M9,4 L9,8 M15,10 L15,14 M7,16 L7,20";
}

public sealed class ApprovalWindow : Window
{
    private readonly DispatcherTimer timer;
    public ApprovalWindow(ApprovalPrompt prompt)
    {
        Title = "Approve browser · PC Remote"; Width = 480; SizeToContent = SizeToContent.Height; CanResize = false; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var body = new StackPanel { Margin = new Thickness(24), Spacing = 14 };
        body.Children.Add(MainWindow.Text("Approve new device?", "dialog-title"));
        body.Children.Add(MainWindow.Text($"Allow {prompt.Request.Name} from {prompt.Address} to control this computer? Approve only a request you started on your own phone. Approval remembers this browser for 30 days."));
        var remaining = MainWindow.Text("", "caption"); body.Children.Add(remaining);
        var allow = MainWindow.Button("Approve browser", () => { prompt.Decide(true); Close(true); }, "approve-browser"); allow.Classes.Add("primary");
        var deny = MainWindow.Button("Deny", () => { prompt.Decide(false); Close(false); }, "deny-browser");
        body.Children.Add(MainWindow.DialogButtons(deny, allow)); Content = body;
        timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        timer.Tick += (_, _) => { prompt.Expire(); remaining.Text = $"Expires in {Math.Max(0, (int)(prompt.Request.Expires - DateTimeOffset.UtcNow).TotalSeconds)} seconds"; if (prompt.Finished) Close(false); };
        Closing += (_, _) => { prompt.Decide(false); timer.Stop(); }; Opened += (_, _) => timer.Start();
        KeyDown += (_, e) => { if (e.Key == Key.Escape) { prompt.Decide(false); Close(false); } };
    }
}
