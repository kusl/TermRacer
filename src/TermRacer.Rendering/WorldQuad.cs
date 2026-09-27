using TermRacer.Core;

namespace TermRacer.Rendering;

public readonly struct WorldQuad
{
    public WorldQuad(Vec2 a, Vec2 b, Vec2 c, Vec2 d, Rgb color)
    {
        A = a;
        B = b;
        C = c;
        D = d;
        Color = color;
        Min = new Vec2(Math.Min(Math.Min(a.X, b.X), Math.Min(c.X, d.X)), Math.Min(Math.Min(a.Y, b.Y), Math.Min(c.Y, d.Y)));
        Max = new Vec2(Math.Max(Math.Max(a.X, b.X), Math.Max(c.X, d.X)), Math.Max(Math.Max(a.Y, b.Y), Math.Max(c.Y, d.Y)));
    }

    public Vec2 A { get; }

    public Vec2 B { get; }

    public Vec2 C { get; }

    public Vec2 D { get; }

    public Rgb Color { get; }

    public Vec2 Min { get; }

    public Vec2 Max { get; }
}
