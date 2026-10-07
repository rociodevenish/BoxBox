using Game.Domain;

namespace Game.Simulation;

public static class StrategyComparison
{
    public static RaceRun Run(RaceConfiguration configuration, IReadOnlyList<Driver> drivers, StrategyPlan plan, int seed)
    {
        var replay = SimulationReplay.Capture(configuration, drivers, plan, seed);
        configuration = replay.Configuration;
        drivers = replay.Drivers;
        plan = replay.Plan;
        var engine = new RaceEngine(configuration, drivers, seed);
        var phases = new List<PhaseResult>();
        PhaseResult result;
        do { result = engine.ResolvePhase(plan); phases.Add(result); } while (!result.Finished);
        return new RaceRun(seed, phases, result, replay, SimulationReplay.Metadata(configuration), engine.GetDesignerDiagnostics());
    }

    public static RaceRun Replay(RaceReplay replay)
    {
        if (replay is null || replay.Configuration is null || replay.Configuration.Track is null
            || string.IsNullOrWhiteSpace(replay.ModelVersion) || string.IsNullOrWhiteSpace(replay.RandomnessVersion))
            throw new ArgumentException("Replay requires explicit contract versions.");
        SimulationReplay.RequireVersion(replay.Configuration, replay.ModelVersion, replay.RandomnessVersion);
        return Run(replay.Configuration, replay.Drivers, replay.Plan, replay.Seed);
    }

    public static ComparisonReport Compare(ComparisonRequest request)
    {
        if (request is null || request.Configuration is null || request.Configuration.Track is null
            || request.Drivers is null || request.Drivers.Any(d => d is null)
            || request.Baseline is null || request.Alternative is null)
            throw new ArgumentException("Comparison configuration, field and strategies are required.");
        if (request.Seeds is null || request.Seeds.Length == 0 || request.Seeds.Distinct().Count() != request.Seeds.Length
            || request.Drivers.All(d => d.Id != request.TargetDriver))
            throw new ArgumentException("Comparison needs a known target and nonempty unique seeds.");
        SimulationReplay.RequireVersion(request.Configuration, request.ModelVersion, request.RandomnessVersion);
        var ids = request.Drivers.Select(d => d.Id).ToArray();
        StrategyEvaluator.Validate(request.Baseline, ids, request.Configuration.Laps);
        StrategyEvaluator.Validate(request.Alternative, ids, request.Configuration.Laps);
        request = request with { Configuration = request.Configuration.WithExplicitTuning(),
            Drivers = request.Drivers.ToArray(), Seeds = request.Seeds.ToArray(),
            Baseline = SimulationReplay.Snapshot(request.Baseline), Alternative = SimulationReplay.Snapshot(request.Alternative),
            ModelVersion = SimulationReplay.Model(request.Configuration), RandomnessVersion = SimulationReplay.Randomness(request.Configuration) };
        if (request.Alternative.Scheduled.Any(s => s.DriverId != request.TargetDriver)
            || request.Alternative.Conditional.Any(c => c.DriverId != request.TargetDriver)
            || request.Alternative.DriverPaces?.Any(d => d.Key != request.TargetDriver) == true)
            throw new ArgumentException("Alternative may change only the target driver.");
        var paces = request.Baseline.DriverPaces?.ToDictionary() ?? [];
        paces[request.TargetDriver] = request.Alternative.DriverPaces?.GetValueOrDefault(request.TargetDriver, request.Alternative.DefaultPace)
            ?? request.Alternative.DefaultPace;
        var alternate = request.Baseline with
        {
            Scheduled = request.Baseline.Scheduled.Where(s => s.DriverId != request.TargetDriver)
                .Concat(request.Alternative.Scheduled).ToArray(),
            Conditional = request.Baseline.Conditional.Where(s => s.DriverId != request.TargetDriver)
                .Concat(request.Alternative.Conditional).ToArray(),
            DriverPaces = paces
        };
        var pairs = new List<ComparisonPair>();
        foreach (var seed in request.Seeds)
        {
            var baseline = Run(request.Configuration, request.Drivers, request.Baseline, seed);
            var alternative = Run(request.Configuration, request.Drivers, alternate, seed);
            var a = baseline.Result.Classification.Single(c => c.DriverId == request.TargetDriver);
            var b = alternative.Result.Classification.Single(c => c.DriverId == request.TargetDriver);
            pairs.Add(new ComparisonPair(seed, baseline, alternative, b.Position - a.Position, a.Laps, b.Laps,
                baseline.Result.Eligibility[request.TargetDriver], alternative.Result.Eligibility[request.TargetDriver],
                a.Laps == b.Laps ? b.FinishTime!.Value - a.FinishTime!.Value : null));
        }
        var eligible = pairs.Where(p => p.BaselineEligible && p.AlternativeEligible).ToArray();
        var groups = pairs.GroupBy(p => (p.BaselineEligible, p.AlternativeEligible, p.BaselineLaps, p.AlternativeLaps))
            .Select(g => new ComparisonGroup(g.Key.BaselineEligible, g.Key.AlternativeEligible, g.Key.BaselineLaps,
                g.Key.AlternativeLaps, Summarize(g.Select(p => (double)p.PositionDifference)),
                Summarize(g.Where(p => p.ComparableTimeDifference.HasValue).Select(p => p.ComparableTimeDifference!.Value))))
            .ToArray();
        return new ComparisonReport(request, pairs, Summarize(eligible.Select(p => (double)p.PositionDifference)),
            Summarize(eligible.Where(p => p.ComparableTimeDifference.HasValue).Select(p => p.ComparableTimeDifference!.Value)),
            Summarize(pairs.Select(p => (double)p.PositionDifference)), groups, SimulationReplay.Metadata(request.Configuration));
    }

    private static Distribution Summarize(IEnumerable<double> source)
    {
        var values = source.ToArray();
        if (values.Length == 0) return new Distribution(0, null, null, null, null);
        var mean = values.Average();
        var deviation = values.Length == 1 ? 0 : Math.Sqrt(values.Sum(v => Math.Pow(v - mean, 2)) / (values.Length - 1));
        return new Distribution(values.Length, mean, deviation, values.Min(), values.Max());
    }
}
