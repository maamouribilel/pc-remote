using System.Text;
using System.Text.Json;
using PcController.Core;
internal static class KeyboardChecks
{
    internal static void Run(Action<bool, string> check)
    {
        bool Parse(string type, object payload, out ControlCommand? command) => ControlProtocol.TryParse(JsonSerializer.SerializeToUtf8Bytes(new { v = 1, seq = 1, type, payload }), 0, out command);
        check(Parse("text.insert", new { text = "été 😀" }, out _), "Unicode text protocol accepts accents and surrogate pairs");
        check(!Parse("text.insert", new { text = new string('a', 257) }, out _) && !Parse("text.insert", new { text = "a\0b" }, out _) && !Parse("text.insert", new { text = "" }, out _), "text length and control-character bounds");
        check(Parse("key.down", new { key = "ArrowLeft" }, out _) && !Parse("key.down", new { key = "F99" }, out _) && !Parse("shortcut", new { action = "runCommand" }, out _), "physical keys and shortcuts allowlisted");
        var replay = JsonSerializer.SerializeToUtf8Bytes(new { v = 1, seq = 2, type = "text.insert", payload = new { text = "sample" } });
        check(!ControlProtocol.TryParse(replay, 2, out _), "text sequence replay rejected before insertion");
        var now = DateTimeOffset.UtcNow; var input = new RecordingMouse();
        using var controller = new InputController(input, new(), () => now, false);
        controller.Execute(new(1, "key.down", Key: "Shift")); controller.Execute(new(2, "key.down", Key: "Shift"));
        check(input.Events.Count(e => e == "key:Shift:True") == 1, "modifier down idempotent");
        controller.Execute(new(3, "key.press", Key: "ArrowLeft")); controller.Execute(new(4, "key.up", Key: "Shift"));
        check(input.Events.TakeLast(2).SequenceEqual(["press:ArrowLeft", "key:Shift:False"]), "navigation preserves modifier ordering");
        controller.Execute(new(5, "key.down", Key: "ArrowDown"));
        for (var i = 0; i < 10; i++) controller.Execute(new(6 + i, "key.down", Key: "ArrowDown"));
        bool limited = false; try { controller.Execute(new(16, "key.down", Key: "ArrowDown")); } catch (InputFailureException) { limited = true; }
        check(limited && input.Events.Last() == "release", "key repeats bounded and excess releases input");
        controller.Execute(new(17, "key.down", Key: "Control")); now = now.AddMilliseconds(1201);
        check(controller.CheckWatchdog() && input.Events.Last() == "release", "key watchdog clears interrupted modifier");
        controller.Execute(new(18, "text.insert", Text: "sample"));
        check(input.Events.Last() == "text-length:6" && !input.Events.Any(e => e.Contains("sample")), "test recording adapter stores only text length");
        bool textLimited = false; try { controller.Execute(new(19, "text.insert", Text: "next")); } catch (InputFailureException) { textLimited = true; }
        check(textLimited, "text insertion rate bound");
        controller.Execute(new(20, "shortcut", Action: "paste")); check(input.Events.Last() == "shortcut:paste", "paste is a declared PC shortcut without clipboard transfer");
        controller.Execute(new(21, "key.down", Key: "Alt")); controller.ReleaseAll();
        check(input.Events.Last() == "release", "releaseAll clears held keyboard and mouse state");
    }
}
