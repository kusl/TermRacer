namespace TermRacer.Rendering;

public static class PixelFont
{
    public const int GlyphHeight = 7;

    private static readonly Dictionary<char, string[]> Glyphs = new()
    {
        ['A'] = [".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#"],
        ['C'] = [".###.", "#...#", "#....", "#....", "#....", "#...#", ".###."],
        ['E'] = ["#####", "#....", "#....", "####.", "#....", "#....", "#####"],
        ['M'] = ["#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#"],
        ['R'] = ["####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#"],
        ['T'] = ["#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.."],
        ['0'] = [".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###."],
        ['1'] = ["..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###."],
        ['2'] = [".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####"],
        ['3'] = ["#####", "...#.", "..#..", "...#.", "....#", "#...#", ".###."],
        ['4'] = ["...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#."],
        ['5'] = ["#####", "#....", "####.", "....#", "....#", "#...#", ".###."],
        ['6'] = ["..##.", ".#...", "#....", "####.", "#...#", "#...#", ".###."],
        ['7'] = ["#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..."],
        ['8'] = [".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###."],
        ['9'] = [".###.", "#...#", "#...#", ".####", "....#", "...#.", ".##.."],
        [':'] = [".", ".", "#", ".", "#", ".", "."],
        ['.'] = [".", ".", ".", ".", ".", ".", "#"],
        ['-'] = ["....", "....", "....", "####", "....", "....", "...."],
    };

    public static int Measure(string text, int scale)
    {
        var width = 0;
        foreach (var character in text)
        {
            width += ((Glyphs.TryGetValue(char.ToUpperInvariant(character), out var rows) ? rows[0].Length : 3) + 1) * scale;
        }

        return Math.Max(0, width - scale);
    }

    public static void Draw(PixelCanvas canvas, string text, int x, int y, int scale, Rgb color)
    {
        foreach (var character in text)
        {
            if (!Glyphs.TryGetValue(char.ToUpperInvariant(character), out var rows))
            {
                x += 4 * scale;
                continue;
            }

            for (var row = 0; row < rows.Length; row++)
            {
                for (var column = 0; column < rows[row].Length; column++)
                {
                    if (rows[row][column] == '#')
                    {
                        canvas.FillRect(x + column * scale, y + row * scale, scale, scale, color);
                    }
                }
            }

            x += (rows[0].Length + 1) * scale;
        }
    }
}
