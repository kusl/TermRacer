using TermRacer.Core;
using Xunit;

namespace TermRacer.Core.Tests;

public sealed class LapTrackerTests
{
    private static Track Track => Fixtures.Track;

    [Fact]
    public void CrossingTheLineStartsTheClockAtTheInterpolatedTime()
    {
        var laps = new LapTracker(Track, RaceMode.SingleLap);
        var gate = Track.Gates[0];
        Assert.Equal(LapEvent.Started, laps.Update(gate.Center - gate.Direction * 1.5, gate.Center + gate.Direction * 0.5, 10, 11));
        Assert.Equal(RacePhase.Running, laps.Phase);
        Assert.Equal(10.75, laps.LapStart, 1e-9);
    }

    [Fact]
    public void CrossingTheLineBackwardsIsIgnored()
    {
        var laps = new LapTracker(Track, RaceMode.SingleLap);
        var gate = Track.Gates[0];
        Assert.Equal(LapEvent.None, laps.Update(gate.Center + gate.Direction, gate.Center - gate.Direction, 0, 1));
        Assert.Equal(RacePhase.Waiting, laps.Phase);
    }

    [Fact]
    public void OneFullLoopFinishesASingleLap()
    {
        var laps = Drive(new LapTracker(Track, RaceMode.SingleLap), 1);
        Assert.Equal(RacePhase.Finished, laps.Phase);
        Assert.Equal(Track.Count * 0.1, Assert.Single(laps.Laps), 0.05);
        Assert.Equal(1, laps.CurrentLap);
    }

    [Fact]
    public void ZenModeKeepsCounting()
    {
        var laps = Drive(new LapTracker(Track, RaceMode.Zen), 3);
        Assert.Equal(RacePhase.Running, laps.Phase);
        Assert.Equal(3, laps.Laps.Count);
        Assert.Equal(4, laps.CurrentLap);
        Assert.Equal(Track.Count * 0.1, laps.BestLap!.Value, 0.05);
    }

    [Fact]
    public void SkippingACheckpointVoidsTheLap()
    {
        var laps = new LapTracker(Track, RaceMode.Zen);
        var checkpoint = (int)Math.Round((double)Track.Count / Track.Gates.Count);
        var time = 0.0;
        var previous = Fixtures.PathPoint(-3);
        void Move(Vec2 next)
        {
            laps.Update(previous, next, time, time + 0.1);
            previous = next;
            time += 0.1;
        }

        for (var k = -2; k < checkpoint - 3; k++)
        {
            Move(Fixtures.PathPoint(k));
        }

        Move(Track.Point(checkpoint - 3) + Track.Normal(checkpoint - 3) * 40);
        Move(Track.Point(checkpoint + 3) + Track.Normal(checkpoint + 3) * 40);
        for (var k = checkpoint + 3; k <= Track.Count + 5; k++)
        {
            Move(Fixtures.PathPoint(k));
        }

        Assert.Equal(RacePhase.Running, laps.Phase);
        Assert.Empty(laps.Laps);
        Assert.Equal(1, laps.NextGate);
    }

    private static LapTracker Drive(LapTracker laps, int loops)
    {
        var time = 0.0;
        var previous = Fixtures.PathPoint(-5);
        for (var k = -4; k <= Track.Count * loops + 5; k++)
        {
            var next = Fixtures.PathPoint(k);
            laps.Update(previous, next, time, time + 0.1);
            previous = next;
            time += 0.1;
        }

        return laps;
    }
}
