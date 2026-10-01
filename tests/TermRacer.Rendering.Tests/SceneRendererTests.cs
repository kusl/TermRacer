using TermRacer.Core;
using Xunit;

namespace TermRacer.Rendering.Tests;

public sealed class SceneRendererTests
{
    [Fact]
    public void SmallTerminalGetsResizePrompt()
    {
        var buffer = new CellBuffer(100, 30);
        new SceneRenderer(Fixtures.Track).Render(Fixtures.NewGame(), buffer);
        var text = Fixtures.Text(buffer);
        Assert.Contains("Terminal too small", text);
        Assert.Contains("120 × 40 needed", text);
    }

    [Fact]
    public void EveryScreenPaintsEveryCell()
    {
        var game = Fixtures.FinishedGame();
        var renderer = new SceneRenderer(Fixtures.Track);
        foreach (var candidate in new[] { Fixtures.NewGame(), RacingGame(), game, Fixtures.BackOnTheMenu(), Fixtures.ReplayList(), Fixtures.WatchingReplay(), Fixtures.ChasingGhost(), EmptyReplayList() })
        {
            var buffer = new CellBuffer(SceneRenderer.MinColumns, SceneRenderer.MinRows);
            renderer.Render(candidate, buffer);
            var unpainted = Enumerable.Range(0, buffer.Height)
                .SelectMany(y => Enumerable.Range(0, buffer.Width).Where(x => buffer[x, y].Glyph == '\0').Select(x => (X: x, Y: y)));
            Assert.Empty(unpainted);
        }
    }

    [Fact]
    public void UnchangedStateRendersIdenticalFrames()
    {
        var game = RacingGame();
        var renderer = new SceneRenderer(Fixtures.Track);
        var first = new CellBuffer(130, 42);
        var second = new CellBuffer(130, 42);
        renderer.Render(game, first);
        renderer.Render(game, second);
        var runs = new List<CellRun>();
        FrameDiff.Compute(first, second, runs);
        Assert.Empty(runs);
    }

    [Fact]
    public void DrivingRedrawsOnlyPartOfTheScreen()
    {
        var game = RacingGame();
        var renderer = new SceneRenderer(Fixtures.Track);
        var first = new CellBuffer(120, 40);
        var second = new CellBuffer(120, 40);
        renderer.Render(game, first);
        game.Advance(0.25);
        renderer.Render(game, second);
        var runs = new List<CellRun>();
        FrameDiff.Compute(first, second, runs);
        Assert.NotEmpty(runs);
        Assert.True(FrameDiff.CellCount(runs) < 120 * 40 / 2);
    }

    [Fact]
    public void MenuShowsTitleAndModes()
    {
        var buffer = new CellBuffer(120, 40);
        new SceneRenderer(Fixtures.Track).Render(Fixtures.NewGame(), buffer);
        var text = Fixtures.Text(buffer);
        Assert.Contains("Single lap", text);
        Assert.Contains("Zen mode", text);
        Assert.Contains(Fixtures.Track.Name, text);
    }

    [Fact]
    public void HudShowsModeAndAutopilotBadge()
    {
        var game = RacingGame();
        game.Press(GameKey.ToggleAutopilot);
        var buffer = new CellBuffer(120, 40);
        new SceneRenderer(Fixtures.Track).Render(game, buffer);
        Assert.Contains("Single lap", buffer.RowText(0));
        Assert.Contains("Autopilot", buffer.RowText(0));
        Assert.Contains("Esc", buffer.RowText(39));
    }

    [Fact]
    public void ResultsShowTheLapTime()
    {
        var game = Fixtures.FinishedGame();
        var buffer = new CellBuffer(120, 40);
        new SceneRenderer(Fixtures.Track).Render(game, buffer);
        var text = Fixtures.Text(buffer);
        Assert.Contains("Lap complete", text);
        Assert.Contains(TimeFormat.Lap(game.Race!.Laps.LastLap), text);
    }

    [Fact]
    public void ResultsAnnounceANewRecord()
    {
        var buffer = new CellBuffer(120, 40);
        new SceneRenderer(Fixtures.Track).Render(Fixtures.FinishedGame(), buffer);
        Assert.Contains("New record", Fixtures.Text(buffer));
    }

    [Fact]
    public void MenuShowsRecordsTrainingAndStorage()
    {
        var buffer = new CellBuffer(120, 40);
        new SceneRenderer(Fixtures.Track) { Version = "v1.0.7" }.Render(Fixtures.BackOnTheMenu(), buffer);
        var text = Fixtures.Text(buffer);
        Assert.Contains("Record 0:", text);
        Assert.Contains("1 saved", text);
        Assert.Contains("Autopilot: 1 practice lap", text);
        Assert.Contains("live in memory", text);
        Assert.Contains("v1.0.7", buffer.RowText(39));
    }

    [Fact]
    public void ReplayListShowsSavedLaps()
    {
        var game = Fixtures.ReplayList();
        var buffer = new CellBuffer(120, 40);
        new SceneRenderer(Fixtures.Track).Render(game, buffer);
        var text = Fixtures.Text(buffer);
        Assert.Contains("Replays", text);
        Assert.Contains(TimeFormat.Lap(Assert.Single(game.ReplayList).Seconds), text);
        Assert.Contains("autopilot", text);
        Assert.Contains("record", text);
    }

    [Fact]
    public void EmptyReplayListSaysSo()
    {
        var buffer = new CellBuffer(120, 40);
        new SceneRenderer(Fixtures.Track).Render(EmptyReplayList(), buffer);
        Assert.Contains("No saved laps yet", Fixtures.Text(buffer));
    }

    [Fact]
    public void ReplayViewerShowsTheLap()
    {
        var buffer = new CellBuffer(120, 40);
        new SceneRenderer(Fixtures.Track).Render(Fixtures.WatchingReplay(), buffer);
        var top = buffer.RowText(0);
        Assert.Contains("Replay", top);
        Assert.Contains("Single lap · autopilot", top);
        Assert.Contains("0:00.250 / ", top);
        Assert.Contains("seek", buffer.RowText(39));
    }

    [Fact]
    public void GhostIsDrawnOnlyWhenVisible()
    {
        var game = Fixtures.ChasingGhost();
        Assert.NotNull(game.Race!.GhostCar);
        var renderer = new SceneRenderer(Fixtures.Track);
        var shown = new CellBuffer(120, 40);
        var hidden = new CellBuffer(120, 40);
        renderer.Render(game, shown);
        game.Press(GameKey.ToggleGhost);
        renderer.Render(game, hidden);
        var runs = new List<CellRun>();
        FrameDiff.Compute(shown, hidden, runs);
        Assert.NotEmpty(runs);
        Assert.Contains("Ghost ", shown.RowText(0));
        Assert.Contains("Ghost off", hidden.RowText(0));
    }

    private static Game EmptyReplayList()
    {
        var game = Fixtures.NewGame();
        game.Press(GameKey.Down);
        game.Press(GameKey.Down);
        Fixtures.Settle(game);
        game.Press(GameKey.Confirm);
        return game;
    }

    private static Game RacingGame()
    {
        var game = Fixtures.NewGame();
        game.Press(GameKey.Confirm);
        game.Press(GameKey.Up);
        game.Advance(2);
        return game;
    }
}
