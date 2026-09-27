namespace TermRacer.Core;

public sealed class Autopilot(RacingLine line, SpeedProfile profile, CarSpec spec)
{
    private int index = -1;
    private double stuckTime;
    private double reverseTime;

    public void Reset()
    {
        index = -1;
        stuckTime = 0;
        reverseTime = 0;
    }

    public ControlInput Drive(in CarState car, Surface surface, double dt)
    {
        var (segment, t) = line.Locate(car.Position, index);
        index = segment;
        var forward = car.Forward;
        var speed = car.Velocity.Dot(forward);
        var lookahead = Math.Clamp(5 + 0.45 * Math.Abs(speed), 7, 26);
        var target = line.PointAhead(segment, t, lookahead) - car.Position;
        var alpha = Math.Atan2(target.Dot(forward.Perpendicular), target.Dot(forward));

        if (reverseTime > 0)
        {
            reverseTime -= dt;
            return new ControlInput(0, 1, -Math.Sign(alpha));
        }

        var magnitude = Math.Max(Math.Abs(speed), 2);
        var curvature = 2 * Math.Sin(alpha) / Math.Max(target.Length, 1);
        var steer = Math.Clamp(magnitude * curvature / CarPhysics.MaxYawRate(spec, magnitude, surface), -1, 1);

        var targetSpeed = profile.MinimumAhead(segment, 6 + Math.Abs(speed) * 0.45);
        if (surface == Surface.Grass)
        {
            targetSpeed = Math.Min(targetSpeed, 16);
        }

        if (Math.Abs(alpha) > 1.0)
        {
            targetSpeed = Math.Min(targetSpeed, 9);
        }

        var error = targetSpeed - speed;
        var brake = Math.Clamp(-error * 0.4, 0, 1);
        var throttle = brake > 0.05 ? 0 : Math.Clamp(0.25 + error * 0.9, 0, 1);

        stuckTime = throttle > 0.5 && Math.Abs(speed) < 1 ? stuckTime + dt : 0;
        if (stuckTime > 1.2)
        {
            stuckTime = 0;
            reverseTime = 1.3;
        }

        return new ControlInput(throttle, brake, steer);
    }
}
