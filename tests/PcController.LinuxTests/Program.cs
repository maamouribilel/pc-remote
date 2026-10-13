using PcController.Core;
using PcController.Linux;
namespace PcController.LinuxTests;
internal static class Program
{
    private static int checks;
    internal static void Check(bool condition, string name) { if (!condition) throw new Exception("FAIL: " + name); checks++; Console.WriteLine("PASS: " + name); }
    internal static void Fails(Action action, string name) { try { action(); } catch (InputFailureException) { Check(true, name); return; } throw new Exception("FAIL: " + name); }
    private static async Task Main(string[] args)
    {
        if (args.Contains("--probe-fails"))
        {
            var storage = new LinuxStorage(Path.Combine(Environment.GetEnvironmentVariable("HOME")!, ".local/share/pc-remote-test"));
            try { storage.Probe(); throw new Exception("unavailable keyring accepted"); }
            catch (IOException) { Check(true, "locked/missing keyring fails explicitly"); }
        }
        else if (args.Contains("--storage-unlocked"))
        {
            var storage = new LinuxStorage(Path.Combine(Environment.GetEnvironmentVariable("HOME")!, ".local/share/pc-remote-test"));
            Check(System.Text.Encoding.UTF8.GetString(storage.Read(Path.Combine(Environment.GetEnvironmentVariable("HOME")!, ".local/share/pc-remote-test/data.protected"))) == "disposable storage test", "provider restart retains original encryption key");
        }
        else if (args.Contains("--portal")) await PortalChecks.Run();
        else if (args.Contains("--storage-restart")) StorageChecks.Restart();
        else if (args.Contains("--x11")) await NativeChecks.Run();
        else if (args.Contains("--storage")) await StorageChecks.Run();
        else FaultChecks();
        Console.WriteLine($"Linux checks passed: {checks}. Recording/Xvfb/storage evidence is separate from GNOME Wayland and phone/LAN acceptance.");
    }
    private static void FaultChecks()
    {
        Check(LinuxSession.Detect("wayland", "wayland-0", ":0") == LinuxBackend.WaylandPortal, "XWayland DISPLAY selects portal");
        Check(LinuxSession.Detect("x11", null, ":0") == LinuxBackend.X11, "explicit X11");
        foreach (var args in new[] { ("x11", "wayland-0", ":0"), ("", "", ":0"), ("wayland", "", ":0"), ("x11", "", "") })
        { try { LinuxSession.Detect(args.Item1, args.Item2, args.Item3); throw new Exception("session accepted"); } catch (IOException) { Check(true, "ambiguous/headless session refused"); } }
        var e = new Recording(); var input = new LinuxInput(e);
        input.Button("left", true); input.Key("Shift", true); input.ReleaseAll(); input.ReleaseAll();
        Check(e.Held.Count == 0, "idempotent tracked keys/buttons");
        input.Button("right", true); input.Key("Alt", true); e.FailUps = true;
        Fails(input.ReleaseAll, "all failed ups retain state"); Check(e.Held.Count == 2, "cleanup holds retained");
        e.FailUps = false; input.ReleaseAll(); Check(e.Held.Count == 0, "cleanup retry");
        e.FailDown = true; Fails(() => input.Key("Control", true), "partial down retained"); e.FailDown = false;
        input.ReleaseAll(); Check(e.Held.Count == 0, "failed down retried up");
        input.Key("ArrowLeft", true); input.Key("ArrowLeft", true); input.Key("ArrowLeft", false);
        Check(e.Held.Count == 0 && e.Calls.TakeLast(4).Select(c => c.Down).SequenceEqual(new[] { true, false, true, false }), "bounded navigation pulses cannot create server auto repeat");
        input.Key("Shift", true); e.Missing = 0xffe1; input.Key("Shift", false); Check(e.Held.Count == 0, "key up retains original token after layout resolution disappears"); e.Missing = 0;
        input.Key("Shift", true); var count = e.Calls.Count; input.Key("Shift", true); Check(e.Calls.Count == count, "modifier repeat idempotent"); input.ReleaseAll();
        input.Shortcut("copy"); Check(e.Calls.TakeLast(4).Select(c => c.Code).SequenceEqual(new[] {0xffe3, (int)'c', (int)'c', 0xffe3}), "Ctrl copy logical symbol mapping");
        input.Shortcut("switchApp"); Check(e.Calls.TakeLast(4).First().Code == 0xffe9, "Alt Tab mapping");
        foreach (var key in KeyboardCommands.Keys) { input.Press(key); Check(e.Held.Count == 0, "navigation/modifier " + key); }
        input.Scroll(60, 60); var before = e.Calls.Count; input.Scroll(60, 60);
        Check(e.Calls.Count == before + 1 && e.Calls[^1].X == 1 && e.Calls[^1].Y == -1, "fractional 120 unit detents converted");
        Fails(() => input.Text("hello"), "text unavailable until GTK opt in"); input.GtkTextEnabled = true;
        input.Text("é😀\r\n\t"); Check(e.Held.Count == 0, "Unicode scalar compose and CRLF/tab cleanup");
        Fails(() => input.Text("\ud800"), "invalid surrogate rejected before input");
        Fails(() => input.Text("\0"), "control character rejected");
        input.Key("Control", true); Fails(() => input.Text("a"), "held key blocks text"); input.ReleaseAll();
        e.Missing = 'u'; before = e.Calls.Count; Fails(() => input.Text("é"), "missing compose layout key fails before emission"); Check(e.Calls.Count == before, "text preflight atomic"); e.Missing = 0;
        e.FailAfter = e.Calls.Count + 2; Fails(() => input.Text("a"), "partial Unicode compose interrupted"); e.FailAfter = int.MaxValue;
        Fails(input.ReleaseAll, "incomplete text cannot silently resume"); Check(e.Held.Count == 0, "held cleanup still attempted with incomplete compose");
        input.ConfirmLocalTextRecovery(); input.ReleaseAll(); Check(true, "explicit local compose recovery permits cleanup");
        input = new(e);
        foreach (var profile in MediaProfiles.Actions.Where(p => p.Key != "General")) foreach (var action in profile.Value)
        { input.Media(profile.Key, action); Check(e.Held.Count == 0, "player mapping and complete cleanup " + profile.Key + ":" + action); }
        input.Media("General", "next"); Check(e.MediaAction == 1, "General playback dispatches native MPRIS");
        e.MediaAvailable = false; Fails(() => input.Media("General", "playPause"), "unavailable or ambiguous MPRIS player fails closed"); e.MediaAvailable = true;
        Fails(() => input.Media("Stremio", "previous"), "unsupported Stremio previous rejected natively");
        e.Missing = 'k'; before = e.Calls.Count; Fails(() => input.Media("YouTube", "playPause"), "unresolvable player layout rejects before emission"); Check(e.Calls.Count == before, "media preflight posts nothing"); e.Missing = 0;
        input.Key("Control", true); Fails(() => input.Media("VLC", "mute"), "held keys reject player batch"); input.ReleaseAll();
        input.Button("left", true); e.MediaAction = -1; Fails(() => input.Media("General", "playPause"), "held drag cannot block watchdog in MPRIS call"); input.ReleaseAll(); Check(e.MediaAction == -1 && e.Held.Count == 0, "drag refusal avoids native media and allows tracked cleanup");
        if (OperatingSystem.IsLinux()) StartupChecks();
        input = new(e); var now = DateTimeOffset.UtcNow;
        using var controller = new InputController(input, new(), () => now, false, new("linux", true, true, false, true, false, false, new HashSet<string>(), new Dictionary<string, IReadOnlySet<string>>()));
        controller.Execute(new(1, "pointer.button", Button: "left", Action: "down")); controller.Execute(new(2, "key.down", Key: "Shift"));
        now += TimeSpan.FromMilliseconds(1300); Check(controller.CheckWatchdog() && e.Held.Count == 0, "shared watchdog releases Linux tracker");
        Fails(() => controller.Execute(new(3, "text.insert", Text:"a")), "backend disabled text rejected");
    }
    private static void StartupChecks()
    {
        if (!OperatingSystem.IsLinux()) return;
        var directory = Path.Combine(Path.GetTempPath(), "PcLogin-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var executable = Path.Combine(directory, "PcController.Desktop"); File.WriteAllText(executable, "test executable");
            var startup = new LinuxLoginStartup(Path.Combine(directory, "autostart"), executable);
            startup.Set(true); Check(startup.Enabled, "isolated XDG registration persists exact executable");
            Check(File.GetUnixFileMode(Path.Combine(directory, "autostart", "pc-remote.desktop")) == (UnixFileMode.UserRead | UnixFileMode.UserWrite), "XDG entry is private");
            startup.Set(false); Check(!startup.Enabled, "only matching autostart entry removed");
            File.WriteAllText(Path.Combine(directory, "autostart", "pc-remote.desktop"), "unrelated entry");
            try { startup.Set(false); throw new Exception("unrelated entry removed"); } catch (IOException) { Check(true, "unrelated startup entry preserved"); }
            File.Delete(Path.Combine(directory, "autostart", "pc-remote.desktop")); File.CreateSymbolicLink(Path.Combine(directory, "autostart", "pc-remote.desktop"), executable);
            try { startup.Set(true); throw new Exception("symlink accepted"); } catch (IOException) { Check(true, "startup symlink rejected"); }
            Check(LinuxLoginStartup.Quote("/tmp/app space/$x").Contains("\\\\$"), "Exec special characters escaped without a shell");
            try { LinuxLoginStartup.Quote("/tmp/%f"); throw new Exception("field code accepted"); } catch (IOException) { Check(true, "startup field code injection refused"); }
        }
        finally { Directory.Delete(directory, true); }
    }
    private sealed class Recording : ILinuxEvents
    {
        internal readonly List<(int Type, int Code, double X, double Y, bool Down)> Calls = [];
        internal readonly HashSet<(int,int)> Held = [];
        internal bool FailUps, FailDown;
        internal bool MediaAvailable = true;
        internal int MediaAction = -1;
        public bool Media(int action) { MediaAction = action; return MediaAvailable; }
        public void GeneralVolume(string action) { }
        internal uint Missing;
        internal int FailAfter = int.MaxValue;
        public int Resolve(uint symbol) => symbol == Missing ? 0 : (int)symbol;
        public bool Send(int type, int code = 0, double x = 0, double y = 0, bool down = false)
        {
            Calls.Add((type,code,x,y,down));
            if (type is 2 or 3) { if (down) Held.Add((type,code)); else if (!FailUps) Held.Remove((type,code)); }
            return Calls.Count < FailAfter && !(down && FailDown) && !(!down && FailUps);
        }
    }
}
