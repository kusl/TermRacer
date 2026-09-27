using TermRacer.Core;
using Xunit;

namespace TermRacer.Rendering.Tests;

public sealed class PixelCanvasTests
{
    private static readonly Rgb Black = new(0, 0, 0);
    private static readonly Rgb White = new(255, 255, 255);

    [Fact]
    public void FillsExactlyThePixelCentersInside()
    {
        var canvas = new PixelCanvas(6, 6);
        canvas.Clear(Black);
        canvas.FillPolygon([new Vec2(1, 1), new Vec2(4, 1), new Vec2(4, 4), new Vec2(1, 4)], White);
        for (var y = 0; y < 6; y++)
        {
            for (var x = 0; x < 6; x++)
            {
                Assert.Equal(x is >= 1 and < 4 && y is >= 1 and < 4 ? White : Black, canvas[x, y]);
            }
        }
    }

    [Fact]
    public void TrianglesSharingAnEdgeLeaveNoGaps()
    {
        Vec2[] quad = [new(0.3, 0.2), new(5.7, 0.9), new(5.2, 5.6), new(0.6, 5.1)];
        var canvas = new PixelCanvas(6, 6);
        canvas.Clear(Black);
        canvas.FillPolygon([quad[0], quad[1], quad[2]], White);
        canvas.FillPolygon([quad[0], quad[2], quad[3]], White);
        for (var y = 0; y < 6; y++)
        {
            for (var x = 0; x < 6; x++)
            {
                Assert.Equal(Inside(quad, new Vec2(x + 0.5, y + 0.5)) ? White : Black, canvas[x, y]);
            }
        }
    }

    [Fact]
    public void PolygonsOffCanvasAreIgnored()
    {
        var canvas = new PixelCanvas(4, 4);
        canvas.Clear(Black);
        canvas.FillPolygon([new Vec2(-10, -10), new Vec2(-5, -10), new Vec2(-5, -5)], White);
        canvas.FillPolygon([new Vec2(10, 10), new Vec2(20, 10), new Vec2(20, 20)], White);
        Assert.Equal(Black, canvas[0, 0]);
        Assert.Equal(Black, canvas[3, 3]);
    }

    [Fact]
    public void ShadeDarkensOnlyInside()
    {
        var canvas = new PixelCanvas(4, 1);
        canvas.Clear(White);
        canvas.ShadePolygon([new Vec2(0, 0), new Vec2(2, 0), new Vec2(2, 1), new Vec2(0, 1)], 0.5);
        Assert.Equal(new Rgb(128, 128, 128), canvas[0, 0]);
        Assert.Equal(White, canvas[3, 0]);
    }

    [Fact]
    public void LinesReachBothEnds()
    {
        var canvas = new PixelCanvas(8, 8);
        canvas.Clear(Black);
        canvas.DrawLine(new Vec2(0.5, 0.5), new Vec2(7.5, 3.5), White);
        Assert.Equal(White, canvas[0, 0]);
        Assert.Equal(White, canvas[7, 3]);
    }

    [Fact]
    public void ComposesPixelPairsIntoHalfBlocks()
    {
        var canvas = new PixelCanvas(2, 2);
        var red = new Rgb(255, 0, 0);
        var blue = new Rgb(0, 0, 255);
        var green = new Rgb(0, 255, 0);
        canvas[0, 0] = red;
        canvas[0, 1] = blue;
        canvas[1, 0] = green;
        canvas[1, 1] = green;
        var cells = new CellBuffer(2, 1);
        canvas.ComposeInto(cells, 0, 0);
        Assert.Equal(new Cell(Cell.UpperHalf, red, blue), cells[0, 0]);
        Assert.Equal(new Cell(' ', green, green), cells[1, 0]);
    }

    private static bool Inside(Vec2[] polygon, Vec2 point)
    {
        for (var i = 0; i < polygon.Length; i++)
        {
            var edge = polygon[(i + 1) % polygon.Length] - polygon[i];
            if (edge.Cross(point - polygon[i]) < 0)
            {
                return false;
            }
        }

        return true;
    }
}
