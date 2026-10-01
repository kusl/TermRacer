namespace TermRacer.Core;

public sealed class LapRecorder(string trackId, double step)
{
    private readonly List<TelemetryFrame> frames = [];
    private TelemetryFrame? last;
    private double lapStart;
    private double lapOdometer;
    private int wallHitsAtStart;

    public bool Recording { get; private set; }

    public int FrameCount => frames.Count;

    public void Add(in TelemetryFrame frame)
    {
        if (Recording)
        {
            frames.Add(frame);
        }

        last = frame;
    }

    public void Begin(double start, double odometer, int wallHits, in TelemetryFrame current)
    {
        frames.Clear();
        if (last is { } before)
        {
            frames.Add(before);
        }

        frames.Add(current);
        last = current;
        lapStart = start;
        lapOdometer = odometer;
        wallHitsAtStart = wallHits;
        Recording = true;
    }

    public CompletedLap Complete(RaceMode mode, int number, double seconds, double finish, double finishOdometer, int wallHits, in TelemetryFrame current)
    {
        frames.Add(current);
        var lap = Summarize(mode, number, seconds, wallHits);
        Begin(finish, finishOdometer, wallHits, current);
        return lap;
    }

    private CompletedLap Summarize(RaceMode mode, int number, double seconds, int wallHits)
    {
        var automatic = 0;
        var manual = 0;
        var grass = 0;
        var faults = new SortedSet<int>();
        foreach (var frame in frames)
        {
            if (frame.Time <= lapStart)
            {
                continue;
            }

            if (frame.Autopilot)
            {
                automatic++;
            }
            else
            {
                manual++;
            }

            grass += frame.OffTrack ? 1 : 0;
            if (frame.Fault)
            {
                faults.Add(frame.Segment);
            }
        }

        var driver = manual == 0 ? Driver.Autopilot : automatic == 0 ? Driver.Manual : Driver.Mixed;
        return new CompletedLap(mode, number, seconds, driver, wallHits - wallHitsAtStart, grass * step, Resample(seconds), [.. faults]);
    }

    private Replay Resample(double seconds)
    {
        var end = frames[^1].Time - lapStart;
        var count = Math.Max(1, (int)Math.Floor(end * Replay.SampleRate + 1e-9) + 1);
        var samples = new ReplaySample[count];
        var j = 0;
        for (var k = 0; k < count; k++)
        {
            var time = lapStart + (double)k / Replay.SampleRate;
            while (j < frames.Count - 2 && frames[j + 1].Time <= time)
            {
                j++;
            }

            var a = frames[j];
            var b = frames[Math.Min(j + 1, frames.Count - 1)];
            var span = b.Time - a.Time;
            var t = span > 1e-12 ? Math.Clamp((time - a.Time) / span, 0, 1) : 0;
            samples[k] = new ReplaySample(
                Vec2.Lerp(a.Car.Position, b.Car.Position, t),
                Geometry.NormalizeAngle(a.Car.Heading + Geometry.NormalizeAngle(b.Car.Heading - a.Car.Heading) * t),
                a.Car.ForwardSpeed + (b.Car.ForwardSpeed - a.Car.ForwardSpeed) * t,
                a.Odometer + (b.Odometer - a.Odometer) * t - lapOdometer,
                b.Input.Throttle,
                b.Input.Brake,
                a.Car.Steer + (b.Car.Steer - a.Car.Steer) * t,
                b.Autopilot,
                b.OffTrack);
        }

        return new Replay(trackId, seconds, samples);
    }
}
