namespace TermRacer.Core;

public readonly record struct ReplaySample(
    Vec2 Position,
    double Heading,
    double Speed,
    double Distance,
    double Throttle,
    double Brake,
    double Steer,
    bool Autopilot,
    bool OffTrack);
