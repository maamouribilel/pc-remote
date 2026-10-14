using PcController.MacOS;
using PcController.Core;
using PcController.Host;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

if (args.Length == 3 && args[0] == "--restart-storage")
{
    var restarted = new MacStorage(args[1], args[2]);
    if (Encoding.UTF8.GetString(restarted.Read(Path.Combine(args[1], "value.protected"))) != "disposable-storage-value") throw new Exception("Restart persistence failed");
    return;
}
if (args.Length == 2 && args[0] == "--serve-tls") { await MacTlsFixture.Run(args[1]); return; }
if (args.Contains("--onboard")) { MacPermissions.OpenAccessibilitySettings(); return; }
var passed = 0;
void Check(bool success, string name) { if (!success) throw new Exception("FAIL: " + name); passed++; Console.WriteLine("PASS: " + name); }
void Fails(Action action, string name) { try { action(); throw new Exception("Expected failure: " + name); } catch (InputFailureException) { Check(true, name); } }
var events = new FaultEvents(); var input = new MacInput(events);
input.Key("Windows", true); input.Key("Windows", true); input.Press("ArrowLeft"); input.Key("Windows", false);
Check(events.Log.Count(l => l == "key:55:1:0:1048576") == 1 && events.Log.Contains("key:123:1:0:1048576") && events.Log.Contains("key:55:0:0:0"), "Command modifier/idempotency and native navigation mapping");
events.Log.Clear(); input.Button("left", true); input.Move(3, 4); input.ReleaseAll(); input.ReleaseAll();
Check(events.Log.Contains("move:3:4:1") && events.Log.Count(l => l.StartsWith("button:0:0")) == 1, "drag events and idempotent tracked button cleanup");
events.Log.Clear(); input.Click("right", true);
Check(events.Log.SequenceEqual(new[] { "button:1:1:1", "button:1:0:1", "button:1:1:2", "button:1:0:2" }), "right double click carries Quartz click count");
events.Log.Clear(); input.Scroll(5, 5); input.Scroll(5, 5);
Check(events.Log.SequenceEqual(new[] { "scroll:1:1" }), "normalized fractional detents convert to pixels");
events.Log.Clear(); input.Text("é😀\r\nx");
Check(events.Log.Contains("unicode:233:1") && events.Log.Contains("unicode:55357,56832:1") && events.Log.Contains("key:36:1:0:0"), "Unicode scalar pairs and CRLF normalization");
events.Log.Clear(); input.Shortcut("copy");
Check(events.Log.Contains("key:8:1:0:1048576") && events.Log.Last() == "key:55:0:0:0", "logical copy resolves layout and releases Command");
events.LayoutMissing = true; Fails(() => input.Shortcut("paste"), "unresolvable layout fails without a held Command"); events.LayoutMissing = false;
foreach (var action in new[] { "paste", "selectAll", "undo", "switchApp" }) input.Shortcut(action);
events.Log.Clear(); events.FailNext = true; Fails(() => input.Key("Alt", true), "uncertain down tracked before submission");
events.FailNext = true; Fails(input.ReleaseAll, "failed cleanup preserves possible held modifier");
input.ReleaseAll(); Check(events.Log.Count(l => l == "key:58:0:0:0") == 2, "cleanup retries native modifier up");
events.Log.Clear(); events.FailUnicodeUp = true; Fails(() => input.Text("😀"), "partial Unicode failure retains packet"); events.FailUnicodeUp = false; input.ReleaseAll();
Check(events.Log.Count(l => l == "unicode:55357,56832:0") == 2, "Unicode up cleanup retry");
events.Log.Clear(); var now = DateTimeOffset.UtcNow;
using (var controller = new InputController(input, new(), () => now, false, MacHostFactory.Capabilities(true)))
{
    controller.Execute(new(1, "key.down", Key: "ArrowDown"));
    for (var i = 0; i < 10; i++) controller.Execute(new(i + 2, "key.down", Key: "ArrowDown"));
    Fails(() => controller.Execute(new(12, "key.down", Key: "ArrowDown")), "bounded repeat excess cleans up");
    controller.Execute(new(13, "pointer.button", Button: "left", Action: "down")); now = now.AddMilliseconds(1201);
    Check(controller.CheckWatchdog(), "held-input expiry uses Mac adapter cleanup");
}
Check(MacHostFactory.Capabilities(true).Volume && MacHostFactory.Capabilities(true).Launcher && MacHostFactory.Capabilities(true).MediaActions.Count == 4 && MacHostFactory.Capabilities(true).PowerActions.Count == 0, "native feature adapters declared; power requires live OS inspection");
Check(MacHostFactory.Capabilities(false).MediaActions.Count == 0 && !MacHostFactory.Capabilities(false).Pointer, "missing permission disables input and media independently");
input = new MacInput(events); events.Log.Clear();
events.Focused = false; Fails(() => input.Media("VLC", "volumeUp"), "wrong foreground player blocks media before insertion"); Check(events.Log.Count == 0, "focus rejection posts nothing"); events.Focused = true;
input.Media("VLC", "mute"); Check(events.Log.Contains("key:58:1:0:1572864") && events.Log.Contains("key:125:0:0:1572864") && events.Log.Last() == "key:55:0:0:0", "VLC Mac mute uses Command Option Down with complete cleanup");
foreach (var profile in MediaProfiles.Actions.Where(p => p.Key != "General")) foreach (var action in profile.Value) { input.Media(profile.Key, action); Check(events.Log.Last().Contains(":0:0:"), "player shortcut mapped/released " + profile.Key + ":" + action); }
events.Log.Clear(); input.Media("General", "playPause"); Check(events.Log.SequenceEqual(new[] { "system:16:1", "system:16:0" }), "General playback uses native system events");
events.FailNext = true; Fails(() => input.Media("General", "next"), "uncertain native media down is retained"); events.FailNext = true; Fails(input.ReleaseAll, "media cleanup failure remains tracked"); input.ReleaseAll(); Check(events.Log.Count(l => l == "system:17:0") == 2, "native media up retried before Resume");
Fails(() => input.Media("Stremio", "previous"), "unsupported player action denied natively");
input.Key("Shift", true); Fails(() => input.Media("YouTube", "next"), "held keyboard blocks player media"); input.ReleaseAll();
input.Button("left", true); var mediaBefore = events.Log.Count(l => l.StartsWith("system:")); Fails(() => input.Media("General", "playPause"), "held drag cannot enter a blocking native media action"); input.ReleaseAll(); Check(events.Log.Count(l => l.StartsWith("system:")) == mediaBefore, "media rejection submits no global playback event during drag");
Check(PcController.Host.DesktopMediaBindings.Chord("macos", "VLC", "next").SequenceEqual(new[] { "Windows", "ArrowRight" }) && PcController.Host.DesktopMediaBindings.Chord("linux", "VLC", "next").SequenceEqual(new[] { "n" }), "VLC OS defaults remain independent");

if (OperatingSystem.IsMacOS())
{
    Check(MacNative.pc_test_layout_key("com.apple.keylayout.US", 'a') == 0 && MacNative.pc_test_layout_key("com.apple.keylayout.US", 'c') == 8, "ACTUAL US Command layout table resolution (no input source change)");
    Check(MacNative.pc_test_layout_key("com.apple.keylayout.French", 'a') == 12 && MacNative.pc_test_layout_key("com.apple.keylayout.French", 'c') == 8, "ACTUAL French AZERTY Command layout table resolution (no native app insertion claimed)");
    Check(new[] { 'c', 'v', 'a', 'z' }.All(c => MacNative.pc_command_key(c) >= 0), "ACTUAL current keyboard layout resolves common Command actions");
    // Host setup and phone shortcuts resolve keys on worker threads. On current macOS the Text Input
    // Source APIs trap off the main queue and never return, so lookups must come from the main-thread cache.
    var (mainCommand, mainPlain) = (MacNative.pc_command_key('c'), MacNative.pc_layout_key('k'));
    var worker = Task.Run(() => (MacNative.pc_command_key('c'), MacNative.pc_layout_key('k'), MacInput.ShortcutsAvailable));
    Check(worker.Wait(TimeSpan.FromSeconds(5)) && worker.Result == (mainCommand, mainPlain, true), "keyboard layout lookups return the main-thread result from a worker thread without trapping");
    // On current macOS the system-wide AX focus query can fail (kAXErrorCannotComplete) for normal
    // front apps; an AX-trusted, active console session without Secure Input must still be interactive.
    var diagnostics = MacNative.pc_diagnostics();
    // A real local desktop check: hosted runners' focus state varies between runs (no ordinary
    // frontmost app, no AX focus value), which the session check correctly treats as not interactive.
    if (Environment.GetEnvironmentVariable("CI") != "true" && (diagnostics & 1) != 0 && (diagnostics & 4) != 0 && (diagnostics & 8) == 0 && (diagnostics & 32) == 0 && MacNative.pc_test_frontmost_regular() == 1)
        Check(MacNative.pc_session() == 1, $"active console session is interactive (system-wide AX focus query {((diagnostics & 16) != 0 ? "succeeded" : "could not complete")})");
    else Console.WriteLine("SKIP: interactive-session check needs a local AX-trusted active console session without Secure Input and an ordinary frontmost app");
    Check((MacNative.pc_session_state() == 0) == (MacNative.pc_session() == 1), $"session state reason {MacNative.pc_session_state()} agrees with the interactive check (focus query {MacNative.pc_focus_status()})");
    Check(MacPermissions.SessionBlocked(3, 0).Contains("Secure Input") && !MacPermissions.SessionBlocked(3, -25204).Contains("error") && MacPermissions.SessionBlocked(6, -25211).Contains("error -25211") && MacPermissions.SessionBlocked(2, 0).Contains("quit and reopen"),
        "session block messages name the reason and only unexpected focus-query errors");
    var (inputBefore, networkBefore) = (MacNative.pc_generation(), MacNative.pc_network_generation());
    MacNative.pc_test_notify(0);
    Check(MacNative.pc_generation() > inputBefore && MacNative.pc_network_generation() == networkBefore, "native Space/display event pauses input without network-trust change");
    MacNative.pc_test_notify(1);
    Check(MacNative.pc_generation() > inputBefore + 1 && MacNative.pc_network_generation() > networkBefore, "native network/sleep/session event advances both input and network-trust generations");
    // Wi-Fi LinkQuality changes every few seconds; it must not revoke LAN approval (it did before 0.3.5).
    Check(MacNative.pc_test_network_key("State:/Network/Interface/en0/LinkQuality") == 0 && MacNative.pc_test_network_key("State:/Network/Interface/en0/AirPort") == 1
        && MacNative.pc_test_network_key("State:/Network/Global/IPv4") == 1, "Wi-Fi signal-quality updates keep LAN approval; a first AirPort record and address changes still revoke it");
    // The AirPort record also changes for scans, AWDL, Bluetooth audio, auto-join timestamps and access-point roaming; only a changed network name revokes.
    Check(MacNative.pc_test_airport("State:/Network/Interface/test0/AirPort", "home") == 1 && MacNative.pc_test_airport("State:/Network/Interface/test0/AirPort", "home") == 0
        && MacNative.pc_test_airport("State:/Network/Interface/test0/AirPort", "office") == 1 && MacNative.pc_test_airport("State:/Network/Interface/test0/AirPort", "office") == 0,
        "AirPort record updates keep LAN approval unless the joined network name changed");
    Check(MacNative.pc_test_airport_key("State:/Network/Interface/en0/AirPort") == 1 && MacNative.pc_test_airport_key("State:/Network/Interface/en0/AirPort/BSSID") == 1
        && MacNative.pc_test_airport_key("State:/Network/Interface/en0/AirPort/AutoJoinTimestamp") == 1 && MacNative.pc_test_airport_key("State:/Network/Interface/en0/IPv4") == 0
        && MacNative.pc_test_airport_key("State:/Network/Global/IPv4") == 0 && MacNative.pc_test_airport_key("Setup:/Network/Interface/en0/AirPort") == 0,
        "every AirPort record field key is judged by the association; address, global and setup keys still revoke");
    var lockDirectory = Path.Combine("/private/tmp", "PcMacLock-" + Guid.NewGuid().ToString("N"));
    try
    {
        using (var first = SingleInstance.TryAcquire(lockDirectory))
        {
            Check(first != null && File.GetUnixFileMode(Path.Combine(lockDirectory, "instance.lock")) == (UnixFileMode.UserRead | UnixFileMode.UserWrite), "instance lock is 0600 although the variadic open mode is unreliable on arm64");
            using var second = SingleInstance.TryAcquire(lockDirectory);
            Check(second == null, "second Mac instance is refused while the lock is held");
        }
        using var restarted = SingleInstance.TryAcquire(lockDirectory);
        Check(restarted != null, "released Mac instance lock can be acquired again");
    }
    finally { Directory.Delete(lockDirectory, true); }
}
if (args.Contains("--storage") || args.Contains("--storage-only"))
{
    if (!OperatingSystem.IsMacOS()) throw new PlatformNotSupportedException();
    var directory = Path.Combine("/private/tmp", "PcMacStorage-" + Guid.NewGuid().ToString("N"));
    var account = "test-" + Guid.NewGuid().ToString("N"); var storage = new MacStorage(directory, account);
    try
    {
        storage.Probe();
        var dataPath = Path.Combine(directory, "value.protected"); var clear = Encoding.UTF8.GetBytes("disposable-storage-value"); storage.Write(dataPath, clear);
        Check(!File.ReadAllBytes(dataPath).AsSpan().SequenceEqual(clear) && storage.Read(dataPath).SequenceEqual(clear), "real Keychain backed encrypted file roundtrip");
        Check(new MacStorage(directory, account).Read(dataPath).SequenceEqual(clear), "real Keychain new storage instance persistence");
        var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false };
        if (Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(FaultEvents).Assembly.Location);
        foreach (var value in new[] { "--restart-storage", directory, account }) start.ArgumentList.Add(value);
        using var child = System.Diagnostics.Process.Start(start)!; await child.WaitForExitAsync();
        Check(child.ExitCode == 0, "real Keychain persistence after process restart");
        var keychainPath = "/private/tmp/PcMacKeychain-" + Guid.NewGuid().ToString("N") + ".keychain";
        var password = RandomNumberGenerator.GetBytes(32);
        try
        {
            Check(MacNative.pc_test_keychain_create(keychainPath, password, password.Length) == 0, "create disposable Keychain without changing default or trust");
            var isolatedDirectory = Path.Combine(directory, "isolated");
            var isolated = new MacStorage(isolatedDirectory, account, keychainPath); isolated.Probe();
            isolated.Write(Path.Combine(isolatedDirectory, "value.protected"), clear);
            // An item whose ACL trusts another code identity models an updated/rebuilt unsigned app:
            // background reads must fail promptly without a dialog, new key or replaced key.
            var foreignAccount = account + "-foreign"; var foreignDirectory = Path.Combine(directory, "foreign");
            var foreign = new MacStorage(foreignDirectory, foreignAccount, keychainPath);
            File.WriteAllBytes(Path.Combine(foreignDirectory, "existing.protected"), new byte[64]);
            var addForeign = new System.Diagnostics.ProcessStartInfo("/usr/bin/security") { UseShellExecute = false };
            foreach (var value in new[] { "add-generic-password", "-s", "local.pc-remote.storage.v1", "-a", foreignAccount, "-w", Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), "-T", "/usr/bin/true", keychainPath }) addForeign.ArgumentList.Add(value);
            using (var add = System.Diagnostics.Process.Start(addForeign)!) { await add.WaitForExitAsync(); Check(add.ExitCode == 0, "disposable item owned by another code identity"); }
            var denial = System.Diagnostics.Stopwatch.StartNew();
            try { foreign.Probe(); throw new Exception("Changed code identity read the key without consent"); }
            catch (IOException e) { Check(denial.Elapsed < TimeSpan.FromSeconds(10) && e.Message.Contains("Allow Keychain access"), "changed app identity fails closed promptly without an implicit dialog or new key"); }
            try { foreign.AuthorizeExistingKey(); throw new Exception("Test storage requested OS consent"); }
            catch (InvalidOperationException) { Check(true, "disposable test storage never opens the explicit Keychain consent dialog"); }
            Check(MacNative.pc_test_keychain_lock(keychainPath) == 0, "lock only disposable test Keychain");
            try { isolated.Read(Path.Combine(isolatedDirectory, "value.protected")); throw new Exception("Locked Keychain accepted"); }
            catch (IOException) { Check(true, "locked real Keychain fails explicitly without plaintext fallback"); }
        }
        finally { CryptographicOperations.ZeroMemory(password); MacNative.pc_test_keychain_delete(keychainPath); }
        if (OperatingSystem.IsMacOS()) Check(File.GetUnixFileMode(dataPath) == (UnixFileMode.UserRead | UnixFileMode.UserWrite), "protected file mode 0600");
        var devices = new ProtectedDevices(directory, storage); var pairing = new PairingService(devices);
        var request = pairing.Request(pairing.IssueChallenge().Value, "Disposable browser"); pairing.Decide(request.Ticket, true); var claim = pairing.Claim(request.Ticket);
        Check(new PairingService(new ProtectedDevices(directory, new MacStorage(directory, account))).Authenticate(claim.Credential!) != null, "remembered pairing persists with real Keychain");
        devices.Revoke(claim.Device!.Id); Check(new ProtectedDevices(directory, storage).List().Length == 0, "revocation persists");
        using var first = new LocalCertificates(Path.Combine(directory, "certificates"), [IPAddress.Loopback], storage, new MacCertificateKeys());
        using var second = new LocalCertificates(Path.Combine(directory, "certificates"), [IPAddress.Loopback, IPAddress.Parse("192.0.2.10")], storage, new MacCertificateKeys());
        Check(first.Fingerprint == second.Fingerprint && first.Server.Thumbprint != second.Server.Thumbprint, "Keychain certificate SAN renewal preserves root");
        using var chain = new X509Chain(); chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust; chain.ChainPolicy.CustomTrustStore.Add(second.Root); chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        Check(chain.Build(second.Server) && second.Server.MatchesHostname("localhost", false, false) && !second.Server.MatchesHostname("wrong.invalid", false, false), "real Mac certificate chain/name checks");
        if (!args.Contains("--storage-only")) await MacHostChecks.Run(directory, storage, first.Fingerprint, check: Check);
        using (var lifecycle = new MacEnvironment(new MacHostFactory(directory, storage, new MacInput())))
        {
            lifecycle.Inspect(); MacNative.pc_test_notify(0);
            var paused = lifecycle.NetworkChangedSinceInspect; var spaceState = lifecycle.Inspect();
            Check(!paused && spaceState.Changed && !spaceState.NetworkChanged, "MacEnvironment reports Space/display change as pause-only (LAN approval kept)");
            MacNative.pc_test_notify(1);
            var revoked = lifecycle.NetworkChangedSinceInspect; var networkState = lifecycle.Inspect();
            Check(revoked && networkState.Changed && networkState.NetworkChanged && !lifecycle.NetworkChangedSinceInspect, "MacEnvironment reports network-trust change synchronously and at inspection");
        }
        var rootPath = Path.Combine(directory, "certificates", "root.protected"); File.Move(rootPath, rootPath + ".saved");
        try
        {
            var factory = new MacHostFactory(directory, storage, new MacInput());
            try { await using var unexpected = factory.Create(new([], [], "missing root fixture")); throw new Exception("Missing root reset"); }
            catch (IOException) { Check(true, "missing installation CA fails instead of silently replacing phone trust"); }
        }
        finally { File.Move(rootPath + ".saved", rootPath); }
        var corrupted = File.ReadAllBytes(dataPath); corrupted[^1] ^= 1; File.WriteAllBytes(dataPath, corrupted);
        try { storage.Read(dataPath); throw new Exception("Corruption accepted"); } catch (IOException) { Check(true, "tampering fails closed"); }
        storage.DeleteTestSecret();
        try { storage.Probe(); throw new Exception("Missing key recreated"); } catch (IOException) { Check(true, "missing Keychain item with existing files fails without root reset"); }
    }
    finally { try { storage.DeleteTestSecret(); } catch (IOException) { } Directory.Delete(directory, true); }
}
if (args.Contains("--native-smoke")) await NativeSmoke.Run(Check);
Console.WriteLine($"{passed} macOS adapter checks passed.");

internal sealed class FaultEvents : IMacEvents
{
    public List<string> Log { get; } = [];
    public bool FailNext, LayoutMissing, FailUnicodeUp;
    public bool Focused = true;
    public bool PlayerFocus(string profile) => Focused;
    public int SystemKey(int code, int down, int guard) => Add($"system:{code}:{down}");
    public void GeneralVolume(string action) => Add("volume:" + action);
    public int LayoutKey(ushort character) => LayoutMissing ? -1 : character;
    private int Add(string value) { Log.Add(value); if (FailNext) { FailNext = false; return 0; } return 1; }
    public int Move(int x, int y, int drag, ulong flags, int guard) => Add($"move:{x}:{y}:{drag}");
    public int Scroll(int x, int y, ulong flags, int guard) => Add($"scroll:{x}:{y}");
    public int Button(int right, int down, int count, ulong flags, int guard) => Add($"button:{right}:{down}:{count}");
    public int Key(ushort key, int down, int repeat, ulong flags, int guard) => Add($"key:{key}:{down}:{repeat}:{flags}");
    public int Unicode(ushort[] text, int length, int down, int guard) { var result = Add($"unicode:{string.Join(',', text)}:{down}"); return FailUnicodeUp && down == 0 ? 0 : result; }
    public int CommandKey(ushort character) => LayoutMissing ? -1 : character switch { 'c' => 8, 'v' => 9, 'a' => 0, 'z' => 6, 'f' => 3, _ => -1 };
    public void Arm() { }
}
