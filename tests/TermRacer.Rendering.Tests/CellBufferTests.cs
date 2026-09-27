using Xunit;

namespace TermRacer.Rendering.Tests;

public sealed class CellBufferTests
{
    private static readonly Rgb Ink = new(1, 2, 3);

    [Fact]
    public void WriteClipsAtTheEdge()
    {
        var buffer = new CellBuffer(5, 1);
        buffer.Fill(new Cell('.', Ink, Ink));
        Assert.Equal(7, buffer.Write(3, 0, "abcd", Ink, Ink));
        Assert.Equal("...ab", buffer.RowText(0));
    }

    [Fact]
    public void WriteCenteredCenters()
    {
        var buffer = new CellBuffer(7, 1);
        buffer.Fill(new Cell('.', Ink, Ink));
        buffer.WriteCentered(0, "abc", Ink, Ink);
        Assert.Equal("..abc..", buffer.RowText(0));
    }

    [Fact]
    public void ResizeReallocates()
    {
        var buffer = new CellBuffer(2, 2);
        buffer.Fill(new Cell('x', Ink, Ink));
        buffer.Resize(3, 1);
        Assert.Equal(3, buffer.Width);
        Assert.Equal(default, buffer[2, 0]);
    }
}
