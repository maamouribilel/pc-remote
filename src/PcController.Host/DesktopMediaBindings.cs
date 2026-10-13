using PcController.Core;
namespace PcController.Host;

// Documented player defaults, resolved by the native adapter in the active layout.
// These strings are internal logical keys, never wire-supplied chords or commands.
public static class DesktopMediaBindings
{
    public static string[] Chord(string platform, string profile, string action)
    {
        if (!MediaProfiles.Supports(profile, action) || profile == "General") throw new InputFailureException("Unsupported player action.");
        return (profile, action) switch
        {
            ("YouTube", "playPause") => ["k"], ("YouTube", "next") => ["Shift", "n"], ("YouTube", "previous") => ["Shift", "p"],
            ("YouTube", "seekBack") => ["j"], ("YouTube", "seekForward") => ["l"],
            ("VLC", "playPause") or ("Stremio", "playPause") => [" "],
            ("VLC", "next") => platform == "macos" ? ["Windows", "ArrowRight"] : ["n"],
            ("VLC", "previous") => platform == "macos" ? ["Windows", "ArrowLeft"] : ["p"],
            ("VLC", "volumeUp") => [platform == "macos" ? "Windows" : "Control", "ArrowUp"],
            ("VLC", "volumeDown") => [platform == "macos" ? "Windows" : "Control", "ArrowDown"],
            ("VLC", "mute") when platform == "macos" => ["Windows", "Alt", "ArrowDown"],
            ("VLC", "seekBack") => platform == "macos" ? ["Windows", "Alt", "ArrowLeft"] : ["Shift", "ArrowLeft"],
            ("VLC", "seekForward") => platform == "macos" ? ["Windows", "Alt", "ArrowRight"] : ["Shift", "ArrowRight"],
            ("VLC", "fullscreen") when platform == "macos" => ["Windows", "f"],
            ("Stremio", "next") => ["Shift", "n"], ("Stremio", "seekBack") => ["ArrowLeft"], ("Stremio", "seekForward") => ["ArrowRight"],
            (_, "volumeUp") => ["ArrowUp"], (_, "volumeDown") => ["ArrowDown"], (_, "mute") => ["m"], (_, "fullscreen") => ["f"],
            _ => throw new InputFailureException("Unsupported player action.")
        };
    }
}
