using TermRacer.Core;

namespace TermRacer.Storage;

public static class AutopilotFormat
{
    public const string Banner = "# TermRacer autopilot training. One speed factor per corner: safe is proven, limit is the lowest that went wrong, trial is the next attempt.";

    public static IReadOnlyList<string> Columns { get; } = ["corner", "start", "length", "safe", "limit", "trial"];

    public static void Write(TextWriter writer, AutopilotModel model)
    {
        writer.Write(Banner + "\n");
        writer.Write("track\t" + model.TrackId + "\n");
        writer.Write("laps\t" + Tsv.Number(model.Laps) + "\n");
        writer.Write(string.Join('\t', Columns) + "\n");
        for (var c = 0; c < model.Corners.Count; c++)
        {
            var corner = model.Corners[c];
            var factor = model.Factors[c];
            writer.Write(string.Join(
                '\t',
                Tsv.Number(c),
                Tsv.Number(corner.Start),
                Tsv.Number(corner.Length),
                Tsv.Exact(factor.Safe),
                Tsv.Exact(factor.Limit),
                Tsv.Exact(factor.Trial)) + "\n");
        }
    }

    public static AutopilotModel? Read(TextReader reader)
    {
        string? track = null;
        var laps = -1;
        var corners = new List<Corner>();
        var factors = new List<CornerFactor>();
        var inRows = false;
        while (reader.ReadLine() is { } raw)
        {
            if (Tsv.IsComment(raw))
            {
                continue;
            }

            var fields = Tsv.Split(raw);
            if (!inRows)
            {
                switch (fields[0])
                {
                    case "track" when fields.Length >= 2:
                        track = fields[1];
                        break;
                    case "laps" when fields.Length >= 2 && Tsv.TryInteger(fields[1], out var count):
                        laps = count;
                        break;
                    case "corner":
                        inRows = true;
                        break;
                }

                continue;
            }

            if (fields.Length < Columns.Count
                || !Tsv.TryInteger(fields[0], out var index) || index != corners.Count
                || !Tsv.TryInteger(fields[1], out var start) || !Tsv.TryInteger(fields[2], out var length)
                || !Tsv.TryNumber(fields[3], out var safe) || !Tsv.TryNumber(fields[4], out var limit) || !Tsv.TryNumber(fields[5], out var trial))
            {
                return null;
            }

            corners.Add(new Corner(start, length));
            factors.Add(new CornerFactor(safe, limit, trial));
        }

        return track is { Length: > 0 } && laps >= 0 && inRows ? new AutopilotModel(track, laps, corners, factors) : null;
    }
}
