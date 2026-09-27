using TermRacer.Core;
using Xunit;

namespace TermRacer.Core.Tests;

public sealed class FixedStepClockTests
{
    [Fact]
    public void CarriesPartialSteps()
    {
        var clock = new FixedStepClock(0.01);
        Assert.Equal(2, clock.Advance(0.025));
        Assert.Equal(1, clock.Advance(0.006));
    }

    [Fact]
    public void LongStallsAreClamped() => Assert.Equal(25, new FixedStepClock(0.01, 0.25).Advance(10));

    [Fact]
    public void NegativeTimeIsIgnored() => Assert.Equal(0, new FixedStepClock(0.01).Advance(-1));
}
