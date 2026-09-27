using TermRacer.Core;

namespace TermRacer.Rendering;

public sealed class PixelCanvas(int width, int height)
{
    private Rgb[] pixels = new Rgb[Math.Max(0, width) * Math.Max(0, height)];

    private interface ISpanPainter
    {
        void Paint(Span<Rgb> span);
    }

    public int Width { get; private set; } = Math.Max(0, width);

    public int Height { get; private set; } = Math.Max(0, height);

    public Rgb this[int x, int y]
    {
        get => pixels[y * Width + x];
        set => pixels[y * Width + x] = value;
    }

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
        pixels = new Rgb[width * height];
    }

    public void Clear(Rgb color) => Array.Fill(pixels, color);

    public void Plot(int x, int y, Rgb color)
    {
        if ((uint)x < (uint)Width && (uint)y < (uint)Height)
        {
            pixels[y * Width + x] = color;
        }
    }

    public void FillRect(int x, int y, int width, int height, Rgb color)
    {
        var left = Math.Max(0, x);
        var right = Math.Min(Width, x + width);
        for (var row = Math.Max(0, y); row < Math.Min(Height, y + height); row++)
        {
            if (right > left)
            {
                pixels.AsSpan(row * Width + left, right - left).Fill(color);
            }
        }
    }

    public void Darken(int x, int y, int width, int height, double factor)
    {
        var left = Math.Max(0, x);
        var right = Math.Min(Width, x + width);
        for (var row = Math.Max(0, y); row < Math.Min(Height, y + height); row++)
        {
            for (var column = left; column < right; column++)
            {
                pixels[row * Width + column] = pixels[row * Width + column].Scale(factor);
            }
        }
    }

    public void FillPolygon(ReadOnlySpan<Vec2> vertices, Rgb color) => Rasterize(vertices, new SolidPainter(color));

    public void ShadePolygon(ReadOnlySpan<Vec2> vertices, double factor) => Rasterize(vertices, new ShadePainter(factor));

    public void DrawLine(Vec2 from, Vec2 to, Rgb color)
    {
        var delta = to - from;
        var steps = (int)Math.Ceiling(Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y)));
        for (var i = 0; i <= steps; i++)
        {
            var point = steps == 0 ? from : from + delta * ((double)i / steps);
            Plot((int)Math.Floor(point.X), (int)Math.Floor(point.Y), color);
        }
    }

    public void Blit(PixelCanvas source, int x, int y)
    {
        for (var row = 0; row < source.Height; row++)
        {
            var targetRow = y + row;
            if ((uint)targetRow >= (uint)Height)
            {
                continue;
            }

            for (var column = 0; column < source.Width; column++)
            {
                var targetColumn = x + column;
                if ((uint)targetColumn < (uint)Width)
                {
                    pixels[targetRow * Width + targetColumn] = source.pixels[row * source.Width + column];
                }
            }
        }
    }

    public void ComposeInto(CellBuffer target, int column, int row)
    {
        for (var cellRow = 0; cellRow * 2 < Height; cellRow++)
        {
            var y = row + cellRow;
            if ((uint)y >= (uint)target.Height)
            {
                continue;
            }

            var topOffset = cellRow * 2 * Width;
            var bottomOffset = cellRow * 2 + 1 < Height ? topOffset + Width : topOffset;
            for (var cellColumn = 0; cellColumn < Width; cellColumn++)
            {
                var x = column + cellColumn;
                if ((uint)x >= (uint)target.Width)
                {
                    continue;
                }

                var top = pixels[topOffset + cellColumn];
                var bottom = pixels[bottomOffset + cellColumn];
                target[x, y] = top == bottom ? new Cell(' ', top, top) : new Cell(Cell.UpperHalf, top, bottom);
            }
        }
    }

    private void Rasterize<TPainter>(ReadOnlySpan<Vec2> vertices, TPainter painter)
        where TPainter : struct, ISpanPainter
    {
        if (vertices.Length < 3 || Width == 0 || Height == 0)
        {
            return;
        }

        var minY = double.MaxValue;
        var maxY = double.MinValue;
        foreach (var vertex in vertices)
        {
            minY = Math.Min(minY, vertex.Y);
            maxY = Math.Max(maxY, vertex.Y);
        }

        var firstRow = Math.Max(0, (int)Math.Ceiling(minY - 0.5));
        var lastRow = Math.Min(Height - 1, (int)Math.Ceiling(maxY - 0.5) - 1);
        if (firstRow > lastRow)
        {
            return;
        }

        Span<double> crossings = vertices.Length <= 64 ? stackalloc double[vertices.Length] : new double[vertices.Length];
        for (var y = firstRow; y <= lastRow; y++)
        {
            var center = y + 0.5;
            var count = 0;
            for (var i = 0; i < vertices.Length; i++)
            {
                var a = vertices[i];
                var b = vertices[i + 1 == vertices.Length ? 0 : i + 1];
                if (a.Y > b.Y)
                {
                    (a, b) = (b, a);
                }

                if (center < a.Y || center >= b.Y)
                {
                    continue;
                }

                crossings[count++] = a.X + (center - a.Y) * (b.X - a.X) / (b.Y - a.Y);
            }

            var row = crossings[..count];
            row.Sort();
            for (var k = 0; k + 1 < count; k += 2)
            {
                var from = Math.Max(0, (int)Math.Ceiling(row[k] - 0.5));
                var to = Math.Min(Width, (int)Math.Ceiling(row[k + 1] - 0.5));
                if (to > from)
                {
                    painter.Paint(pixels.AsSpan(y * Width + from, to - from));
                }
            }
        }
    }

    private readonly record struct SolidPainter(Rgb Color) : ISpanPainter
    {
        public void Paint(Span<Rgb> span) => span.Fill(Color);
    }

    private readonly record struct ShadePainter(double Factor) : ISpanPainter
    {
        public void Paint(Span<Rgb> span)
        {
            foreach (ref var pixel in span)
            {
                pixel = pixel.Scale(Factor);
            }
        }
    }
}
