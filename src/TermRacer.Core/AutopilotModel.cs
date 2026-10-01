namespace TermRacer.Core;

public sealed record AutopilotModel(string TrackId, int Laps, IReadOnlyList<Corner> Corners, IReadOnlyList<CornerFactor> Factors);
