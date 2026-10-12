namespace PcController.Core;
public static class MediaProfiles
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string[]>> Mappings = new Dictionary<string, IReadOnlyDictionary<string, string[]>>
    {
        ["General"] = new Dictionary<string, string[]> { ["playPause"] = ["MediaPlayPause"], ["next"] = ["MediaNext"], ["previous"] = ["MediaPrevious"], ["volumeUp"] = ["VolumeUp"], ["volumeDown"] = ["VolumeDown"], ["mute"] = ["VolumeMute"] },
        ["YouTube"] = new Dictionary<string, string[]> { ["playPause"] = ["K"], ["next"] = ["Shift", "N"], ["previous"] = ["Shift", "P"], ["volumeUp"] = ["ArrowUp"], ["volumeDown"] = ["ArrowDown"], ["mute"] = ["M"], ["seekBack"] = ["J"], ["seekForward"] = ["L"], ["fullscreen"] = ["F"] },
        ["VLC"] = new Dictionary<string, string[]> { ["playPause"] = ["Space"], ["next"] = ["N"], ["previous"] = ["P"], ["volumeUp"] = ["Control", "ArrowUp"], ["volumeDown"] = ["Control", "ArrowDown"], ["mute"] = ["M"], ["seekBack"] = ["Shift", "ArrowLeft"], ["seekForward"] = ["Shift", "ArrowRight"], ["fullscreen"] = ["F"] },
        ["Stremio"] = new Dictionary<string, string[]> { ["playPause"] = ["Space"], ["next"] = ["Shift", "N"], ["volumeUp"] = ["ArrowUp"], ["volumeDown"] = ["ArrowDown"], ["mute"] = ["M"], ["seekBack"] = ["ArrowLeft"], ["seekForward"] = ["ArrowRight"], ["fullscreen"] = ["F"] }
    };
    public static bool Supports(string profile, string action) => Mappings.TryGetValue(profile, out var commands) && commands.ContainsKey(action);
}
