using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using PcController.Core;
using PcController.Linux;
namespace PcController.LinuxTests;
internal static class NativeChecks
{
    [DllImport("PcControllerLinux")] private static extern int pc_linux_test_x11(uint window);
    [DllImport("PcControllerLinux")] private static extern int pc_linux_test_unfocus();
    internal static async Task Run()
    {
        if (Environment.GetEnvironmentVariable("PC_REMOTE_ISOLATED_XVFB") != "1") throw new IOException("Run scripts/portable.py linux-x11; arbitrary active desktop injection is forbidden.");
        var scratch = Environment.GetEnvironmentVariable("PC_REMOTE_LINUX_CHECK_DIRECTORY") ?? throw new IOException("Missing disposable target directory.");
        var report = Path.Combine(scratch, "receipt.txt");
        using var target = Process.Start(new ProcessStartInfo(Path.Combine(scratch, "target")) { ArgumentList = { report }, RedirectStandardOutput = true })!;
        var xid = uint.Parse((await target.StandardOutput.ReadLineAsync())!);
        var callback = new LinuxNative.UnsafeCallback(() => { });
        _ = LinuxNative.pc_linux_open(1, callback); // Real logind intentionally absent in isolated Xvfb.
        var input = new LinuxInput { GtkTextEnabled = true };
        try
        {
            Program.Check(pc_linux_test_x11(xid) == 1 && LinuxNative.pc_linux_arm(LinuxNative.pc_linux_generation()) == 1, "isolated X11 guard and XTEST initialized");
            async Task<string[]> Read() { await Task.Delay(300); return File.ReadAllLines(report); }
            string Text(string[] receipt) => Encoding.UTF8.GetString(Convert.FromBase64String(receipt[0]));
            input.Text("é😀 Linux");
            var textDeadline = DateTime.UtcNow.AddSeconds(5); var receipt = await Read();
            while (Text(receipt) != "é😀 Linux" && DateTime.UtcNow < textDeadline) receipt = await Read();
            Program.Check(Text(receipt) == "é😀 Linux", "real GTK Unicode accent and non BMP receipt (" + Text(receipt) + ")");
            input.Press("Backspace"); Program.Check(Text(await Read()) == "é😀 Linu", "native Backspace");
            input.Shortcut("selectAll"); input.Text("replacement"); Program.Check(Text(await Read()) == "replacement", "Ctrl A layout resolved and Unicode replacement");
            input.Shortcut("selectAll"); input.Shortcut("copy"); input.Press("End"); input.Shortcut("paste"); Program.Check(Text(await Read()) == "replacementreplacement", "native Ctrl copy paste isolated clipboard");
            input.Press("Home"); input.Press("ArrowRight"); input.Text("X");
            Program.Check(Text(await Read()) == "rXeplacementreplacement", "Home and ArrowRight native caret movement");
            input.Press("Backspace"); input.Press("End"); input.Text("\r\n\té");
            Program.Check(Text(await Read()) == "replacementreplacement\n\té", "native normalized Return and Tab insertion");
            input.Key("Backspace", true); await Task.Delay(700);
            Program.Check(Text(await Read()) == "replacementreplacement\n\t", "navigation hold has one pulse and no server auto repeat"); input.Key("Backspace", false);
            var pointerBefore = await Read();
            input.Move(20, 10); input.Click("left", false); input.Scroll(0, 120);
            var state = await Read(); Program.Check(int.Parse(state[3]) > int.Parse(pointerBefore[3]) && int.Parse(state[4]) > int.Parse(pointerBefore[4]) && int.Parse(state[5]) > int.Parse(pointerBefore[5]), "real pointer click motion scroll receipt");
            Program.Check(int.Parse(state[8]) == int.Parse(pointerBefore[8]) + 20 && int.Parse(state[9]) == int.Parse(pointerBefore[9]) + 10, "actual relative pointer displacement");
            input.Click("right", false); await Read(); input.Press("Escape"); await Read(); await Task.Delay(500); input.Click("left", true); state = await Read(); Program.Check(int.Parse(state[6]) >= 1 && int.Parse(state[7]) >= 1, "native right click and recognized GTK double click");
            input.Press("Escape"); // Close target context menu if present.
            input.Button("left", true); input.Move(10, 0); input.Key("Shift", true);
            state = await Read(); Program.Check(int.Parse(state[1]) != 0 && int.Parse(state[2]) != 0, "drag and modifier received held");
            input.ReleaseAll(); state = await Read(); Program.Check(int.Parse(state[1]) == 0 && int.Parse(state[2]) == 0, "real held release cleanup");
            var audio = new LinuxPcActions(); var originalAudio = audio.GetVolume();
            Program.Check(originalAudio.Available, "native audio reads private PulseAudio null sink (no user's output)");
            var changedAudio = audio.SetVolume(37); Program.Check(changedAudio.Available && changedAudio.Level == 37 && !changedAudio.Muted, "native exact level writes and reads actual null sink");
            input.Media("General", "mute"); Program.Check(audio.GetVolume().Muted, "native General mute toggles isolated system output");
            input.Media("General", "volumeUp"); Program.Check(audio.GetVolume() is { Level: 42, Muted: false }, "General volume step unmutes/read back");
            audio.SetVolume(originalAudio.Level);
            var desktop = Path.Combine(scratch, "disposable.desktop");
            File.WriteAllText(desktop, "[Desktop Entry]\nType=Application\nName=Disposable no-op\nExec=/usr/bin/true %U\nTerminal=false\n");
            var app = new LauncherApp(new string('a', 32), "Disposable no-op", desktop, "General");
            Program.Check(new LinuxLauncherPlatform().Available(app) && !audio.Launch(app).Focused, "native GIO configured entry launches isolated no-op; no focus claim");
            foreach (var exec in new[] { "/bin/sh -c true", "/usr/bin/env true", "/usr/bin/true --arbitrary-command", "/usr/bin/python3" })
            {
                File.WriteAllText(desktop, "[Desktop Entry]\nType=Application\nName=Denied\nExec=" + exec + "\nTerminal=false\n");
                Program.Check(!new LinuxLauncherPlatform().Available(app), "launcher rejects interpreter/wrapper/arbitrary arguments " + exec.Split(' ')[0]);
            }
            File.WriteAllText(desktop, "[Desktop Entry]\nType=Application\nName=Denied\nExec=/usr/bin/true\nTerminal=true\n"); Program.Check(!new LinuxLauncherPlatform().Available(app), "terminal launcher refused");
            foreach (var profile in MediaProfiles.Actions.Where(p => p.Key != "General")) foreach (var action in profile.Value) input.Media(profile.Key, action);
            state = await Read(); Program.Check(int.Parse(state[2]) == 0, "all native player chords release modifiers on private GTK target (not player acceptance)");
            var now = DateTimeOffset.UtcNow;
            using var controller = new InputController(input, new(), () => now, false);
            var ownership = new SessionOwnership(); ownership.ReleaseInput += controller.ReleaseAll;
            foreach (var reason in new[] { "pause", "revoke", "disconnect", "takeover", "replacement", "exit" })
            {
                ownership.Pause(false);
                var reservation = ownership.Reserve("native-disposable", false);
                var session = ownership.Connect("native-disposable", reservation.Ticket)!;
                controller.Execute(new(1, "pointer.button", Button:"left", Action:"down")); controller.Execute(new(2,"key.down",Key:"Shift"));
                state = await Read(); Program.Check(int.Parse(state[1]) != 0 && int.Parse(state[2]) != 0, reason + " precondition native hold");
                switch (reason)
                {
                    case "pause": ownership.Pause(true); break;
                    case "revoke": ownership.Revoke("native-disposable"); break;
                    case "disconnect": ownership.End(session); break;
                    case "takeover": ownership.Reserve("replacement-disposable", true); ownership.EndCurrent(); break;
                    default: ownership.Shutdown(); break;
                }
                state = await Read(); Program.Check(session.Stop.IsCancellationRequested && int.Parse(state[1]) == 0 && int.Parse(state[2]) == 0, reason + " Core ownership interruption native cleanup");
            }
            controller.Execute(new(3,"key.down",Key:"Control")); now += TimeSpan.FromMilliseconds(1300);
            Program.Check(controller.CheckWatchdog(), "watchdog expired"); state = await Read(); Program.Check(int.Parse(state[2]) == 0, "native watchdog key release");
            input.Button("left", true); input.Key("Shift", true); await Read();
            Program.Check(pc_linux_test_unfocus() == 1, "disposable target focus removed");
            Program.Fails(() => input.Move(10, 0), "focus loss blocks new native input");
            input.ReleaseAll(); state = await Read(); Program.Check(int.Parse(state[1]) == 0 && int.Parse(state[2]) == 0, "focus loss permits only tracked cleanup ups");
        }
        finally
        {
            try { input.ReleaseAll(); } finally { LinuxNative.pc_linux_close(); if (!target.HasExited) target.Kill(); await target.WaitForExitAsync(); GC.KeepAlive(callback); }
        }
    }
}
