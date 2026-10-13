using System.Net;

namespace PcController.Core;

// Only addresses approved by the platform trust policy belong in PrivateAddresses.
// TrustIdentity lets adapters force reapproval/rebind even when the IP stays the same.
public sealed record NetworkSnapshot(IPAddress[] PrivateAddresses, IPAddress[] AllAddresses, string Notice, string TrustIdentity = "");
public interface ITrustedNetworkDiscovery { NetworkSnapshot Read(); }
public interface ILauncherPlatform
{
    string Normalize(string path);
    StringComparer PathComparer { get; }
    bool ValidExecutable(string path);
    bool Available(LauncherApp app);
    LauncherApp[] Discover();
}
public interface ILauncherSettings
{
    LauncherApp[] List();
    LauncherApp? Find(string id);
    bool Available(LauncherApp app);
    void Add(string executable, string name, string profile);
    void Remove(string id);
}

// Protocol v1 retains the historical Windows token for the logical Meta modifier.
public enum LogicalModifier { Control, Alt, Shift, Meta }
public static class InputSemantics
{
    public const int ScrollUnitsPerDetent = 120;
    public static LogicalModifier Modifier(string wireKey) => wireKey switch
    {
        "Control" => LogicalModifier.Control, "Alt" => LogicalModifier.Alt,
        "Shift" => LogicalModifier.Shift, "Windows" => LogicalModifier.Meta,
        _ => throw new ArgumentOutOfRangeException(nameof(wireKey))
    };
}
public interface IMediaInput { void Media(string profile, string action); }

// Availability must come from real adapters/permission state in production.
public sealed record PlatformCapabilities(string Platform, bool Pointer, bool Keyboard, bool Text,
    bool Shortcuts, bool Volume, bool Launcher, IReadOnlySet<string> PowerActions,
    IReadOnlyDictionary<string, IReadOnlySet<string>> MediaActions)
{
    public PlatformLabels Labels => PlatformLabels.For(Platform);
    public bool InputEnabled => Pointer || Keyboard || Text || Shortcuts || MediaActions.Values.Any(actions => actions.Count > 0);
    public bool Supports(ControlCommand command) => command.Type switch
    {
        "ping" or "input.releaseAll" => true,
        "pointer.move" or "pointer.scroll" or "pointer.button" => Pointer,
        "key.down" or "key.up" or "key.press" => Keyboard,
        "text.insert" => Text,
        "shortcut" => Shortcuts,
        "media.action" => MediaActions.TryGetValue(command.Profile, out var actions) && actions.Contains(command.Action),
        _ => false
    };
    public static PlatformCapabilities ProtocolV1 { get; } = new("legacy-v1", true, true, true, true, true, true,
        new HashSet<string> { "sleep", "restart", "shutdown" }, MediaProfiles.Actions);
    public static PlatformCapabilities Unavailable(string platform) => new(platform, false, false, false, false, false, false,
        new HashSet<string>(), new Dictionary<string, IReadOnlySet<string>>());
}

public sealed record PlatformLabels(string Computer, string Control, string Alt, string Meta, string ShortcutModifier, string SwitchApp)
{
    public static PlatformLabels For(string platform) => platform switch
    {
        "macos" => new("Mac", "Control", "Option", "Command", "Command", "Command + Tab"),
        "linux" => new("Computer", "Ctrl", "Alt", "Super", "Ctrl", "Alt + Tab"),
        "windows" or "legacy-v1" or "recording" => new("PC", "Ctrl", "Alt", "Win", "Ctrl", "Alt + Tab"),
        _ => new("Computer", "Control", "Alt", "Meta", "Control", "Switch app")
    };
}
