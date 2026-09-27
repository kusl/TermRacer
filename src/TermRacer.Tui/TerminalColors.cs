using TermRacer.Rendering;
using TuiAttribute = Terminal.Gui.Drawing.Attribute;
using TuiColor = Terminal.Gui.Drawing.Color;

namespace TermRacer.Tui;

internal static class TerminalColors
{
    public static TuiAttribute ToAttribute(in Cell cell) => new(ToColor(cell.Foreground), ToColor(cell.Background));

    private static TuiColor ToColor(Rgb rgb) => new(rgb.R, rgb.G, rgb.B);
}
