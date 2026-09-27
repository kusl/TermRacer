using TermRacer.Core;

namespace TermRacer.Rendering;

public sealed class ChaseCamera(double metersPerPixel = 0.72, double leadSeconds = 0.85, double leadFraction = 0.3, double response = 2.8)
{
    private RaceSession? session;
    private Vec2 lead;
    private double time;

    public Camera Frame(RaceSession race, int width, int height)
    {
        var desired = Desired(race, width, height);
        if (!ReferenceEquals(race, session))
        {
            session = race;
            lead = desired;
            time = race.Time;
        }

        var elapsed = Math.Max(0, race.Time - time);
        time = race.Time;
        lead = Vec2.Lerp(lead, desired, 1 - Math.Exp(-response * elapsed));
        var center = race.Car.Position + lead;
        var snapped = new Vec2(Math.Round(center.X / metersPerPixel) * metersPerPixel, Math.Round(center.Y / metersPerPixel) * metersPerPixel);
        return new Camera(snapped, metersPerPixel, width, height);
    }

    private Vec2 Desired(RaceSession race, int width, int height)
    {
        var target = race.Car.Velocity * leadSeconds;
        var limitX = width * metersPerPixel * leadFraction;
        var limitY = height * metersPerPixel * leadFraction;
        return new Vec2(Math.Clamp(target.X, -limitX, limitX), Math.Clamp(target.Y, -limitY, limitY));
    }
}
