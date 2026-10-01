using System.Globalization;
using TermRacer.Core;

namespace TermRacer.Rendering;

public static class Hud
{
    private const int Spacing = 2;

    private static readonly (string Keys, string Action)[] RaceHints =
    [
        ("↑ W", "throttle"),
        ("↓ S", "brake"),
        ("← → A D", "steer"),
        ("Tab", "autopilot"),
        ("G", "ghost"),
        ("Bksp", "menu"),
        ("Esc", "quit"),
    ];

    private static readonly (string Keys, string Action)[] ReplayHints =
    [
        ("← →", "seek"),
        ("↑ ↓", "speed"),
        ("Enter", "pause"),
        ("G", "ghost"),
        ("Bksp", "back"),
        ("Esc", "quit"),
    ];

    public static void TopBar(CellBuffer target, RaceSession race, bool ghostVisible)
    {
        var x = Start(target);
        x = Label(target, x, race.Mode == RaceMode.SingleLap ? "Single lap" : "Zen mode");
        var laps = race.Laps;
        x = Field(target, x, "Lap", race.Mode == RaceMode.SingleLap ? "1/1" : laps.CurrentLap.ToString(CultureInfo.InvariantCulture));
        x = Field(target, x, "Time", laps.Phase == RacePhase.Waiting ? TimeFormat.Empty : TimeFormat.Lap(laps.CurrentLapTime(race.Time)));
        if (race.Mode == RaceMode.Zen)
        {
            x = Field(target, x, "Last", TimeFormat.Lap(laps.LastLap));
            x = Field(target, x, "Best", TimeFormat.Lap(laps.BestLap));
        }

        if (race.Ghost is not null)
        {
            x = GhostField(target, x, race.GhostGap, ghostVisible);
        }

        Field(target, x, "Speed", TimeFormat.Speed(race.Car.Speed));
        Badge(target, race.AutopilotEngaged ? " Autopilot " : " Manual ", race.AutopilotEngaged ? Palette.Orange : Palette.Powder);
    }

    public static void ReplayTopBar(CellBuffer target, ReplayPlayer player, bool ghostVisible)
    {
        var x = Start(target);
        x = Label(target, x, "Replay");
        x = target.Write(x, 0, Describe(player.Record), Palette.Cream, Palette.Navy) + Spacing;
        x = Field(target, x, "Time", $"{TimeFormat.Lap(player.Time)} / {TimeFormat.Lap(player.Replay.LapTime)}");
        if (player.Ghost is not null)
        {
            x = GhostField(target, x, player.GhostGap, ghostVisible);
        }

        Field(target, x, "Speed", TimeFormat.Speed(Math.Abs(player.Sample.Speed)));
        var badge = player.Paused ? " Paused " : string.Create(CultureInfo.InvariantCulture, $" {player.Rate:0.##}x ");
        Badge(target, badge, player.Paused ? Palette.Orange : Palette.Powder);
    }

    public static void BottomBar(CellBuffer target, RaceSession race)
    {
        var y = Hints(target, RaceHints);
        var right = Indicators(target, y, race.Input.Throttle > 0, race.Input.Brake > 0);
        if (race.Surface == Surface.Grass)
        {
            WriteRight(target, right, y, "Off track", Palette.Orange);
        }
        else if (race.AutopilotIntervening)
        {
            WriteRight(target, right, y, "Lifting", Palette.Powder);
        }
    }

    public static void ReplayBottomBar(CellBuffer target, ReplayPlayer player)
    {
        var y = Hints(target, ReplayHints);
        var sample = player.Sample;
        var right = Indicators(target, y, sample.Throttle > 0, sample.Brake > 0);
        if (sample.OffTrack)
        {
            WriteRight(target, right, y, "Off track", Palette.Orange);
        }
    }

    public static void Banner(CellBuffer target, RaceSession race, int row, bool record = false) => Show(target, row, BannerText(race, record));

    public static void Message(CellBuffer target, int row, string text, Rgb background) => Show(target, row, (text, background));

    public static string Describe(LapRecord record)
    {
        var lap = record.Mode == RaceMode.SingleLap ? "Single lap" : string.Create(CultureInfo.InvariantCulture, $"Zen lap {record.Lap}");
        return $"{lap} · {DriverName(record.Driver)}";
    }

    public static string DriverName(Driver driver) => driver switch
    {
        Driver.Autopilot => "autopilot",
        Driver.Mixed => "mixed",
        _ => "manual",
    };

    public static void Box(CellBuffer target, int left, int top, int width, int height, Rgb border, Rgb background)
    {
        for (var x = left + 1; x < left + width - 1; x++)
        {
            target.Write(x, top, "─", border, background);
            target.Write(x, top + height - 1, "─", border, background);
        }

        for (var y = top + 1; y < top + height - 1; y++)
        {
            target.Write(left, y, "│", border, background);
            target.Write(left + width - 1, y, "│", border, background);
        }

        target.Write(left, top, "╭", border, background);
        target.Write(left + width - 1, top, "╮", border, background);
        target.Write(left, top + height - 1, "╰", border, background);
        target.Write(left + width - 1, top + height - 1, "╯", border, background);
    }

    public static void KeyHints(CellBuffer target, int y, (string Keys, string Action)[] hints, Rgb background)
    {
        var width = hints.Sum(hint => hint.Keys.Length + 1 + hint.Action.Length) + 5 * (hints.Length - 1);
        var x = (target.Width - width) / 2;
        foreach (var (keys, action) in hints)
        {
            x = target.Write(x, y, keys, Palette.Powder, background) + 1;
            x = target.Write(x, y, action, Palette.PowderDim, background) + 5;
        }
    }

    private static int Start(CellBuffer target)
    {
        target.Fill(0, 0, target.Width, 1, new Cell(' ', Palette.Cream, Palette.Navy));
        return target.Write(1, 0, "TermRacer", Palette.Orange, Palette.Navy) + Spacing;
    }

    private static int Label(CellBuffer target, int x, string text) => target.Write(x, 0, text, Palette.Powder, Palette.Navy) + Spacing;

    private static void Badge(CellBuffer target, string text, Rgb background) =>
        target.Write(target.Width - text.Length - 1, 0, text, Palette.Navy, background);

    private static int Field(CellBuffer target, int x, string label, string value)
    {
        x = target.Write(x, 0, label, Palette.PowderDim, Palette.Navy) + 1;
        return target.Write(x, 0, value, Palette.Cream, Palette.Navy) + Spacing;
    }

    private static int GhostField(CellBuffer target, int x, double? gap, bool visible)
    {
        x = target.Write(x, 0, "Ghost", Palette.PowderDim, Palette.Navy) + 1;
        if (!visible)
        {
            return target.Write(x, 0, "off", Palette.PowderDim, Palette.Navy) + Spacing;
        }

        var color = gap switch
        {
            < -0.0005 => Palette.Ahead,
            > 0.0005 => Palette.Warning,
            _ => Palette.Cream,
        };
        return target.Write(x, 0, TimeFormat.Gap(gap), color, Palette.Navy) + Spacing;
    }

    private static int Hints(CellBuffer target, (string Keys, string Action)[] hints)
    {
        var y = target.Height - 1;
        target.Fill(0, y, target.Width, 1, new Cell(' ', Palette.Cream, Palette.Navy));
        var x = 1;
        foreach (var (keys, action) in hints)
        {
            x = target.Write(x, y, keys, Palette.Powder, Palette.Navy) + 1;
            x = target.Write(x, y, action, Palette.PowderDim, Palette.Navy) + Spacing;
        }

        return y;
    }

    private static int Indicators(CellBuffer target, int y, bool throttle, bool brake)
    {
        var right = WriteRight(target, target.Width - 1, y, "Brake", brake ? Palette.Warning : Palette.PowderDim) - 2;
        return WriteRight(target, right, y, "Throttle", throttle ? Palette.Orange : Palette.PowderDim) - 2;
    }

    private static int WriteRight(CellBuffer target, int right, int y, string text, Rgb color)
    {
        var x = right - text.Length;
        target.Write(x, y, text, color, Palette.Navy);
        return x;
    }

    private static void Show(CellBuffer target, int row, (string? Text, Rgb Background) banner)
    {
        if (banner.Text is not { } text)
        {
            return;
        }

        var padded = $"  {text}  ";
        var foreground = banner.Background == Palette.Warning ? Palette.Cream : Palette.Navy;
        target.Write((target.Width - padded.Length) / 2, row, padded, foreground, banner.Background);
    }

    private static (string? Text, Rgb Background) BannerText(RaceSession race, bool record)
    {
        if (race.WrongWay)
        {
            return ("Wrong way", Palette.Warning);
        }

        if (race.Laps.Phase == RacePhase.Waiting)
        {
            return ("Cross the line to start the clock", Palette.Powder);
        }

        if (race.Time - race.LastEventTime < 3)
        {
            switch (race.LastEvent)
            {
                case LapEvent.Started:
                    return ("Clock running", Palette.Powder);
                case LapEvent.Completed when race.Laps.LastLap is { } lap:
                    var note = record ? "  new record" : race.Laps.Laps.Count > 1 && race.Laps.BestLap == lap ? "  new best" : string.Empty;
                    return ($"Lap {race.Laps.Laps.Count}  {TimeFormat.Lap(lap)}{note}", Palette.Orange);
            }
        }

        return (null, default);
    }
}
