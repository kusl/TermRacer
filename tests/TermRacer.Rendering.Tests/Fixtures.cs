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

    public static string Text(CellBuffer buffer) => string.Join('\n', Enumerable.Range(0, buffer.Height).Select(buffer.RowText));
}
