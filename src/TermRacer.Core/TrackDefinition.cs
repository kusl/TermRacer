namespace TermRacer.Core;

public sealed record TrackDefinition(
    string Name,
    Vec2 Origin,
    double Heading,
    IReadOnlyList<TrackSection> Sections,
    double HalfWidth = 7,
    double Runoff = 9,
    double Spacing = 2,
    int Checkpoints = 12,
    double StartOffset = 18);
