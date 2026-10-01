using System.Globalization;
using TermRacer.Core;

namespace TermRacer.Storage;

internal static class Tsv
{
    public static string Number(double value, string format) => value.ToString(format, CultureInfo.InvariantCulture);

    public static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    public static string Exact(double value) =>
        double.IsPositiveInfinity(value) ? "inf" : value.ToString("R", CultureInfo.InvariantCulture);

    public static bool TryNumber(string text, out double value)
    {
        if (text == "inf")
        {
            value = double.PositiveInfinity;
            return true;
        }

        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !double.IsNaN(value) && !double.IsNegativeInfinity(value);
    }

    public static bool TryInteger(string text, out int value) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

    public static string Timestamp(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

    public static bool TryTimestamp(string text, out DateTimeOffset value) =>
        DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out value);

    public static string Mode(RaceMode mode) => mode == RaceMode.SingleLap ? "single" : "zen";

    public static RaceMode? ParseMode(string text) => text switch
    {
        "single" => RaceMode.SingleLap,
        "zen" => RaceMode.Zen,
        _ => null,
    };

    public static string Driver(Core.Driver driver) => driver switch
    {
        Core.Driver.Autopilot => "autopilot",
        Core.Driver.Mixed => "mixed",
        _ => "manual",
    };

    public static Core.Driver? ParseDriver(string text) => text switch
    {
        "manual" => Core.Driver.Manual,
        "autopilot" => Core.Driver.Autopilot,
        "mixed" => Core.Driver.Mixed,
        _ => null,
    };

    public static string[] Split(string line) => line.TrimEnd('\r').Split('\t');

    public static bool IsComment(string line) => line.Length == 0 || line[0] == '#' || line.All(char.IsWhiteSpace);
}
