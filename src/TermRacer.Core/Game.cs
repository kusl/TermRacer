using System.Globalization;

namespace TermRacer.Core;

public sealed class Game
{
    public const double KeyGuard = 0.3;

    private readonly FixedStepClock clock = new(RaceSession.Step);
    private readonly IRecordStore store;
    private readonly TimeProvider time;
    private readonly Dictionary<string, Replay> replays = new(StringComparer.Ordinal);
    private double lastToggle = double.NegativeInfinity;
    private double lastGhostToggle = double.NegativeInfinity;
    private double lastPause = double.NegativeInfinity;
    private double screenSince = double.NegativeInfinity;

    public Game()
        : this(Track.Build(Circuits.Gullwing), CarSpec.Default)
    {
    }

    public Game(IRecordStore store, TimeProvider? time = null)
        : this(Track.Build(Circuits.Gullwing), CarSpec.Default, store, time)
    {
    }

    public Game(Track track, CarSpec spec, IRecordStore? store = null, TimeProvider? time = null)
        : this(track, RacingLine.Build(track), spec, store, time)
    {
    }

    public Game(Track track, RacingLine line, CarSpec spec, IRecordStore? store = null, TimeProvider? time = null)
    {
        Track = track;
        Line = line;
        Spec = spec;
        this.store = store ?? new MemoryRecordStore();
        this.time = time ?? TimeProvider.System;
        History = new LapHistory(track.Id, this.store.LoadLaps(), this.store.ListReplays());
        Trainer = new AutopilotTrainer(line, spec, this.store.LoadAutopilot());
        Demo = new RaceSession(track, line, spec, RaceMode.Zen, autopilotEngaged: true, Trainer.SafeProfile, record: false);
    }

    public Track Track { get; }

    public RacingLine Line { get; }

    public CarSpec Spec { get; }

    public RaceSession Demo { get; }

    public RaceSession? Race { get; private set; }

    public GameScreen Screen { get; private set; }

    public MenuEntry MenuChoice { get; private set; }

    public RaceMode SelectedMode => MenuChoice == MenuEntry.Zen ? RaceMode.Zen : RaceMode.SingleLap;

    public bool ExitRequested { get; private set; }

    public bool Paused { get; set; }

    public double Clock { get; private set; }

    public bool GhostVisible { get; private set; } = true;

    public LapHistory History { get; }

    public AutopilotTrainer Trainer { get; }

    public string StorageLocation => store.Location;

    public string? StorageProblem => store.Problem;

    public LapRecord? LastRecord { get; private set; }

    public LapRecord? PreviousBest { get; private set; }

    public IReadOnlyList<LapRecord> ReplayList { get; private set; } = [];

    public int ReplayChoice { get; private set; }

    public ReplayPlayer? Viewer { get; private set; }

    public RaceSession ActiveSession => Race is not null && Screen is GameScreen.Race or GameScreen.Results ? Race : Demo;

    public bool LastLapWasRecord => LastRecord is { } last && (PreviousBest is null || last.Seconds < PreviousBest.Seconds);

    public void Press(GameKey key)
    {
        if (key == GameKey.Exit)
        {
            ExitRequested = true;
            return;
        }

        if (Paused)
        {
            return;
        }

        switch (Screen)
        {
            case GameScreen.Menu:
                PressMenu(key);
                break;
            case GameScreen.Race:
                PressRace(key);
                break;
            case GameScreen.Results when key is GameKey.Confirm or GameKey.Back && Clock - screenSince >= KeyGuard:
                Race = null;
                SetScreen(GameScreen.Menu);
                break;
            case GameScreen.Replays:
                PressReplays(key);
                break;
            case GameScreen.Replay:
                PressReplay(key);
                break;
        }
    }

    public void Release(GameKey key)
    {
        if (Screen == GameScreen.Race && Race is not null && ToControl(key) is { } control)
        {
            Race.Release(control);
        }
    }

    public void Advance(double elapsedSeconds)
    {
        if (Paused || ExitRequested)
        {
            clock.Reset();
            return;
        }

        Clock += Math.Max(0, elapsedSeconds);
        var steps = clock.Advance(elapsedSeconds);
        switch (Screen)
        {
            case GameScreen.Replay:
                Viewer?.Advance(Math.Clamp(elapsedSeconds, 0, 0.25));
                break;
            case GameScreen.Replays:
                break;
            default:
                ActiveSession.Advance(steps);
                break;
        }

        if (Race is not null)
        {
            Collect(Race);
        }

        if (Screen == GameScreen.Race && Race is { IsFinished: true })
        {
            SetScreen(GameScreen.Results);
        }
    }

    public Replay? GhostFor(bool standing)
    {
        foreach (var candidate in History.FastestWithReplay(standing))
        {
            if (Load(candidate.Replay!) is { } replay)
            {
                return replay;
            }
        }

        return null;
    }

    private void PressMenu(GameKey key)
    {
        switch (key)
        {
            case GameKey.Up or GameKey.Left:
                MenuChoice = (MenuEntry)Math.Max(0, (int)MenuChoice - 1);
                break;
            case GameKey.Down or GameKey.Right:
                MenuChoice = (MenuEntry)Math.Min((int)MenuEntry.Replays, (int)MenuChoice + 1);
                break;
            case GameKey.Confirm when Clock - screenSince >= KeyGuard:
                if (MenuChoice == MenuEntry.Replays)
                {
                    OpenReplays();
                }
                else
                {
                    StartRace(SelectedMode);
                }

                break;
        }
    }

    private void PressRace(GameKey key)
    {
        if (Race is null)
        {
            return;
        }

        switch (key)
        {
            case GameKey.ToggleAutopilot:
                if (Clock - lastToggle >= KeyGuard)
                {
                    lastToggle = Clock;
                    Race.ToggleAutopilot();
                }

                return;
            case GameKey.ToggleGhost:
                ToggleGhost();
                return;
            case GameKey.Back:
                Collect(Race);
                Race = null;
                SetScreen(GameScreen.Menu);
                return;
        }

        if (ToControl(key) is { } control)
        {
            Race.Press(control);
        }
    }

    private void PressReplays(GameKey key)
    {
        switch (key)
        {
            case GameKey.Up or GameKey.Left:
                ReplayChoice = Math.Max(0, ReplayChoice - 1);
                break;
            case GameKey.Down or GameKey.Right:
                ReplayChoice = Math.Min(Math.Max(0, ReplayList.Count - 1), ReplayChoice + 1);
                break;
            case GameKey.Confirm when Clock - screenSince >= KeyGuard && ReplayList.Count > 0:
                Watch(ReplayList[ReplayChoice]);
                break;
            case GameKey.Back:
                SetScreen(GameScreen.Menu);
                break;
        }
    }

    private void PressReplay(GameKey key)
    {
        if (Viewer is null)
        {
            return;
        }

        switch (key)
        {
            case GameKey.Left:
                Viewer.Seek(-ReplayPlayer.SeekStep);
                break;
            case GameKey.Right:
                Viewer.Seek(ReplayPlayer.SeekStep);
                break;
            case GameKey.Up:
                Viewer.Faster();
                break;
            case GameKey.Down:
                Viewer.Slower();
                break;
            case GameKey.Confirm when Clock - lastPause >= KeyGuard && Clock - screenSince >= KeyGuard:
                lastPause = Clock;
                Viewer.TogglePause();
                break;
            case GameKey.ToggleGhost:
                ToggleGhost();
                break;
            case GameKey.Back:
                Viewer = null;
                OpenReplays();
                break;
        }
    }

    private void ToggleGhost()
    {
        if (Clock - lastGhostToggle >= KeyGuard)
        {
            lastGhostToggle = Clock;
            GhostVisible = !GhostVisible;
        }
    }

    private void StartRace(RaceMode mode)
    {
        Race = new RaceSession(Track, Line, Spec, mode, profile: Trainer.TrialProfile);
        Race.SetGhosts(GhostFor(true), GhostFor(false));
        LastRecord = null;
        PreviousBest = null;
        clock.Reset();
        SetScreen(GameScreen.Race);
    }

    private void OpenReplays()
    {
        ReplayList = History.WithReplays();
        ReplayChoice = Math.Clamp(ReplayChoice, 0, Math.Max(0, ReplayList.Count - 1));
        SetScreen(GameScreen.Replays);
    }

    private void Watch(LapRecord record)
    {
        if (Load(record.Replay!) is not { } replay)
        {
            OpenReplays();
            return;
        }

        var ghost = GhostFor(record.StandingStart);
        Viewer = new ReplayPlayer(record, replay, ReferenceEquals(ghost, replay) ? null : ghost);
        SetScreen(GameScreen.Replay);
    }

    private Replay? Load(string name)
    {
        if (replays.TryGetValue(name, out var cached))
        {
            return cached;
        }

        if (store.LoadReplay(name) is { } loaded && loaded.TrackId == Track.Id)
        {
            replays[name] = loaded;
            return loaded;
        }

        History.Forget(name);
        return null;
    }

    private void Collect(RaceSession race)
    {
        foreach (var lap in race.TakeCompletedLaps())
        {
            Record(race, lap);
        }
    }

    private void Record(RaceSession race, CompletedLap lap)
    {
        var previous = History.Best(lap.StandingStart);
        var recordedAt = time.GetUtcNow();
        var name = ReplayName(recordedAt, lap);
        var record = new LapRecord(recordedAt, Track.Id, lap.Mode, lap.Number, lap.Driver, lap.Seconds, lap.WallHits, lap.OffTrackSeconds, name);
        if (store.SaveReplay(name, record, lap.Replay))
        {
            replays[name] = lap.Replay;
        }
        else
        {
            record = record with { Replay = null };
        }

        store.AppendLap(record);
        History.Add(record);
        foreach (var stale in History.Prune())
        {
            store.DeleteReplay(stale);
            replays.Remove(stale);
        }

        race.SetGhosts(GhostFor(true), GhostFor(false));
        if (lap.Driver == Driver.Autopilot)
        {
            Trainer.Learn(lap.FaultPoints);
            store.SaveAutopilot(Trainer.Model);
            race.UseAutopilotProfile(Trainer.TrialProfile);
            Demo.UseAutopilotProfile(Trainer.SafeProfile);
        }

        LastRecord = record;
        PreviousBest = previous;
    }

    private string ReplayName(DateTimeOffset recordedAt, CompletedLap lap)
    {
        var mode = lap.Mode == RaceMode.SingleLap ? "single" : "zen";
        var stem = string.Create(CultureInfo.InvariantCulture, $"{recordedAt.UtcDateTime:yyyyMMdd-HHmmssfff}-{mode}-{lap.Number}");
        var name = stem;
        for (var suffix = 2; History.Contains(name); suffix++)
        {
            name = string.Create(CultureInfo.InvariantCulture, $"{stem}-{suffix}");
        }

        return name;
    }

    private void SetScreen(GameScreen screen)
    {
        Screen = screen;
        screenSince = Clock;
    }

    private static DriveControl? ToControl(GameKey key) => key switch
    {
        GameKey.Up => DriveControl.Throttle,
        GameKey.Down => DriveControl.Brake,
        GameKey.Left => DriveControl.SteerLeft,
        GameKey.Right => DriveControl.SteerRight,
        _ => null,
    };
}
