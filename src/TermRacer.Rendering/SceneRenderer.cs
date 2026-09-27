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

    private static readonly (string Keys, string Action)[] MenuHints = [("↑ ↓", "choose"), ("Enter", "start"), ("Esc", "quit")];
    private static readonly (string Keys, string Action)[] ResultHints = [("Enter", "back to menu"), ("Esc", "quit")];

    private readonly TrackGeometry geometry = new(track);
    private readonly PixelCanvas canvas = new(0, 0);
    private readonly PixelCanvas preview = new(0, 0);
    private readonly ChaseCamera chase = new();

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

        if (game.Screen == GameScreen.Menu)
        {
            RenderMenu(game, target);
            return;
        }

        RenderRace(game.ActiveSession, target, game.Screen == GameScreen.Results);
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

    private void RenderMenu(Game game, CellBuffer target)
    {
        canvas.Resize(target.Width, target.Height * 2);
        for (var y = 0; y < canvas.Height; y++)
        {
            canvas.FillRect(0, y, canvas.Width, 1, MenuBackground(y, canvas.Height));
        }

        const string title = "TERMRACER";
        const int scale = 2;
        var titleWidth = PixelFont.Measure(title, scale);
        var titleX = (canvas.Width - titleWidth) / 2;
        PixelFont.Draw(canvas, title, titleX, 4, scale, Palette.Powder);
        canvas.FillRect(titleX, 4 + PixelFont.GlyphHeight * scale + 3, titleWidth, 2, Palette.Orange);

        var previewTop = 26;
        var previewHeight = canvas.Height - 24 - previewTop;
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
        target.WriteCentered(nameRow, name, Palette.PowderDim, MenuBackground(nameRow * 2, canvas.Height));

        var optionsRow = target.Height - 9;
        MenuOption(target, optionsRow, "Single lap", "One timed lap. The clock starts when you cross the line.", game.SelectedMode == RaceMode.SingleLap);
        MenuOption(target, optionsRow + 2, "Zen mode", "Endless laps with lap times. Leave whenever you like.", game.SelectedMode == RaceMode.Zen);
        var hintRow = target.Height - 3;
        Hud.KeyHints(target, hintRow, MenuHints, MenuBackground(hintRow * 2, canvas.Height));
    }

    private static void MenuOption(CellBuffer target, int row, string name, string description, bool selected)
    {
        var background = MenuBackground(row * 2, target.Height * 2);
        const int nameWidth = 14;
        var width = 2 + nameWidth + description.Length;
        var x = (target.Width - width) / 2;
        x = target.Write(x, row, selected ? "▸ " : "  ", Palette.Orange, background);
        x = target.Write(x, row, name.PadRight(nameWidth), selected ? Palette.Orange : Palette.PowderDim, background);
        target.Write(x, row, description, selected ? Palette.Cream : Palette.PowderDim, background);
    }

    private void RenderRace(RaceSession race, CellBuffer target, bool finished)
    {
        var worldRows = target.Height - 2;
        canvas.Resize(target.Width, worldRows * 2);
        var camera = chase.Frame(race, canvas.Width, canvas.Height);
        WorldPainter.PaintTrack(canvas, camera, geometry, true);
        WorldPainter.PaintCar(canvas, camera, race.Car, race.Spec, race.AutopilotEngaged, 1, true);
        WorldPainter.PaintMinimap(canvas, canvas.Width - MapColumns - 2, 2, MapColumns, MapRows * 2, geometry, race);

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
        Hud.TopBar(target, race);
        Hud.BottomBar(target, race);
        if (!finished)
        {
            Hud.Banner(target, race, 2);
            return;
        }

        Hud.Box(target, left, top, PanelWidth, PanelHeight, Palette.Powder, Palette.Navy);
        target.Write(left + (PanelWidth - 12) / 2, top + 2, "Lap complete", Palette.Cream, Palette.Navy);
        target.Write(left + (PanelWidth - time.Length) / 2, top + 9, time, Palette.PowderDim, Palette.Navy);
        var hints = new CellBuffer(PanelWidth - 2, 1);
        hints.Fill(new Cell(' ', Palette.Cream, Palette.Navy));
        Hud.KeyHints(hints, 0, ResultHints, Palette.Navy);
        for (var x = 0; x < hints.Width; x++)
        {
            target[left + 1 + x, top + PanelHeight - 2] = hints[x, 0];
        }
    }
}
