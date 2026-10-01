namespace TermRacer.Core;

public sealed class MemoryRecordStore : IRecordStore
{
    private readonly List<LapRecord> laps = [];
    private readonly Dictionary<string, Replay> replays = new(StringComparer.Ordinal);

    public string Location => "memory";

    public string? Problem => null;

    public IReadOnlyList<LapRecord> Laps => laps;

    public IReadOnlyDictionary<string, Replay> Replays => replays;

    public AutopilotModel? Autopilot { get; private set; }

    public IReadOnlyList<LapRecord> LoadLaps() => [.. laps];

    public IReadOnlyCollection<string> ListReplays() => [.. replays.Keys];

    public bool AppendLap(LapRecord record)
    {
        laps.Add(record);
        return true;
    }

    public bool SaveReplay(string name, LapRecord record, Replay replay)
    {
        replays[name] = replay;
        return true;
    }

    public Replay? LoadReplay(string name) => replays.GetValueOrDefault(name);

    public void DeleteReplay(string name) => replays.Remove(name);

    public AutopilotModel? LoadAutopilot() => Autopilot;

    public bool SaveAutopilot(AutopilotModel model)
    {
        Autopilot = model;
        return true;
    }
}
