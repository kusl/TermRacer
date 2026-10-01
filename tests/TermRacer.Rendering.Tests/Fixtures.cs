using TermRacer.Core;

namespace TermRacer.Rendering.Tests;

internal static class Fixtures
{
    public static Track Track { get; } = Track.Build(Circuits.Gullwing);

    public static RacingLine Line { get; } = RacingLine.Build(Track);

    public static Game NewGame() => new(Track, Line, CarSpec.Default);

    public static Game FinishedGame()
    {
        var game = NewGame();
        game.Press(GameKey.Confirm);
        game.Press(GameKey.ToggleAutopilot);
        for (var i = 0; i < 2000 && game.Screen != GameScreen.Results; i++)
        {
            game.Advance(0.1);
        }

        return game;
    }

    public static Game BackOnTheMenu()
    {
        var game = FinishedGame();
        Settle(game);
        game.Press(GameKey.Confirm);
        return game;
    }

    public static Game ReplayList()
    {
        var game = BackOnTheMenu();
        game.Press(GameKey.Down);
        game.Press(GameKey.Down);
        Settle(game);
        game.Press(GameKey.Confirm);
        return game;
    }

    public static Game WatchingReplay()
    {
        var game = ReplayList();
        Settle(game);
        game.Press(GameKey.Confirm);
        game.Advance(0.25);
        return game;
    }

    public static Game ChasingGhost()
    {
        var game = BackOnTheMenu();
        Settle(game);
        game.Press(GameKey.Confirm);
        game.Press(GameKey.Up);
        for (var i = 0; i < 40 && game.Race!.Laps.Phase == RacePhase.Waiting; i++)
        {
            game.Advance(0.25);
        }

        game.Advance(0.25);
        return game;
    }

    public static void Settle(Game game) => game.Advance(Game.KeyGuard + 0.05);

    public static string Text(CellBuffer buffer) => string.Join('\n', Enumerable.Range(0, buffer.Height).Select(buffer.RowText));
}
