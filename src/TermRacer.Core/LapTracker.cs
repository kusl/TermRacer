namespace TermRacer.Core;

public sealed class LapTracker(Track track, RaceMode mode)
{
    private readonly List<double> laps = [];

    public RaceMode Mode => mode;

    public RacePhase Phase { get; private set; }

    public int NextGate { get; private set; }

    public double LapStart { get; private set; }

    public IReadOnlyList<double> Laps => laps;

    public double? LastLap => laps.Count > 0 ? laps[^1] : null;

    public double? BestLap => laps.Count > 0 ? laps.Min() : null;

    public int CurrentLap => Phase == RacePhase.Finished ? laps.Count : laps.Count + 1;

    public double CurrentLapTime(double now) => Phase switch
    {
        RacePhase.Running => now - LapStart,
        RacePhase.Finished => laps[^1],
        _ => 0,
    };

    public LapEvent Update(Vec2 from, Vec2 to, double fromTime, double toTime)
    {
        if (Phase == RacePhase.Finished)
        {
            return LapEvent.None;
        }

        var gate = track.Gates[Phase == RacePhase.Waiting ? 0 : NextGate];
        if ((to - from).Dot(gate.Direction) <= 0 || !Geometry.TryIntersect(from, to, gate.A, gate.B, out var t))
        {
            return LapEvent.None;
        }

        var time = fromTime + (toTime - fromTime) * t;
        var firstCheckpoint = track.Gates.Count > 1 ? 1 : 0;
        if (Phase == RacePhase.Waiting)
        {
            Phase = RacePhase.Running;
            LapStart = time;
            NextGate = firstCheckpoint;
            return LapEvent.Started;
        }

        if (NextGate != 0)
        {
            NextGate = (NextGate + 1) % track.Gates.Count;
            return LapEvent.Checkpoint;
        }

        laps.Add(time - LapStart);
        LapStart = time;
        NextGate = firstCheckpoint;
        if (mode == RaceMode.SingleLap)
        {
            Phase = RacePhase.Finished;
        }

        return LapEvent.Completed;
    }
}
