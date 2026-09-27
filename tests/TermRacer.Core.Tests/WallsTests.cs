using TermRacer.Core;
using Xunit;

namespace TermRacer.Core.Tests;

public sealed class WallsTests
{
    [Fact]
    public void CarPastTheWallIsPushedBackAndBounces()
    {
        var track = Fixtures.Track;
        const int index = 100;
        var normal = track.Normal(index);
        var state = new CarState(track.Point(index) + normal * (track.WallOffset + 2), track.Tangent(index).Angle, normal * 10);
        var impact = Walls.Resolve(ref state, track, CarSpec.Default, track.Project(state.Position));
        Assert.NotNull(impact);
        Assert.InRange(track.Project(state.Position).Distance, 0, track.WallOffset);
        Assert.True(state.Velocity.Dot(normal) <= 0);
    }

    [Fact]
    public void CarInsideTheWallsIsUntouched()
    {
        var track = Fixtures.Track;
        var state = CarState.At(track.StartPose) with { Velocity = new Vec2(5, 1) };
        var before = state;
        Assert.Null(Walls.Resolve(ref state, track, CarSpec.Default, track.Project(state.Position)));
        Assert.Equal(before, state);
    }
}
