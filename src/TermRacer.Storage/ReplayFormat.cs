using TermRacer.Core;

namespace TermRacer.Storage;

public static class ReplayFormat
{
    public const string Banner = "# TermRacer replay. Header fields, then one sample per row at the given rate (samples per second).";

    public static IReadOnlyList<string> Columns { get; } = ["t", "x", "y", "heading", "speed", "distance", "throttle", "brake", "steer", "flags"];

    public static void Write(TextWriter writer, LapRecord record, Replay replay)
    {
        writer.Write(Banner + "\n");
        Field(writer, "track", replay.TrackId);
        Field(writer, "recorded_utc", Tsv.Timestamp(record.RecordedAt));
        Field(writer, "mode", Tsv.Mode(record.Mode));
        Field(writer, "lap", Tsv.Number(record.Lap));
        Field(writer, "driver", Tsv.Driver(record.Driver));
        Field(writer, "seconds", Tsv.Exact(replay.LapTime));
        Field(writer, "rate", Tsv.Number(Replay.SampleRate));
        Field(writer, "samples", Tsv.Number(replay.Samples.Count));
        writer.Write(string.Join('\t', Columns) + "\n");
        for (var k = 0; k < replay.Samples.Count; k++)
        {
            var sample = replay.Samples[k];
            writer.Write(string.Join(
                '\t',
                Tsv.Number((double)k / Replay.SampleRate, "0.000"),
                Tsv.Number(sample.Position.X, "0.00"),
                Tsv.Number(sample.Position.Y, "0.00"),
                Tsv.Number(sample.Heading, "0.0000"),
                Tsv.Number(sample.Speed, "0.00"),
                Tsv.Number(sample.Distance, "0.00"),
                Tsv.Number(sample.Throttle, "0.##"),
                Tsv.Number(sample.Brake, "0.##"),
                Tsv.Number(sample.Steer, "0.###"),
                Flags(sample)));
            writer.Write('\n');
        }
    }

    public static Replay? Read(TextReader reader)
    {
        var header = new Dictionary<string, string>(StringComparer.Ordinal);
        var samples = new List<ReplaySample>();
        var inSamples = false;
        while (reader.ReadLine() is { } raw)
        {
            if (Tsv.IsComment(raw))
            {
                continue;
            }

            var fields = Tsv.Split(raw);
            if (!inSamples)
            {
                if (fields[0] == Columns[0] && fields.Length >= Columns.Count)
                {
                    inSamples = true;
                }
                else if (fields.Length >= 2)
                {
                    header[fields[0]] = fields[1];
                }

                continue;
            }

            if (Sample(fields) is not { } sample)
            {
                return null;
            }

            samples.Add(sample);
        }

        if (!header.TryGetValue("track", out var track) || track.Length == 0
            || !header.TryGetValue("seconds", out var secondsText) || !Tsv.TryNumber(secondsText, out var seconds) || seconds <= 0 || double.IsInfinity(seconds)
            || !header.TryGetValue("rate", out var rateText) || !Tsv.TryInteger(rateText, out var rate) || rate != Replay.SampleRate
            || samples.Count < 2
            || (header.TryGetValue("samples", out var countText) && (!Tsv.TryInteger(countText, out var count) || count != samples.Count)))
        {
            return null;
        }

        return new Replay(track, seconds, samples);
    }

    private static void Field(TextWriter writer, string name, string value) => writer.Write(name + "\t" + value + "\n");

    private static string Flags(in ReplaySample sample) =>
        (sample.Autopilot, sample.OffTrack) switch
        {
            (true, true) => "AG",
            (true, false) => "A",
            (false, true) => "G",
            _ => "-",
        };

    private static ReplaySample? Sample(string[] fields)
    {
        if (fields.Length < Columns.Count)
        {
            return null;
        }

        var values = new double[8];
        for (var i = 0; i < values.Length; i++)
        {
            if (!Tsv.TryNumber(fields[i + 1], out values[i]) || double.IsInfinity(values[i]))
            {
                return null;
            }
        }

        var flags = fields[9];
        return new ReplaySample(new Vec2(values[0], values[1]), values[2], values[3], values[4], values[5], values[6], values[7], flags.Contains('A'), flags.Contains('G'));
    }
}
