using TermRacer.Core;
using Xunit;

namespace TermRacer.Core.Tests;

public sealed class Vec2Tests
{
    [Fact]
    public void PerpendicularPointsToTheRightOfTravel()
    {
        var right = Vec2.FromAngle(0).Perpendicular;
        Assert.Equal(0, right.X, 1e-12);
        Assert.Equal(1, right.Y, 1e-12);
    }

    [Fact]
    public void RotateMatchesFromAngle()
    {
        var rotated = new Vec2(1, 0).Rotate(Math.PI / 3);
        var expected = Vec2.FromAngle(Math.PI / 3);
        Assert.Equal(expected.X, rotated.X, 1e-12);
        Assert.Equal(expected.Y, rotated.Y, 1e-12);
    }

    [Fact]
    public void NormalizingZeroIsSafe() => Assert.Equal(Vec2.Zero, Vec2.Zero.Normalized());

    [Theory]
    [InlineData(3, 4, 5)]
    [InlineData(-6, 8, 10)]
    public void LengthIsEuclidean(double x, double y, double length) => Assert.Equal(length, new Vec2(x, y).Length, 1e-12);

    [Fact]
    public void CrossIsPositiveForRightTurns() => Assert.True(new Vec2(1, 0).Cross(new Vec2(0, 1)) > 0);
}
