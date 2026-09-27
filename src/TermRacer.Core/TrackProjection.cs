namespace TermRacer.Core;

public readonly record struct TrackProjection(int Segment, double T, Vec2 Point, Vec2 Tangent, Vec2 Away, double Distance, double Lateral, double S);
