namespace TermRacer.Rendering;

public readonly record struct Cell(char Glyph, Rgb Foreground, Rgb Background)
{
    public const char UpperHalf = '\u2580';
}
