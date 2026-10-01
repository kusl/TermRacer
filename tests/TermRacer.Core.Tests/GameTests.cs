using TermRacer.Core;
using Xunit;

namespace TermRacer.Core.Tests;

public sealed class GameTests
{
    [Fact]
    public void StartsOnTheMenuWithSingleLapSelected()
    {
        var game = Fixtures.NewGame();
        Assert.Equal(GameScreen.Menu, game.Screen);
        Assert.Equal(RaceMode.SingleLap, game.SelectedMode);
        Assert.Null(game.Race);
    }

    [Fact]
    public void MenuSelectsModeAndStartsRace()
    {
        var game = Fixtures.NewGame();
        game.Press(GameKey.Down);
        Assert.Equal(RaceMode.Zen, game.SelectedMode);
        game.Press(GameKey.Confirm);
        Assert.Equal(GameScreen.Race, game.Screen);
        Assert.Equal(RaceMode.Zen, game.Race!.Mode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExitIsImmediate(bool racing)
    {
        var game = Fixtures.NewGame();
        if (racing)
        {
            game.Press(GameKey.Confirm);
        }

        game.Press(GameKey.Exit);
        Assert.True(game.ExitRequested);
    }

    [Fact]
    public void ToggleSwitchesAutopilotAndIgnoresBounce()
    {
        var game = Fixtures.NewGame();
        game.Press(GameKey.Confirm);
        game.Press(GameKey.ToggleAutopilot);
        game.Press(GameKey.ToggleAutopilot);
        Assert.True(game.Race!.AutopilotEngaged);
        game.Advance(Game.KeyGuard + 0.05);
        game.Press(GameKey.ToggleAutopilot);
        Assert.False(game.Race.AutopilotEngaged);
    }

    [Fact]
    public void DrivingKeysReachTheCar()
    {
        var game = Fixtures.NewGame();
        game.Press(GameKey.Confirm);
        game.Press(GameKey.Up);
        game.Advance(0.1);
        Assert.Equal(1, game.Race!.Input.Throttle);
        Assert.True(game.Race.Car.Speed > 0);
    }

    [Fact]
    public void SingleLapEndsOnResultsAndReturnsToMenu()
    {
        var game = Fixtures.NewGame();
        game.Press(GameKey.Confirm);
        game.Press(GameKey.ToggleAutopilot);
        for (var i = 0; i < 2000 && game.Screen != GameScreen.Results; i++)
        {
            game.Advance(0.1);
        }

        Assert.Equal(GameScreen.Results, game.Screen);
        Assert.Single(game.Race!.Laps.Laps);
        game.Advance(Game.KeyGuard + 0.05);
        game.Press(GameKey.Confirm);
        Assert.Equal(GameScreen.Menu, game.Screen);
        Assert.Null(game.Race);
    }

    [Fact]
    public void PausedGameHoldsTime()
    {
        var game = Fixtures.NewGame();
        game.Press(GameKey.Confirm);
        game.Paused = true;
        game.Advance(1);
        Assert.Equal(0, game.Race!.Time);
    }

    [Fact]
    public void CompletedLapsAreRecordedWithReplays()
    {
        var store = new MemoryRecordStore();
        var game = Fixtures.NewGame(store);
        Fixtures.FinishSingleLap(game);
        var record = Assert.Single(store.Laps);
        Assert.Equal(Driver.Autopilot, record.Driver);
        Assert.Equal(1, record.Lap);
        Assert.Equal(Fixtures.Track.Id, record.TrackId);
        Assert.Equal(game.Race!.Laps.LastLap!.Value, record.Seconds);
        Assert.NotNull(store.LoadReplay(record.Replay!));
        Assert.True(game.LastLapWasRecord);
        Assert.Equal(1, game.Trainer.Laps);
        Assert.NotNull(store.Autopilot);
    }

    [Fact]
    public void HistoryAndTrainingCarryIntoTheNextGame()
    {
        var store = new MemoryRecordStore();
        Fixtures.FinishSingleLap(Fixtures.NewGame(store));
        var game = Fixtures.NewGame(store);
        Assert.Single(game.History.Laps);
        Assert.Equal(1, game.Trainer.Laps);
        Assert.NotNull(game.GhostFor(true));
        Assert.Null(game.GhostFor(false));
    }

    [Fact]
    public void TheNextRaceChasesTheGhost()
    {
        var game = Fixtures.NewGame();
        Fixtures.FinishSingleLap(game);
        Fixtures.Settle(game);
        game.Press(GameKey.Confirm);
        Fixtures.Settle(game);
        game.Press(GameKey.Confirm);
        Assert.Equal(GameScreen.Race, game.Screen);
        Assert.Null(game.Race!.Ghost);
        game.Press(GameKey.Up);
        for (var i = 0; i < 40 && game.Race.Laps.Phase == RacePhase.Waiting; i++)
        {
            game.Advance(0.25);
        }

        game.Advance(0.25);
        Assert.Equal(RacePhase.Running, game.Race.Laps.Phase);
        Assert.NotNull(game.Race.Ghost);
        Assert.NotNull(game.Race.GhostCar);
        Assert.NotNull(game.Race.GhostGap);
    }

    [Fact]
    public void GhostToggleIsDebounced()
    {
        var game = Fixtures.NewGame();
        game.Press(GameKey.Confirm);
        game.Press(GameKey.ToggleGhost);
        game.Press(GameKey.ToggleGhost);
        Assert.False(game.GhostVisible);
        Fixtures.Settle(game);
        game.Press(GameKey.ToggleGhost);
        Assert.True(game.GhostVisible);
    }

    [Fact]
    public void BackLeavesARaceForTheMenu()
    {
        var game = Fixtures.NewGame();
        game.Press(GameKey.Down);
        game.Press(GameKey.Confirm);
        game.Press(GameKey.Back);
        Assert.Equal(GameScreen.Menu, game.Screen);
        Assert.Null(game.Race);
        Assert.False(game.ExitRequested);
    }

    [Fact]
    public void ReplaysCanBeBrowsedAndWatched()
    {
        var game = Fixtures.NewGame();
        Fixtures.FinishSingleLap(game);
        Fixtures.Settle(game);
        game.Press(GameKey.Confirm);
        game.Press(GameKey.Down);
        game.Press(GameKey.Down);
        Assert.Equal(MenuEntry.Replays, game.MenuChoice);
        Fixtures.Settle(game);
        game.Press(GameKey.Confirm);
        Assert.Equal(GameScreen.Replays, game.Screen);
        Assert.Single(game.ReplayList);
        Fixtures.Settle(game);
        game.Press(GameKey.Confirm);
        Assert.Equal(GameScreen.Replay, game.Screen);
        var viewer = game.Viewer!;
        Assert.Null(viewer.Ghost);
        game.Advance(0.2);
        Assert.Equal(0.2, viewer.Time, 1e-9);
        game.Press(GameKey.Right);
        Assert.Equal(5.2, viewer.Time, 1e-9);
        game.Press(GameKey.Back);
        Assert.Equal(GameScreen.Replays, game.Screen);
        game.Press(GameKey.Back);
        Assert.Equal(GameScreen.Menu, game.Screen);
    }

    [Fact]
    public void MixedLapsDoNotTrainTheAutopilot()
    {
        var store = new MemoryRecordStore();
        var game = Fixtures.NewGame(store);
        game.Press(GameKey.Confirm);
        game.Press(GameKey.Up);
        for (var i = 0; i < 40 && game.Race!.Laps.Phase == RacePhase.Waiting; i++)
        {
            game.Advance(0.25);
        }

        game.Advance(0.25);
        game.Press(GameKey.ToggleAutopilot);
        for (var i = 0; i < 2000 && game.Screen != GameScreen.Results; i++)
        {
            game.Advance(0.1);
        }

        Assert.Equal(Driver.Mixed, Assert.Single(store.Laps).Driver);
        Assert.Equal(0, game.Trainer.Laps);
        Assert.Null(store.Autopilot);
    }
}
