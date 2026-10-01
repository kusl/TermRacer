using TermRacer.Core;

namespace TermRacer.Storage.Tests;

internal static class Fixtures
{
    public static readonly DateTimeOffset Moment = new(2026, 10, 1, 22, 51, 3, 123, TimeSpan.Zero);

    public static LapRecord Lap(double seconds = 56.409, int lap = 1, Driver driver = Driver.Manual, string? replay = "20261001-225103123-single-1") =>
        new(Moment, "gullwing-park-5e8f8786", lap == 1 ? RaceMode.SingleLap : RaceMode.Zen, lap, driver, seconds, 1, 0.25, replay);

    public static Replay Replay(int count = 90, double seconds = 2.95)
    {
        var samples = Enumerable.Range(0, count).Select(k => new ReplaySample(
            new Vec2(k * 1.25, -k * 0.5),
            Math.Sin(k * 0.1),
            30 + k * 0.1,
            k * 1.3,
            k % 2,
            k % 3 == 0 ? 0.5 : 0,
            Math.Cos(k * 0.2) * 0.8,
            k % 4 == 0,
            k % 5 == 0));
        return new Replay("gullwing-park-5e8f8786", seconds, samples);
    }
}
