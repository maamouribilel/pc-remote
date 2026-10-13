using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PcController.Core;
using QRCoder;

namespace PcController.Desktop;
public sealed class MainWindow : Window
{
    private readonly DesktopController controller;
    private readonly TextBlock status = Text("Setup required"), readiness = Text(""), networkNote = Text(""), pairingNote = Text("");
    private readonly TextBox address = new() { IsReadOnly = true }, pairing = new() { IsReadOnly = true };
    private readonly Image qr = new() { Width = 180, Height = 180 };
    private readonly ComboBox networks = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly StackPanel launcher = new() { Spacing = 8 };
    private readonly NumericUpDown volume = new() { Minimum = 0, Maximum = 100, Increment = 1, Value = 0 };
    private readonly TextBlock audioState = Text(""), powerState = Text("");
    private readonly CheckBox startup = new() { Content = "Start PC Remote at sign-in (starts paused; LAN approval stays session-only)" };
    private readonly Button addApp, setVolume, cancelPower;
    private readonly Button? authorizeStorage;
    private readonly Dictionary<string, Button> powerButtons = [];
    private readonly StackPanel devices = new() { Spacing = 8 };
    private readonly Button newPair, pause, release, save, export, approveNetwork;
    private readonly TextBox fingerprint = new() { IsReadOnly = true };
    private readonly NumericUpDown sensitivity = new() { Minimum = .25m, Maximum = 4, Increment = .25m, Value = 1 };
    private readonly CheckBox natural = new() { Content = "Natural scrolling" };
    private readonly ComboBox profile = new() { ItemsSource = new[] { "General", "YouTube", "VLC", "Stremio" }, SelectedIndex = 0 };
    private readonly CheckBox hide = new() { Content = "Keep running on close (verify the menu-bar icon; Linux retains its taskbar window)" };
    private readonly DispatcherTimer timer;
    private readonly Expander preferencesExpander;
    private string? renderedPair;
    private Bitmap? qrBitmap;
    private bool exiting, refreshing;
    private VolumeState lastVolume = new(false);
    private PcController.Host.CompanionServer? renderedServer;
    public event Action? ExitRequested;
    public MainWindow(DesktopController controller)
    {
        this.controller = controller;
        Title = "PC Remote · development shell"; Width = 860; Height = 820; MinWidth = 560; MinHeight = 480;
        Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://PcController.Desktop/Assets/tray.ico")));
        var body = new StackPanel { Spacing = 16, Margin = new Thickness(24) };
        body.Children.Add(Text("PC Remote", 24)); body.Children.Add(status); body.Children.Add(readiness);
        var actions = new WrapPanel { Orientation = Orientation.Horizontal };
        pause = Button("Pause / Resume", controller.PauseResume, "pause"); release = Button("Release input", controller.ReleaseInput, "release");
        actions.Children.Add(pause); actions.Children.Add(release); actions.Children.Add(Button("Exit", RequestExit, "exit")); body.Children.Add(actions);
        var pairingCard = new StackPanel { Spacing = 12 };
        pairingCard.Children.Add(Heading("Connection & pairing"));
        pairingCard.Children.Add(address); SetAccessibleName(address, "Connection address", "address");
        pairingCard.Children.Add(Text("Open this exact HTTPS address after certificate setup. Copy the link below if scanning is unavailable.", secondary: true));
        pairingCard.Children.Add(qr); AutomationProperties.SetName(qr, "Short-lived pairing QR code");
        pairingCard.Children.Add(pairing); SetAccessibleName(pairing, "Manual pairing link", "pairing-link"); pairingCard.Children.Add(pairingNote);
        pairingCard.Children.Add(Text("Certificate fingerprint", secondary: true)); pairingCard.Children.Add(fingerprint); SetAccessibleName(fingerprint, "Certificate SHA-256 fingerprint", "fingerprint");
        export = Button("Save iPhone trust profile", () => _ = ExportProfile(), "export-profile"); pairingCard.Children.Add(export);
        newPair = Button("Create new pairing link", controller.NewPairing, "new-pairing"); newPair.Classes.Add("primary"); pairingCard.Children.Add(newPair);
        var devicesCard = new StackPanel { Spacing = 12 };
        devicesCard.Children.Add(Heading("Remembered devices")); devicesCard.Children.Add(devices);
        devicesCard.Children.Add(Text("Approve only your own browser. Revoke a lost device here to cancel its controller session.", secondary: true));
        Border Card(Control content) => new() { Child = content, Background = new SolidColorBrush(Color.Parse("#171D25")), CornerRadius = new CornerRadius(16), Padding = new Thickness(20) };
        var cards = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), RowDefinitions = new RowDefinitions("Auto,Auto"), ColumnSpacing = 16, RowSpacing = 16 };
        var connectionCard = Card(pairingCard); var rememberedCard = Card(devicesCard);
        Grid.SetColumn(rememberedCard, 1); cards.Children.Add(connectionCard); cards.Children.Add(rememberedCard); body.Children.Add(cards);
        SizeChanged += (_, _) =>
        {
            var narrow = ClientSize.Width < 760;
            cards.ColumnDefinitions = new ColumnDefinitions(narrow ? "*" : "*,*");
            Grid.SetColumn(rememberedCard, narrow ? 0 : 1); Grid.SetRow(rememberedCard, narrow ? 1 : 0);
        };
        var setup = new StackPanel { Spacing = 12 };
        setup.Children.Add(Text("Select only a network you trust. Approval lasts this app session; changing connectivity blocks LAN access.", secondary: true));
        setup.Children.Add(networks); SetAccessibleName(networks, "Trusted network interface", "networks");
        approveNetwork = Button("Approve selected network", () => _ = ApproveNetwork(), "approve-network");
        networks.SelectionChanged += (_, _) => approveNetwork.IsEnabled = networks.SelectedItem is NetworkCandidate;
        setup.Children.Add(approveNetwork);
        setup.Children.Add(Button("Block LAN access", () => _ = controller.BlockLanAsync(), "block-network")); setup.Children.Add(networkNote);
        setup.Children.Add(Text(controller.Platform == "macos" ? "Accessibility is required for pointer and keyboard control. Enable this PC Remote.app in System Settings → Privacy & Security → Accessibility, quit and reopen it, then choose Resume. After rebuilding, remove stale entries and add the same candidate again. Secure Input/password fields block control. Review the selected player before media control. System audio requires a writable output. Restart/shutdown are unavailable on Mac." : "Linux requires a local unlocked GNOME session and password-encrypted login keyring. Wayland needs explicit Remote Desktop portal consent; XWayland is not input permission. GTK Unicode text is optional and target-specific. General playback uses MPRIS; player shortcuts require focus. Audio needs PulseAudio/PipeWire-Pulse. Power is gated by logind permission.", secondary: true));
        if (controller.HasPermissionOnboarding) setup.Children.Add(Button(controller.Platform == "macos" ? "Open Accessibility settings" : "Request Wayland Remote Desktop consent", controller.OpenPermissionSettings, "accessibility"));
        if (controller.HasStorageAuthorization)
        {
            authorizeStorage = Button("Allow Keychain access after update…", () => _ = AuthorizeStorage(), "authorize-keychain");
            setup.Children.Add(authorizeStorage);
            setup.Children.Add(Text("An updated or rebuilt unsigned app has a new code identity. Use this only for the PC Remote.app you installed; macOS asks for your login Keychain password. Deny keeps storage locked; nothing is reset.", secondary: true));
        }
        if (controller.HasLinuxTextMode)
        {
            var gtk = new CheckBox { Content = "Enable Unicode text for a verified GTK target only (Ctrl+Shift+U)" };
            SetAccessibleName(gtk, "GTK Unicode text mode", "linux-gtk-text");
            gtk.IsCheckedChanged += (_, _) => controller.EnableLinuxGtkText(gtk.IsChecked == true);
            setup.Children.Add(gtk);
            setup.Children.Add(Button("Recover ended input session…", () => _ = RecoverEndedBackend(), "linux-backend-recovery"));
            setup.Children.Add(Button("Recover interrupted GTK text…", () => _ = RecoverGtk(), "linux-gtk-recovery"));
            setup.Children.Add(Text("Verify a disposable GTK editor first. Other apps/IMEs may execute these chords instead. Interrupted composition must be cancelled locally; then confirm recovery here before Resume or Exit.", secondary: true));
        }
        body.Children.Add(new Expander { Header = "Trusted network & permissions", Content = setup, IsExpanded = true });
        var preferences = new StackPanel { Spacing = 12 };
        preferences.Children.Add(Text("Pointer sensitivity")); preferences.Children.Add(sensitivity); SetAccessibleName(sensitivity, "Pointer sensitivity", "sensitivity");
        preferences.Children.Add(natural); preferences.Children.Add(Text("Media profile")); preferences.Children.Add(profile); SetAccessibleName(profile, "Media profile", "profile");
        save = Button("Save preferences", () => controller.SavePreferences(new((double)(sensitivity.Value ?? 1), natural.IsChecked == true), profile.SelectedItem as string ?? "General"), "save-preferences");
        preferences.Children.Add(save); preferences.Children.Add(hide);
        startup.IsChecked = controller.StartupEnabled; startup.IsEnabled = controller.HasStartup;
        SetAccessibleName(startup, "Start at sign-in", "startup"); preferences.Children.Add(startup);
        var startupSave = Button("Save sign-in preference", () => controller.SetStartup(startup.IsChecked == true), "save-startup");
        startupSave.IsEnabled = controller.HasStartup; preferences.Children.Add(startupSave);
        preferences.Children.Add(Text("Configured apps")); preferences.Children.Add(launcher);
        addApp = Button(controller.Platform == "macos" ? "Add local .app…" : "Add local .desktop app…", () => _ = AddApp(), "add-app"); preferences.Children.Add(addApp);
        preferences.Children.Add(Text("System output volume (0–100)")); preferences.Children.Add(volume); SetAccessibleName(volume, "System output volume", "system-volume");
        setVolume = Button("Set volume", () => controller.SetVolume((int)(volume.Value ?? 0)), "set-volume"); preferences.Children.Add(setVolume); preferences.Children.Add(audioState);
        var power = new WrapPanel();
        foreach (var (action, label) in new[] { ("sleep", "Sleep…"), ("restart", "Restart…"), ("shutdown", "Shut down…") })
        { var button = Button(label, () => _ = ConfirmPower(action), "power-" + action); powerButtons[action] = button; power.Children.Add(button); }
        cancelPower = Button("Cancel power action", controller.CancelPower, "cancel-power"); power.Children.Add(cancelPower); preferences.Children.Add(power); preferences.Children.Add(powerState);
        preferencesExpander = new Expander { Header = "Preferences", Content = preferences };
        body.Children.Add(preferencesExpander);
        body.Children.Add(Text("Experimental macOS/Linux preview · Windows remains the primary platform. Native control is reported separately from pairing and transport.", secondary: true));
        Content = new ScrollViewer { Content = body };
        controller.Changed += Changed; controller.ApprovalRequested += Approval;
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
        Closed += (_, _) => { timer?.Stop(); controller.Changed -= Changed; controller.ApprovalRequested -= Approval; qrBitmap?.Dispose(); };
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
            finally { refreshing = false; }
        };
        timer.Start(); Opened += async (_, _) => await controller.RefreshAsync(); Render();
    }
    private async Task AddApp()
    {
        var server = controller.Server; if (server == null) return;
        var mac = controller.Platform == "macos";
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = mac ? "Choose a local application bundle" : "Choose an installed application entry", AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(mac ? "macOS application" : "Desktop application") { Patterns = [mac ? "*.app" : "*.desktop"], AppleUniformTypeIdentifiers = mac ? ["com.apple.application-bundle"] : null }] });
        var path = files.FirstOrDefault()?.TryGetLocalPath(); if (path == null || controller.Server != server) return;
        var dialog = new Window { Title = "Configure launcher app", Width = 460, SizeToContent = SizeToContent.Height, CanResize = false };
        var body = new StackPanel { Spacing = 12, Margin = new Thickness(24) };
        var name = new TextBox { Text = Path.GetFileNameWithoutExtension(path), MaxLength = 64 };
        var media = new ComboBox { ItemsSource = new[] { "General", "YouTube", "VLC", "Stremio" }, SelectedIndex = 0 };
        body.Children.Add(Text("Display name")); body.Children.Add(name); body.Children.Add(Text("Media profile")); body.Children.Add(media);
        body.Children.Add(Text("The phone receives a configured ID/name, never this local path. Opening requests activation; focus and playback still require local review.", secondary: true));
        body.Children.Add(Button("Add app", () => dialog.Close(true), "confirm-app")); body.Children.Add(Button("Cancel", () => dialog.Close(false), "cancel-app")); dialog.Content = body;
        if (await dialog.ShowDialog<bool>(this) && controller.Server == server) controller.AddLauncher(path, name.Text ?? "", media.SelectedItem as string ?? "General");
    }
    private async Task ConfirmPower(string action)
    {
        var server = controller.Server; if (server == null) return;
        controller.ReleaseInput();
        var dialog = new Window { Title = "Confirm " + action, Width = 460, SizeToContent = SizeToContent.Height, CanResize = false };
        var body = new StackPanel { Spacing = 12, Margin = new Thickness(24) };
        body.Children.Add(Text($"Request {action} on this computer after a cancellable 30-second countdown? Save work first. OS permissions/inhibitors can refuse execution. Once dispatched, cancellation is local to the OS.", secondary: true));
        body.Children.Add(Button("Start countdown", () => dialog.Close(true), "confirm-power")); body.Children.Add(Button("Cancel", () => dialog.Close(false), "dismiss-power")); dialog.Content = body;
        if (await dialog.ShowDialog<bool>(this) && controller.Server == server) controller.SchedulePower(action);
    }
    private async Task ExportProfile()
    {
        if (controller.Server is not { } server) return;
        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save public iPhone trust profile", SuggestedFileName = "pc-remote.mobileconfig",
                FileTypeChoices = [new FilePickerFileType("iPhone certificate profile") { Patterns = ["*.mobileconfig"] }]
            });
            if (file == null || controller.Server != server) return;
            // Only the already-exported public CA profile is copied. No private keys.
            await using var source = File.OpenRead(server.Certificates.ProfilePath);
            await using var target = await file.OpenWriteAsync(); await source.CopyToAsync(target);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { readiness.Text = "Public profile could not be saved. Check the destination and retry."; }
    }
    private void Changed() => Dispatcher.UIThread.Post(Render);
    private async Task RecoverEndedBackend()
    {
        var dialog = new Window { Title = "Verify ended-session cleanup locally", Width = 480, SizeToContent = SizeToContent.Height, CanResize = false };
        var content = new StackPanel { Spacing = 12, Margin = new Thickness(24) };
        content.Children.Add(Text("Use this only after the portal/display connection ended and Release input cannot reach it. Verify on this computer that every held key/button and GTK composition is cleared, using local input as needed. Confirmation retires unreachable tracking; it does not establish successful automatic cleanup. Wayland requires fresh consent; X11 requires app restart. Control remains paused.", secondary: true));
        content.Children.Add(Button("I verified all input is released locally", () => dialog.Close(true), "confirm-backend-recovery"));
        content.Children.Add(Button("Cancel", () => dialog.Close(false), "cancel-backend-recovery")); dialog.Content = content;
        if (await dialog.ShowDialog<bool>(this)) controller.ConfirmLinuxEndedBackendRecovery();
    }
    private async Task AuthorizeStorage()
    {
        var dialog = new Window { Title = "Allow existing Keychain storage key", Width = 480, SizeToContent = SizeToContent.Height, CanResize = false };
        var content = new StackPanel { Spacing = 12, Margin = new Thickness(24) };
        content.Children.Add(Text("macOS will show its own Keychain dialog for this PC Remote.app and the existing local.pc-remote.storage.v1 key. Continue only if you installed this app from the official release and placed it in its stable location. Choose Always Allow to keep the current certificate root and remembered devices; Deny keeps storage locked. PC Remote never resets the key, root or devices here.", secondary: true));
        content.Children.Add(Button("Continue to macOS dialog", () => dialog.Close(true), "confirm-keychain"));
        content.Children.Add(Button("Cancel", () => dialog.Close(false), "cancel-keychain")); dialog.Content = content;
        if (await dialog.ShowDialog<bool>(this)) await controller.AuthorizeStorageAsync();
    }
    private async Task RecoverGtk()
    {
        var dialog = new Window { Title = "Confirm local text recovery", Width = 460, SizeToContent = SizeToContent.Height, CanResize = false };
        var content = new StackPanel { Spacing = 12, Margin = new Thickness(24) };
        content.Children.Add(Text("Cancel Unicode composition in the original GTK target locally (Escape), restore the original backend and choose Release input. Confirm only after checking the target; control will remain paused.", secondary: true));
        content.Children.Add(Button("I cancelled composition locally", () => dialog.Close(true), "confirm-gtk-recovery"));
        content.Children.Add(Button("Cancel", () => dialog.Close(false), "cancel-gtk-recovery")); dialog.Content = content;
        if (await dialog.ShowDialog<bool>(this)) controller.ConfirmLinuxTextRecovery();
    }
    private async Task ApproveNetwork()
    {
        if (networks.SelectedItem is not NetworkCandidate candidate) return;
        var dialog = new Window { Title = "Approve trusted network", Width = 460, SizeToContent = SizeToContent.Height, CanResize = false };
        var content = new StackPanel { Margin = new Thickness(20), Spacing = 12 };
        content.Children.Add(Text($"Trust {candidate}? Only approve your own trusted LAN. All peers on this interface can reach the pairing service."));
        content.Children.Add(Button("Approve for this session", () => dialog.Close(true), "confirm-network"));
        content.Children.Add(Button("Cancel", () => dialog.Close(false), "cancel-network")); dialog.Content = content;
        if (await dialog.ShowDialog<bool>(this)) await controller.ApproveNetworkAsync(candidate);
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
    public void ShowPreferences() { Open(); controller.ReleaseInput(); preferencesExpander.IsExpanded = true; if (Content is ScrollViewer scroll) scroll.ScrollToEnd(); }
    public void RequestExit() { if (exiting) return; exiting = true; timer.Stop(); hide.IsChecked = false; ExitRequested?.Invoke(); }
    public void CancelExit() { exiting = false; timer.Start(); Open(); Render(); }
    private void Render()
    {
        if (exiting) return;
        var server = controller.Server;
        if (server != renderedServer)
        {
            renderedServer = server;
            if (server != null) { sensitivity.Value = (decimal)server.PointerSettings.Value.Sensitivity; natural.IsChecked = server.PointerSettings.Value.NaturalScroll; profile.SelectedItem = server.MediaSettings.Profile; }
        }
        status.Text = controller.Status; readiness.Text = controller.Message;
        fingerprint.Text = server?.Certificates.Fingerprint ?? "Available after secure certificate setup";
        address.Text = server?.ConnectionAddress ?? "Unavailable — secure storage setup required";
        var nextPair = server?.PairingAddress;
        qr.IsVisible = nextPair != null;
        pairing.Text = nextPair ?? "Create a fresh link when HTTPS is ready.";
        if (nextPair != renderedPair)
        {
            renderedPair = nextPair; qr.Source = null; qrBitmap?.Dispose(); qrBitmap = null;
            if (nextPair != null) { using var data = QRCodeGenerator.GenerateQrCode(nextPair, QRCodeGenerator.ECCLevel.Q); using var png = new PngByteQRCode(data); using var stream = new MemoryStream(png.GetGraphic(6)); qrBitmap = new Bitmap(stream); qr.Source = qrBitmap; }
        }
        pairingNote.Text = server?.Pairing.CurrentChallenge is { } challenge ? $"Link expires in {Math.Max(0, (int)(challenge.Expires - DateTimeOffset.UtcNow).TotalSeconds)} seconds." : "Pairing links and pending requests expire after two minutes.";
        newPair.IsEnabled = export.IsEnabled = pause.IsEnabled = release.IsEnabled = save.IsEnabled = server != null;
        sensitivity.IsEnabled = natural.IsEnabled = server?.Capabilities.Pointer == true;
        profile.IsEnabled = server?.Capabilities.MediaActions.Count > 0;
        addApp.IsEnabled = server?.Capabilities.Launcher == true;
        volume.IsEnabled = setVolume.IsEnabled = server?.Capabilities.Volume == true && !server.Sessions.Paused;
        var audio = server?.Capabilities.Volume == true ? lastVolume : new VolumeState(false);
        audioState.Text = audio.Available ? $"Actual output: {audio.Level}%{(audio.Muted ? " · muted" : "")}" : "Writable audio output unavailable";
        foreach (var (action, button) in powerButtons) button.IsEnabled = server?.Capabilities.PowerActions.Contains(action) == true && !server.Sessions.Paused;
        cancelPower.IsEnabled = server?.Power.Pending != null;
        powerState.Text = server?.Power.Pending is { } pending ? $"{pending.Action} in {Math.Max(0, (int)(pending.ExecuteAt - DateTimeOffset.UtcNow).TotalSeconds)} seconds" : server?.PowerMessage ?? "";
        var apps = server?.Launcher.List() ?? [];
        var appSignature = string.Join("|", apps.Select(a => a.Id + a.Name + a.Profile));
        if (!Equals(launcher.Tag, appSignature))
        {
            launcher.Tag = appSignature; launcher.Children.Clear();
            foreach (var app in apps) { var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 }; row.Children.Add(Text(app.Name + " · " + app.Profile)); row.Children.Add(Button("Remove " + app.Name, () => controller.RemoveLauncher(app.Id), "remove-app-" + app.Id)); launcher.Children.Add(row); }
            if (apps.Length == 0) launcher.Children.Add(Text("No configured apps", secondary: true));
        }
        pause.Content = server?.Sessions.Paused == true ? "Resume control" : "Pause control";
        var selected = networks.SelectedItem as NetworkCandidate;
        var candidates = controller.Networks.Candidates;
        if (!((networks.ItemsSource as NetworkCandidate[]) ?? []).Select(c => c.Identity).SequenceEqual(candidates.Select(c => c.Identity)))
        { networks.ItemsSource = candidates; networks.SelectedItem = candidates.FirstOrDefault(c => c.Identity == selected?.Identity); }
        approveNetwork.IsEnabled = networks.SelectedItem is NetworkCandidate;
        if (authorizeStorage != null) authorizeStorage.IsEnabled = !controller.StorageReady;
        networkNote.Text = server?.Network.Notice ?? "No listener is running. Network approval alone cannot enable missing secure storage/native adapters.";
        // Rebuild only when device content changes to preserve keyboard focus.
        var records = server?.Devices.List() ?? [];
        var signature = string.Join("|", records.Select(d => d.Id + d.Name + d.Expires));
        if (!Equals(devices.Tag, signature))
        {
            devices.Tag = signature; devices.Children.Clear();
            foreach (var device in records) { var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 }; row.Children.Add(Text($"{device.Name} · expires {device.Expires.LocalDateTime:d}")); row.Children.Add(Button("Revoke " + device.Name, () => controller.Revoke(device.Id), "revoke-" + device.Id)); devices.Children.Add(row); }
            if (records.Length == 0) devices.Children.Add(Text("No remembered devices", secondary: true));
        }
    }
    internal static TextBlock Text(string text, double size = 16, bool secondary = false)
    { var block = new TextBlock { Text = text, FontSize = size, TextWrapping = TextWrapping.Wrap }; if (secondary) block.Classes.Add("secondary"); return block; }
    private static TextBlock Heading(string text) => new() { Text = text, FontSize = 20, FontWeight = FontWeight.SemiBold };
    internal static Button Button(string title, Action action, string id)
    { var button = new Button { Content = title, Margin = new Thickness(0, 0, 8, 0) }; SetAccessibleName(button, title, id); button.Click += (_, _) => action(); return button; }
    internal static void SetAccessibleName(Control control, string title, string id) { AutomationProperties.SetName(control, title); AutomationProperties.SetAutomationId(control, id); }
}

public sealed class ApprovalWindow : Window
{
    private readonly DispatcherTimer timer;
    public ApprovalWindow(ApprovalPrompt prompt)
    {
        Title = "Approve browser · PC Remote"; Width = 460; SizeToContent = SizeToContent.Height; CanResize = false;
        var body = new StackPanel { Margin = new Thickness(24), Spacing = 12 };
        body.Children.Add(MainWindow.Text($"Allow {prompt.Request.Name} from {prompt.Address}? Approve only a request you initiated."));
        var remaining = MainWindow.Text(""); body.Children.Add(remaining);
        var allow = MainWindow.Button("Approve browser", () => { prompt.Decide(true); Close(true); }, "approve-browser"); allow.Classes.Add("primary"); body.Children.Add(allow);
        body.Children.Add(MainWindow.Button("Deny", () => { prompt.Decide(false); Close(false); }, "deny-browser")); Content = body;
        timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        timer.Tick += (_, _) => { prompt.Expire(); remaining.Text = $"Expires in {Math.Max(0, (int)(prompt.Request.Expires - DateTimeOffset.UtcNow).TotalSeconds)} seconds"; if (prompt.Finished) Close(false); };
        Closing += (_, _) => { prompt.Decide(false); timer.Stop(); }; Opened += (_, _) => timer.Start();
        KeyDown += (_, e) => { if (e.Key == Avalonia.Input.Key.Escape) { prompt.Decide(false); Close(false); } };
    }
}
