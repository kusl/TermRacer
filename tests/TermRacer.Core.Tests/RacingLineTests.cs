using TermRacer.Core;
using Xunit;

namespace TermRacer.Core.Tests;

public sealed class RacingLineTests
{
    private static RacingLine Line => Fixtures.Line;

    [Fact]
    public void LineStaysInsideTheTrack()
    {
        for (var i = 0; i < Line.Count; i++)
        {
            Assert.InRange(Line.Offset(i), -Fixtures.Track.HalfWidth + 1.7 - 1e-9, Fixtures.Track.HalfWidth - 1.7 + 1e-9);
        }
    }

    [Fact]
    public void LineIsSmootherThanTheCenterline()
    {
        var line = Enumerable.Range(0, Line.Count).Sum(i => Line.Curvature(i) * Line.Curvature(i));
        var center = Enumerable.Range(0, Fixtures.Track.Count).Sum(i => Fixtures.Track.Curvature(i) * Fixtures.Track.Curvature(i));
        Assert.True(line < center * 0.8, $"line {line:0.0000} centre {center:0.0000}");
    }

    [Fact]
    public void SpeedProfileIsReachableUnderBraking()
    {
        var profile = new SpeedProfile(Line, CarSpec.Default);
        for (var i = 0; i < profile.Count; i++)
        {
            var next = profile[i + 1];
            Assert.True(profile[i] * profile[i] <= next * next + 2 * profile.BrakeDeceleration * Line.SegmentLength(i) + 1e-6);
            Assert.InRange(profile[i], 5, 44);
        }
    }
}
