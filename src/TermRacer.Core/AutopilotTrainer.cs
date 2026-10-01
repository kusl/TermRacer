namespace TermRacer.Core;

public sealed class AutopilotTrainer
{
    public const double StartFactor = 0.9;
    public const double MaxFactor = 1.6;
    public const double MinFactor = 0.6;
    public const double Probe = 0.04;
    public const double Retreat = 0.03;
    public const double Tolerance = 0.01;

    private readonly RacingLine line;
    private readonly CarSpec spec;
    private readonly CornerFactor[] factors;

    public AutopilotTrainer(RacingLine line, CarSpec spec, AutopilotModel? saved = null)
    {
        this.line = line;
        this.spec = spec;
        Map = CornerMap.Build(line, spec);
        if (saved is not null && Fits(saved))
        {
            factors = [.. saved.Factors];
            Laps = saved.Laps;
        }
        else
        {
            factors = [.. Enumerable.Repeat(new CornerFactor(StartFactor, double.PositiveInfinity, StartFactor), Map.Count)];
        }

        (TrialProfile, SafeProfile) = Build();
    }

    public CornerMap Map { get; }

    public int Laps { get; private set; }

    public IReadOnlyList<CornerFactor> Factors => factors;

    public SpeedProfile TrialProfile { get; private set; }

    public SpeedProfile SafeProfile { get; private set; }

    public bool Converged => factors.All(factor => factor.Trial <= factor.Safe);

    public AutopilotModel Model => new(line.Track.Id, Laps, Map.Corners, [.. factors]);

    public static CornerFactor Next(CornerFactor factor, bool blamed)
    {
        var (safe, limit, trial) = factor;
        if (blamed)
        {
            if (trial > safe + 1e-9)
            {
                limit = Math.Min(limit, trial);
            }
            else
            {
                limit = Math.Min(limit, safe);
                safe = Math.Max(MinFactor, safe - Retreat);
            }
        }
        else
        {
            safe = Math.Max(safe, trial);
        }

        trial = double.IsPositiveInfinity(limit) ? Math.Min(MaxFactor, safe + Probe) : limit - safe > Tolerance ? (safe + limit) / 2 : safe;
        return new CornerFactor(safe, limit, trial);
    }

    public void Learn(IEnumerable<int> faultPoints)
    {
        var blamed = new bool[Map.Count];
        foreach (var point in faultPoints)
        {
            foreach (var corner in Map.Influencing(point))
            {
                blamed[corner] = true;
            }
        }

        for (var c = 0; c < factors.Length; c++)
        {
            factors[c] = Next(factors[c], blamed[c]);
        }

        Laps++;
        (TrialProfile, SafeProfile) = Build();
    }

    private (SpeedProfile Trial, SpeedProfile Safe) Build() =>
        (Profile(factor => factor.Trial), Profile(factor => factor.Safe));

    private SpeedProfile Profile(Func<CornerFactor, double> pick) =>
        new(line, spec, point => Map.CornerAt(point) is var corner and >= 0 ? pick(factors[corner]) : MaxFactor);

    private bool Fits(AutopilotModel saved) =>
        saved.TrackId == line.Track.Id
        && saved.Corners.SequenceEqual(Map.Corners)
        && saved.Factors.Count == Map.Count
        && saved.Factors.All(factor => Usable(factor.Safe) && Usable(factor.Trial) && factor.Limit >= MinFactor);

    private static bool Usable(double value) => value is >= MinFactor and <= MaxFactor;
}
