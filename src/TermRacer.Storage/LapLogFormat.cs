using TermRacer.Core;

namespace TermRacer.Storage;

public static class LapLogFormat
{
    public const string Banner = "# TermRacer lap log. One lap per row, tab separated, times in seconds, timestamps in UTC.";

    public static IReadOnlyList<string> Columns { get; } =
        ["recorded_utc", "track", "mode", "lap", "driver", "seconds", "wall_hits", "off_track_seconds", "replay"];

    public static string Header => string.Join('\t', Columns);

    public static string Format(LapRecord record) => string.Join(
        '\t',
        Tsv.Timestamp(record.RecordedAt),
        record.TrackId,
        Tsv.Mode(record.Mode),
        Tsv.Number(record.Lap),
        Tsv.Driver(record.Driver),
        Tsv.Number(record.Seconds, "0.000"),
        Tsv.Number(record.WallHits),
        Tsv.Number(record.OffTrackSeconds, "0.00"),
        record.Replay ?? "-");

    public static IEnumerable<string> Lines(IEnumerable<LapRecord> records, IEnumerable<string> unreadable)
    {
        yield return Banner;
        yield return Header;
        foreach (var record in records)
        {
            yield return Format(record);
        }

        foreach (var line in unreadable)
        {
            yield return "# unreadable: " + line;
        }
    }

    public static LapLog Parse(IEnumerable<string> lines)
    {
        var records = new List<LapRecord>();
        var unreadable = new List<string>();
        IReadOnlyList<string>? columns = null;
        Dictionary<string, int> index = [];
        foreach (var raw in lines)
        {
            var line = raw.TrimEnd('\r');
            if (Tsv.IsComment(line))
            {
                continue;
            }

            var fields = Tsv.Split(line);
            if (columns is null)
            {
                if (fields.Contains("seconds"))
                {
                    columns = fields;
                    index = fields.Select((name, position) => (name, position)).GroupBy(pair => pair.name).ToDictionary(group => group.Key, group => group.First().position);
                    continue;
                }

                columns = [];
                index = Columns.Select((name, position) => (name, position)).ToDictionary(pair => pair.name, pair => pair.position);
            }

            if (TryRecord(fields, index) is { } record)
            {
                records.Add(record);
            }
            else
            {
                unreadable.Add(line);
            }
        }

        return new LapLog(records, columns ?? Columns, unreadable);
    }

    private static LapRecord? TryRecord(string[] fields, Dictionary<string, int> index)
    {
        string? Field(string name) => index.TryGetValue(name, out var position) && position < fields.Length ? fields[position] : null;

        if (Field("recorded_utc") is not { } recordedText || !Tsv.TryTimestamp(recordedText, out var recorded)
            || Field("track") is not { Length: > 0 } track
            || Field("mode") is not { } modeText || Tsv.ParseMode(modeText) is not { } mode
            || Field("lap") is not { } lapText || !Tsv.TryInteger(lapText, out var lap) || lap < 1
            || Field("driver") is not { } driverText || Tsv.ParseDriver(driverText) is not { } driver
            || Field("seconds") is not { } secondsText || !Tsv.TryNumber(secondsText, out var seconds) || seconds <= 0 || double.IsInfinity(seconds))
        {
            return null;
        }

        var walls = Field("wall_hits") is { } wallText && Tsv.TryInteger(wallText, out var parsedWalls) ? parsedWalls : 0;
        var offTrack = Field("off_track_seconds") is { } offText && Tsv.TryNumber(offText, out var parsedOff) && !double.IsInfinity(parsedOff) ? parsedOff : 0;
        var replay = Field("replay") is { Length: > 0 } name && name != "-" ? name : null;
        return new LapRecord(recorded, track, mode, lap, driver, seconds, walls, offTrack, replay);
    }
}
