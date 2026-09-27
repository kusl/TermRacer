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
}
