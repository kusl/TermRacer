using TermRacer.Core;

namespace TermRacer.Core.Tests;

internal static class Fixtures
{
    public static Track Track { get; } = Track.Build(Circuits.Gullwing);

    public static RacingLine Line { get; } = RacingLine.Build(Track);

    public static Game NewGame() => new(Track, Line, CarSpec.Default);

    public static Game NewGame(IRecordStore store) => new(Track, Line, CarSpec.Default, store);

    public static Vec2 PathPoint(int index) => (Track.Point(index) + Track.Point(index + 1)) / 2 + Track.Normal(index);

    public static CompletedLap AutopilotLap(RaceMode mode = RaceMode.SingleLap)
    {
        var session = new RaceSession(Track, Line, CarSpec.Default, mode, autopilotEngaged: true);
        for (var i = 0; i < 120 * 200 && session.Laps.Laps.Count == 0; i++)
        {
            session.StepOnce();
        }

        return session.TakeCompletedLaps().Single();
    }

    public static void FinishSingleLap(Game game)
    {
        game.Press(GameKey.Confirm);
        game.Press(GameKey.ToggleAutopilot);
        for (var i = 0; i < 2000 && game.Screen != GameScreen.Results; i++)
        {
            game.Advance(0.1);
        }
    }

    public static void Settle(Game game) => game.Advance(Game.KeyGuard + 0.05);
}
