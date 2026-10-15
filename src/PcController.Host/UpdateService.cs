using System.Text.Json;

namespace PcController.Host;

// Automatic is null until the person answers the one-time question; Available is the last verified notice.
public sealed record UpdatePreferences(bool? Automatic = null, DateTimeOffset? LastCheck = null, string? Skipped = null, UpdateRelease? Available = null);

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
    private string? dismissed;
    private volatile bool disposed;
    public Version Current { get; }
    public UpdatePreferences Preferences { get; private set; }
    public bool Checking { get; private set; }
    public string? Problem { get; private set; }
    public event Action? Changed;

    public static Version ProductVersion => UpdateChecker.Normalize(typeof(UpdateService).Assembly.GetName().Version ?? new Version(0, 0, 0));
    // Production composition: the fixed GitHub feed and the embedded release keys.
    public static UpdateService Create(string directory)
    {
        var checker = UpdateChecker.CreateProduction();
        try { return new(directory, ProductVersion, checker.CheckAsync, checker); }
        catch { checker.Dispose(); throw; }
    }
    internal UpdateService(string directory, Version current, Func<Version, CancellationToken, Task<UpdateResult>> check, IDisposable? owned = null)
    {
        path = Path.Combine(directory, FileName); this.check = check; this.owned = owned; Current = UpdateChecker.Normalize(current);
        Preferences = Load(directory, path);
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
        disposed = true; stopping.Cancel(); timer.Dispose(); owned?.Dispose();
    }
}
