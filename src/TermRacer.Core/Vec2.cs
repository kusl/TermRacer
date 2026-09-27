namespace TermRacer.Core;

public readonly record struct Vec2(double X, double Y)
{
    public static Vec2 Zero => default;

    public double Length => Math.Sqrt(X * X + Y * Y);

    public double LengthSquared => X * X + Y * Y;

    public double Angle => Math.Atan2(Y, X);

    public Vec2 Perpendicular => new(-Y, X);

    public static Vec2 FromAngle(double radians)
    {
        var (sin, cos) = Math.SinCos(radians);
        return new Vec2(cos, sin);
    }

    public static Vec2 Lerp(Vec2 from, Vec2 to, double t) => from + (to - from) * t;

    public Vec2 Normalized()
    {
        var length = Length;
        return length > 1e-12 ? new Vec2(X / length, Y / length) : Zero;
    }

    public Vec2 Rotate(double radians)
    {
        var (sin, cos) = Math.SinCos(radians);
        return new Vec2(X * cos - Y * sin, X * sin + Y * cos);
    }

    public double Dot(Vec2 other) => X * other.X + Y * other.Y;

    public double Cross(Vec2 other) => X * other.Y - Y * other.X;

    public double DistanceTo(Vec2 other) => (other - this).Length;

    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);

    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);

    public static Vec2 operator -(Vec2 value) => new(-value.X, -value.Y);

    public static Vec2 operator *(Vec2 value, double scale) => new(value.X * scale, value.Y * scale);

    public static Vec2 operator *(double scale, Vec2 value) => new(value.X * scale, value.Y * scale);

    public static Vec2 operator /(Vec2 value, double scale) => new(value.X / scale, value.Y / scale);
}
