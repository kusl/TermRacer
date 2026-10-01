using TermRacer.Core;

namespace TermRacer.Storage;

public sealed record LapLog(IReadOnlyList<LapRecord> Records, IReadOnlyList<string> Columns, IReadOnlyList<string> Unreadable)
{
    public bool Current => Unreadable.Count == 0 && Columns.SequenceEqual(LapLogFormat.Columns);
}
