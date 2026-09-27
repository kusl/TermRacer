namespace TermRacer.Core;

public readonly record struct TrackSection(double Length, double Radius, double Degrees)
{
    public bool IsArc => Radius > 0;

    public static TrackSection Straight(double length) => new(length, 0, 0);

    public static TrackSection Arc(double radius, double degrees) => new(radius * Math.Abs(degrees) * Math.PI / 180, radius, degrees);
}
