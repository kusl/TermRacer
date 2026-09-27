namespace TermRacer.Core;

public sealed class SpeedProfile
{
    private readonly RacingLine line;
    private readonly double[] speeds;

    public SpeedProfile(RacingLine line, CarSpec spec, double gripFactor = 0.9, double brakeFactor = 0.72, double topSpeed = 44)
    {
        this.line = line;
        var count = line.Count;
        speeds = new double[count];
        var lateral = spec.TarmacGrip * gripFactor;
        for (var i = 0; i < count; i++)
        {
            var curvature = Math.Max(Math.Abs(line.Curvature(i - 1)), Math.Max(Math.Abs(line.Curvature(i)), Math.Abs(line.Curvature(i + 1))));
            speeds[i] = Math.Min(topSpeed, Math.Sqrt(lateral / Math.Max(curvature, 1e-6)));
        }

        BrakeDeceleration = spec.BrakeDeceleration * brakeFactor;
        for (var pass = 0; pass < 2; pass++)
        {
            for (var i = count - 1; i >= 0; i--)
            {
                var next = speeds[(i + 1) % count];
                speeds[i] = Math.Min(speeds[i], Math.Sqrt(next * next + 2 * BrakeDeceleration * line.SegmentLength(i)));
            }
        }
    }

    public double BrakeDeceleration { get; }

    public int Count => speeds.Length;

    public double this[int index] => speeds[line.Track.Wrap(index)];

    public double MinimumAhead(int index, double distance)
    {
        var i = line.Track.Wrap(index);
        var minimum = speeds[i];
        for (var covered = 0.0; covered < distance; i = (i + 1) % speeds.Length)
        {
            minimum = Math.Min(minimum, speeds[i]);
            covered += line.SegmentLength(i);
        }

        return minimum;
    }
}
