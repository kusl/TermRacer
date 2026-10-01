using Xunit;

namespace TermRacer.Rendering.Tests;

public sealed class TimeFormatTests
{
    [Theory]
    [InlineData(52.3184, "0:52.318")]
    [InlineData(605.0, "10:05.000")]
    [InlineData(59.9996, "1:00.000")]
    [InlineData(0.0, "0:00.000")]
    public void FormatsLapTimes(double seconds, string expected) => Assert.Equal(expected, TimeFormat.Lap(seconds));

    [Fact]
    public void MissingTimeUsesPlaceholder() => Assert.Equal(TimeFormat.Empty, TimeFormat.Lap(null));

    [Fact]
    public void SpeedIsShownInKilometresPerHour() => Assert.Equal("144 km/h", TimeFormat.Speed(40));

    [Theory]
    [InlineData(1.2344, "+1.234")]
    [InlineData(-0.5675, "-0.568")]
    [InlineData(-0.0004, "+0.000")]
    [InlineData(75.5, "+1:15.500")]
    public void FormatsGaps(double seconds, string expected) => Assert.Equal(expected, TimeFormat.Gap(seconds));

    [Fact]
    public void MissingGapUsesPlaceholder() => Assert.Equal("-.---", TimeFormat.Gap(null));
}
