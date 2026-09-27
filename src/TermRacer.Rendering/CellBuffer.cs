namespace TermRacer.Rendering;

public sealed class CellBuffer(int width, int height)
{
    private Cell[] cells = new Cell[Math.Max(0, width) * Math.Max(0, height)];

    public int Width { get; private set; } = Math.Max(0, width);

    public int Height { get; private set; } = Math.Max(0, height);

    public Cell this[int x, int y]
    {
        get => cells[y * Width + x];
        set => cells[y * Width + x] = value;
    }

    public ReadOnlySpan<Cell> Row(int y) => cells.AsSpan(y * Width, Width);

    public string RowText(int y)
    {
        var row = Row(y);
        var glyphs = new char[row.Length];
        for (var x = 0; x < row.Length; x++)
        {
            glyphs[x] = row[x].Glyph;
        }

        return new string(glyphs);
    }

    public bool Contains(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height;

    public void Resize(int width, int height)
    {
        width = Math.Max(0, width);
        height = Math.Max(0, height);
        if (width == Width && height == Height)
        {
            return;
        }

        Width = width;
        Height = height;
        cells = new Cell[width * height];
    }

    public void Fill(Cell cell) => Array.Fill(cells, cell);

    public void Fill(int x, int y, int width, int height, Cell cell)
    {
        var left = Math.Max(0, x);
        var right = Math.Min(Width, x + width);
        for (var row = Math.Max(0, y); row < Math.Min(Height, y + height); row++)
        {
            if (right > left)
            {
                cells.AsSpan(row * Width + left, right - left).Fill(cell);
            }
        }
    }

    public int Write(int x, int y, string text, Rgb foreground, Rgb background)
    {
        foreach (var glyph in text)
        {
            if (Contains(x, y))
            {
                cells[y * Width + x] = new Cell(glyph, foreground, background);
            }

            x++;
        }

        return x;
    }

    public int WriteCentered(int y, string text, Rgb foreground, Rgb background) =>
        Write((Width - text.Length) / 2, y, text, foreground, background);
}
