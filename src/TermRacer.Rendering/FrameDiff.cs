namespace TermRacer.Rendering;

public static class FrameDiff
{
    public static void Compute(CellBuffer previous, CellBuffer current, List<CellRun> runs)
    {
        runs.Clear();
        if (previous.Width != current.Width || previous.Height != current.Height)
        {
            AppendFull(current, runs);
            return;
        }

        for (var y = 0; y < current.Height; y++)
        {
            var before = previous.Row(y);
            var after = current.Row(y);
            var x = 0;
            while (x < after.Length)
            {
                if (before[x] == after[x])
                {
                    x++;
                    continue;
                }

                var start = x;
                while (x < after.Length && before[x] != after[x])
                {
                    x++;
                }

                runs.Add(new CellRun(y, start, x - start));
            }
        }
    }

    public static void Full(CellBuffer current, List<CellRun> runs)
    {
        runs.Clear();
        AppendFull(current, runs);
    }

    public static int CellCount(IEnumerable<CellRun> runs) => runs.Sum(run => run.Length);

    private static void AppendFull(CellBuffer current, List<CellRun> runs)
    {
        if (current.Width == 0)
        {
            return;
        }

        for (var y = 0; y < current.Height; y++)
        {
            runs.Add(new CellRun(y, 0, current.Width));
        }
    }
}
