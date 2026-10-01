using TermRacer.Core;
using Xunit;

namespace TermRacer.Core.Tests;

public sealed class ReplayTests
{
    private static Replay Line(int count, double speed = 30)
    {
        var samples = Enumerable.Range(0, count).Select(k =>
        {
            var distance = speed * k / Replay.SampleRate;
            return new ReplaySample(new Vec2(distance, 0), 0, speed, distance, 1, 0, 0, false, false);
        });
        return new Replay("test", (count - 1) / (double)Replay.SampleRate, samples);
    }

    [Fact]
    public void RecordedLapStartsOnTheLineAndCoversTheLap()
    {
        var lap = Fixtures.AutopilotLap();
        var replay = lap.Replay;
        var gate = Fixtures.Track.Gates[0];
        Assert.Equal(Fixtures.Track.Id, replay.TrackId);
        Assert.Equal(lap.Seconds, replay.LapTime);
        Assert.InRange(replay.Samples[0].Distance, -0.01, 0.01);
        Assert.InRange((replay.Samples[0].Position - gate.Center).Dot(gate.Direction), -0.4, 0.4);
        Assert.InRange(replay.Duration, lap.Seconds - 1.0 / Replay.SampleRate, lap.Seconds + 1.0 / Replay.SampleRate);
        Assert.InRange(replay.At(lap.Seconds).Distance, Fixtures.Line.Length - 60, Fixtures.Track.Length + 1);
        Assert.All(replay.Samples, sample => Assert.True(sample.Autopilot));
        Assert.Equal(Driver.Autopilot, lap.Driver);
        Assert.Equal(1, lap.Number);
        Assert.Empty(lap.FaultPoints);
    }

    [Fact]
    public void SamplesInterpolateBetweenNeighbours()
    {
        var replay = Line(10);
        var halfway = replay.At(1.5 / Replay.SampleRate);
        Assert.Equal(1.5, halfway.Position.X, 1e-9);
        Assert.Equal(1.5, halfway.Distance, 1e-9);
        Assert.Equal(replay.Samples[^1], replay.At(100));
        Assert.Equal(replay.Samples[0], replay.At(-1));
    }

    [Fact]
    public void TimeAtInvertsDistance()
    {
        var replay = Line(31);
        Assert.Equal(0.5, replay.TimeAt(15)!.Value, 1e-9);
        Assert.Equal(0, replay.TimeAt(-4));
        Assert.Null(replay.TimeAt(31));
    }

    [Fact]
    public void GhostOfTheSameDrivingShowsNoGap()
    {
        var lap = Fixtures.AutopilotLap();
        var session = new RaceSession(Fixtures.Track, Fixtures.Line, CarSpec.Default, RaceMode.SingleLap, autopilotEngaged: true);
        session.SetGhosts(lap.Replay, null);
        Assert.Null(session.GhostCar);
        var checks = 0;
        for (var i = 0; i < 120 * 200 && !session.IsFinished; i++)
        {
            session.StepOnce();
            if (i % 600 == 0 && session.GhostCar is { } ghost)
            {
                Assert.InRange(ghost.Position.DistanceTo(session.Car.Position), 0, 0.6);
                Assert.InRange(session.GhostGap!.Value, -0.03, 0.03);
                checks++;
            }
        }

        Assert.InRange(checks, 5, 20);
    }

    [Fact]
    public void FlyingLapsUseTheFlyingGhost()
    {
        var standing = Line(5);
        var flying = Line(6);
        var session = new RaceSession(Fixtures.Track, Fixtures.Line, CarSpec.Default, RaceMode.Zen, autopilotEngaged: true);
        session.SetGhosts(standing, flying);
        for (var i = 0; i < 120 * 200 && session.Laps.Laps.Count == 0; i++)
        {
            session.StepOnce();
            if (session.Laps.Phase == RacePhase.Running && session.Laps.Laps.Count == 0)
            {
                Assert.Same(standing, session.Ghost);
            }
        }

        Assert.Same(flying, session.Ghost);
        var completed = Assert.Single(session.TakeCompletedLaps());
        Assert.True(completed.StandingStart);
        Assert.Empty(session.TakeCompletedLaps());
    }

    [Fact]
    public void PlayerSeeksPausesAndChangesSpeed()
    {
        var replay = Line(301);
        var player = new ReplayPlayer(new LapRecord(DateTimeOffset.UnixEpoch, "test", RaceMode.Zen, 2, Driver.Manual, replay.LapTime), replay);
        player.Advance(1);
        Assert.Equal(1, player.Time, 1e-9);
        player.Faster();
        player.Advance(1);
        Assert.Equal(3, player.Time, 1e-9);
        player.TogglePause();
        player.Advance(1);
        Assert.Equal(3, player.Time, 1e-9);
        player.Seek(100);
        Assert.True(player.AtEnd);
        player.TogglePause();
        Assert.Equal(0, player.Time);
        Assert.False(player.Paused);
    }
}
