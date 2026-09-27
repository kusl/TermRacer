using TermRacer.Core;
using Xunit;

namespace TermRacer.Core.Tests;

public sealed class AutopilotTests
{
    [Fact]
    public void AutopilotLapsCleanly()
    {
        var session = new RaceSession(Fixtures.Track, Fixtures.Line, CarSpec.Default, RaceMode.Zen, autopilotEngaged: true);
        var offTrack = 0;
        var steps = (int)(300 / RaceSession.Step);
        for (var i = 0; i < steps && session.Laps.Laps.Count < 3; i++)
        {
            session.StepOnce();
            offTrack += session.Surface == Surface.Grass ? 1 : 0;
        }

        Assert.Equal(3, session.Laps.Laps.Count);
        Assert.Equal(0, session.WallHits);
        Assert.Equal(0, offTrack);
    }

    [Fact]
    public void SingleLapOnAutopilotFinishes()
    {
        var session = new RaceSession(Fixtures.Track, Fixtures.Line, CarSpec.Default, RaceMode.SingleLap, autopilotEngaged: true);
        session.Advance((int)(180 / RaceSession.Step));
        Assert.True(session.IsFinished);
        Assert.InRange(Assert.Single(session.Laps.Laps), 35, 75);
    }

    [Fact]
    public void AutopilotRecoversFacingBackwardsOnTheGrass()
    {
        var track = Fixtures.Track;
        var index = track.Count / 2;
        var session = new RaceSession(track, Fixtures.Line, CarSpec.Default, RaceMode.Zen, autopilotEngaged: true);
        session.Place(new CarState(track.Point(index) + track.Normal(index) * (track.HalfWidth + 5), track.Tangent(index).Angle + Math.PI, Vec2.Zero));
        var steps = (int)(240 / RaceSession.Step);
        for (var i = 0; i < steps && session.Laps.Laps.Count == 0; i++)
        {
            session.StepOnce();
        }

        Assert.NotEmpty(session.Laps.Laps);
    }
}
