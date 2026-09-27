using TermRacer.Core;
using Xunit;

namespace TermRacer.Core.Tests;

public sealed class CarPhysicsTests
{
    private static readonly CarSpec Spec = CarSpec.Default;

    private static CarState Run(CarState state, ControlInput input, double seconds, Surface surface = Surface.Tarmac)
    {
        var steps = (int)Math.Round(seconds / RaceSession.Step);
        for (var i = 0; i < steps; i++)
        {
            state = CarPhysics.Step(state, input, Spec, surface, RaceSession.Step);
        }

        return state;
    }

    private static CarState Rest => new(Vec2.Zero, 0, Vec2.Zero);

    [Fact]
    public void FullThrottleAcceleratesBriskly() => Assert.InRange(Run(Rest, new ControlInput(1, 0, 0), 2).Speed, 14, 19);

    [Fact]
    public void TopSpeedIsBounded() => Assert.InRange(Run(Rest, new ControlInput(1, 0, 0), 60).Speed, 40, 50);

    [Fact]
    public void GrassCostsTopSpeed() =>
        Assert.True(Run(Rest, new ControlInput(1, 0, 0), 30, Surface.Grass).Speed < Run(Rest, new ControlInput(1, 0, 0), 30).Speed - 10);

    [Fact]
    public void BrakingStopsWithinExpectedDistance()
    {
        var state = new CarState(Vec2.Zero, 0, new Vec2(40, 0));
        for (var i = 0; i < 2000 && state.ForwardSpeed > CarPhysics.ReverseThreshold; i++)
        {
            state = CarPhysics.Step(state, new ControlInput(0, 1, 0), Spec, Surface.Tarmac, RaceSession.Step);
        }

        Assert.InRange(state.Position.X, 40, 51);
    }

    [Fact]
    public void CorneringStaysWithinGrip()
    {
        var state = new CarState(Vec2.Zero, 0, new Vec2(30, 0));
        for (var i = 0; i < 240; i++)
        {
            state = CarPhysics.Step(state, new ControlInput(0.4, 0, 1), Spec, Surface.Tarmac, RaceSession.Step);
            Assert.True(Math.Abs(state.YawRate * state.ForwardSpeed) <= Spec.TarmacGrip + 1e-9);
        }

        Assert.True(state.Heading > 0);
    }

    [Fact]
    public void BrakeAtStandstillReverses() =>
        Assert.InRange(Run(Rest, new ControlInput(0, 1, 0), 4).ForwardSpeed, -Spec.MaxReverseSpeed - 0.01, -1);

    [Fact]
    public void NoSteeringHoldsTheLine()
    {
        var state = Run(Rest, new ControlInput(1, 0, 0), 3);
        Assert.Equal(0, state.Heading, 1e-12);
        Assert.Equal(0, state.Position.Y, 1e-12);
    }

    [Fact]
    public void SteeringRampsAtSteerRate() =>
        Assert.Equal(Spec.SteerRate * RaceSession.Step, CarPhysics.Step(Rest, new ControlInput(0, 0, 1), Spec, Surface.Tarmac, RaceSession.Step).Steer, 1e-12);
}
