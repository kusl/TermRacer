using TermRacer.Core;

namespace TermRacer.Rendering;

public sealed class ChaseCamera(double metersPerPixel = 0.72, double leadSeconds = 0.85, double leadFraction = 0.3, double response = 2.8)
{
    private object? subject;
    private Vec2 lead;
    private double time;

    public Camera Frame(RaceSession race, int width, int height) => Frame(race, race.Car, race.Time, width, height);

    public Camera Frame(object target, in CarState car, double now, int width, int height)
    {
        var desired = Desired(car, width, height);
        if (!ReferenceEquals(target, subject))
        {
            subject = target;
            lead = desired;
            time = now;
        }

        var elapsed = Math.Max(0, now - time);
        time = now;
        lead = Vec2.Lerp(lead, desired, 1 - Math.Exp(-response * elapsed));
        var center = car.Position + lead;
        var snapped = new Vec2(Math.Round(center.X / metersPerPixel) * metersPerPixel, Math.Round(center.Y / metersPerPixel) * metersPerPixel);
        return new Camera(snapped, metersPerPixel, width, height);
    }

    private Vec2 Desired(in CarState car, int width, int height)
    {
        var target = car.Velocity * leadSeconds;
        var limitX = width * metersPerPixel * leadFraction;
        var limitY = height * metersPerPixel * leadFraction;
        return new Vec2(Math.Clamp(target.X, -limitX, limitX), Math.Clamp(target.Y, -limitY, limitY));
    }
}
