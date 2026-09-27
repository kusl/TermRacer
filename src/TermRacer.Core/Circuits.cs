namespace TermRacer.Core;

public static class Circuits
{
    public static TrackDefinition Gullwing { get; } = new(
        "Gullwing Park",
        Vec2.Zero,
        0,
        [
            TrackSection.Straight(233.777),
            TrackSection.Arc(70, 90),
            TrackSection.Straight(36.125),
            TrackSection.Arc(55, -50),
            TrackSection.Arc(42, 95),
            TrackSection.Straight(70),
            TrackSection.Arc(32, 45),
            TrackSection.Straight(320),
            TrackSection.Arc(27, 180),
            TrackSection.Straight(110),
            TrackSection.Arc(38, -90),
            TrackSection.Straight(35),
            TrackSection.Arc(34, -90),
            TrackSection.Straight(170),
            TrackSection.Arc(48, 90),
            TrackSection.Arc(60, 90),
            TrackSection.Straight(110),
        ]);
}
