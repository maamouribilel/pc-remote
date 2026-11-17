using System.Windows;
using System.Net.NetworkInformation;
using System.Windows.Threading;
using Microsoft.Win32;
using PcController.Windows;
using PcController.Core;
using PcController.Host;
using Forms = System.Windows.Forms;

namespace PcController.App;

public partial class App : Application
{
    private UserInstance? instance;
    private ServerLifecycle? lifecycle;
    private Forms.NotifyIcon? tray;
    private Forms.ContextMenuStrip? menu;
    private Forms.ToolStripMenuItem? pauseItem, resumeItem, updateItem;
    private UpdateService? updates;
    private string? announcedUpdate;
    private readonly DispatcherTimer networkTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private MainWindow? setup;
    private bool exiting;
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Length == 1 && e.Args[0] is "--firewall-install" or "--firewall-remove") { await ConfigureFirewall(e.Args[0]); return; }
        if (e.Args.Length > 1 || (e.Args.Length == 1 && e.Args[0] is not ("--pause" or "--resume" or "--exit" or "--background"))) { Shutdown(1); return; }
        var action = e.Args.SingleOrDefault() switch { "--pause" => LocalAction.Pause, "--resume" => LocalAction.Resume, "--exit" => LocalAction.Exit, _ => LocalAction.Open };
        instance = new UserInstance();
        if (!instance.IsPrimary)
        {
            var sent = await instance.SendAsync(action, waitForExit: action == LocalAction.Exit);
            Shutdown(sent ? 0 : 1); return;
        }
        if (action != LocalAction.Open) { Shutdown(action == LocalAction.Exit ? 0 : 1); return; }
        setup = new MainWindow(); MainWindow = setup;
        setup.Closing += (_, args) => { if (!exiting) { args.Cancel = true; setup.Hide(); } };
        setup.Show();
        if (e.Args.Contains("--background")) setup.Hide();
        // Update checks keep their own plain preferences and run even if Windows integration fails below.
        try
        {
            var data = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PcController");
            // Only the registered installer copy can install updates itself; a portable ZIP copy keeps notices only.
            updates = UpdateService.Create(data, WindowsUpdateInstaller.ForInstalledCopy(data));
            setup.AttachUpdates(updates); updates.Changed += UpdatesChanged; updates.Start();
        }
        catch (Exception exception) when (exception is System.IO.IOException or UnauthorizedAccessException or InvalidOperationException or System.Security.Cryptography.CryptographicException) { updates = null; }
        try
        {
            var nativeInput = new WindowsInput(); // Retains native hold tracking across network restarts.
            lifecycle = new ServerLifecycle(snapshot => { var server = WindowsHostFactory.Create(snapshot, nativeInput); server.ApproveDeviceAsync = setup.ApproveDeviceAsync; return server; }, new WindowsTrustedNetworkDiscovery().Read);
            lifecycle.Updating += () => Dispatcher.BeginInvoke(() => setup.ShowUpdating());
            lifecycle.Ready += server => Dispatcher.BeginInvoke(() => { if (!exiting) { setup.ShowReady(server); UpdateTray(); } });
            lifecycle.Failed += message => Dispatcher.BeginInvoke(() => { if (!exiting) { setup.ShowFailure(message); UpdateTray(); } });
            menu = new Forms.ContextMenuStrip();
            menu.Items.Add("Open", null, (_, _) => OpenSetup());
            updateItem = (Forms.ToolStripMenuItem)menu.Items.Add("Update available…", null, (_, _) => OpenSetup()); updateItem.Visible = false;
            pauseItem = (Forms.ToolStripMenuItem)menu.Items.Add("Pause control", null, (_, _) => SetPaused(true));
            resumeItem = (Forms.ToolStripMenuItem)menu.Items.Add("Resume control", null, (_, _) => SetPaused(false));
            menu.Items.Add(new Forms.ToolStripSeparator()); menu.Items.Add("Exit", null, (_, _) => ExitCompanion());
            menu.Opening += (_, _) => UpdateTray();
            tray = new Forms.NotifyIcon { Icon = new System.Drawing.Icon(System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "pc-remote-v2.ico"), 32, 32), Text = "PC Remote", ContextMenuStrip = menu, Visible = true };
            tray.DoubleClick += (_, _) => OpenSetup();
            tray.BalloonTipClicked += (_, _) => OpenSetup();
            UpdatesChanged();
            _ = instance.ListenAsync(command => Dispatcher.InvokeAsync(() => Handle(command)).Task);
            networkTimer.Tick += async (_, _) => { if (lifecycle != null) await lifecycle.RefreshAsync(); UpdateTray(); };
            NetworkChange.NetworkAddressChanged += NetworkChanged;
            NetworkChange.NetworkAvailabilityChanged += AvailabilityChanged;
            SystemEvents.PowerModeChanged += PowerChanged;
            networkTimer.Start(); await lifecycle.RefreshAsync();
        }
        catch
        {
            setup.ShowFailure("Windows integration failed. Check port 8443, the user profile and certificate setup; exit and restart PC Remote.");
            if (tray == null) { exiting = true; Shutdown(1); }
        }
    }
    private void UpdatesChanged() => Dispatcher.BeginInvoke(() =>
    {
        if (exiting || updates == null) return;
        setup?.RenderUpdates();
        var notice = updates.Notice;
        if (updateItem != null) { updateItem.Visible = notice != null; if (notice != null) updateItem.Text = (notice.Security ? "Security update: " : "") + $"PC Remote {notice.Version} available…"; }
        // One tray balloon per version while this companion runs; the window banner stays until Skip or Later.
        if (notice != null && tray != null && announcedUpdate != notice.Version)
        {
            announcedUpdate = notice.Version;
            tray.ShowBalloonTip(10000, notice.Security ? "PC Remote security update" : "PC Remote update available", $"Version {notice.Version} is available. Open PC Remote to view the release.", Forms.ToolTipIcon.Info);
        }
    });
    private void OpenSetup() { setup?.Show(); if (setup != null) { setup.WindowState = WindowState.Normal; setup.Activate(); } }
    private void Handle(LocalAction command) { switch (command) { case LocalAction.Open: OpenSetup(); break; case LocalAction.Pause: SetPaused(true); break; case LocalAction.Resume: SetPaused(false); break; case LocalAction.Exit: Dispatcher.BeginInvoke(ExitCompanion, DispatcherPriority.Background); break; } }
    private void SetPaused(bool paused)
    {
        if (lifecycle?.Server is not { } server) return;
        try { if (!paused) server.Input.ReleaseAll(); server.Sessions.Pause(paused); }
        catch (InputFailureException) { server.Sessions.Pause(true); setup?.ShowFailure("Windows could not release held input. Control remains paused; check target privileges and the interactive desktop."); }
        UpdateTray();
    }
    private void UpdateTray()
    {
        if (tray == null || pauseItem == null || resumeItem == null) return;
        var server = lifecycle?.Server;
        pauseItem.Enabled = server != null && !server.Sessions.Paused;
        resumeItem.Enabled = server != null && server.Sessions.Paused;
        tray.Text = server == null ? "PC Remote · HTTPS unavailable" : server.Sessions.Paused ? "PC Remote · paused" : "PC Remote · ready";
    }
    private void NetworkChanged(object? sender, EventArgs e) => ScheduleRefresh();
    private void AvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e) => ScheduleRefresh();
    private void ScheduleRefresh() { if (!exiting) Dispatcher.BeginInvoke(async () => { if (lifecycle != null) await lifecycle.RefreshAsync(); }); }
    private void PowerChanged(object sender, PowerModeChangedEventArgs e)
    { Dispatcher.BeginInvoke(() => { if (e.Mode == PowerModes.Suspend) lifecycle?.Server?.Sessions.EndCurrent(); else if (e.Mode == PowerModes.Resume) ScheduleRefresh(); }); }
    private void ExitCompanion() { exiting = true; Shutdown(); }
    private async Task ConfigureFirewall(string argument)
    {
        // This exclusive helper path never creates a server, tray, input adapter or device store.
        try
        {
            var executable = Environment.ProcessPath!;
            if (argument == "--firewall-remove" && !FirewallRules.Exists(executable)) { Shutdown(); return; }
            var principal = new System.Security.Principal.WindowsPrincipal(System.Security.Principal.WindowsIdentity.GetCurrent());
            if (!principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
            {
                using var helper = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(executable, argument) { UseShellExecute = true, Verb = "runas", WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden })!;
                await helper.WaitForExitAsync(); Shutdown(helper.ExitCode); return;
            }
            FirewallRules.Configure(executable, argument == "--firewall-install"); Shutdown();
        }
        catch (Exception exception)
        {
            var detail = exception is System.ComponentModel.Win32Exception windows
                ? $"Windows error {windows.NativeErrorCode}." : $"{exception.GetType().Name}, Windows error 0x{exception.HResult:X8}.";
            MessageBox.Show("Firewall setup could not complete. Approve only this app's Windows administrator prompt, or ask your administrator to permit TCP 8443 for this executable on Private profiles from LocalSubnet. No network profile was changed.\n\n" + detail, "PC Remote firewall setup");
            Shutdown(1);
        }
    }
    protected override void OnExit(ExitEventArgs e)
    {
        exiting = true; networkTimer.Stop();
        NetworkChange.NetworkAddressChanged -= NetworkChanged;
        NetworkChange.NetworkAvailabilityChanged -= AvailabilityChanged;
        SystemEvents.PowerModeChanged -= PowerChanged;
        if (updates != null) { updates.Changed -= UpdatesChanged; updates.Dispose(); }
        tray?.Dispose(); menu?.Dispose();
        if (lifecycle != null) Task.Run(async () => await lifecycle.DisposeAsync()).GetAwaiter().GetResult();
        instance?.Dispose();
        base.OnExit(e);
    }
}
