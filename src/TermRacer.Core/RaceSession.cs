namespace TermRacer.Core;

public sealed class RaceSession
{
    public const double Step = 1.0 / 120;

    private readonly Autopilot autopilot;
    private bool inContact;
    private double wrongWayTime;

    public RaceSession(Track track, RacingLine line, CarSpec spec, RaceMode mode, bool autopilotEngaged = false)
    {
        Track = track;
        Spec = spec;
        Mode = mode;
        autopilot = new Autopilot(line, new SpeedProfile(line, spec), spec);
        Laps = new LapTracker(track, mode);
        AutopilotEngaged = autopilotEngaged;
        Place(CarState.At(track.StartPose));
    }

    public Track Track { get; }

    public CarSpec Spec { get; }

    public RaceMode Mode { get; }

    public LapTracker Laps { get; }

    public DriverControls Controls { get; } = new();

    public CarState Car { get; private set; }

    public TrackProjection Projection { get; private set; }

    public Surface Surface { get; private set; }

    public ControlInput Input { get; private set; }

    public bool AutopilotEngaged { get; private set; }

    public double Time { get; private set; }

    public int WallHits { get; private set; }

    public LapEvent LastEvent { get; private set; }

    public double LastEventTime { get; private set; } = double.NegativeInfinity;

    public bool WrongWay => wrongWayTime > 0.8;

    public bool IsFinished => Laps.Phase == RacePhase.Finished;

    public void Place(CarState car)
    {
        Car = car;
        Projection = Track.Project(car.Position);
        Surface = Track.SurfaceAt(Projection);
        autopilot.Reset();
        inContact = false;
        wrongWayTime = 0;
    }

    public void Press(DriveControl control)
    {
        if (!AutopilotEngaged)
        {
            Controls.Press(control, Time);
        }
    }

    public void Release(DriveControl control) => Controls.Release(control, Time);

    public void ToggleAutopilot()
    {
        AutopilotEngaged = !AutopilotEngaged;
        Controls.Clear();
        autopilot.Reset();
    }

    public void Advance(int steps)
    {
        for (var i = 0; i < steps && !IsFinished; i++)
        {
            StepOnce();
        }
    }

    public void StepOnce()
    {
        if (IsFinished)
        {
            return;
        }

        Input = AutopilotEngaged ? autopilot.Drive(Car, Surface, Step) : Controls.Sample(Time);
        var from = Car.Position;
        var next = CarPhysics.Step(Car, Input, Spec, Surface, Step);
        var projection = Track.Project(next.Position);
        if (Walls.Resolve(ref next, Track, Spec, projection) is { } impact)
        {
            if (!inContact && impact > 1)
            {
                WallHits++;
            }

            inContact = true;
            projection = Track.Project(next.Position);
        }
        else
        {
            inContact = false;
        }

        Car = next;
        Projection = projection;
        Surface = Track.SurfaceAt(projection);
        var lapEvent = Laps.Update(from, next.Position, Time, Time + Step);
        Time += Step;
        if (lapEvent != LapEvent.None)
        {
            LastEvent = lapEvent;
            LastEventTime = Time;
        }

        wrongWayTime = Car.Velocity.Dot(projection.Tangent) < -2 ? wrongWayTime + Step : 0;
    }
}
