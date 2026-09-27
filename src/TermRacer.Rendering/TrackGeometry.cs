using TermRacer.Core;

namespace TermRacer.Rendering;

public sealed class TrackGeometry
{
    private const double CurbCurvature = 1 / 160.0;

    public TrackGeometry(Track track)
    {
        Track = track;
        var wall = track.WallOffset;
        Runoff = Band(track, -wall, wall, i => i / 6 % 2 == 0 ? Palette.GrassLight : Palette.GrassDark);
        Barriers = [.. Band(track, -wall - 1.2, -wall, BarrierColor), .. Band(track, wall, wall + 1.2, BarrierColor)];
        Asphalt = Band(track, -track.HalfWidth, track.HalfWidth, _ => Palette.Asphalt);
        Curbs = [.. Curb(track, -1), .. Curb(track, 1)];
        Checker = BuildChecker(track);
    }

    public Track Track { get; }

    public WorldQuad[] Runoff { get; }

    public WorldQuad[] Barriers { get; }

    public WorldQuad[] Asphalt { get; }

    public WorldQuad[] Curbs { get; }

    public WorldQuad[] Checker { get; }

    private static Rgb BarrierColor(int index) => index / 2 % 2 == 0 ? Palette.BarrierLight : Palette.BarrierDark;

    private static WorldQuad[] Band(Track track, double inner, double outer, Func<int, Rgb> color)
    {
        var count = track.Count;
        var near = new Vec2[count];
        var far = new Vec2[count];
        for (var i = 0; i < count; i++)
        {
            near[i] = track.Point(i) + track.Normal(i) * inner;
            far[i] = track.Point(i) + track.Normal(i) * outer;
        }

        var quads = new WorldQuad[count];
        for (var i = 0; i < count; i++)
        {
            var j = (i + 1) % count;
            quads[i] = new WorldQuad(near[i], near[j], far[j], far[i], color(i));
        }

        return quads;
    }

    private static IEnumerable<WorldQuad> Curb(Track track, int side)
    {
        var inner = (track.HalfWidth - 0.4) * side;
        var outer = (track.HalfWidth + 1.1) * side;
        for (var i = 0; i < track.Count; i++)
        {
            if (Math.Abs(track.Curvature(i)) < CurbCurvature && Math.Abs(track.Curvature(i + 1)) < CurbCurvature)
            {
                continue;
            }

            yield return new WorldQuad(
                track.Point(i) + track.Normal(i) * inner,
                track.Point(i + 1) + track.Normal(i + 1) * inner,
                track.Point(i + 1) + track.Normal(i + 1) * outer,
                track.Point(i) + track.Normal(i) * outer,
                i % 2 == 0 ? Palette.CurbRed : Palette.CurbWhite);
        }
    }

    private static WorldQuad[] BuildChecker(Track track)
    {
        const int columns = 10;
        var origin = track.Point(0);
        var along = track.Tangent(0);
        var across = track.Normal(0);
        var size = 2 * track.HalfWidth / columns;
        var quads = new List<WorldQuad>();
        for (var row = 0; row < 2; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                var a0 = (row - 1) * size;
                var a1 = row * size;
                var b0 = -track.HalfWidth + column * size;
                var b1 = b0 + size;
                quads.Add(new WorldQuad(
                    origin + along * a0 + across * b0,
                    origin + along * a1 + across * b0,
                    origin + along * a1 + across * b1,
                    origin + along * a0 + across * b1,
                    (row + column) % 2 == 0 ? Palette.CheckerLight : Palette.CheckerDark));
            }
        }

        return [.. quads];
    }
}
