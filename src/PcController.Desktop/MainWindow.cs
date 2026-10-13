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
        setup.Children.Add(Text(controller.Platform == "macos" ? "Accessibility: native control is unavailable until the macOS adapter is implemented. Keychain storage is also required." : "Desktop permission: X11/Wayland control and secure keyring storage are unavailable until the Linux adapters are implemented.", secondary: true));
        body.Children.Add(new Expander { Header = "Trusted network & permissions", Content = setup, IsExpanded = true });
        var preferences = new StackPanel { Spacing = 12 };
        preferences.Children.Add(Text("Pointer sensitivity")); preferences.Children.Add(sensitivity); SetAccessibleName(sensitivity, "Pointer sensitivity", "sensitivity");
        preferences.Children.Add(natural); preferences.Children.Add(Text("Media profile")); preferences.Children.Add(profile); SetAccessibleName(profile, "Media profile", "profile");
        save = Button("Save preferences", () => controller.SavePreferences(new((double)(sensitivity.Value ?? 1), natural.IsChecked == true), profile.SelectedItem as string ?? "General"), "save-preferences");
        preferences.Children.Add(save); preferences.Children.Add(hide);
        preferencesExpander = new Expander { Header = "Preferences", Content = preferences };
        body.Children.Add(preferencesExpander);
        body.Children.Add(Text("Development shell · public release remains Windows 0.2.1. Native control is reported separately from pairing and transport.", secondary: true));
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
            try { await controller.RefreshAsync(); } finally { refreshing = false; }
        };
        timer.Start(); Opened += async (_, _) => await controller.RefreshAsync(); Render();
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
        sensitivity.IsEnabled = natural.IsEnabled = profile.IsEnabled = server != null;
        pause.Content = server?.Sessions.Paused == true ? "Resume control" : "Pause control";
        var selected = networks.SelectedItem as NetworkCandidate;
        var candidates = controller.Networks.Candidates;
        if (!((networks.ItemsSource as NetworkCandidate[]) ?? []).Select(c => c.Identity).SequenceEqual(candidates.Select(c => c.Identity)))
        { networks.ItemsSource = candidates; networks.SelectedItem = candidates.FirstOrDefault(c => c.Identity == selected?.Identity); }
        approveNetwork.IsEnabled = networks.SelectedItem is NetworkCandidate;
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
