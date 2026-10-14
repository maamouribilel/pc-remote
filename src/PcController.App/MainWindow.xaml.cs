using System.IO;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;
using PcController.Core;
using PcController.Host;
using QRCoder;
using PcController.Windows;

namespace PcController.App;

public partial class MainWindow : Window
{
    private string? profile;
    private CompanionServer? host;
    private readonly DispatcherTimer refresh = new() { Interval = TimeSpan.FromSeconds(1) };
    private string? lastLink;
    private string? lastDevices;
    private string? lastMediaProfile;
    private string? lastApps;
    private bool updatingVolume;
    private bool desktopVolumeEdited;
    public MainWindow()
    {
        InitializeComponent();
        Title = $"PC Remote {typeof(App).Assembly.GetName().Version?.ToString(3)} — connection setup";
        ApplyDarkTitleBar(this);
        StartupAtSignIn.IsChecked = SignInStartup.Enabled;
        refresh.Tick += (_, _) => RefreshPairing();
        refresh.Start(); Closed += (_, _) => refresh.Stop();
    }
    public void ShowReady(CompanionServer server)
    {
        host = server;
        lastLink = lastDevices = lastMediaProfile = lastApps = null;
        Sensitivity.Text = server.PointerSettings.Value.Sensitivity.ToString(System.Globalization.CultureInfo.InvariantCulture);
        NaturalScroll.IsChecked = server.PointerSettings.Value.NaturalScroll;
        MediaProfile.ItemsSource = MediaProfiles.Actions.Keys.ToArray();
        Status.Text = server.Network.PrivateAddresses.Length > 0 ? "Secure LAN ready" : "Local setup · LAN unavailable";
        StatusDot.Fill = (Brush)FindResource(server.Network.PrivateAddresses.Length > 0 ? "AccentBrush" : "MutedBrush");
        NetworkNotice.Text = server.Network.Notice;
        Address.Text = server.ConnectionAddress;
        OtherAddresses.Text = server.Network.PrivateAddresses.Length == 0
            ? "Detected LAN IPs (not listening until Private): " + string.Join(", ", server.Network.AllAddresses.Select(a => a.ToString()))
            : "Address and QR refresh automatically after network changes. Open the updated address on your phone; trust remains valid while this PC's root certificate is unchanged.";
        Fingerprint.Text = server.Certificates.Fingerprint;
        profile = server.Certificates.ProfilePath;
        Export.IsEnabled = true;
        CertificateSettings.IsExpanded = server.Network.PrivateAddresses.Length == 0;
        RefreshPairing();
    }
    private void RefreshPairing()
    {
        if (host == null) return;
        if (lastMediaProfile != host.MediaSettings.Profile) { lastMediaProfile = host.MediaSettings.Profile; MediaProfile.SelectedItem = lastMediaProfile; }
        var link = host.PairingAddress;
        if (lastLink != link)
        {
            lastLink = link; PairLink.Text = link ?? "Create a new link to pair another browser.";
            QrPlaceholder.Visibility = link == null ? Visibility.Visible : Visibility.Collapsed;
            PairQr.Source = null;
            if (link != null)
            {
                using var data = QRCodeGenerator.GenerateQrCode(link, QRCodeGenerator.ECCLevel.M);
                using var qr = new PngByteQRCode(data);
                using var stream = new MemoryStream(qr.GetGraphic(6));
                var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream; bitmap.EndInit(); bitmap.Freeze(); PairQr.Source = bitmap;
            }
        }
        PairStatus.Text = host.Pairing.CurrentChallenge is { } challenge
            ? $"Link expires in {Math.Max(0, (int)(challenge.Expires - DateTimeOffset.UtcNow).TotalSeconds)} seconds"
            : $"Pairing: {host.Pairing.Status}.";
        PauseButton.Content = host.Sessions.Paused ? "Resume control" : "Pause control";
        var devices = host.Devices.List();
        OwnerStatus.Text = host.InputProblem ? "Windows input failed. Check target privileges/desktop; control may be paused. Resume only after resolving it." : host.Sessions.Paused ? "Control paused" : host.Sessions.OwnerDeviceId is { } owner
            ? "Controller: " + (devices.FirstOrDefault(d => d.Id == owner)?.Name ?? "revoked device") : "No active controller";
        PauseButton.SetResourceReference(StyleProperty, host.Sessions.Paused ? "PrimaryButton" : typeof(Button));
        RefreshPcActions();
        var deviceKey = string.Join("|", devices.Select(d => d.Id)) + "|" + host.Sessions.OwnerDeviceId;
        if (deviceKey == lastDevices) return;
        lastDevices = deviceKey; DeviceList.Children.Clear();
        foreach (var device in devices)
        {
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M5,1 L17,1 Q19,1 19,3 L19,27 Q19,29 17,29 L5,29 Q3,29 3,27 L3,3 Q3,1 5,1 M9,25 L13,25"), Stroke = (Brush)FindResource("MutedBrush"), StrokeThickness = 1.8, Width = 20, Height = 28, Stretch = Stretch.Uniform, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 16, 0) });
            var label = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
            label.Children.Add(new TextBlock { Text = device.Name, TextWrapping = TextWrapping.Wrap, FontSize = 16, FontWeight = FontWeights.SemiBold });
            var expiry = new TextBlock { Text = $"{(host.Sessions.OwnerDeviceId == device.Id ? "Controlling · " : "")}expires {device.Expires.LocalDateTime:d}", Margin = new Thickness(0, 4, 0, 0) };
            expiry.SetResourceReference(StyleProperty, "MutedText"); label.Children.Add(expiry); Grid.SetColumn(label, 1); row.Children.Add(label);
            var revoke = new Button { Content = "Revoke", MinHeight = 44, VerticalAlignment = VerticalAlignment.Center };
            System.Windows.Automation.AutomationProperties.SetName(revoke, "Revoke " + device.Name); Grid.SetColumn(revoke, 2);
            revoke.Click += (_, _) =>
            {
                try { host.RevokeDevice(device.Id); RefreshPairing(); }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                { MessageBox.Show(this, "Could not save revocation. Pause control and check the user data directory.", "Revocation failed"); }
            };
            row.Children.Add(revoke);
            DeviceList.Children.Add(new Border { Child = row, Background = (Brush)FindResource("ControlBrush"), BorderBrush = (Brush)FindResource(host.Sessions.OwnerDeviceId == device.Id ? "AccentBrush" : "BorderBrush"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = new Thickness(16), Margin = new Thickness(0, 0, 0, 12) });
        }
        if (devices.Length == 0) { var empty = new TextBlock { Text = "No paired devices. Create a link to pair your phone.", TextWrapping = TextWrapping.Wrap }; empty.SetResourceReference(StyleProperty, "MutedText"); DeviceList.Children.Add(empty); }
    }
    private void NewPairing(object sender, RoutedEventArgs e) { host?.Pairing.IssueChallenge(); RefreshPairing(); }
    private void RefreshPcActions()
    {
        if (host == null) return;
        if (Preferences.Visibility == Visibility.Visible && !DesktopVolume.IsMouseCaptureWithin && !DesktopVolume.IsKeyboardFocusWithin)
        {
            var audio = host.PcActions.GetVolume(); updatingVolume = true;
            DesktopVolume.IsEnabled = audio.Available; DesktopVolume.Value = audio.Level;
            DesktopVolumeValue.Text = audio.Available ? $"{audio.Level}%" : "—";
            DesktopAudioStatus.Text = !audio.Available ? "No Windows audio output is available." : audio.Muted ? "Windows is muted. Moving the slider unmutes it." : "System volume, separate from the player’s volume.";
            updatingVolume = false;
        }
        var pending = host.Power.Pending;
        DesktopPowerStatus.Text = pending != null ? $"{pending.Action}: {Math.Max(0, (int)Math.Ceiling((pending.ExecuteAt - DateTimeOffset.UtcNow).TotalSeconds))} seconds remaining." : host.PowerMessage;
        CancelDesktopPower.IsEnabled = pending != null;
        var apps = host.Launcher.List();
        var key = string.Join("|", apps.Select(a => a.Id)); if (key == lastApps) return;
        lastApps = key; LauncherList.Children.Clear();
        foreach (var app in apps)
        {
            var row = new Grid { Margin = new Thickness(0,0,0,10) };
            row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var label = new StackPanel { Margin = new Thickness(0,0,12,0), VerticalAlignment = VerticalAlignment.Center };
            label.Children.Add(new TextBlock { Text = app.Name, FontWeight = FontWeights.SemiBold, FontSize = 16 });
            var detail = new TextBlock { Text = app.Profile + " · " + app.Executable, TextWrapping = TextWrapping.Wrap, FontSize = 12 }; detail.SetResourceReference(StyleProperty, "MutedText"); label.Children.Add(detail); row.Children.Add(label);
            var open = new Button { Content = "Open", Margin = new Thickness(0,0,8,0), VerticalAlignment = VerticalAlignment.Center }; Grid.SetColumn(open,1);
            open.Click += (_, _) => { try { host.Input.ReleaseAll(); host.PcActions.Launch(app); host.MediaSettings.Save(app.Profile); } catch (Exception exception) when (exception is IOException or System.ComponentModel.Win32Exception or UnauthorizedAccessException or InputFailureException) { MessageBox.Show(this,"Could not open this app. Check its path and Windows permissions.","App launcher"); } }; row.Children.Add(open);
            var remove = new Button { Content = "Remove", VerticalAlignment = VerticalAlignment.Center }; Grid.SetColumn(remove,2);
            remove.Click += (_, _) => { try { host.Launcher.Remove(app.Id); RefreshPcActions(); } catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { MessageBox.Show(this,"Could not save launcher settings.","App launcher"); } }; row.Children.Add(remove);
            LauncherList.Children.Add(row);
        }
    }
    private void AddLauncherApp(object sender, RoutedEventArgs e)
    {
        if (host == null) return;
        var picker = new OpenFileDialog { Filter = "Windows apps (*.exe)|*.exe", Title = "Choose an app for the phone launcher" };
        if (picker.ShowDialog(this) != true) return;
        var dialog = new Window { Owner = this, Title = "Add launcher app", Width = 440, SizeToContent = SizeToContent.Height, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize, Background = (Brush)FindResource("BackgroundBrush"), Foreground = (Brush)FindResource("TextBrush") };
        ApplyDarkTitleBar(dialog);
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock { Text = "App name", Margin = new Thickness(0,0,0,8) });
        var name = new TextBox { Text = Path.GetFileNameWithoutExtension(picker.FileName), MaxLength = 64 }; panel.Children.Add(name);
        panel.Children.Add(new TextBlock { Text = "Media profile", Margin = new Thickness(0,16,0,8) });
        var profilePicker = new ComboBox { ItemsSource = MediaProfiles.Actions.Keys.ToArray(), SelectedItem = "General" }; panel.Children.Add(profilePicker);
        var save = new Button { Content = "Add app", Margin = new Thickness(0,20,0,0) }; save.SetResourceReference(StyleProperty,"PrimaryButton");
        save.Click += (_, _) =>
        {
            try { host.Launcher.Add(picker.FileName,name.Text, (string)profilePicker.SelectedItem); dialog.Close(); RefreshPcActions(); }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException) { MessageBox.Show(dialog,exception is ArgumentException or InvalidOperationException ? exception.Message : "Could not save the app list.","App launcher"); }
        };
        panel.Children.Add(save); dialog.Content = panel; dialog.ShowDialog();
    }
    private void DesktopVolumeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    { if (!updatingVolume && DesktopVolumeValue != null) { desktopVolumeEdited = true; DesktopVolumeValue.Text = $"{(int)e.NewValue}%"; } }
    private void CommitDesktopVolume(object sender, RoutedEventArgs e)
    {
        if (host == null || updatingVolume || !DesktopVolume.IsEnabled || !desktopVolumeEdited) return;
        desktopVolumeEdited = false;
        var result = host.PcActions.SetVolume((int)DesktopVolume.Value);
        DesktopAudioStatus.Text = result.Available ? $"Windows volume set to {result.Level}%." : "No Windows audio output is available.";
    }
    private void RequestLocalPower(object sender, RoutedEventArgs e)
    {
        if (host == null || sender is not Button { Tag: string action }) return;
        var label = action == "shutdown" ? "Shut down" : action == "restart" ? "Restart" : "Sleep";
        if (MessageBox.Show(this,$"{label} this PC in 30 seconds? Save your work first. You can cancel the countdown here or on your phone.","Confirm power action",MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.No) != MessageBoxResult.Yes) return;
        try { host.ScheduleLocalPower(action); RefreshPcActions(); }
        catch (Exception exception) when (exception is InputFailureException or InvalidOperationException) { MessageBox.Show(this,"Could not schedule this action. Release held input and cancel any pending power action first.","Power controls"); }
    }
    private void CancelLocalPower(object sender, RoutedEventArgs e) { host?.CancelPower(); RefreshPcActions(); }
    private void TogglePause(object sender, RoutedEventArgs e)
    {
        if (host == null) return;
        try { if (host.Sessions.Paused) host.Input.ReleaseAll(); host.Sessions.Pause(!host.Sessions.Paused); }
        catch (InputFailureException) { MessageBox.Show(this, "Windows could not release held input. Keep control paused and check the interactive desktop and target privileges.", "Input cleanup failed"); }
        RefreshPairing();
    }
    private void SavePointerSettings(object sender, RoutedEventArgs e)
    {
        if (host == null) return;
        if (!double.TryParse(Sensitivity.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value) || value < .25 || value > 4 || !double.IsFinite(value))
        { MessageBox.Show(this, "Enter a sensitivity from 0.25 to 4, using a decimal point.", "Invalid sensitivity"); return; }
        try { var preferences = new PointerPreferences(value, NaturalScroll.IsChecked == true); host.PointerSettings.Save(preferences); host.Input.Configure(preferences); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        { MessageBox.Show(this, "Could not save pointer settings.", "Save failed"); }
    }
    private void SaveMediaProfile(object sender, RoutedEventArgs e)
    {
        if (host == null || MediaProfile.SelectedItem is not string selected) return;
        try { host.MediaSettings.Save(selected); RefreshPairing(); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        { MessageBox.Show(this, "Could not save media profile.", "Save failed"); }
    }
    public Task<bool> ApproveDeviceAsync(PairingRequest request, string address)
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.BeginInvoke(() =>
        {
            if (Dispatcher.HasShutdownStarted || request.Expires <= DateTimeOffset.UtcNow) { completion.TrySetResult(false); return; }
            Show(); WindowState = WindowState.Normal; Activate();
            var dialog = new Window { Owner = this, Title = "Approve new PC Remote device", Width = 420,
                SizeToContent = SizeToContent.Height, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize, Background = (Brush)FindResource("BackgroundBrush"), Foreground = (Brush)FindResource("TextBrush") };
            var panel = new StackPanel { Margin = new Thickness(24) };
            ApplyDarkTitleBar(dialog);
            panel.Children.Add(new TextBlock { Text = $"Allow {request.Name} from {address} to control this PC?\n\nApprove only a request you initiated. Approval remembers this browser for 30 days.", TextWrapping = TextWrapping.Wrap, FontSize = 17 });
            var allow = new Button { Content = "Approve device", MinHeight = 44, Margin = new Thickness(0, 16, 0, 8) };
            allow.SetResourceReference(StyleProperty, "PrimaryButton");
            var deny = new Button { Content = "Deny", MinHeight = 44 };
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            allow.Click += (_, _) => { completion.TrySetResult(request.Expires > DateTimeOffset.UtcNow); dialog.Close(); };
            deny.Click += (_, _) => dialog.Close();
            timer.Tick += (_, _) => { if (request.Expires <= DateTimeOffset.UtcNow || host?.Pairing.Status != "pending") dialog.Close(); };
            dialog.Closed += (_, _) => { timer.Stop(); completion.TrySetResult(false); };
            panel.Children.Add(allow); panel.Children.Add(deny); dialog.Content = panel;
            dialog.Show(); timer.Start(); dialog.Activate();
        });
        return completion.Task;
    }
    public void ShowUpdating() { host = null; Status.Text = "Updating secure connection…"; StatusDot.Fill = (Brush)FindResource("MutedBrush"); PairQr.Source = null; QrPlaceholder.Visibility = Visibility.Visible; PairLink.Text = "Updating connection address…"; }
    public void ShowFailure(string message) { Status.Text = "Setup failed"; StatusDot.Fill = new SolidColorBrush(Color.FromRgb(255, 181, 177)); NetworkNotice.Text = message; CertificateSettings.IsExpanded = true; }
    private void OpenSettings(object sender, RoutedEventArgs e)
    {
        var opening = Preferences.Visibility != Visibility.Visible;
        Preferences.Visibility = opening ? Visibility.Visible : Visibility.Collapsed;
        Preferences.IsExpanded = opening;
        if (opening) Preferences.BringIntoView(); else DashboardScroll.ScrollToTop();
    }
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);
    private static void ApplyDarkTitleBar(Window window) => window.SourceInitialized += (_, _) =>
    {
        var handle = new WindowInteropHelper(window).Handle;
        var dark = 1; DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int));
        var background = 0x00140f0b; DwmSetWindowAttribute(handle, 35, ref background, sizeof(int));
        var text = 0x00f8f6f4; DwmSetWindowAttribute(handle, 36, ref text, sizeof(int));
        // Earlier supported Windows versions can ignore the caption color attributes.
    };
    private void SaveStartup(object sender, RoutedEventArgs e)
    {
        try { SignInStartup.SetEnabled(StartupAtSignIn.IsChecked == true, Environment.ProcessPath!); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException or ArgumentException)
        { StartupAtSignIn.IsChecked = SignInStartup.Enabled; MessageBox.Show(this, "Could not save sign-in startup. Use the installed app path and check your Windows account permissions.", "Startup setting failed"); }
    }
    private void ExitApp(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    private async void ConfigurePrivateFirewall(object sender, RoutedEventArgs e)
    {
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!, "--firewall-install") { UseShellExecute = false, CreateNoWindow = true })!;
        await process.WaitForExitAsync();
        MessageBox.Show(this, process.ExitCode == 0 ? "Private LocalSubnet firewall rule configured for this executable. Keep the companion unelevated." : "Firewall setup failed or was cancelled. Check administrator permissions and the trusted network's Private profile.", "Private LAN access");
    }
    private void ExportProfile(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { FileName = "pc-remote.mobileconfig", Filter = "iPhone/iPad trust profile|*.mobileconfig" };
        if (profile != null && dialog.ShowDialog(this) == true)
        {
            try { if (!Path.GetFullPath(dialog.FileName).Equals(Path.GetFullPath(profile), StringComparison.OrdinalIgnoreCase)) File.Copy(profile, dialog.FileName, true); }
            catch (IOException) { MessageBox.Show(this, "Could not save the public profile. Choose another writable location.", "Export failed"); }
            catch (UnauthorizedAccessException) { MessageBox.Show(this, "Choose a location your Windows account can write to.", "Export failed"); }
        }
    }
}
