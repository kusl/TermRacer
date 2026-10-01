using TermRacer.Core;
using Xunit;

namespace TermRacer.Storage.Tests;

public sealed class ReplayFormatTests
{
    private static string Write(Replay replay)
    {
        using var writer = new StringWriter();
        ReplayFormat.Write(writer, Fixtures.Lap(), replay);
        return writer.ToString();
    }

    [Fact]
    public void SamplesRoundTripWithinStoredPrecision()
    {
        var original = Fixtures.Replay();
        var copy = ReplayFormat.Read(new StringReader(Write(original)));
        Assert.NotNull(copy);
        Assert.Equal(original.TrackId, copy.TrackId);
        Assert.Equal(original.LapTime, copy.LapTime);
        Assert.Equal(original.Samples.Count, copy.Samples.Count);
        for (var k = 0; k < original.Samples.Count; k++)
        {
            var a = original.Samples[k];
            var b = copy.Samples[k];
            Assert.Equal(a.Position.X, b.Position.X, 0.006);
            Assert.Equal(a.Position.Y, b.Position.Y, 0.006);
            Assert.Equal(a.Heading, b.Heading, 0.0001);
            Assert.Equal(a.Speed, b.Speed, 0.006);
            Assert.Equal(a.Distance, b.Distance, 0.006);
            Assert.Equal(a.Steer, b.Steer, 0.001);
            Assert.Equal(a.Throttle, b.Throttle, 0.006);
            Assert.Equal(a.Brake, b.Brake, 0.006);
            Assert.Equal(a.Autopilot, b.Autopilot);
            Assert.Equal(a.OffTrack, b.OffTrack);
        }
    }

    [Fact]
    public void HeaderDescribesTheLap()
    {
        var text = Write(Fixtures.Replay());
        Assert.Contains("track\tgullwing-park-5e8f8786\n", text);
        Assert.Contains("recorded_utc\t2026-10-01T22:51:03.123Z\n", text);
        Assert.Contains("rate\t30\n", text);
        Assert.Contains("t\tx\ty\theading\tspeed\tdistance\tthrottle\tbrake\tsteer\tflags\n", text);
    }

    [Fact]
    public void TruncatedFilesAreRejected()
    {
        var text = Write(Fixtures.Replay());
        Assert.Null(ReplayFormat.Read(new StringReader(text[..(text.Length / 2)])));
    }

    [Fact]
    public void ForeignRatesAreRejected()
    {
        var text = Write(Fixtures.Replay()).Replace("rate\t30", "rate\t60", StringComparison.Ordinal);
        Assert.Null(ReplayFormat.Read(new StringReader(text)));
    }
}
