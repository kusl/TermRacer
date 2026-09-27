using Xunit;

namespace TermRacer.Rendering.Tests;

public sealed class FrameDiffTests
{
    private static readonly Rgb Ink = new(9, 9, 9);

    private static CellBuffer Blank()
    {
        var buffer = new CellBuffer(10, 3);
        buffer.Fill(new Cell(' ', Ink, Ink));
        return buffer;
    }

    [Fact]
    public void IdenticalFramesWriteNothing()
    {
        var runs = new List<CellRun>();
        FrameDiff.Compute(Blank(), Blank(), runs);
        Assert.Empty(runs);
    }

    [Fact]
    public void ChangedCellsFormRuns()
    {
        var current = Blank();
        current[2, 1] = new Cell('a', Ink, Ink);
        current[3, 1] = new Cell('b', Ink, Ink);
        current[7, 1] = new Cell('c', Ink, Ink);
        var runs = new List<CellRun>();
        FrameDiff.Compute(Blank(), current, runs);
        Assert.Equal(2, runs.Count);
        Assert.Equal(new CellRun(1, 2, 2), runs[0]);
        Assert.Equal(new CellRun(1, 7, 1), runs[1]);
    }

    [Fact]
    public void ColourOnlyChangesCount()
    {
        var current = Blank();
        current[0, 0] = new Cell(' ', Ink, new Rgb(1, 1, 1));
        var runs = new List<CellRun>();
        FrameDiff.Compute(Blank(), current, runs);
        Assert.Equal(new CellRun(0, 0, 1), Assert.Single(runs));
    }

    [Fact]
    public void SizeChangeRedrawsEverything()
    {
        var runs = new List<CellRun>();
        FrameDiff.Compute(new CellBuffer(4, 4), Blank(), runs);
        Assert.Equal(30, FrameDiff.CellCount(runs));
    }

    [Fact]
    public void FullCoversEveryCell()
    {
        var runs = new List<CellRun>();
        FrameDiff.Full(Blank(), runs);
        Assert.Equal(3, runs.Count);
        Assert.Equal(30, FrameDiff.CellCount(runs));
    }
}
