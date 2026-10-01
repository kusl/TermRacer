namespace TermRacer.Core;

public sealed record CompletedLap(
    RaceMode Mode,
    int Number,
    double Seconds,
    Driver Driver,
    int WallHits,
    double OffTrackSeconds,
    Replay Replay,
    IReadOnlyList<int> FaultPoints)
{
    public bool StandingStart => Number <= 1;
}
