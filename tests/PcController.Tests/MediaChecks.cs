using System.Text.Json;
using PcController.Core;
using PcController.Windows;
internal static class MediaChecks
{
    internal static void Run(Action<bool, string> check)
    {
        bool Parse(string profile, string action) => ControlProtocol.TryParse(JsonSerializer.SerializeToUtf8Bytes(new { v = 1, seq = 1, type = "media.action", payload = new { profile, action } }), 0, out _);
        check(Parse("General", "playPause") && !Parse("General", "seekForward") && !Parse("General", "fullscreen"), "General profile rejects unsupported seek/fullscreen");
        check(!Parse("Shell", "playPause") && !Parse("VLC", "execute"), "media profile and action allowlists");
        check(Parse("Stremio", "next") && Parse("Stremio", "mute") && !Parse("Stremio", "previous"), "Stremio supports next episode and rejects unsupported previous");
        check(MediaProfiles.Mappings["Stremio"]["next"].SequenceEqual(["Shift", "N"]) && MediaProfiles.Mappings["Stremio"]["seekBack"].SequenceEqual(["ArrowLeft"]), "Stremio next episode and seek shortcuts");
        check(MediaProfiles.Mappings["YouTube"]["previous"].SequenceEqual(["Shift", "P"]) && MediaProfiles.Mappings["VLC"]["volumeUp"].SequenceEqual(["Control", "ArrowUp"]), "verified application-specific media chords");
        var now = DateTimeOffset.UtcNow; var input = new RecordingMouse();
        using var controller = new InputController(input, new(), () => now, false);
        foreach (var profile in MediaProfiles.Mappings)
        {
            foreach (var action in profile.Value)
            {
                now = now.AddSeconds(1);
                controller.Execute(new(1, "media.action", Action: action.Key, Profile: profile.Key));
                check(input.Events.Last() == "chord:" + string.Join("+", action.Value), "declarative media mapping: " + profile.Key + "/" + action.Key);
            }
        }
        now = now.AddSeconds(1);
        for (var i = 0; i < 10; i++) controller.Execute(new(1, "media.action", Action: "mute", Profile: "General"));
        bool limited = false; try { controller.Execute(new(1, "media.action", Action: "mute", Profile: "General")); } catch (InputFailureException) { limited = true; }
        check(limited && input.Events.Last() == "release", "media action rate bounded with cleanup");
        var directory = Path.Combine(Path.GetTempPath(), "PcMediaTests", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try { var settings = new MediaSettings(directory); settings.Save("Stremio"); check(new MediaSettings(directory).Profile == "Stremio", "media profile persists across reload"); }
        finally { Directory.Delete(directory, true); }
    }
}
