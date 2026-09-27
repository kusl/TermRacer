using TermRacer.Core;

namespace TermRacer.Core.Tests;

internal static class Fixtures
{
    public static Track Track { get; } = Track.Build(Circuits.Gullwing);

    public static RacingLine Line { get; } = RacingLine.Build(Track);

    public static Game NewGame() => new(Track, Line, CarSpec.Default);

    public static Vec2 PathPoint(int index) => (Track.Point(index) + Track.Point(index + 1)) / 2 + Track.Normal(index);
}
