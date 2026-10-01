using TermRacer.Core;
using Xunit;

namespace TermRacer.Core.Tests;

public sealed class LapHistoryTests
{
    private const string TrackId = "test-track";

    private static LapRecord Lap(int minute, double seconds, int lap = 2, Driver driver = Driver.Autopilot, string? track = null) =>
        new(DateTimeOffset.UnixEpoch.AddMinutes(minute), track ?? TrackId, RaceMode.Zen, lap, driver, seconds, Replay: $"r{minute}");

    [Fact]
    public void BestLapsAreSplitByStartAndDriver()
    {
        var history = new LapHistory(TrackId, [Lap(1, 58, 1, Driver.Manual), Lap(2, 55), Lap(3, 54, 2, Driver.Manual), Lap(4, 51), Lap(5, 40, track: "other")], []);
        Assert.Equal(58, history.Best(true)!.Seconds);
        Assert.Equal(51, history.Best(false)!.Seconds);
        Assert.Equal(54, history.Best(false, Driver.Manual)!.Seconds);
        Assert.Null(history.Best(true, Driver.Autopilot));
        Assert.Equal(4, history.Laps.Count);
    }

    [Fact]
    public void ReplayListIsNewestFirstAndOnlyListsSavedFiles()
    {
        var history = new LapHistory(TrackId, [Lap(1, 58), Lap(2, 57), Lap(3, 56)], ["r1", "r3"]);
        Assert.Equal(["r3", "r1"], history.WithReplays().Select(lap => lap.Replay));
        history.Forget("r3");
        Assert.Equal("r1", Assert.Single(history.WithReplays()).Replay);
    }

    [Fact]
    public void PruningKeepsRecentAndFastestLaps()
    {
        var laps = Enumerable.Range(0, 40).Select(minute => Lap(minute, minute == 3 ? 50 : 60 + minute, minute == 0 ? 1 : 2)).ToList();
        var history = new LapHistory(TrackId, laps, laps.Select(lap => lap.Replay!));
        var stale = history.Prune();
        var kept = history.WithReplays().Select(lap => lap.Replay).ToHashSet();
        Assert.Contains("r0", kept);
        Assert.Contains("r3", kept);
        Assert.Contains("r39", kept);
        Assert.Contains("r20", kept);
        Assert.DoesNotContain("r10", kept);
        Assert.Equal(40, stale.Count + kept.Count);
        Assert.Empty(history.Prune());
    }
}
