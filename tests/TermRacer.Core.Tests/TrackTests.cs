using TermRacer.Core;
using Xunit;

namespace TermRacer.Core.Tests;

public sealed class TrackTests
{
    private static Track Track => Fixtures.Track;

    [Fact]
    public void SamplesAreEvenlySpacedAroundTheLoop()
    {
        for (var i = 0; i < Track.Count; i++)
        {
            Assert.InRange(Track.Point(i).DistanceTo(Track.Point(i + 1)), Track.Spacing * 0.9, Track.Spacing * 1.1);
        }
    }

    [Fact]
    public void LapLengthIsRaceable() => Assert.InRange(Track.Length, 1500, 2000);

    [Fact]
    public void CornersAreWiderThanTheRunoff()
    {
        var tightest = Enumerable.Range(0, Track.Count).Max(i => Math.Abs(Track.Curvature(i)));
        Assert.True(tightest < 1 / (Track.WallOffset + 6), $"Tightest radius {1 / tightest:0.0} m");
    }

    [Fact]
    public void SeparateSectionsNeverShareRunoff()
    {
        var closest = double.MaxValue;
        for (var i = 0; i < Track.Count; i++)
        {
            for (var j = i + 1; j < Track.Count; j++)
            {
                var along = Math.Min(j - i, Track.Count - (j - i)) * Track.Spacing;
                if (along > 3.5 * Track.WallOffset)
                {
                    closest = Math.Min(closest, Track.Point(i).DistanceTo(Track.Point(j)));
                }
            }
        }

        Assert.True(closest > 2 * Track.WallOffset + 2, $"Closest approach {closest:0.0} m");
    }

    [Fact]
    public void ProjectionRecoversLateralOffset()
    {
        var index = Track.Count / 3;
        var projection = Track.Project(Track.Point(index) + Track.Normal(index) * 3);
        Assert.Equal(3, projection.Lateral, 0.05);
        Assert.Equal(index * Track.Spacing, projection.S, Track.Spacing);
    }

    [Fact]
    public void SurfaceChangesPastTheTrackEdge()
    {
        const int index = 40;
        Assert.Equal(Surface.Tarmac, Track.SurfaceAt(Track.Point(index)));
        Assert.Equal(Surface.Grass, Track.SurfaceAt(Track.Point(index) + Track.Normal(index) * (Track.HalfWidth + 3)));
    }

    [Fact]
    public void StartPoseSitsBehindTheLineFacingIt()
    {
        var start = Track.StartPose;
        var ahead = (Track.Gates[0].Center - start.Position).Dot(Vec2.FromAngle(start.Heading));
        Assert.InRange(ahead, Circuits.Gullwing.StartOffset - 1, Circuits.Gullwing.StartOffset + 1);
    }

    [Fact]
    public void GatesStartAtTheFinishLine()
    {
        Assert.Equal(Circuits.Gullwing.Checkpoints, Track.Gates.Count);
        Assert.Equal(0, Track.Gates[0].Center.DistanceTo(Track.Point(0)), 1e-9);
        Assert.True(Track.Gates[0].Direction.Dot(Track.Tangent(0)) > 0.999);
    }

    [Fact]
    public void OpenLayoutIsRejected() =>
        Assert.Throws<ArgumentException>(() => Track.Build(new TrackDefinition("Open", Vec2.Zero, 0, [TrackSection.Straight(100), TrackSection.Arc(30, 90)])));
}
