namespace TermRacer.Core;

public sealed record LapRecord(
    DateTimeOffset RecordedAt,
    string TrackId,
    RaceMode Mode,
    int Lap,
    Driver Driver,
    double Seconds,
    int WallHits = 0,
    double OffTrackSeconds = 0,
    string? Replay = null)
{
    public bool StandingStart => Lap <= 1;
}
