namespace TermRacer.Core;

public static class Geometry
{
    public static double MoveTowards(double current, double target, double maxDelta) =>
        Math.Abs(target - current) <= maxDelta ? target : current + Math.Sign(target - current) * maxDelta;

    public static double NormalizeAngle(double radians) => Math.IEEERemainder(radians, Math.Tau);

    public static double ClosestParameter(Vec2 start, Vec2 end, Vec2 point)
    {
        var segment = end - start;
        var lengthSquared = segment.LengthSquared;
        return lengthSquared < 1e-12 ? 0 : Math.Clamp((point - start).Dot(segment) / lengthSquared, 0, 1);
    }

    public static bool TryIntersect(Vec2 from, Vec2 to, Vec2 lineStart, Vec2 lineEnd, out double t)
    {
        var path = to - from;
        var line = lineEnd - lineStart;
        var denominator = path.Cross(line);
        t = 0;
        if (Math.Abs(denominator) < 1e-12)
        {
            return false;
        }

        var offset = lineStart - from;
        t = offset.Cross(line) / denominator;
        var u = offset.Cross(path) / denominator;
        return t is >= 0 and < 1 && u is >= 0 and <= 1;
    }
}
