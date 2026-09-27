using TermRacer.Core;
using Xunit;

namespace TermRacer.Rendering.Tests;

public sealed class CameraTests
{
    [Fact]
    public void CenterMapsToTheMiddle()
    {
        var camera = new Camera(new Vec2(50, -20), 0.5, 120, 80);
        Assert.Equal(new Vec2(60, 40), camera.ToPixel(new Vec2(50, -20)));
    }

    [Fact]
    public void PixelAndWorldRoundTrip()
    {
        var camera = new Camera(new Vec2(3, 4), 0.72, 100, 60);
        var world = camera.ToWorld(camera.ToPixel(new Vec2(-12.5, 33.25)));
        Assert.Equal(-12.5, world.X, 1e-9);
        Assert.Equal(33.25, world.Y, 1e-9);
    }

    [Fact]
    public void FitKeepsBoundsInView()
    {
        var camera = Camera.Fit(new Vec2(-100, -50), new Vec2(300, 150), 90, 40, 2);
        var low = camera.ToPixel(new Vec2(-100, -50));
        var high = camera.ToPixel(new Vec2(300, 150));
        Assert.InRange(low.X, 0, 90);
        Assert.InRange(high.X, 0, 90);
        Assert.InRange(low.Y, 0, 40);
        Assert.InRange(high.Y, 0, 40);
    }
}
