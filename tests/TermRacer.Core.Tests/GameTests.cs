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
}
