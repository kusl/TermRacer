namespace TermRacer.Core;

public static class CarPhysics
{
    public const double ReverseThreshold = 0.5;

    public static double Grip(CarSpec spec, Surface surface) => surface == Surface.Grass ? spec.GrassGrip : spec.TarmacGrip;

    public static double MaxYawRate(CarSpec spec, double speed, Surface surface)
    {
        var magnitude = Math.Abs(speed);
        return Math.Min(magnitude * Math.Tan(spec.MaxSteerAngle) / spec.Wheelbase, Grip(spec, surface) / Math.Max(magnitude, 0.5));
    }

    public static CarState Step(in CarState state, in ControlInput input, CarSpec spec, Surface surface, double dt)
    {
        var onGrass = surface == Surface.Grass;
        var grip = Grip(spec, surface);
        var forward = state.Forward;
        var forwardSpeed = state.Velocity.Dot(forward);
        var lateralSpeed = state.Velocity.Dot(forward.Perpendicular);
        var throttle = Math.Clamp(input.Throttle, 0, 1);
        var brake = Math.Clamp(input.Brake, 0, 1);
        var steer = Geometry.MoveTowards(state.Steer, Math.Clamp(input.Steer, -1, 1), spec.SteerRate * dt);

        var acceleration = 0.0;
        var reversing = false;
        if (throttle > 0)
        {
            var power = spec.Power * (onGrass ? spec.GrassPowerFactor : 1);
            acceleration += throttle * Math.Min(spec.MaxAcceleration, power / Math.Max(Math.Abs(forwardSpeed), 1));
        }

        if (brake > 0)
        {
            if (forwardSpeed > ReverseThreshold)
            {
                acceleration -= brake * spec.BrakeDeceleration;
            }
            else if (throttle <= 0)
            {
                acceleration -= brake * spec.ReverseAcceleration;
                reversing = true;
            }
        }

        acceleration -= spec.AeroDrag * forwardSpeed * Math.Abs(forwardSpeed);
        var speed = forwardSpeed + acceleration * dt;
        if (reversing)
        {
            speed = Math.Max(speed, Math.Min(forwardSpeed, -spec.MaxReverseSpeed));
        }

        speed = Geometry.MoveTowards(speed, 0, (spec.RollingResistance + (onGrass ? spec.GrassDrag : 0)) * dt);

        var yawRate = steer * MaxYawRate(spec, speed, surface) * Math.Sign(speed);
        var heading = Geometry.NormalizeAngle(state.Heading + yawRate * dt);
        var remainingGrip = Math.Max(grip - Math.Abs(speed * yawRate), grip * 0.3);
        var lateral = Geometry.MoveTowards(lateralSpeed, 0, remainingGrip * dt);
        var direction = Vec2.FromAngle(heading);
        var velocity = direction * speed + direction.Perpendicular * lateral;
        return new CarState(state.Position + velocity * dt, heading, velocity, steer, yawRate);
    }
}
