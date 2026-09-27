using TermRacer.Core;

namespace TermRacer.Rendering;

public readonly record struct Camera(Vec2 Center, double MetersPerPixel, int Width, int Height)
{
    public Vec2 WorldMin => ToWorld(Vec2.Zero);

    public Vec2 WorldMax => ToWorld(new Vec2(Width, Height));

    public Vec2 ToPixel(Vec2 world) =>
        new((world.X - Center.X) / MetersPerPixel + Width * 0.5, (world.Y - Center.Y) / MetersPerPixel + Height * 0.5);

    public Vec2 ToWorld(Vec2 pixel) =>
        new((pixel.X - Width * 0.5) * MetersPerPixel + Center.X, (pixel.Y - Height * 0.5) * MetersPerPixel + Center.Y);

    public bool Overlaps(Vec2 min, Vec2 max)
    {
        var low = WorldMin;
        var high = WorldMax;
        return max.X >= low.X && min.X <= high.X && max.Y >= low.Y && min.Y <= high.Y;
    }

    public static Camera Fit(Vec2 min, Vec2 max, int width, int height, double padding = 1)
    {
        var size = max - min;
        var scale = Math.Max(size.X / Math.Max(1, width - 2 * padding), size.Y / Math.Max(1, height - 2 * padding));
        return new Camera((min + max) / 2, Math.Max(scale, 1e-6), width, height);
    }
}
