using PcController.Core;

internal sealed class RecordingPcActions : IPcActions
{
    private readonly object gate = new();
    private VolumeState volume = new(true,42);
    private readonly List<string> events = [];
    public string[] Events { get { lock (gate) return events.ToArray(); } }
    public VolumeState GetVolume() { lock (gate) return volume; }
    public VolumeState SetVolume(int level) { lock (gate) { events.Add("volume:" + level); return volume = new(true,level); } }
    public LaunchResult Launch(LauncherApp app) { lock (gate) events.Add("launch:" + app.Name); return new(true); }
    public void Power(string action) { lock (gate) events.Add("power:" + action); }
}
