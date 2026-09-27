namespace TermRacer.Core;

public sealed class Game
{
    public const double KeyGuard = 0.3;

    private readonly FixedStepClock clock = new(RaceSession.Step);
    private double lastToggle = double.NegativeInfinity;
    private double screenSince = double.NegativeInfinity;

    public Game()
        : this(Track.Build(Circuits.Gullwing), CarSpec.Default)
    {
    }

    public Game(Track track, CarSpec spec)
        : this(track, RacingLine.Build(track), spec)
    {
    }

    public Game(Track track, RacingLine line, CarSpec spec)
    {
        Track = track;
        Line = line;
        Spec = spec;
        Demo = new RaceSession(track, line, spec, RaceMode.Zen, autopilotEngaged: true);
    }

    public Track Track { get; }

    public RacingLine Line { get; }

    public CarSpec Spec { get; }

    public RaceSession Demo { get; }

    public RaceSession? Race { get; private set; }

    public GameScreen Screen { get; private set; }

    public RaceMode SelectedMode { get; private set; }

    public bool ExitRequested { get; private set; }

    public bool Paused { get; set; }

    public double Clock { get; private set; }

    public RaceSession ActiveSession => Screen == GameScreen.Menu || Race is null ? Demo : Race;

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
            case GameScreen.Results when key == GameKey.Confirm && Clock - screenSince >= KeyGuard:
                Race = null;
                SetScreen(GameScreen.Menu);
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
        ActiveSession.Advance(clock.Advance(elapsedSeconds));
        if (Screen == GameScreen.Race && Race is { IsFinished: true })
        {
            SetScreen(GameScreen.Results);
        }
    }

    private void PressMenu(GameKey key)
    {
        switch (key)
        {
            case GameKey.Up or GameKey.Left:
                SelectedMode = RaceMode.SingleLap;
                break;
            case GameKey.Down or GameKey.Right:
                SelectedMode = RaceMode.Zen;
                break;
            case GameKey.Confirm when Clock - screenSince >= KeyGuard:
                Race = new RaceSession(Track, Line, Spec, SelectedMode);
                clock.Reset();
                SetScreen(GameScreen.Race);
                break;
        }
    }

    private void PressRace(GameKey key)
    {
        if (Race is null)
        {
            return;
        }

        if (key == GameKey.ToggleAutopilot)
        {
            if (Clock - lastToggle >= KeyGuard)
            {
                lastToggle = Clock;
                Race.ToggleAutopilot();
            }

            return;
        }

        if (ToControl(key) is { } control)
        {
            Race.Press(control);
        }
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
