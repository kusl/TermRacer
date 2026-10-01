using TermRacer.Core;
using Xunit;

namespace TermRacer.Storage.Tests;

public sealed class LapLogFormatTests
{
    [Fact]
    public void RecordsRoundTrip()
    {
        var records = new[] { Fixtures.Lap(), Fixtures.Lap(51.4044, 3, Driver.Autopilot, null) };
        var log = LapLogFormat.Parse(LapLogFormat.Lines(records, []));
        Assert.True(log.Current);
        Assert.Equal(2, log.Records.Count);
        Assert.Equal(records[0], log.Records[0]);
        Assert.Equal(records[1] with { Seconds = 51.404 }, log.Records[1]);
    }

    [Fact]
    public void RowsFollowTheDocumentedLayout() =>
        Assert.Equal(
            "2026-10-01T22:51:03.123Z\tgullwing-park-5e8f8786\tsingle\t1\tmanual\t56.409\t1\t0.25\t20261001-225103123-single-1",
            LapLogFormat.Format(Fixtures.Lap()));

    [Fact]
    public void ColumnsAreMatchedByName()
    {
        string[] lines =
        [
            "replay\tseconds\tdriver\tlap\tmode\ttrack\trecorded_utc\textra",
            "-\t60.5\tmixed\t2\tzen\tsome-track\t2026-01-02T03:04:05.006Z\tignored",
        ];
        var log = LapLogFormat.Parse(lines);
        var record = Assert.Single(log.Records);
        Assert.Equal(new LapRecord(new DateTimeOffset(2026, 1, 2, 3, 4, 5, 6, TimeSpan.Zero), "some-track", RaceMode.Zen, 2, Driver.Mixed, 60.5), record);
        Assert.False(log.Current);
    }

    [Fact]
    public void BrokenRowsAreKeptAsideAndHeaderlessFilesUseTheDefaultLayout()
    {
        string[] lines = ["# comment", LapLogFormat.Format(Fixtures.Lap()), "garbage\tline", string.Empty];
        var log = LapLogFormat.Parse(lines);
        Assert.Single(log.Records);
        Assert.Equal("garbage\tline", Assert.Single(log.Unreadable));
        Assert.False(log.Current);
        Assert.Contains("# unreadable: garbage\tline", LapLogFormat.Lines(log.Records, log.Unreadable));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-3")]
    [InlineData("NaN")]
    [InlineData("inf")]
    public void ImpossibleTimesAreRejected(string seconds)
    {
        var row = LapLogFormat.Format(Fixtures.Lap()).Replace("56.409", seconds, StringComparison.Ordinal);
        Assert.Empty(LapLogFormat.Parse([LapLogFormat.Header, row]).Records);
    }
}
