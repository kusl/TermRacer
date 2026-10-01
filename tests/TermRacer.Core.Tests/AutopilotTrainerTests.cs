using TermRacer.Core;
using Xunit;

namespace TermRacer.Core.Tests;

public sealed class AutopilotTrainerTests
{
    [Fact]
    public void CornersCoverEveryGripLimitedPoint()
    {
        var map = CornerMap.Build(Fixtures.Line, CarSpec.Default);
        Assert.InRange(map.Count, 4, 12);
        for (var i = 0; i < Fixtures.Line.Count; i++)
        {
            var limit = Math.Sqrt(CarSpec.Default.TarmacGrip * 0.9 / Math.Max(SpeedProfile.PeakCurvature(Fixtures.Line, i), 1e-6));
            if (limit < 44)
            {
                Assert.InRange(map.CornerAt(i), 0, map.Count - 1);
                Assert.Contains(map.CornerAt(i), map.Influencing(i));
            }
        }
    }

    [Fact]
    public void UntrainedAutopilotDrivesTheDefaultProfile()
    {
        var trainer = new AutopilotTrainer(Fixtures.Line, CarSpec.Default);
        var reference = new SpeedProfile(Fixtures.Line, CarSpec.Default);
        for (var i = 0; i < reference.Count; i++)
        {
            Assert.Equal(reference[i], trainer.TrialProfile[i]);
            Assert.Equal(reference[i], trainer.SafeProfile[i]);
        }
    }

    [Fact]
    public void CleanCornersProbeFasterAndFaultsBisect()
    {
        var start = new CornerFactor(0.9, double.PositiveInfinity, 0.9);
        var probing = AutopilotTrainer.Next(start, false);
        Assert.Equal(new CornerFactor(0.9, double.PositiveInfinity, 0.9 + AutopilotTrainer.Probe), probing);
        var faulted = AutopilotTrainer.Next(probing, true);
        Assert.Equal(0.9, faulted.Safe);
        Assert.Equal(probing.Trial, faulted.Limit);
        Assert.Equal((faulted.Safe + faulted.Limit) / 2, faulted.Trial, 1e-12);
        var settled = AutopilotTrainer.Next(new CornerFactor(1.1, 1.105, 1.1), false);
        Assert.Equal(settled.Safe, settled.Trial);
        var retreat = AutopilotTrainer.Next(settled, true);
        Assert.Equal(1.1 - AutopilotTrainer.Retreat, retreat.Safe, 1e-12);
        Assert.InRange(retreat.Trial, retreat.Safe, 1.1);
    }

    [Fact]
    public void FaultsAreBlamedOnTheCornerTheyFollow()
    {
        var trainer = new AutopilotTrainer(Fixtures.Line, CarSpec.Default);
        var point = Enumerable.Range(0, Fixtures.Line.Count).First(i => trainer.Map.Influencing(i).Count == 1 && trainer.Map.CornerAt(i) < 0);
        var corner = trainer.Map.Influencing(point)[0];
        trainer.Learn([]);
        trainer.Learn([point]);
        Assert.Equal(AutopilotTrainer.StartFactor + AutopilotTrainer.Probe, trainer.Factors[corner].Limit, 1e-12);
        Assert.Equal(trainer.Map.Count - 1, trainer.Factors.Count(factor => double.IsPositiveInfinity(factor.Limit)));

        Assert.Equal(2, trainer.Laps);
    }

    [Fact]
    public void TrainingSurvivesARestartButNotATrackChange()
    {
        var trainer = new AutopilotTrainer(Fixtures.Line, CarSpec.Default);
        trainer.Learn([]);
        trainer.Learn([]);
        var restored = new AutopilotTrainer(Fixtures.Line, CarSpec.Default, trainer.Model);
        Assert.Equal(trainer.Factors, restored.Factors);
        Assert.Equal(2, restored.Laps);
        var foreign = new AutopilotTrainer(Fixtures.Line, CarSpec.Default, trainer.Model with { TrackId = "elsewhere" });
        Assert.Equal(0, foreign.Laps);
        Assert.All(foreign.Factors, factor => Assert.Equal(AutopilotTrainer.StartFactor, factor.Trial));
    }

    [Fact]
    public void PracticeMakesTheAutopilotFaster()
    {
        var store = new MemoryRecordStore();
        var game = Fixtures.NewGame(store);
        game.Press(GameKey.Down);
        game.Press(GameKey.Confirm);
        game.Press(GameKey.ToggleAutopilot);
        for (var i = 0; i < 20000 && store.Laps.Count < 6; i++)
        {
            game.Advance(0.25);
        }

        Assert.Equal(6, store.Laps.Count);
        Assert.All(store.Laps, lap => Assert.Equal(0, lap.OffTrackSeconds));
        Assert.All(store.Laps, lap => Assert.Equal(0, lap.WallHits));
        Assert.InRange(store.Laps[1].Seconds, 54, 56);
        Assert.InRange(store.Laps.Skip(2).Min(lap => lap.Seconds), 49, store.Laps[1].Seconds - 1.5);
        Assert.Equal(6, game.Trainer.Laps);
        Assert.Equal(6, store.Autopilot!.Laps);
    }
}
