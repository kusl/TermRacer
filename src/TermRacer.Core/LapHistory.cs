namespace TermRacer.Core;

public sealed class LapHistory
{
    public const int KeepRecent = 20;
    public const int KeepFastest = 5;

    private readonly List<LapRecord> laps = [];
    private readonly HashSet<string> replays = new(StringComparer.Ordinal);

    public LapHistory(string trackId, IEnumerable<LapRecord> records, IEnumerable<string> availableReplays)
    {
        TrackId = trackId;
        replays.UnionWith(availableReplays);
        laps.AddRange(records.Where(record => record.TrackId == trackId).OrderBy(record => record.RecordedAt));
    }

    public string TrackId { get; }

    public IReadOnlyList<LapRecord> Laps => laps;

    public LapRecord? Best(bool standing, Driver? driver = null) =>
        laps.Where(lap => lap.StandingStart == standing && (driver is null || lap.Driver == driver)).MinBy(lap => lap.Seconds);

    public bool HasReplay(LapRecord record) => record.Replay is { } name && replays.Contains(name);

    public IReadOnlyList<LapRecord> WithReplays() => [.. laps.Where(HasReplay).Reverse()];

    public IReadOnlyList<LapRecord> FastestWithReplay(bool standing) =>
        [.. laps.Where(lap => lap.StandingStart == standing && HasReplay(lap)).OrderBy(lap => lap.Seconds)];

    public bool Contains(string replay) => laps.Any(lap => lap.Replay == replay);

    public void Add(LapRecord record)
    {
        if (record.TrackId != TrackId)
        {
            return;
        }

        laps.Add(record);
        if (record.Replay is { } name)
        {
            replays.Add(name);
        }
    }

    public void Forget(string replay) => replays.Remove(replay);

    public IReadOnlyList<string> Prune()
    {
        var saved = laps.Where(HasReplay).ToList();
        var keep = new HashSet<string>(StringComparer.Ordinal);
        keep.UnionWith(saved.TakeLast(KeepRecent).Select(lap => lap.Replay!));
        foreach (var standing in new[] { true, false })
        {
            var start = saved.Where(lap => lap.StandingStart == standing).ToList();
            keep.UnionWith(start.OrderBy(lap => lap.Seconds).Take(KeepFastest).Select(lap => lap.Replay!));
            keep.UnionWith(start.Where(lap => lap.Driver == Driver.Manual).OrderBy(lap => lap.Seconds).Take(KeepFastest).Select(lap => lap.Replay!));
        }

        var stale = saved.Select(lap => lap.Replay!).Where(name => !keep.Contains(name)).Distinct(StringComparer.Ordinal).ToList();
        foreach (var name in stale)
        {
            replays.Remove(name);
        }

        return stale;
    }
}
