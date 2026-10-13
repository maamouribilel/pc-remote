using System.Runtime.InteropServices;
using PcController.Core;
using PcController.Linux;
namespace PcController.LinuxTests;
internal static class PortalChecks
{
    [DllImport("PcControllerLinux")] private static extern int pc_linux_test_wayland();
    internal static async Task Run()
    {
        var scratch = Environment.GetEnvironmentVariable("PC_REMOTE_LINUX_CHECK_DIRECTORY") ?? throw new IOException("Private bus fixture required.");
        var callback = new LinuxNative.UnsafeCallback(() => { });
        _ = LinuxNative.pc_linux_open(2, callback);
        try
        {
            Program.Check(pc_linux_test_wayland() == 1, "test-only session seam in isolated portal fixture");
            foreach (var mode in new[] { "denied", "partial" })
            {
                File.WriteAllText(Path.Combine(scratch, "portal-mode"), mode);
                Program.Check(LinuxNative.pc_linux_consent() == 0 && LinuxNative.pc_linux_ready() == 0, "portal " + mode + " grant fails closed");
            }
            File.WriteAllText(Path.Combine(scratch, "portal-mode"), "success");
            Program.Check(LinuxNative.pc_linux_consent() == 1, "Create Select Start response handshake");
            var before = LinuxNative.pc_linux_generation(); Program.Check(LinuxNative.pc_linux_arm(before) == 1, "portal inspected generation arm");
            var input = new LinuxInput { GtkTextEnabled = true };
            input.Move(5, -3); input.Click("left", false); input.Scroll(120, -120); input.Key("Shift", true); input.Key("Shift", false); input.Shortcut("copy"); input.Text("é😀"); input.ReleaseAll();
            var events = File.ReadAllLines(Path.Combine(scratch, "portal-events"));
            Program.Check(events.Any(s => s.Contains("NotifyPointerMotion")) && events.Any(s => s.Contains("NotifyPointerButton")) && events.Any(s => s.Contains("NotifyPointerAxisDiscrete")), "supported Notify pointer transport (recorded only)");
            Program.Check(events.Count(s => s.Contains("NotifyKeyboardKeysym")) > 10, "keyboard and GTK compose use NotifyKeysym (recorded only)");
            foreach (var action in new[] { "playPause", "next", "previous" }) input.Media("General", action);
            var features = File.ReadAllLines(Path.Combine(scratch, "feature-events"));
            Program.Check(features.Any(s => s.Contains("PlayPause")) && features.Any(s => s.Contains("Previous")) && features.Any(s => s.Contains("Next")), "native MPRIS methods target private fake player; no playback claim");
            File.WriteAllText(Path.Combine(scratch, "mpris-secondary"), "start");
            var mediaDeadline = DateTime.UtcNow.AddSeconds(3);
            while ((!File.Exists(Path.Combine(scratch, "mpris-secondary-ready")) || File.ReadAllText(Path.Combine(scratch, "mpris-secondary-ready")) != "True") && DateTime.UtcNow < mediaDeadline) await Task.Delay(20);
            Program.Check(LinuxNative.pc_linux_media(0) != 0, "General selects sole playing player among paused peers");
            File.WriteAllText(Path.Combine(scratch, "mpris-secondary-playing"), "playing");
            Program.Check(LinuxNative.pc_linux_media(0) == 0, "ambiguous playing MPRIS peers refused"); File.Delete(Path.Combine(scratch, "mpris-secondary-playing"));
            File.WriteAllText(Path.Combine(scratch, "mpris-deny-next"), "deny");
            Program.Check(LinuxNative.pc_linux_media(1) == 0, "unsupported active-player action cannot fall through to another paused player");
            File.Delete(Path.Combine(scratch, "mpris-deny-next")); File.Delete(Path.Combine(scratch, "mpris-secondary"));
            mediaDeadline = DateTime.UtcNow.AddSeconds(3);
            while (File.ReadAllText(Path.Combine(scratch, "mpris-secondary-ready")) != "False" && DateTime.UtcNow < mediaDeadline) await Task.Delay(20);
            foreach (var permission in new[] { "no", "na", "challenge" })
            { File.WriteAllText(Path.Combine(scratch, "power-permission"), permission); Program.Check(LinuxPcActions.PowerCapabilities().Count == 0 && LinuxNative.pc_linux_power(0) == 0, "logind " + permission + " denies without prompting or execution"); }
            File.WriteAllText(Path.Combine(scratch, "power-permission"), "yes");
            Program.Check(LinuxPcActions.PowerCapabilities().Count == 3, "only logind yes advertises supported power");
            foreach (var action in new[] { "sleep", "restart", "shutdown" }) new LinuxPcActions().Power(action);
            features = File.ReadAllLines(Path.Combine(scratch, "feature-events"));
            Program.Check(features.Contains("[\"Suspend\", false]") && features.Contains("[\"Reboot\", false]") && features.Contains("[\"PowerOff\", false]"), "native power requests reach ONLY private fake logind with interactive=false");
            input.Button("left", true); input.Key("Shift", true);
            File.WriteAllText(Path.Combine(scratch, "portal-close"), "close");
            var deadline = DateTime.UtcNow.AddSeconds(3);
            while (LinuxNative.pc_linux_ready() != 0 && DateTime.UtcNow < deadline) await Task.Delay(20);
            Program.Check(LinuxNative.pc_linux_ready() == 0 && LinuxNative.pc_linux_generation() > before, "Closed signal invalidates portal and generation");
            Program.Check(LinuxNative.pc_linux_power(0) == 0 && LinuxNative.pc_linux_media(0) == 0, "ended session rejects native media/power before dispatch");
            Program.Fails(() => input.Move(1, 1), "revoked portal blocks new input");
            Program.Check(LinuxNative.pc_linux_arm(before) == 0, "stale generation cannot rearm revoked session");
            Program.Fails(input.ReleaseAll, "closed session retains unreachable holds for local recovery");
            input.ConfirmEndedBackendRecovery(); input.ReleaseAll(); Program.Check(true, "explicit local ended-backend recovery clears tracking only");
            File.WriteAllText(Path.Combine(scratch, "portal-mode"), "pending");
            var pending = Task.Run(LinuxNative.pc_linux_consent);
            deadline = DateTime.UtcNow.AddSeconds(3);
            while (!File.Exists(Path.Combine(scratch, "portal-pending")) && DateTime.UtcNow < deadline) await Task.Delay(20);
            Program.Check(File.Exists(Path.Combine(scratch, "portal-pending")), "pending portal consent reached Start");
            LinuxNative.pc_linux_cancel_consent();
            Program.Check(await pending.WaitAsync(TimeSpan.FromSeconds(3)) == 0 && LinuxNative.pc_linux_ready() == 0, "Exit cancellation closes pending consent promptly");
            LinuxNative.pc_linux_close(); _ = LinuxNative.pc_linux_open(2, callback);
            Program.Check(pc_linux_test_wayland() == 1, "fresh isolated backend after Exit cancellation");
            File.WriteAllText(Path.Combine(scratch, "portal-mode"), "success");
            Program.Check(LinuxNative.pc_linux_consent() == 1 && LinuxNative.pc_linux_arm(LinuxNative.pc_linux_generation()) == 1, "fresh consent arms isolated feature teardown check");
            File.WriteAllText(Path.Combine(scratch, "mpris-slow"), "slow");
            var inflight = Task.Run(() => LinuxNative.pc_linux_media(0)); deadline = DateTime.UtcNow.AddSeconds(3);
            while (!File.Exists(Path.Combine(scratch, "feature-inflight")) && DateTime.UtcNow < deadline) await Task.Delay(10);
            Program.Check(File.Exists(Path.Combine(scratch, "feature-inflight")), "native feature request reaches delayed private bus");
            var close = Task.Run(LinuxNative.pc_linux_close);
            Program.Check(await inflight.WaitAsync(TimeSpan.FromSeconds(4)) == 1, "feature dispatch completes before native bus is freed");
            await close.WaitAsync(TimeSpan.FromSeconds(4)); Program.Check(LinuxNative.pc_linux_ready() == 0, "feature teardown closes original native session safely");
        }
        finally { LinuxNative.pc_linux_close(); GC.KeepAlive(callback); }
    }
}
