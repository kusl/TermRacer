namespace TermRacer.Core;

public sealed class RacingLine
{
    private readonly Vec2[] points;
    private readonly double[] offsets;
    private readonly double[] lengths;
    private readonly double[] curvatures;

    private RacingLine(Track track, double[] offsets)
    {
        Track = track;
        this.offsets = offsets;
        var count = track.Count;
        points = new Vec2[count];
        lengths = new double[count];
        curvatures = new double[count];
        for (var i = 0; i < count; i++)
        {
            points[i] = track.Point(i) + track.Normal(i) * offsets[i];
        }

        for (var i = 0; i < count; i++)
        {
            lengths[i] = points[i].DistanceTo(points[(i + 1) % count]);
            Length += lengths[i];
            curvatures[i] = Menger(points[(i - 2 + count) % count], points[i], points[(i + 2) % count]);
        }
    }

    public Track Track { get; }

    public int Count => points.Length;

    public double Length { get; }

    public Vec2 Point(int index) => points[Track.Wrap(index)];

    public double Offset(int index) => offsets[Track.Wrap(index)];

    public double Curvature(int index) => curvatures[Track.Wrap(index)];

    public double SegmentLength(int index) => lengths[Track.Wrap(index)];

    public static RacingLine Build(Track track, double margin = 1.7)
    {
        var limit = Math.Max(0, track.HalfWidth - margin);
        var offsets = new double[track.Count];
        var coarseCount = Math.Max(5, track.Count / 5);
        var coarse = Enumerable.Range(0, coarseCount).Select(k => (int)Math.Round((double)k * track.Count / coarseCount)).ToArray();
        Relax(track, offsets, coarse, limit, 4000);
        Interpolate(track.Count, offsets, coarse);
        Relax(track, offsets, [.. Enumerable.Range(0, track.Count)], limit, 600);
        return new RacingLine(track, offsets);
    }

    public (int Index, double T) Locate(Vec2 position, int hint = -1)
    {
        var global = hint < 0;
        var start = global ? 0 : hint - 20;
        var span = global ? points.Length : Math.Min(points.Length, 80);
        var bestIndex = 0;
        var bestT = 0.0;
        var bestDistance = double.MaxValue;
        for (var j = 0; j < span; j++)
        {
            var i = Track.Wrap(start + j);
            var from = points[i];
            var to = points[(i + 1) % points.Length];
            var t = Geometry.ClosestParameter(from, to, position);
            var distance = (position - Vec2.Lerp(from, to, t)).LengthSquared;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
                bestT = t;
            }
        }

        return !global && bestDistance > 900 ? Locate(position) : (bestIndex, bestT);
    }

    public Vec2 PointAhead(int index, double t, double distance)
    {
        var i = Track.Wrap(index);
        var remaining = distance + t * lengths[i];
        for (var guard = 0; remaining > lengths[i] && guard < points.Length; guard++)
        {
            remaining -= lengths[i];
            i = (i + 1) % points.Length;
        }

        var fraction = lengths[i] > 1e-9 ? Math.Min(1, remaining / lengths[i]) : 0;
        return Vec2.Lerp(points[i], points[(i + 1) % points.Length], fraction);
    }

    private static void Relax(Track track, double[] offsets, int[] indices, double limit, int iterations)
    {
        var count = indices.Length;
        var positions = new Vec2[count];
        for (var k = 0; k < count; k++)
        {
            positions[k] = track.Point(indices[k]) + track.Normal(indices[k]) * offsets[indices[k]];
        }

        for (var iteration = 0; iteration < iterations; iteration++)
        {
            for (var k = 0; k < count; k++)
            {
                var near = positions[(k - 1 + count) % count] + positions[(k + 1) % count];
                var far = positions[(k - 2 + count) % count] + positions[(k + 2) % count];
                var target = (4 * near - far) / 6;
                var index = indices[k];
                var anchor = track.Point(index);
                var normal = track.Normal(index);
                var offset = Math.Clamp((target - anchor).Dot(normal), -limit, limit);
                offsets[index] = offset;
                positions[k] = anchor + normal * offset;
            }
        }
    }

    private static void Interpolate(int count, double[] offsets, int[] indices)
    {
        for (var k = 0; k < indices.Length; k++)
        {
            var from = indices[k];
            var to = k + 1 < indices.Length ? indices[k + 1] : indices[0] + count;
            for (var i = from + 1; i < to; i++)
            {
                var t = (double)(i - from) / (to - from);
                offsets[i % count] = offsets[from] + (offsets[to % count] - offsets[from]) * t;
            }
        }
    }

    private static double Menger(Vec2 a, Vec2 b, Vec2 c)
    {
        var ab = b - a;
        var bc = c - b;
        var denominator = ab.Length * bc.Length * (c - a).Length;
        return denominator < 1e-9 ? 0 : 2 * ab.Cross(bc) / denominator;
    }
}
