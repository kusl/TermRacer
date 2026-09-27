using TermRacer.Core;
using Xunit;

namespace TermRacer.Core.Tests;

public sealed class DriverControlsTests
{
    [Fact]
    public void LegacyThrottleLatchesUntilBrake()
    {
        var controls = new DriverControls();
        controls.Press(DriveControl.Throttle, 0);
        Assert.Equal(1, controls.Sample(30).Throttle);
        controls.Press(DriveControl.Brake, 30);
        Assert.Equal(new ControlInput(0, 1, 0), controls.Sample(30.1));
    }

    [Fact]
    public void LegacySteeringExpiresWithoutRepeats()
    {
        var controls = new DriverControls(0.55, 0.16);
        controls.Press(DriveControl.SteerLeft, 0);
        Assert.Equal(-1, controls.Sample(0.5).Steer);
        Assert.Equal(0, controls.Sample(0.6).Steer);
    }

    [Fact]
    public void LegacyRepeatsShortenTheHold()
    {
        var controls = new DriverControls(0.55, 0.16);
        controls.Press(DriveControl.SteerRight, 0);
        controls.Press(DriveControl.SteerRight, 0.5);
        Assert.True(controls.Active(DriveControl.SteerRight, 0.65));
        Assert.False(controls.Active(DriveControl.SteerRight, 0.67));
    }

    [Fact]
    public void OppositeSteeringCancels()
    {
        var controls = new DriverControls();
        controls.Press(DriveControl.SteerLeft, 0);
        controls.Press(DriveControl.SteerRight, 0.1);
        Assert.Equal(1, controls.Sample(0.2).Steer);
    }

    [Fact]
    public void ReleaseEventsSwitchToExactHolding()
    {
        var controls = new DriverControls();
        controls.Press(DriveControl.Throttle, 0);
        controls.Release(DriveControl.Throttle, 1);
        Assert.True(controls.ReleaseAware);
        Assert.Equal(0, controls.Sample(1).Throttle);
        controls.Press(DriveControl.SteerRight, 2);
        Assert.Equal(1, controls.Sample(100).Steer);
        controls.Release(DriveControl.SteerRight, 101);
        Assert.Equal(0, controls.Sample(101).Steer);
    }

    [Fact]
    public void SwitchingModesKeepsHeldControls()
    {
        var controls = new DriverControls();
        controls.Press(DriveControl.Throttle, 0);
        controls.Press(DriveControl.SteerLeft, 0.1);
        controls.Release(DriveControl.SteerLeft, 0.3);
        Assert.Equal(new ControlInput(1, 0, 0), controls.Sample(5));
    }
}
