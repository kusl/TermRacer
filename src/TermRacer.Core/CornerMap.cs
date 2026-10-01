namespace TermRacer.Core;

public sealed class CornerMap
{
    private readonly int[] cornerAt;
    private readonly int[][] influence;

    private CornerMap(Corner[] corners, int[] cornerAt, int runoff)
    {
        Corners = corners;
        this.cornerAt = cornerAt;
        var count = cornerAt.Length;
        var lists = new SortedSet<int>[count];
        for (var i = 0; i < count; i++)
        {
            lists[i] = [];
        }

        for (var point = 0; point < count; point++)
        {
            if (cornerAt[point] < 0)
            {
                continue;
            }

            for (var j = 0; j <= runoff; j++)
            {
                lists[(point + j) % count].Add(cornerAt[point]);
            }
        }

        influence = [.. lists.Select(list => list.ToArray())];
    }

    public IReadOnlyList<Corner> Corners { get; }

    public int Count => Corners.Count;

    public int CornerAt(int point) => cornerAt[Wrap(point)];

    public IReadOnlyList<int> Influencing(int point) => influence[Wrap(point)];

    public static CornerMap Build(RacingLine line, CarSpec spec, double gripFactor = 0.9, double topSpeed = 44, int mergeGap = 2, int runoff = 20)
    {
        var count = line.Count;
        var limited = new bool[count];
        for (var i = 0; i < count; i++)
        {
            limited[i] = Math.Sqrt(spec.TarmacGrip * gripFactor / Math.Max(SpeedProfile.PeakCurvature(line, i), 1e-6)) < topSpeed;
        }

        var cornerAt = Enumerable.Repeat(-1, count).ToArray();
        var first = GapStart(limited, mergeGap);
        if (first < 0)
        {
            Array.Fill(cornerAt, limited.Any(value => value) ? 0 : -1);
            return new CornerMap(limited.Any(value => value) ? [new Corner(0, count)] : [], cornerAt, runoff);
        }

        var corners = new List<Corner>();
        var start = -1;
        var previous = -1;
        for (var k = 0; k < count; k++)
        {
            if (!limited[(first + k) % count])
            {
                continue;
            }

            if (start >= 0 && k - previous - 1 > mergeGap)
            {
                corners.Add(new Corner((first + start) % count, previous - start + 1));
                start = -1;
            }

            if (start < 0)
            {
                start = k;
            }

            previous = k;
        }

        if (start >= 0)
        {
            corners.Add(new Corner((first + start) % count, previous - start + 1));
        }

        for (var c = 0; c < corners.Count; c++)
        {
            for (var j = 0; j < corners[c].Length; j++)
            {
                cornerAt[(corners[c].Start + j) % count] = c;
            }
        }

        return new CornerMap([.. corners], cornerAt, runoff);
    }

    private static int GapStart(bool[] limited, int mergeGap)
    {
        var count = limited.Length;
        for (var i = 0; i < count; i++)
        {
            if (limited[i] || !limited[(i - 1 + count) % count])
            {
                continue;
            }

            var run = 0;
            while (run < count && !limited[(i + run) % count])
            {
                run++;
            }

            if (run > mergeGap)
            {
                return i;
            }
        }

        return limited.All(value => !value) ? 0 : -1;
    }

    private int Wrap(int point) => (point % cornerAt.Length + cornerAt.Length) % cornerAt.Length;
}
