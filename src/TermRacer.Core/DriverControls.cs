namespace TermRacer.Core;

public sealed class DriverControls(double initialHold = 0.55, double repeatHold = 0.16)
{
    private readonly double[] expiry = new double[4];
    private readonly bool[] held = new bool[4];

    public bool ReleaseAware { get; private set; }

    public bool ThrottleLatched { get; private set; }

    public void Press(DriveControl control, double now)
    {
        if (ReleaseAware)
        {
            held[(int)control] = true;
            return;
        }

        switch (control)
        {
            case DriveControl.Throttle:
                ThrottleLatched = true;
                expiry[(int)DriveControl.Brake] = 0;
                break;
            case DriveControl.Brake:
                ThrottleLatched = false;
                Extend(control, now);
                break;
            default:
                Extend(control, now);
                expiry[(int)(control == DriveControl.SteerLeft ? DriveControl.SteerRight : DriveControl.SteerLeft)] = 0;
                break;
        }
    }

    public void Release(DriveControl control, double now)
    {
        if (!ReleaseAware)
        {
            for (var i = 0; i < held.Length; i++)
            {
                held[i] = LegacyActive((DriveControl)i, now);
            }

            ReleaseAware = true;
            ThrottleLatched = false;
            Array.Clear(expiry);
        }

        held[(int)control] = false;
    }

    public bool Active(DriveControl control, double now) => ReleaseAware ? held[(int)control] : LegacyActive(control, now);

    public ControlInput Sample(double now) => new(
        Active(DriveControl.Throttle, now) ? 1 : 0,
        Active(DriveControl.Brake, now) ? 1 : 0,
        (Active(DriveControl.SteerRight, now) ? 1 : 0) - (Active(DriveControl.SteerLeft, now) ? 1 : 0));

    public void Clear()
    {
        Array.Clear(expiry);
        Array.Clear(held);
        ThrottleLatched = false;
    }

    private bool LegacyActive(DriveControl control, double now) =>
        control == DriveControl.Throttle ? ThrottleLatched : now < expiry[(int)control];

    private void Extend(DriveControl control, double now)
    {
        var index = (int)control;
        expiry[index] = now + (now < expiry[index] ? repeatHold : initialHold);
    }
}
