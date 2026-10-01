namespace TermRacer.Core;

public sealed class ReplayPlayer(LapRecord record, Replay replay, Replay? ghost = null)
{
    public const double SeekStep = 5;

    private static readonly double[] Rates = [0.25, 0.5, 1, 2, 4];

    private int rate = 2;

    public LapRecord Record => record;

    public Replay Replay => replay;

    public Replay? Ghost => ghost;

    public double Time { get; private set; }

    public bool Paused { get; private set; }

    public double Rate => Rates[rate];

    public bool AtEnd => Time >= replay.LapTime;

    public ReplaySample Sample => replay.At(Time);

    public CarState Car => replay.CarAt(Time);

    public CarState? GhostCar => ghost is { } other && Time <= other.LapTime ? other.CarAt(Time) : null;

    public double? GhostGap => ghost?.TimeAt(Sample.Distance) is { } at ? Time - at : null;

    public void Advance(double seconds)
    {
        if (!Paused && seconds > 0)
        {
            Time = Math.Min(replay.LapTime, Time + seconds * Rate);
        }
    }

    public void TogglePause()
    {
        if (AtEnd)
        {
            Time = 0;
            Paused = false;
            return;
        }

        Paused = !Paused;
    }

    public void Seek(double seconds) => Time = Math.Clamp(Time + seconds, 0, replay.LapTime);

    public void Faster() => rate = Math.Min(Rates.Length - 1, rate + 1);

    public void Slower() => rate = Math.Max(0, rate - 1);
}
