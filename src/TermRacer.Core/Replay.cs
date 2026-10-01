namespace TermRacer.Core;

public sealed class Replay
{
    public const int SampleRate = 30;

    private readonly ReplaySample[] samples;
    private readonly double[] reach;

    public Replay(string trackId, double lapTime, IEnumerable<ReplaySample> samples)
    {
        TrackId = trackId;
        LapTime = lapTime;
        this.samples = [.. samples];
        if (this.samples.Length == 0)
        {
            throw new ArgumentException("A replay needs at least one sample.", nameof(samples));
        }

        reach = new double[this.samples.Length];
        var furthest = double.NegativeInfinity;
        for (var i = 0; i < this.samples.Length; i++)
        {
            furthest = Math.Max(furthest, this.samples[i].Distance);
            reach[i] = furthest;
        }
    }

    public string TrackId { get; }

    public double LapTime { get; }

    public IReadOnlyList<ReplaySample> Samples => samples;

    public double Duration => (samples.Length - 1) / (double)SampleRate;

    public ReplaySample At(double time)
    {
        var position = Math.Max(0, time) * SampleRate;
        if (position >= samples.Length - 1)
        {
            return samples[^1];
        }

        var index = (int)position;
        var a = samples[index];
        var b = samples[index + 1];
        var t = position - index;
        return a with
        {
            Position = Vec2.Lerp(a.Position, b.Position, t),
            Heading = Geometry.NormalizeAngle(a.Heading + Geometry.NormalizeAngle(b.Heading - a.Heading) * t),
            Speed = a.Speed + (b.Speed - a.Speed) * t,
            Distance = a.Distance + (b.Distance - a.Distance) * t,
            Steer = a.Steer + (b.Steer - a.Steer) * t,
        };
    }

    public CarState CarAt(double time)
    {
        var sample = At(time);
        return new CarState(sample.Position, sample.Heading, Vec2.FromAngle(sample.Heading) * sample.Speed, sample.Steer);
    }

    public double? TimeAt(double distance)
    {
        if (double.IsNaN(distance) || distance > reach[^1])
        {
            return null;
        }

        if (distance <= reach[0])
        {
            return 0;
        }

        var low = 1;
        var high = reach.Length - 1;
        while (low < high)
        {
            var middle = (low + high) / 2;
            if (reach[middle] < distance)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        var before = reach[low - 1];
        var span = reach[low] - before;
        var fraction = span > 1e-9 ? (distance - before) / span : 1;
        return (low - 1 + fraction) / SampleRate;
    }
}
