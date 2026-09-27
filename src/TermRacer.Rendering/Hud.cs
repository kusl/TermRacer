using System.Globalization;
using TermRacer.Core;

namespace TermRacer.Rendering;

public static class Hud
{
    private static readonly (string Keys, string Action)[] Hints =
    [
        ("↑ W", "throttle"),
        ("↓ S", "brake"),
        ("← → A D", "steer"),
        ("Tab", "autopilot"),
        ("Esc", "quit"),
    ];

    public static void TopBar(CellBuffer target, RaceSession race)
    {
        target.Fill(0, 0, target.Width, 1, new Cell(' ', Palette.Cream, Palette.Navy));
        var x = target.Write(1, 0, "TermRacer", Palette.Orange, Palette.Navy) + 3;
        x = target.Write(x, 0, race.Mode == RaceMode.SingleLap ? "Single lap" : "Zen mode", Palette.Powder, Palette.Navy) + 3;
        var laps = race.Laps;
        x = Field(target, x, "Lap", race.Mode == RaceMode.SingleLap ? "1/1" : laps.CurrentLap.ToString(CultureInfo.InvariantCulture));
        x = Field(target, x, "Time", laps.Phase == RacePhase.Waiting ? TimeFormat.Empty : TimeFormat.Lap(laps.CurrentLapTime(race.Time)));
        if (race.Mode == RaceMode.Zen)
        {
            x = Field(target, x, "Last", TimeFormat.Lap(laps.LastLap));
            x = Field(target, x, "Best", TimeFormat.Lap(laps.BestLap));
        }

        Field(target, x, "Speed", TimeFormat.Speed(race.Car.Speed));
        var badge = race.AutopilotEngaged ? " Autopilot " : " Manual ";
        target.Write(target.Width - badge.Length - 1, 0, badge, Palette.Navy, race.AutopilotEngaged ? Palette.Orange : Palette.Powder);
    }

    public static void BottomBar(CellBuffer target, RaceSession race)
    {
        var y = target.Height - 1;
        target.Fill(0, y, target.Width, 1, new Cell(' ', Palette.Cream, Palette.Navy));
        var x = 1;
        foreach (var (keys, action) in Hints)
        {
            x = target.Write(x, y, keys, Palette.Powder, Palette.Navy) + 1;
            x = target.Write(x, y, action, Palette.PowderDim, Palette.Navy) + 3;
        }

        var right = WriteRight(target, target.Width - 1, y, "Brake", race.Input.Brake > 0 ? Palette.Warning : Palette.PowderDim) - 2;
        right = WriteRight(target, right, y, "Throttle", race.Input.Throttle > 0 ? Palette.Orange : Palette.PowderDim) - 2;
        if (race.Surface == Surface.Grass)
        {
            WriteRight(target, right, y, "Off track", Palette.Orange);
        }
    }

    public static void Banner(CellBuffer target, RaceSession race, int row)
    {
        var (text, background) = BannerText(race);
        if (text is null)
        {
            return;
        }

        var padded = $"  {text}  ";
        var foreground = background == Palette.Warning ? Palette.Cream : Palette.Navy;
        target.Write((target.Width - padded.Length) / 2, row, padded, foreground, background);
    }

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

    private static int Field(CellBuffer target, int x, string label, string value)
    {
        x = target.Write(x, 0, label, Palette.PowderDim, Palette.Navy) + 1;
        return target.Write(x, 0, value, Palette.Cream, Palette.Navy) + 3;
    }

    private static int WriteRight(CellBuffer target, int right, int y, string text, Rgb color)
    {
        var x = right - text.Length;
        target.Write(x, y, text, color, Palette.Navy);
        return x;
    }

    private static (string? Text, Rgb Background) BannerText(RaceSession race)
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
                    var best = race.Laps.Laps.Count > 1 && race.Laps.BestLap == lap ? "  new best" : string.Empty;
                    return ($"Lap {race.Laps.Laps.Count}  {TimeFormat.Lap(lap)}{best}", Palette.Orange);
            }
        }

        return (null, default);
    }
}
