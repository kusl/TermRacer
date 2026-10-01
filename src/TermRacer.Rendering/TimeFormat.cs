using System.Globalization;

namespace TermRacer.Rendering;

public static class TimeFormat
{
    public const string Empty = "-:--.---";

    public static string Lap(double? seconds)
    {
        if (seconds is not { } value || double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            return Empty;
        }

        var milliseconds = (long)Math.Round(value * 1000, MidpointRounding.AwayFromZero);
        var rest = milliseconds % 60000;
        return string.Create(CultureInfo.InvariantCulture, $"{milliseconds / 60000}:{rest / 1000:00}.{rest % 1000:000}");
    }

    public static string Gap(double? seconds)
    {
        if (seconds is not { } value || double.IsNaN(value) || double.IsInfinity(value))
        {
            return "-.---";
        }

        var milliseconds = (long)Math.Round(Math.Abs(value) * 1000, MidpointRounding.AwayFromZero);
        var sign = value < 0 && milliseconds > 0 ? "-" : "+";
        return milliseconds < 60000
            ? string.Create(CultureInfo.InvariantCulture, $"{sign}{milliseconds / 1000}.{milliseconds % 1000:000}")
            : sign + Lap(milliseconds / 1000.0);
    }

    public static string Moment(DateTimeOffset moment) =>
        moment.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    public static string Speed(double metersPerSecond) =>
        string.Create(CultureInfo.InvariantCulture, $"{metersPerSecond * 3.6:0} km/h");
}
