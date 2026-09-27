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
        foreach (var candidate in new[] { Fixtures.NewGame(), RacingGame(), game })
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

    private static Game RacingGame()
    {
        var game = Fixtures.NewGame();
        game.Press(GameKey.Confirm);
        game.Press(GameKey.Up);
        game.Advance(2);
        return game;
    }
}
