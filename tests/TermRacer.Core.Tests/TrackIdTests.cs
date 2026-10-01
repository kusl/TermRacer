using TermRacer.Core;
using Xunit;

namespace TermRacer.Core.Tests;

public sealed class TrackIdTests
{
    [Fact]
    public void IdNamesTheCircuitAndIsStable()
    {
        Assert.Matches("^gullwing-park-[0-9a-f]{8}$", Fixtures.Track.Id);
        Assert.Equal(Fixtures.Track.Id, Track.Build(Circuits.Gullwing).Id);
    }

    [Fact]
    public void ChangingTheLayoutChangesTheId() =>
        Assert.NotEqual(Fixtures.Track.Id, Track.Build(Circuits.Gullwing with { HalfWidth = 7.5 }).Id);

    [Fact]
    public void HintedProjectionMatchesTheFullSearch()
    {
        var track = Fixtures.Track;
        for (var i = 0; i < track.Count; i += 7)
        {
            foreach (var lateral in new[] { -track.WallOffset, -3.0, 0.0, 5.5, track.WallOffset + 0.5 })
            {
                var position = track.Point(i) + track.Normal(i) * lateral + track.Tangent(i) * 0.7;
                Assert.Equal(track.Project(position), track.Project(position, i + 9));
            }
        }
    }

    [Fact]
    public void FarFromTheHintFallsBackToTheFullSearch()
    {
        var track = Fixtures.Track;
        var position = track.Point(track.Count / 2);
        Assert.Equal(track.Project(position), track.Project(position, 0));
    }
}
