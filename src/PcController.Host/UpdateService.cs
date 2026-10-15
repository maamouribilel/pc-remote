using System.Text.Json;

namespace PcController.Host;

// Automatic is null until the person answers the one-time question; Available is the last verified notice.
public sealed record UpdatePreferences(bool? Automatic = null, DateTimeOffset? LastCheck = null, string? Skipped = null, UpdateRelease? Available = null);
public enum UpdateStage { Idle, Downloading, Ready, Installing }

// Platform hook for "Restart to update" (AUTO_UPDATE_PLAN.md Phase 2). Without one the service only notifies.
public interface IUpdateInstaller
{
    string Platform { get; }          // manifest platform, e.g. win-x64
    string Kind { get; }              // manifest kind, e.g. installer
    string DownloadDirectory { get; } // owned by this feature; only release file names are written or cleaned there
    // Starts the verified installer while its file stays locked. The task completes with the installer's exit code
    // if this process is still running when it ends; a successful update closes the companion before that.
    Task<int> Launch(VerifiedDownload download);
}
internal delegate Task<VerifiedDownload> UpdateDownloader(string version, string platform, string kind, string directory, IProgress<double>? progress, CancellationToken token);

// Owns the opt-in, schedule and notice state shared by the Windows and macOS/Linux companions.
// It is independent of the companion host: checks keep working while secure storage is locked
// or setup failed. Changed is raised on a worker thread; shells marshal to their UI thread.
public sealed class UpdateService : IDisposable
{
    public static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(45), Interval = TimeSpan.FromHours(24);
    private const string FileName = "update-settings.json";
    private readonly string path;
    private readonly Func<Version, CancellationToken, Task<UpdateResult>> check;
    private readonly IDisposable? owned;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly CancellationTokenSource stopping = new();
    private readonly Timer timer;
    private readonly IUpdateInstaller? installer;
    private readonly UpdateDownloader? download;
    private VerifiedDownload? prepared;
    private CancellationTokenSource? downloadCancel;
    private string? dismissed;
    private volatile bool disposed;
    public Version Current { get; }
    public UpdatePreferences Preferences { get; private set; }
    public bool Checking { get; private set; }
    public string? Problem { get; private set; }
    // Phase 2: only with a platform installer (Windows installer copies); downloads happen only on request.
    public bool CanInstall => installer != null;
    public UpdateStage Stage { get; private set; }
    public double DownloadProgress { get; private set; }
    public string? InstallProblem { get; private set; }
    public string? ReadyVersion => prepared?.Version;
    public event Action? Changed;

    public static Version ProductVersion => UpdateChecker.Normalize(typeof(UpdateService).Assembly.GetName().Version ?? new Version(0, 0, 0));
    // Production composition: the fixed GitHub feed and the embedded release keys. A platform installer
    // is passed only by a shell that can install updates (the Windows installer copy).
    public static UpdateService Create(string directory, IUpdateInstaller? installer = null)
    {
        var checker = UpdateChecker.CreateProduction();
        try { return new(directory, ProductVersion, checker.CheckAsync, checker, installer, checker.DownloadAsync); }
        catch { checker.Dispose(); throw; }
    }
    internal UpdateService(string directory, Version current, Func<Version, CancellationToken, Task<UpdateResult>> check, IDisposable? owned = null,
        IUpdateInstaller? installer = null, UpdateDownloader? download = null)
    {
        if (installer != null && download == null) throw new ArgumentException("Installing requires a verified downloader.", nameof(download));
        path = Path.Combine(directory, FileName); this.check = check; this.owned = owned; this.installer = installer; this.download = download;
        Current = UpdateChecker.Normalize(current);
        Preferences = Load(directory, path);
        if (installer != null) CleanDownloads(installer.DownloadDirectory);
        timer = new Timer(_ => _ = RunAsync(manual: false));
    }

    public bool NeedsConsent => Preferences.Automatic == null;
    // The banner shows only a verified release that is newer than this build, not skipped and not set aside this session.
    public UpdateRelease? Notice => Preferences.Available is { } release && UpdateChecker.TryParseVersion(release.Version) > Current
        && release.Version != Preferences.Skipped && release.Version != dismissed ? release : null;
    public string Status
    {
        get
        {
            switch (Stage)
            {
                case UpdateStage.Downloading: return $"Downloading PC Remote {Notice?.Version ?? ReadyVersion}… {DownloadProgress:P0}";
                case UpdateStage.Ready: return InstallProblem ?? $"PC Remote {ReadyVersion} is downloaded and verified. Choose Restart to update.";
                case UpdateStage.Installing: return "Installing the update. PC Remote will close and reopen.";
            }
            if (InstallProblem != null) return InstallProblem;
            if (Checking) return "Checking GitHub for a newer version…";
            var last = Preferences.LastCheck is { } time ? $" Last checked {time.ToLocalTime():g}." : "";
            if (Problem != null) return "Update check failed: " + Problem + last;
            if (Preferences.Available is { } release && UpdateChecker.TryParseVersion(release.Version) > Current)
                return $"PC Remote {release.Version} is available (this is {Display(Current)})." + (release.Version == Preferences.Skipped ? " You chose to skip it." : "") + last;
            if (Preferences.LastCheck != null) return $"PC Remote {Display(Current)} is the newest published version." + last;
            return Preferences.Automatic == true ? "Automatic checks are on; the first check runs shortly." : "Automatic update checks are off. Choose Check now to look once.";
        }
    }
    public static string Display(Version version) => $"{version.Major}.{version.Minor}.{version.Build}";

    public void Start() { if (Preferences.Automatic == true && !disposed) timer.Change(StartupDelay, Timeout.InfiniteTimeSpan); }
    public void SetAutomatic(bool enabled)
    {
        Save(Preferences with { Automatic = enabled });
        if (disposed) return;
        // Turning checks on looks right away, so the answer is visible next to the choice.
        if (enabled) timer.Change(TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan); else timer.Change(Timeout.Infinite, Timeout.Infinite);
        Changed?.Invoke();
    }
    // An explicit, one-time request; allowed while automatic checks are off.
    public Task CheckNowAsync() => RunAsync(manual: true);
    public void Skip() { if (Notice is { } release) { Save(Preferences with { Skipped = release.Version }); Changed?.Invoke(); } }
    public void Later() { if (Notice is { } release) { dismissed = release.Version; Changed?.Invoke(); } }

    // Phase 2: downloads the offered release's installer for this platform, verified against a freshly re-read signed manifest.
    public async Task DownloadAsync()
    {
        if (installer == null || download == null || disposed || Notice is not { } release || Stage is UpdateStage.Downloading or UpdateStage.Installing) return;
        if (prepared?.Version == release.Version) { Stage = UpdateStage.Ready; Changed?.Invoke(); return; }
        prepared?.Dispose(); prepared = null;
        var cancel = downloadCancel = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token);
        Stage = UpdateStage.Downloading; DownloadProgress = 0; InstallProblem = null; Changed?.Invoke();
        var progress = new Reporter(value =>
        {
            // About one notification per percent keeps the UI current without flooding its dispatcher.
            if (value < 1 && value - DownloadProgress < .01) return;
            DownloadProgress = value; Changed?.Invoke();
        });
        try
        {
            var verified = await download(release.Version, installer.Platform, installer.Kind, installer.DownloadDirectory, progress, cancel.Token).ConfigureAwait(false);
            if (disposed) { verified.Dispose(); return; }
            prepared = verified; Stage = UpdateStage.Ready;
        }
        catch (Exception) when (disposed) { return; }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested) { Stage = UpdateStage.Idle; InstallProblem = "The download was cancelled."; }
        catch (UpdateCheckException e) { Stage = UpdateStage.Idle; InstallProblem = "The update was not downloaded: " + e.Message; }
        catch (Exception e) when (e is not OutOfMemoryException) { Stage = UpdateStage.Idle; InstallProblem = "The update could not be downloaded. Check the connection and try again."; }
        finally
        {
            downloadCancel = null; cancel.Dispose();
            if (!disposed) Changed?.Invoke();
        }
    }
    public void CancelDownload() { try { downloadCancel?.Cancel(); } catch (ObjectDisposedException) { } }
    // Starts the verified installer. The shell first checks that no phone controls the computer and no power action is pending.
    public void Install()
    {
        if (installer == null || prepared is not { } verified || Stage != UpdateStage.Ready || disposed) throw new InvalidOperationException("No verified update is ready to install.");
        Stage = UpdateStage.Installing; InstallProblem = null; Changed?.Invoke();
        Task<int> run;
        try { run = installer.Launch(verified); }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException or IOException or UnauthorizedAccessException)
        { Stage = UpdateStage.Ready; InstallProblem = "The installer could not be started. Install the update from the release page instead."; Changed?.Invoke(); return; }
        _ = WatchAsync(run);
    }
    private async Task WatchAsync(Task<int> run)
    {
        int code;
        try { code = await run.ConfigureAwait(false); } catch (Exception e) when (e is not OutOfMemoryException) { code = -1; }
        if (disposed) return;
        // Reaching here means this companion is still running, so the update did not replace it.
        Stage = UpdateStage.Ready;
        InstallProblem = code switch
        {
            7 => "The installer could not close PC Remote. Release input, then choose Restart to update again.",
            2 or 5 => "The update was cancelled. PC Remote was not changed.",
            _ => $"The update was not installed (installer code {code}). PC Remote keeps running; try again or install from the release page.",
        };
        Changed?.Invoke();
    }
    private static void CleanDownloads(string directory)
    {
        // Only release files and partial downloads this feature wrote; a file still in use (the installer that
        // just relaunched this companion) is left for the next start.
        try
        {
            if (!Directory.Exists(directory)) return;
            foreach (var file in Directory.EnumerateFiles(directory, "PCRemote-*"))
                if (UpdateChecker.IsDownloadName(Path.GetFileName(file)))
                    try { File.Delete(file); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }
    private sealed class Reporter(Action<double> report) : IProgress<double> { public void Report(double value) => report(value); }

    private async Task RunAsync(bool manual)
    {
        if (disposed || (!manual && Preferences.Automatic != true)) return;
        if (!await gate.WaitAsync(0).ConfigureAwait(false)) return;
        try
        {
            Checking = true; Changed?.Invoke();
            UpdateResult result;
            try { result = await check(Current, stopping.Token).ConfigureAwait(false); }
            catch (Exception) when (disposed || stopping.IsCancellationRequested) { return; }
            // A background check must never take the companion down; report and retry on schedule.
            catch (Exception e) when (e is not OutOfMemoryException) { result = new(UpdateOutcome.Failed, Problem: "The update check stopped unexpectedly."); }
            if (result.Outcome == UpdateOutcome.Failed) Problem = result.Problem ?? "GitHub could not be reached.";
            else
            {
                Problem = null;
                // A verified newer release replaces the stored notice; UpToDate clears one this build has passed.
                var available = result.Outcome == UpdateOutcome.Available && result.Release is { } release && UpdateChecker.ValidRelease(release) ? result.Release
                    : Preferences.Available is { } stored && UpdateChecker.TryParseVersion(stored.Version) > Current ? stored : null;
                Save(Preferences with { LastCheck = DateTimeOffset.UtcNow, Available = available });
            }
        }
        finally
        {
            Checking = false;
            if (!disposed && Preferences.Automatic == true) timer.Change(Interval, Timeout.InfiniteTimeSpan);
            gate.Release();
            if (!disposed) Changed?.Invoke();
        }
    }

    private static UpdatePreferences Load(string directory, string path)
    {
        if (OperatingSystem.IsWindows()) Directory.CreateDirectory(directory);
        else Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        try
        {
            if (!File.Exists(path) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) return new();
            var stored = JsonSerializer.Deserialize<UpdatePreferences>(File.ReadAllBytes(path)) ?? new();
            // The file is user-writable: keep only a notice that still names this repository's release page.
            if (stored.Available is { } release && !UpdateChecker.ValidRelease(release)) stored = stored with { Available = null };
            if (stored.Skipped != null && UpdateChecker.TryParseVersion(stored.Skipped) == null) stored = stored with { Skipped = null };
            return stored;
        }
        // A damaged file only resets update preferences; certificates and devices are elsewhere.
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException) { return new(); }
    }
    private void Save(UpdatePreferences value)
    {
        Preferences = value;
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
            if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            using (var stream = new FileStream(temporary, options)) JsonSerializer.Serialize(stream, value);
            File.Move(temporary, path, true);
        }
        // Preferences still apply for this session; the next save retries.
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { try { File.Delete(temporary); } catch (Exception) { } }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; stopping.Cancel(); timer.Dispose(); prepared?.Dispose(); owned?.Dispose();
    }
}
