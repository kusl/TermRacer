namespace TermRacer.Core;

public sealed class FixedStepClock(double step, double maxElapsed = 0.25)
{
    private double accumulator;

    public double Step => step;

    public int Advance(double elapsedSeconds)
    {
        accumulator += Math.Clamp(elapsedSeconds, 0, maxElapsed);
        var steps = (int)Math.Floor(accumulator / step + 1e-9);
        accumulator = Math.Max(0, accumulator - steps * step);
        return steps;
    }

    public void Reset() => accumulator = 0;
}
