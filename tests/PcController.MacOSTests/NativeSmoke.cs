using System.Diagnostics;
using System.Text.Json;
using PcController.Core;
using PcController.Host;
namespace PcController.MacOS;
internal static class NativeSmoke
{
    public static async Task Run(Action<bool, string> check)
    {
        if (!OperatingSystem.IsMacOS()) throw new PlatformNotSupportedException();
        Console.WriteLine($"Native readiness bits (AX=1, post=2, console=4, secure-input=8, focused-AX=16, inactive=32): {MacNative.pc_diagnostics()}");
        if (!MacPermissions.Accessibility)
        { Console.WriteLine("UNAVAILABLE: native smoke requires Accessibility for this test runner. No events sent."); return; }
        var directory = "/private/tmp/PcMacSmoke-" + Guid.NewGuid().ToString("N"); System.IO.Directory.CreateDirectory(directory);
        var targetPath = Path.Combine(AppContext.BaseDirectory, "Native Target.app", "Contents", "MacOS", "NativeTarget");
        if (!File.Exists(targetPath)) throw new IOException("Build the test-only NativeTarget with scripts/portable.py mac-smoke.");
        using var target = Process.Start(new ProcessStartInfo("/usr/bin/open") { ArgumentList = { "-n", "-W", Path.GetFullPath(Path.Combine(targetPath, "../../..")), "--args", directory } })!;
        MacInput? input = null; int targetPid = 0; var storage = new MacStorage(directory, "test-" + Guid.NewGuid().ToString("N"));
        CompanionServer? server = null;
        try
        {
            for (var i = 0; i < 100; i++)
            {
                if (target.HasExited) throw new IOException("Disposable target failed to start.");
                if (File.Exists(Path.Combine(directory, "state.json")))
                {
                    using var ready=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory, "state.json")));
                    targetPid=ready.RootElement.GetProperty("pid").GetInt32();
                    if (MacNative.pc_frontmost()==targetPid && MacPermissions.Interactive) break;
                }
                await Task.Delay(50);
            }
            input = new MacInput(targetPid);
            void Focus() { if (target.HasExited || targetPid==0 || MacNative.pc_frontmost() != targetPid || !MacPermissions.Interactive) throw new InputFailureException("ABORT: disposable target focus lost. Only tracked releases are permitted."); }
            JsonElement State() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory, "state.json"))).RootElement.Clone();
            async Task Settle() { await Task.Delay(200); Focus(); }
            Focus(); MacNative.pc_pointer(out var x, out var y); var state = State(); input.Move((int)(state.GetProperty("x").GetDouble()-x), (int)(state.GetProperty("y").GetDouble()-y));
            await Settle(); input.Click("left", false); input.Text("é😀 native"); await Settle();
            check(State().GetProperty("text").GetString()!.Contains("é😀 native"), "ACTUAL Quartz Unicode received in disposable Cocoa text view");
            input.Press("ArrowLeft"); input.Press("Backspace"); input.Text("Z"); await Settle();
            check(State().GetProperty("text").GetString()!.Contains("Z"), "ACTUAL navigation/backspace/text insertion");
            var original = State().GetProperty("text").GetString()!;
            input.Shortcut("selectAll"); await Settle();
            check(State().GetProperty("selectionLength").GetInt32()==original.Length, "ACTUAL Command selectAll selects the full disposable text");
            input.Shortcut("copy"); await Settle(); input.Text("replacement"); await Settle();
            check(State().GetProperty("text").GetString()=="replacement", "ACTUAL text replaces the selected disposable content");
            input.Shortcut("selectAll"); await Settle(); input.Shortcut("paste"); await Settle();
            check(State().GetProperty("text").GetString()==original && State().GetProperty("counts").GetProperty("copy").GetInt32()>0 && State().GetProperty("counts").GetProperty("paste").GetInt32()>0, "ACTUAL Command copy/paste restores target-private scratch clipboard (text=" + State().GetProperty("text").GetString() + "; expected=" + original + "; clipboard=" + State().GetProperty("clipboard").GetString() + "; counts=" + State().GetProperty("counts").GetRawText() + ")");
            input.Shortcut("undo"); await Settle();
            check(State().GetProperty("counts").GetProperty("undo").GetInt32()>0 && State().GetProperty("text").GetString()!=original, "ACTUAL Command undo changes disposable target text"); input.Click("right", false); input.Click("left", true); input.Scroll(120,120); input.Button("left",true); MacNative.pc_pointer(out var beforeX, out var beforeY); input.Move(5,5); input.Key("Shift",true); input.ReleaseAll(); await Settle();
            MacNative.pc_pointer(out var afterX, out var afterY);
            check(Math.Abs(afterX-beforeX-5)<1 && Math.Abs(afterY-beforeY-5)<1, "ACTUAL relative pointer displacement in Quartz coordinates");
            var counts=State().GetProperty("counts");
            check(counts.TryGetProperty("3",out var right) && right.GetInt32()>0 && counts.GetProperty("maxClick").GetInt32()==2, "ACTUAL right click and double-click count received");
            check(counts.TryGetProperty("drag",out var drag) && drag.GetInt32()>0 && counts.TryGetProperty("scroll",out var scroll) && scroll.GetInt32()>0, "ACTUAL dragged/scroll events received by disposable target (counts=" + counts.GetRawText() + ")");
            check(counts.TryGetProperty("2",out var up) && up.GetInt32()>0 && (State().GetProperty("flags").GetUInt64() & ((1UL<<17)|(1UL<<20)))==0, "ACTUAL button/modifier cleanup observed");
            var factory = new MacHostFactory(directory, storage, input);
            server = factory.Create(new([], [], "Native smoke loopback only")); await server.StartAsync(); server.Sessions.Pause(false);
            ControlSession Own(string device) { var reservation=server.Sessions.Reserve(device,true); return server.Sessions.Connect(device,reservation.Ticket)!; }
            async Task Hold(ControlSession owner)
            {
                Focus(); server.Sessions.Execute(owner, () => { server.Input.Execute(new(1,"pointer.button",Button:"left",Action:"down")); server.Input.Execute(new(2,"key.down",Key:"Shift")); });
                await Settle(); check(State().GetProperty("leftHeld").GetBoolean() && State().GetProperty("shiftHeld").GetBoolean(), "ACTUAL target receives held button and Shift before interruption");
            }
            bool Released() => !State().GetProperty("leftHeld").GetBoolean() && !State().GetProperty("shiftHeld").GetBoolean();
            var owner=Own("scratch"); await Hold(owner); server.Sessions.Pause(true); await Settle(); check(Released() && server.Sessions.OwnerDeviceId==null, "ACTUAL native held input released on host pause");
            server.Sessions.Pause(false); owner=Own("scratch"); await Hold(owner); server.RevokeDevice("scratch"); await Settle(); check(Released() && owner.Stop.IsCancellationRequested, "ACTUAL native held input released on revoke");
            owner=Own("scratch"); await Hold(owner); server.Sessions.End(owner); await Settle(); check(Released() && owner.Stop.IsCancellationRequested, "ACTUAL native held input released on disconnect ownership end");
            owner=Own("scratch"); await Hold(owner); var replacement=Own("other"); await Settle(); check(Released() && owner.Stop.IsCancellationRequested, "ACTUAL native cleanup before takeover");
            await Hold(replacement); await Task.Delay(1600); Focus(); check(Released() && server.Sessions.OwnerDeviceId==null, "ACTUAL native watchdog expiry clears ownership");
            owner=Own("scratch"); await Hold(owner); var fingerprint=server.Certificates.Fingerprint; await server.DisposeAsync(); server=null; await Settle();
            server=factory.Create(new([], [], "Native smoke loopback only")); await server.StartAsync(); check(Released() && server.Certificates.Fingerprint==fingerprint && server.Sessions.Paused, "ACTUAL host restart retains Keychain CA and starts paused");
            await SwitchApp.Run(input, targetPid, directory, check);
            Focus();
            check(MacNative.pc_player_focus("VLC") == 0 && MacNative.pc_player_focus("YouTube") == 0 && MacNative.pc_player_focus("Stremio") == 0, "ACTUAL unrelated disposable Cocoa bundle rejected by native player focus guard");
            var mediaInput = new MacInput(new GuardedPlayerEvents(targetPid), targetPid);
            var keysBefore = State().GetProperty("counts").GetProperty("10").GetInt32();
            foreach (var profile in MediaProfiles.Actions.Where(p => p.Key != "General")) foreach (var action in profile.Value) { Focus(); mediaInput.Media(profile.Key, action); }
            mediaInput.ReleaseAll(); await Settle();
            check(State().GetProperty("counts").GetProperty("10").GetInt32() >= keysBefore + 26 && (State().GetProperty("flags").GetUInt64() & ((1UL << 17) | (1UL << 18) | (1UL << 19) | (1UL << 20))) == 0,
                "ACTUAL player chords reach guarded Cocoa target and release modifiers; not installed-player playback acceptance");
            Console.WriteLine("PENDING: native alternate layouts, permission revocation, physical lock/suspend, phone/LAN; no active-work or trust changes performed.");
        }
        finally
        {
            // Focus loss aborts test actions. Only up events for tracked remote-owned
            // input are allowed during final cleanup; no new input goes to another app.
            if (server != null) await server.DisposeAsync();
            try { input?.ReleaseAll(); } catch (InputFailureException) { Console.WriteLine("PENDING: cleanup was refused after permission loss; restore Accessibility and verify held input locally. This run is not a passed cleanup test."); }
            File.WriteAllText(Path.Combine(directory,"stop"),"stop");
            if (!target.HasExited) { using var timeout=new CancellationTokenSource(3000); try { await target.WaitForExitAsync(timeout.Token); } catch(OperationCanceledException) { target.Kill(); await target.WaitForExitAsync(); } }
            try { storage.DeleteTestSecret(); } catch(IOException) { }
            System.IO.Directory.Delete(directory,true);
        }
    }
    // Test-only focus substitution confines chord receipt to this explicit disposable target.
    private sealed class GuardedPlayerEvents(int pid) : IMacEvents
    {
        private readonly NativeMacEvents native = new();
        public bool PlayerFocus(string profile) => MacNative.pc_frontmost() == pid;
        public int Move(int x, int y, int drag, ulong flags, int guard) => native.Move(x, y, drag, flags, pid);
        public int Scroll(int x, int y, ulong flags, int guard) => native.Scroll(x, y, flags, pid);
        public int Button(int right, int down, int count, ulong flags, int guard) => native.Button(right, down, count, flags, pid);
        public int Key(ushort key, int down, int repeat, ulong flags, int guard) => native.Key(key, down, repeat, flags, pid);
        public int Unicode(ushort[] text, int length, int down, int guard) => native.Unicode(text, length, down, pid);
        public int CommandKey(ushort character) => native.CommandKey(character);
        public int LayoutKey(ushort character) => native.LayoutKey(character);
    }

}
