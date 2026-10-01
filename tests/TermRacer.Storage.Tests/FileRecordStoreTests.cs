using TermRacer.Core;
using Xunit;

namespace TermRacer.Storage.Tests;

public sealed class FileRecordStoreTests : IDisposable
{
    private readonly TemporaryDirectory folder = new();

    private string Data => Path.Combine(folder.Path, "data");

    public void Dispose() => folder.Dispose();

    [Fact]
    public void LapsAreAppendedAndReadBack()
    {
        var store = new FileRecordStore(Data);
        Assert.Empty(store.LoadLaps());
        Assert.True(store.AppendLap(Fixtures.Lap()));
        Assert.True(store.AppendLap(Fixtures.Lap(55.022, 2, Driver.Autopilot, null)));
        var lines = File.ReadAllLines(Path.Combine(Data, FileRecordStore.LapsFile));
        Assert.Equal(LapLogFormat.Banner, lines[0]);
        Assert.Equal(LapLogFormat.Header, lines[1]);
        Assert.Equal(4, lines.Length);
        var reloaded = new FileRecordStore(Data).LoadLaps();
        Assert.Equal([56.409, 55.022], reloaded.Select(lap => lap.Seconds));
        Assert.Null(store.Problem);
    }

    [Fact]
    public void OutdatedLogsAreRewrittenWithoutLosingRows()
    {
        Directory.CreateDirectory(Data);
        var path = Path.Combine(Data, FileRecordStore.LapsFile);
        File.WriteAllLines(path, ["seconds\tmode\tlap\tdriver\ttrack\trecorded_utc", "52.5\tzen\t4\tautopilot\tt-1\t2026-02-03T04:05:06.000Z", "nonsense"]);
        var laps = new FileRecordStore(Data).LoadLaps();
        Assert.Equal(52.5, Assert.Single(laps).Seconds);
        var lines = File.ReadAllLines(path);
        Assert.Equal(LapLogFormat.Header, lines[1]);
        Assert.Contains("# unreadable: nonsense", lines);
        Assert.Equal(52.5, Assert.Single(new FileRecordStore(Data).LoadLaps()).Seconds);
    }

    [Fact]
    public void ReplaysAreCompressedListedAndDeleted()
    {
        var store = new FileRecordStore(Data);
        const string name = "20261001-225103123-single-1";
        Assert.True(store.SaveReplay(name, Fixtures.Lap(), Fixtures.Replay()));
        Assert.True(File.Exists(Path.Combine(Data, FileRecordStore.ReplayFolder, name + FileRecordStore.ReplayExtension)));
        Assert.Equal([name], store.ListReplays());
        Assert.Equal(90, store.LoadReplay(name)!.Samples.Count);
        store.DeleteReplay(name);
        Assert.Empty(store.ListReplays());
        Assert.Null(store.LoadReplay(name));
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("a/b")]
    [InlineData("")]
    public void UnsafeReplayNamesAreRefused(string name)
    {
        var store = new FileRecordStore(Data);
        Assert.False(store.SaveReplay(name, Fixtures.Lap(), Fixtures.Replay()));
        Assert.Null(store.LoadReplay(name));
    }

    [Fact]
    public void CorruptReplaysLoadAsMissing()
    {
        var replays = Path.Combine(Data, FileRecordStore.ReplayFolder);
        Directory.CreateDirectory(replays);
        File.WriteAllText(Path.Combine(replays, "broken" + FileRecordStore.ReplayExtension), "not gzip");
        Assert.Null(new FileRecordStore(Data).LoadReplay("broken"));
    }

    [Fact]
    public void AutopilotModelSurvivesARestart()
    {
        var model = new AutopilotModel("t", 3, [new Corner(4, 5)], [new CornerFactor(1, 1.08, 1.04)]);
        Assert.True(new FileRecordStore(Data).SaveAutopilot(model));
        var copy = new FileRecordStore(Data).LoadAutopilot();
        Assert.NotNull(copy);
        Assert.Equal(model.Factors, copy.Factors);
        Assert.False(File.Exists(Path.Combine(Data, FileRecordStore.AutopilotFile + ".tmp")));
    }

    [Fact]
    public void UnusableFoldersAreReportedInsteadOfThrown()
    {
        File.WriteAllText(Data, "a file where the folder should be");
        var store = new FileRecordStore(Data);
        Assert.False(store.AppendLap(Fixtures.Lap()));
        Assert.False(store.SaveReplay("name", Fixtures.Lap(), Fixtures.Replay()));
        Assert.NotNull(store.Problem);
    }
}
