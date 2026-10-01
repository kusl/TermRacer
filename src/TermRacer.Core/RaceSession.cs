namespace TermRacer.Core;

public sealed class RaceSession
{
    public const double Step = 1.0 / 120;

    private readonly Autopilot autopilot;
    private readonly LapRecorder? recorder;
    private readonly List<CompletedLap> completed = [];
    private bool inContact;
    private double wrongWayTime;
    private double odometer;
    private double lapOdometer;
    private Replay? standingGhost;
    private Replay? flyingGhost;

    public RaceSession(Track track, RacingLine line, CarSpec spec, RaceMode mode, bool autopilotEngaged = false, SpeedProfile? profile = null, bool record = true)
    {
        Track = track;
        Spec = spec;
        Mode = mode;
        autopilot = new Autopilot(line, profile ?? new SpeedProfile(line, spec), spec);
        Laps = new LapTracker(track, mode);
        AutopilotEngaged = autopilotEngaged;
        recorder = record ? new LapRecorder(track.Id, Step) : null;
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

    public bool AutopilotIntervening => AutopilotEngaged && autopilot.Intervening;

    public SpeedProfile AutopilotProfile => autopilot.Profile;

    public double Time { get; private set; }

    public int WallHits { get; private set; }

    public LapEvent LastEvent { get; private set; }

    public double LastEventTime { get; private set; } = double.NegativeInfinity;

    public bool WrongWay => wrongWayTime > 0.8;

    public bool IsFinished => Laps.Phase == RacePhase.Finished;

    public double LapTime => Laps.CurrentLapTime(Time);

    public double LapDistance => Laps.Phase == RacePhase.Running ? odometer - lapOdometer : 0;

    public Replay? Ghost => Laps.Phase != RacePhase.Running ? null : Laps.CurrentLap <= 1 ? standingGhost : flyingGhost;

    public CarState? GhostCar => Ghost is { } ghost && LapTime <= ghost.LapTime ? ghost.CarAt(LapTime) : null;

    public double? GhostGap => Ghost?.TimeAt(LapDistance) is { } at ? LapTime - at : null;

    public void Place(CarState car)
    {
        Car = car;
        Projection = Track.Project(car.Position);
        Surface = Track.SurfaceAt(Projection);
        autopilot.Reset();
        inContact = false;
        wrongWayTime = 0;
    }

    public void SetGhosts(Replay? standing, Replay? flying)
    {
        standingGhost = standing;
        flyingGhost = flying;
    }

    public void UseAutopilotProfile(SpeedProfile profile) => autopilot.UseProfile(profile);

    public IReadOnlyList<CompletedLap> TakeCompletedLaps()
    {
        if (completed.Count == 0)
        {
            return [];
        }

        CompletedLap[] laps = [.. completed];
        completed.Clear();
        return laps;
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

        Input = AutopilotEngaged ? autopilot.Drive(Car, Surface, Projection, Step) : Controls.Sample(Time);
        var intervening = AutopilotIntervening;
        var from = Car.Position;
        var next = CarPhysics.Step(Car, Input, Spec, Surface, Step);
        var projection = Track.Project(next.Position, Projection.Segment);
        var contact = false;
        if (Walls.Resolve(ref next, Track, Spec, projection) is { } impact)
        {
            if (!inContact && impact > 1)
            {
                WallHits++;
            }

            inContact = true;
            contact = true;
            projection = Track.Project(next.Position, projection.Segment);
        }
        else
        {
            inContact = false;
        }

        var before = odometer;
        odometer += Travel(Projection.S, projection.S);
        Car = next;
        Projection = projection;
        Surface = Track.SurfaceAt(projection);
        var start = Time;
        var lapEvent = Laps.Update(from, next.Position, Time, Time + Step);
        Time += Step;
        if (lapEvent != LapEvent.None)
        {
            LastEvent = lapEvent;
            LastEventTime = Time;
        }

        var crossing = lapOdometer;
        if (lapEvent is LapEvent.Started or LapEvent.Completed)
        {
            crossing = before + (odometer - before) * Math.Clamp((Laps.LapStart - start) / Step, 0, 1);
        }

        if (recorder is not null)
        {
            var frame = new TelemetryFrame(Time, next, Input, AutopilotEngaged, Surface == Surface.Grass, intervening || contact || Surface == Surface.Grass, odometer, projection.Segment);
            switch (lapEvent)
            {
                case LapEvent.Started:
                    recorder.Begin(Laps.LapStart, crossing, WallHits, frame);
                    break;
                case LapEvent.Completed:
                    completed.Add(recorder.Complete(Mode, Laps.Laps.Count, Laps.Laps[^1], Laps.LapStart, crossing, WallHits, frame));
                    break;
                default:
                    recorder.Add(frame);
                    break;
            }
        }

        lapOdometer = crossing;
        wrongWayTime = Car.Velocity.Dot(projection.Tangent) < -2 ? wrongWayTime + Step : 0;
    }

    private double Travel(double from, double to)
    {
        var delta = to - from;
        var half = Track.Length / 2;
        return delta > half ? delta - Track.Length : delta < -half ? delta + Track.Length : delta;
    }
}
