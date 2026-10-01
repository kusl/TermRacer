using TermRacer.Core;

namespace TermRacer.Rendering;

public static class WorldPainter
{
    private static readonly Vec2[] Body = [new(0.5, -0.3), new(0.5, 0.3), new(0.3, 0.5), new(-0.5, 0.5), new(-0.5, -0.5), new(0.3, -0.5)];
    private static readonly Vec2[] Nose = [new(0.5, -0.3), new(0.5, 0.3), new(0.3, 0.5), new(0.22, 0.5), new(0.22, -0.5), new(0.3, -0.5)];
    private static readonly Vec2[] Cabin = [new(0.1, -0.3), new(0.1, 0.3), new(-0.24, 0.3), new(-0.24, -0.3)];

    public static void PaintTrack(PixelCanvas canvas, Camera camera, TrackGeometry geometry, bool detailed)
    {
        canvas.Clear(Palette.Forest);
        Paint(canvas, camera, geometry.Runoff, detailed ? null : Palette.GrassDark);
        if (detailed)
        {
            Paint(canvas, camera, geometry.Barriers, null);
        }

        Paint(canvas, camera, geometry.Asphalt, null);
        if (detailed)
        {
            Paint(canvas, camera, geometry.Curbs, null);
            Paint(canvas, camera, geometry.Checker, null);
            return;
        }

        var track = geometry.Track;
        var across = track.Normal(0) * track.HalfWidth;
        canvas.DrawLine(camera.ToPixel(track.Point(0) - across), camera.ToPixel(track.Point(0) + across), Palette.Cream);
    }

    public static void PaintCar(PixelCanvas canvas, Camera camera, in CarState car, CarSpec spec, bool autopilot, double scale, bool shadow)
    {
        Span<Vec2> corners = stackalloc Vec2[Body.Length];
        if (shadow)
        {
            Shape(corners, Body, camera, car, spec, scale, new Vec2(0.5, 0.6));
            canvas.ShadePolygon(corners, 0.55);
        }

        Shape(corners, Body, camera, car, spec, scale, Vec2.Zero);
        canvas.FillPolygon(corners, Palette.Powder);
        Shape(corners, Nose, camera, car, spec, scale, Vec2.Zero);
        canvas.FillPolygon(corners, Palette.Orange);
        var cabin = corners[..Cabin.Length];
        Shape(cabin, Cabin, camera, car, spec, scale, Vec2.Zero);
        canvas.FillPolygon(cabin, autopilot ? Palette.Cream : Palette.Cockpit);
    }

    public static void PaintGhost(PixelCanvas canvas, Camera camera, in CarState car, CarSpec spec)
    {
        Span<Vec2> corners = stackalloc Vec2[Body.Length];
        Shape(corners, Body, camera, car, spec, 1, Vec2.Zero);
        canvas.BlendPolygon(corners, Palette.Ghost, 0.5);
        Shape(corners, Nose, camera, car, spec, 1, Vec2.Zero);
        canvas.BlendPolygon(corners, Palette.Orange, 0.35);
        var cabin = corners[..Cabin.Length];
        Shape(cabin, Cabin, camera, car, spec, 1, Vec2.Zero);
        canvas.BlendPolygon(cabin, Palette.Cockpit, 0.3);
    }

    public static void PaintMinimap(PixelCanvas canvas, int x, int y, int width, int height, TrackGeometry geometry, Vec2 car, Vec2? ghost)
    {
        canvas.Darken(x, y, width, height, 0.35);
        var track = geometry.Track;
        var camera = Camera.Fit(track.BoundsMin, track.BoundsMax, width, height, 1.5);
        var origin = new Vec2(x, y);
        const int stride = 3;
        for (var i = 0; i < track.Count; i += stride)
        {
            canvas.DrawLine(camera.ToPixel(track.Point(i)) + origin, camera.ToPixel(track.Point(Math.Min(i + stride, track.Count))) + origin, Palette.MapTrack);
        }

        var start = camera.ToPixel(track.Point(0)) + origin;
        canvas.Plot((int)Math.Floor(start.X), (int)Math.Floor(start.Y), Palette.Powder);
        if (ghost is { } other)
        {
            var mark = camera.ToPixel(other) + origin;
            canvas.FillRect((int)Math.Floor(mark.X - 0.5), (int)Math.Floor(mark.Y - 0.5), 2, 2, Palette.Ghost);
        }

        var dot = camera.ToPixel(car) + origin;
        canvas.FillRect((int)Math.Floor(dot.X - 0.5), (int)Math.Floor(dot.Y - 0.5), 2, 2, Palette.Orange);
    }

    private static void Paint(PixelCanvas canvas, Camera camera, WorldQuad[] quads, Rgb? color)
    {
        var low = camera.WorldMin;
        var high = camera.WorldMax;
        Span<Vec2> corners = stackalloc Vec2[4];
        foreach (var quad in quads)
        {
            if (quad.Max.X < low.X || quad.Min.X > high.X || quad.Max.Y < low.Y || quad.Min.Y > high.Y)
            {
                continue;
            }

            corners[0] = camera.ToPixel(quad.A);
            corners[1] = camera.ToPixel(quad.B);
            corners[2] = camera.ToPixel(quad.C);
            corners[3] = camera.ToPixel(quad.D);
            canvas.FillPolygon(corners, color ?? quad.Color);
        }
    }

    private static void Shape(Span<Vec2> output, ReadOnlySpan<Vec2> shape, Camera camera, in CarState car, CarSpec spec, double scale, Vec2 shift)
    {
        var forward = car.Forward;
        var along = forward * (spec.Length * scale);
        var across = forward.Perpendicular * (spec.Width * scale);
        for (var i = 0; i < shape.Length; i++)
        {
            output[i] = camera.ToPixel(car.Position + shift + along * shape[i].X + across * shape[i].Y);
        }
    }
}
