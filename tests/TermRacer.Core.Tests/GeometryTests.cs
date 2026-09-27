using TermRacer.Core;
using Xunit;

namespace TermRacer.Core.Tests;

public sealed class GeometryTests
{
    [Fact]
    public void CrossingSegmentsReportPathParameter()
    {
        Assert.True(Geometry.TryIntersect(new Vec2(0, 0), new Vec2(4, 0), new Vec2(1, -1), new Vec2(1, 1), out var t));
        Assert.Equal(0.25, t, 1e-12);
    }

    [Fact]
    public void ParallelSegmentsDoNotIntersect() =>
        Assert.False(Geometry.TryIntersect(new Vec2(0, 0), new Vec2(1, 0), new Vec2(0, 1), new Vec2(1, 1), out _));

    [Fact]
    public void TouchingAtAStepBoundaryCountsOnce()
    {
        var first = Geometry.TryIntersect(new Vec2(0, 0), new Vec2(1, 0), new Vec2(1, -1), new Vec2(1, 1), out _);
        var second = Geometry.TryIntersect(new Vec2(1, 0), new Vec2(2, 0), new Vec2(1, -1), new Vec2(1, 1), out _);
        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData(1.0, 1.0)]
    [InlineData(7.0, 7.0 - 2 * Math.PI)]
    [InlineData(-4.0, -4.0 + 2 * Math.PI)]
    public void NormalizeAngleWrapsIntoHalfTurn(double input, double expected) =>
        Assert.Equal(expected, Geometry.NormalizeAngle(input), 1e-12);

    [Theory]
    [InlineData(0.0, 1.0, 0.25, 0.25)]
    [InlineData(0.9, 1.0, 0.25, 1.0)]
    [InlineData(1.0, -1.0, 0.5, 0.5)]
    public void MoveTowardsNeverOvershoots(double current, double target, double step, double expected) =>
        Assert.Equal(expected, Geometry.MoveTowards(current, target, step), 1e-12);
}
