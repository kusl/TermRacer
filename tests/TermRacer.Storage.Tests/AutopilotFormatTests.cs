using TermRacer.Core;
using Xunit;

namespace TermRacer.Storage.Tests;

public sealed class AutopilotFormatTests
{
    [Fact]
    public void ModelsRoundTripExactly()
    {
        var model = new AutopilotModel(
            "gullwing-park-5e8f8786",
            14,
            [new Corner(189, 20), new Corner(214, 89)],
            [new CornerFactor(1.19, double.PositiveInfinity, 1.23), new CornerFactor(1.1425, 1.15, 1.14625)]);
        using var writer = new StringWriter();
        AutopilotFormat.Write(writer, model);
        var copy = AutopilotFormat.Read(new StringReader(writer.ToString()));
        Assert.NotNull(copy);
        Assert.Equal(model.TrackId, copy.TrackId);
        Assert.Equal(model.Laps, copy.Laps);
        Assert.Equal(model.Corners, copy.Corners);
        Assert.Equal(model.Factors, copy.Factors);
    }

    [Fact]
    public void DamagedRowsInvalidateTheModel()
    {
        var text = "track\tx\nlaps\t3\ncorner\tstart\tlength\tsafe\tlimit\ttrial\n0\t1\t2\t0.9\tinf\tbroken\n";
        Assert.Null(AutopilotFormat.Read(new StringReader(text)));
    }
}
