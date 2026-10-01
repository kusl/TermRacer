using System.Globalization;
using TermRacer.Core;

namespace TermRacer.Rendering;

public sealed class SceneRenderer(Track track)
{
    public const int MinColumns = 120;
    public const int MinRows = 40;

    private const int MapColumns = 30;
    private const int MapRows = 11;
    private const int PanelWidth = 52;
    private const int PanelHeight = 12;
    private const int MenuRows = 13;
    private const int NameWidth = 14;
    private const int DescriptionWidth = 56;
    private const int ExtraWidth = 34;
    private const int ListWidth = 72;

    private static readonly (string Keys, string Action)[] ResultHints = [("Enter", "back to menu"), ("Esc", "quit")];
    private static readonly (string Keys, string Action)[] ListHints = [("↑ ↓", "choose"), ("Enter", "watch"), ("Bksp", "back"), ("Esc", "quit")];

    private readonly TrackGeometry geometry = new(track);
    private readonly PixelCanvas canvas = new(0, 0);
    private readonly PixelCanvas preview = new(0, 0);
    private readonly ChaseCamera chase = new();

    public string? Version { get; init; }

    public static bool Fits(int columns, int rows) => columns >= MinColumns && rows >= MinRows;

    public void Render(Game game, CellBuffer target)
    {
        if (target.Width == 0 || target.Height == 0)
        {
            return;
        }

        if (!Fits(target.Width, target.Height))
        {
            RenderTooSmall(target);
            return;
        }

        switch (game.Screen)
        {
            case GameScreen.Menu:
                RenderMenu(game, target);
                break;
            case GameScreen.Replay when game.Viewer is { } viewer:
                RenderReplay(game, viewer, target);
                break;
            case GameScreen.Replays or GameScreen.Replay:
                RenderReplays(game, target);
                break;
            default:
                RenderRace(game, game.ActiveSession, target, game.Screen == GameScreen.Results);
                break;
        }
    }

    private static void RenderTooSmall(CellBuffer target)
    {
        target.Fill(new Cell(' ', Palette.Cream, Palette.Navy));
        var top = Math.Max(0, target.Height / 2 - 2);
        target.WriteCentered(top, "Terminal too small", Palette.Orange, Palette.Navy);
        var size = string.Create(CultureInfo.InvariantCulture, $"{target.Width} × {target.Height} now, {MinColumns} × {MinRows} needed");
        target.WriteCentered(top + 2, size, Palette.Cream, Palette.Navy);
        target.WriteCentered(top + 3, "Resize the window to continue, or press Esc to quit", Palette.PowderDim, Palette.Navy);
    }

    private static Rgb MenuBackground(int pixelRow, int height) => Rgb.Lerp(Palette.Navy, Palette.NavyDeep, (double)pixelRow / Math.Max(1, height));

    private static Rgb RowBackground(CellBuffer target, int row) => MenuBackground(row * 2, target.Height * 2);

    private static string Fit(string text, int width) =>
        text.Length <= width ? text : width <= 1 ? text[..Math.Max(0, width)] : string.Concat(text.AsSpan(0, width - 1), "…");

    private static string RecordText(LapHistory history, bool standing)
    {
        if (history.Best(standing) is not { } best)
        {
            return "No record yet";
        }

        var text = $"Record {TimeFormat.Lap(best.Seconds)}";
        if (best.Driver != Driver.Manual)
        {
            text += best.Driver == Driver.Autopilot ? " auto" : " mixed";
            if (history.Best(standing, Driver.Manual) is { } yours)
            {
                text += $"  you {TimeFormat.Lap(yours.Seconds)}";
            }
        }

        return text;
    }

    private static string AutopilotStatus(Game game)
    {
        var trainer = game.Trainer;
        if (trainer.Laps == 0)
        {
            return "Autopilot is untrained. Give it whole laps with Tab and it learns every corner.";
        }

        var best = game.History.Best(false, Driver.Autopilot) ?? game.History.Best(true, Driver.Autopilot);
        var laps = string.Create(CultureInfo.InvariantCulture, $"{trainer.Laps} practice {(trainer.Laps == 1 ? "lap" : "laps")}");
        return $"Autopilot: {laps}, best {TimeFormat.Lap(best?.Seconds)}, {(trainer.Converged ? "fully trained" : "still learning")}";
    }

    private static void MenuOption(CellBuffer target, int left, int row, string name, string description, string extra, bool selected)
    {
        var background = RowBackground(target, row);
        var x = target.Write(left, row, selected ? "▸ " : "  ", Palette.Orange, background);
        x = target.Write(x, row, name.PadRight(NameWidth), selected ? Palette.Orange : Palette.PowderDim, background);
        target.Write(x, row, description, selected ? Palette.Cream : Palette.PowderDim, background);
        target.Write(left + 2 + NameWidth + DescriptionWidth + 2, row, Fit(extra, ExtraWidth), selected ? Palette.Powder : Palette.PowderDim, background);
    }

    private static string Note(LapHistory history, LapRecord record)
    {
        if (Equals(history.Best(record.StandingStart), record))
        {
            return "record";
        }

        return Equals(history.Best(record.StandingStart, Driver.Manual), record) ? "your best" : string.Empty;
    }

    private void PaintBackdrop(CellBuffer target)
    {
        canvas.Resize(target.Width, target.Height * 2);
        for (var y = 0; y < canvas.Height; y++)
        {
            canvas.FillRect(0, y, canvas.Width, 1, MenuBackground(y, canvas.Height));
        }
    }

    private void RenderMenu(Game game, CellBuffer target)
    {
        PaintBackdrop(target);
        const string title = "TERMRACER";
        const int scale = 2;
        var titleWidth = PixelFont.Measure(title, scale);
        var titleX = (canvas.Width - titleWidth) / 2;
        PixelFont.Draw(canvas, title, titleX, 4, scale, Palette.Powder);
        canvas.FillRect(titleX, 4 + PixelFont.GlyphHeight * scale + 3, titleWidth, 2, Palette.Orange);

        var previewTop = 26;
        var previewHeight = canvas.Height - 2 * MenuRows - previewTop;
        var bounds = track.BoundsMax - track.BoundsMin;
        var previewWidth = Math.Min(canvas.Width - 8, (int)Math.Round(previewHeight * bounds.X / bounds.Y));
        preview.Resize(previewWidth, previewHeight);
        var camera = Camera.Fit(track.BoundsMin, track.BoundsMax, previewWidth, previewHeight, 1);
        WorldPainter.PaintTrack(preview, camera, geometry, false);
        WorldPainter.PaintCar(preview, camera, game.Demo.Car, game.Spec, true, 4, false);
        canvas.Blit(preview, (canvas.Width - previewWidth) / 2, previewTop);
        canvas.ComposeInto(target, 0, 0);

        var nameRow = previewTop / 2 - 1;
        var name = string.Create(CultureInfo.InvariantCulture, $"{track.Name}   {track.Length / 1000:0.00} km");
        target.WriteCentered(nameRow, name, Palette.PowderDim, RowBackground(target, nameRow));

        var optionsRow = target.Height - 12;
        var left = (target.Width - (2 + NameWidth + DescriptionWidth + 2 + ExtraWidth)) / 2;
        var history = game.History;
        var saved = game.History.WithReplays().Count;
        MenuOption(target, left, optionsRow, "Single lap", "One timed lap. The clock starts when you cross the line.", RecordText(history, true), game.MenuChoice == MenuEntry.SingleLap);
        MenuOption(target, left, optionsRow + 2, "Zen mode", "Endless laps with lap times. Leave whenever you like.", RecordText(history, false), game.MenuChoice == MenuEntry.Zen);
        MenuOption(target, left, optionsRow + 4, "Replays", "Watch saved laps with the fastest lap as a ghost.", saved == 0 ? "None yet" : string.Create(CultureInfo.InvariantCulture, $"{saved} saved"), game.MenuChoice == MenuEntry.Replays);

        var statusRow = target.Height - 6;
        target.WriteCentered(statusRow, Fit(AutopilotStatus(game), target.Width - 4), Palette.PowderDim, RowBackground(target, statusRow));
        var (storage, color) = game.StorageProblem is { } problem
            ? ($"Lap times are not being saved: {problem}", Palette.Warning)
            : ($"Lap times, replays and autopilot training live in {game.StorageLocation}", Palette.PowderDim);
        target.WriteCentered(statusRow + 1, Fit(storage, target.Width - 4), color, RowBackground(target, statusRow + 1));

        var hintRow = target.Height - 3;
        (string Keys, string Action)[] hints = [("↑ ↓", "choose"), ("Enter", game.MenuChoice == MenuEntry.Replays ? "open" : "start"), ("Esc", "quit")];
        Hud.KeyHints(target, hintRow, hints, RowBackground(target, hintRow));
        if (Version is { Length: > 0 } version)
        {
            var row = target.Height - 1;
            target.Write(target.Width - version.Length - 2, row, version, Palette.PowderDim, RowBackground(target, row));
        }
    }

    private void RenderReplays(Game game, CellBuffer target)
    {
        PaintBackdrop(target);
        canvas.ComposeInto(target, 0, 0);
        target.WriteCentered(2, "Replays", Palette.Orange, RowBackground(target, 2));
        var keep = string.Create(CultureInfo.InvariantCulture, $"{track.Name}   the fastest laps are kept, plus the {LapHistory.KeepRecent} most recent");
        target.WriteCentered(3, keep, Palette.PowderDim, RowBackground(target, 3));
        var list = game.ReplayList;
        var left = (target.Width - ListWidth) / 2;
        if (list.Count == 0)
        {
            var row = target.Height / 2;
            target.WriteCentered(row, "No saved laps yet. Finish a lap and it appears here.", Palette.Cream, RowBackground(target, row));
        }
        else
        {
            const int headerRow = 5;
            const int firstRow = 7;
            var header = string.Create(CultureInfo.InvariantCulture, $"  {"When",-16}  {"Mode",-10}  {"Lap",4}  {"Driver",-9}  {"Time",9}  Note");
            target.Write(left, headerRow, header, Palette.PowderDim, RowBackground(target, headerRow));
            var visible = Math.Max(1, target.Height - 12);
            var first = Math.Clamp(game.ReplayChoice - visible / 2, 0, Math.Max(0, list.Count - visible));
            for (var i = first; i < Math.Min(list.Count, first + visible); i++)
            {
                var record = list[i];
                var row = firstRow + i - first;
                var selected = i == game.ReplayChoice;
                var mode = record.Mode == RaceMode.SingleLap ? "Single lap" : "Zen";
                var text = string.Create(
                    CultureInfo.InvariantCulture,
                    $"{TimeFormat.Moment(record.RecordedAt),-16}  {mode,-10}  {record.Lap,4}  {Hud.DriverName(record.Driver),-9}  {TimeFormat.Lap(record.Seconds),9}  {Note(game.History, record)}");
                var background = RowBackground(target, row);
                var x = target.Write(left, row, selected ? "▸ " : "  ", Palette.Orange, background);
                target.Write(x, row, text, selected ? Palette.Cream : Palette.PowderDim, background);
            }

            if (list.Count > visible)
            {
                var row = firstRow + visible;
                var more = string.Create(CultureInfo.InvariantCulture, $"{game.ReplayChoice + 1} of {list.Count}");
                target.WriteCentered(row, more, Palette.PowderDim, RowBackground(target, row));
            }
        }

        var hintRow = target.Height - 3;
        Hud.KeyHints(target, hintRow, ListHints, RowBackground(target, hintRow));
    }

    private void RenderReplay(Game game, ReplayPlayer viewer, CellBuffer target)
    {
        var worldRows = target.Height - 2;
        canvas.Resize(target.Width, worldRows * 2);
        var car = viewer.Car;
        var camera = chase.Frame(viewer, car, viewer.Time, canvas.Width, canvas.Height);
        WorldPainter.PaintTrack(canvas, camera, geometry, true);
        var ghost = game.GhostVisible ? viewer.GhostCar : null;
        if (ghost is { } shadow)
        {
            WorldPainter.PaintGhost(canvas, camera, shadow, game.Spec);
        }

        WorldPainter.PaintCar(canvas, camera, car, game.Spec, viewer.Sample.Autopilot, 1, true);
        WorldPainter.PaintMinimap(canvas, canvas.Width - MapColumns - 2, 2, MapColumns, MapRows * 2, geometry, car.Position, ghost?.Position);
        canvas.ComposeInto(target, 0, 1);
        Hud.ReplayTopBar(target, viewer, game.GhostVisible);
        Hud.ReplayBottomBar(target, viewer);
        if (viewer.AtEnd)
        {
            Hud.Message(target, 2, "End of lap. Enter watches it again, Bksp goes back to the list", Palette.Powder);
        }
        else if (viewer.Paused)
        {
            Hud.Message(target, 2, "Paused", Palette.Powder);
        }
    }

    private void RenderRace(Game game, RaceSession race, CellBuffer target, bool finished)
    {
        var worldRows = target.Height - 2;
        canvas.Resize(target.Width, worldRows * 2);
        var camera = chase.Frame(race, canvas.Width, canvas.Height);
        WorldPainter.PaintTrack(canvas, camera, geometry, true);
        var ghost = game.GhostVisible ? race.GhostCar : null;
        if (ghost is { } shadow)
        {
            WorldPainter.PaintGhost(canvas, camera, shadow, race.Spec);
        }

        WorldPainter.PaintCar(canvas, camera, race.Car, race.Spec, race.AutopilotEngaged, 1, true);
        WorldPainter.PaintMinimap(canvas, canvas.Width - MapColumns - 2, 2, MapColumns, MapRows * 2, geometry, race.Car.Position, ghost?.Position);

        var left = (target.Width - PanelWidth) / 2;
        var top = 1 + (worldRows - PanelHeight) / 2;
        var time = TimeFormat.Lap(race.Laps.LastLap);
        if (finished)
        {
            canvas.FillRect(left, (top - 1) * 2, PanelWidth, PanelHeight * 2, Palette.Navy);
            var digitsWidth = PixelFont.Measure(time, 1);
            PixelFont.Draw(canvas, time, left + (PanelWidth - digitsWidth) / 2, (top + 3) * 2, 1, Palette.Orange);
        }

        canvas.ComposeInto(target, 0, 1);
        Hud.TopBar(target, race, game.GhostVisible);
        Hud.BottomBar(target, race);
        if (!finished)
        {
            Hud.Banner(target, race, 2, game.LastLapWasRecord);
            return;
        }

        Hud.Box(target, left, top, PanelWidth, PanelHeight, Palette.Powder, Palette.Navy);
        target.Write(left + (PanelWidth - 12) / 2, top + 2, "Lap complete", Palette.Cream, Palette.Navy);
        target.Write(left + (PanelWidth - time.Length) / 2, top + 8, time, Palette.PowderDim, Palette.Navy);
        if (game.LastRecord is { } last)
        {
            var (text, color) = game.LastLapWasRecord || game.PreviousBest is not { } previous
                ? ("New record", Palette.Orange)
                : ($"Record {TimeFormat.Lap(previous.Seconds)}   {TimeFormat.Gap(last.Seconds - previous.Seconds)}", Palette.Powder);
            target.Write(left + (PanelWidth - text.Length) / 2, top + 9, text, color, Palette.Navy);
        }

        var hints = new CellBuffer(PanelWidth - 2, 1);
        hints.Fill(new Cell(' ', Palette.Cream, Palette.Navy));
        Hud.KeyHints(hints, 0, ResultHints, Palette.Navy);
        for (var x = 0; x < hints.Width; x++)
        {
            target[left + 1 + x, top + PanelHeight - 2] = hints[x, 0];
        }
    }
}
