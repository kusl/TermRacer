namespace TermRacer.Core;

public readonly record struct CarState(Vec2 Position, double Heading, Vec2 Velocity, double Steer = 0, double YawRate = 0)
{
    public Vec2 Forward => Vec2.FromAngle(Heading);

    public double ForwardSpeed => Velocity.Dot(Forward);

    public double Speed => Velocity.Length;

    public static CarState At(Pose pose) => new(pose.Position, pose.Heading, Vec2.Zero);
}
