using System.Text;
using PcController.Core;
using PcController.Windows;
internal sealed class RecordingMouse : IMouseInput, IKeyboardInput, IMediaInput
{
    private readonly object gate = new();
    private readonly List<string> events = [];
    public bool FailNext { get; set; }
    public string[] Events { get { lock (gate) return events.ToArray(); } }
    private void Add(string value) { lock (gate) { if (FailNext) { FailNext = false; throw new InputFailureException(); } events.Add(value); } }
    public void Move(int x, int y) => Add($"move:{x},{y}");
    public void Scroll(int x, int y) => Add($"scroll:{x},{y}");
    public void Button(string button, bool down) => Add($"{button}:{down}");
    public void Click(string button, bool twice) => Add($"click:{button}:{twice}");
    public void ReleaseAll() => Add("release");
    public void Key(string key, bool down) => Add($"key:{key}:{down}");
    public void Press(string key) => Add($"press:{key}");
    public void Text(string text) => Add("text-length:" + text.Length); // Never record typed text.
    public void Shortcut(string action) => Add("shortcut:" + action);
    public void Media(string profile, string action) => Chord(WindowsMediaMappings.Mappings[profile][action]);
    public void Chord(IReadOnlyList<string> keys) => Add("chord:" + string.Join("+", keys));
}
internal static class MouseChecks
{
    internal static void Run(Action<bool, string> check)
    {
        bool Parse(string type, string payload, out ControlCommand? command) => ControlProtocol.TryParse(Encoding.UTF8.GetBytes($"{{\"v\":1,\"seq\":1,\"type\":\"{type}\",\"payload\":{payload}}}"), 0, out command);
        check(Parse("pointer.move", "{\"dx\":-500,\"dy\":0.25}", out _), "bounded fractional relative movement accepted");
        check(Parse("pointer.scroll", "{\"dx\":1200,\"dy\":-1200}", out _), "bounded wheel deltas accepted");
        foreach (var payload in new[] { "{\"dx\":501,\"dy\":0}", "{\"dx\":1e999,\"dy\":0}", "{\"dx\":\"1\",\"dy\":0}", "{\"dx\":0,\"dx\":0,\"dy\":0}", "{\"dx\":0,\"dy\":0,\"extra\":1}" })
            check(!Parse("pointer.move", payload, out _), "reject invalid numeric or duplicate mouse payload");
        check(!Parse("pointer.button", "{\"button\":\"middle\",\"action\":\"click\"}", out _) && !Parse("pointer.button", "{\"button\":\"left\",\"action\":\"execute\"}", out _), "mouse button/action allowlist");
        check(Parse("input.releaseAll", "{}", out _) && !Parse("media.action", "{}", out _), "release allowed and later milestone commands rejected");
        var now = DateTimeOffset.UtcNow; var input = new RecordingMouse();
        using var controller = new InputController(input, new(), () => now, false);
        controller.Execute(new(1, "pointer.move", .4, .4)); controller.Execute(new(2, "pointer.move", .7, -.4));
        check(input.Events.SequenceEqual(["move:1,0"]), "fractional motion accumulates without directional drift");
        controller.Execute(new(3, "pointer.button", Button: "left", Action: "down")); controller.Execute(new(4, "pointer.button", Button: "left", Action: "down"));
        check(input.Events.Count(e => e == "left:True") == 1, "duplicate button down suppressed");
        now = now.AddMilliseconds(1100); check(!controller.CheckWatchdog(), "watchdog respects live hold lease");
        controller.Execute(new(5, "ping")); now = now.AddMilliseconds(1199); check(!controller.CheckWatchdog(), "held button heartbeat refreshes lease");
        now = now.AddMilliseconds(2); check(controller.CheckWatchdog() && input.Events.Last() == "release", "watchdog releases held input after 1200ms");
        controller.Configure(new(2, false)); controller.Execute(new(6, "pointer.move", 10, -10));
        check(input.Events.Last() == "move:20,-20", "pointer sensitivity applied");
        controller.Execute(new(7, "pointer.scroll", 120, 120)); check(input.Events.Last() == "scroll:120,-120", "standard scroll signs map Windows wheel convention");
        controller.Configure(new(1, true)); controller.Execute(new(8, "pointer.scroll", 120, 120)); check(input.Events.Last() == "scroll:-120,120", "natural scrolling follows finger direction");
        input.FailNext = true; bool failed = false;
        try { controller.Execute(new(9, "pointer.button", Button: "right", Action: "down")); } catch (InputFailureException) { failed = true; }
        check(failed && input.Events.Last() == "release", "input insertion failure triggers immediate cleanup");
        var sessions = new SessionOwnership(); sessions.ReleaseInput += controller.ReleaseAll;
        var owner = sessions.Reserve("test", false); sessions.Connect("test", owner.Ticket); controller.Execute(new(10, "pointer.button", Button: "left", Action: "down")); sessions.End(owner.Session);
        check(input.Events.Last() == "release", "disconnect ownership releases adapter");
        owner = sessions.Reserve("test", false); sessions.Connect("test", owner.Ticket); controller.Execute(new(11, "pointer.button", Button: "left", Action: "down")); input.FailNext = true;
        bool paused = false; try { sessions.Reserve("replacement", true); } catch (InvalidOperationException e) { paused = e.Message == "paused"; }
        check(paused && sessions.Paused && owner.Session.Stop.IsCancellationRequested && sessions.OwnerDeviceId == null, "release failure blocks takeover and pauses control");
    }
}
