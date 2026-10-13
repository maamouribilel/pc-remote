namespace PcController.Core;
public static class MediaProfiles
{
    public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Actions = new Dictionary<string, IReadOnlySet<string>>
    {
        ["General"] = new HashSet<string> { "playPause", "next", "previous", "volumeUp", "volumeDown", "mute" },
        ["YouTube"] = new HashSet<string> { "playPause", "next", "previous", "volumeUp", "volumeDown", "mute", "seekBack", "seekForward", "fullscreen" },
        ["VLC"] = new HashSet<string> { "playPause", "next", "previous", "volumeUp", "volumeDown", "mute", "seekBack", "seekForward", "fullscreen" },
        ["Stremio"] = new HashSet<string> { "playPause", "next", "volumeUp", "volumeDown", "mute", "seekBack", "seekForward", "fullscreen" },
    };
    public static bool Supports(string profile, string action) => Actions.TryGetValue(profile, out var actions) && actions.Contains(action);
}
