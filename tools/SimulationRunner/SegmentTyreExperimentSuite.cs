using System.Text;
using System.Text.Json;
using Game.Domain;
using Game.Simulation;

namespace SimulationRunner;

internal record TyreCase(string Id, string Layout, string Field, string Purpose, string Setting,
    RaceConfiguration Configuration, Driver[] Drivers, StrategyPlan Baseline, StrategyPlan Alternative);
internal record TyreEvidence(string Id, string Layout, string Field, string Purpose, string Setting,
    RaceConfiguration Configuration, ReplayMetadata Metadata, StrategyPlan BaselinePlan, StrategyPlan AlternativePlan,
    Distribution RawEqualDistanceTimeDifference, Distribution EligibleEqualDistanceTimeDifference,
    Distribution EligiblePositionDifference, IReadOnlyList<ComparisonGroup> Groups,
    RaceCounters Baseline, RaceCounters Alternative, Distribution TargetTrafficLossBaseline,
    Distribution TargetTrafficLossAlternative, Distribution AlternativeOutLapSeconds,
    int RepaidCount, IReadOnlyList<int> UnrepaidSeeds, Distribution FirstRepaidLap,
    double? MeanIntervalLow, double? MeanIntervalHigh, string Verdict, string ReplayFile, string SampleHistoryFile);

internal static partial class SegmentExperimentSuite
{
    public static void RunTyrePit(string output, int seed, int runs, JsonSerializerOptions json)
    {
        if (runs < 20) throw new ArgumentException("Principal segment evidence requires at least 20 paired seeds.");
        var seeds = Enumerable.Range(seed, runs).ToArray();
        var defaults = new TyreTuning();
        // One factor at a time; bounds selected before any outcomes are examined.
        var settings = new (string Name, TyreTuning Tyres)[] {
            ("default", defaults), ("degradation-low", defaults with { DegradationPerLap = .0125 }),
            ("degradation-high", defaults with { DegradationPerLap = .05 }),
            ("exhaustion-off", defaults with { ExhaustionPenalty = 0 }),
            ("exhaustion-high", defaults with { ExhaustionPenalty = 30 }),
            ("warmup-off", defaults with { WarmupPenalty = 0 }),
            ("warmup-high", defaults with { WarmupPenalty = 2 }) };
        var fields = new Dictionary<string, Driver[]> { ["equal-all"] = LaboratoryFixtures.SegmentedField(false),
            ["mixed-car-fit"] = LaboratoryFixtures.SegmentedField(true) };
        var cases = new List<TyreCase>();
        foreach (var restricted in new[] { false, true })
        foreach (var (fieldName, drivers) in fields)
        {
            var layout = restricted ? "restricted" : "open";
            var track = LaboratoryFixtures.SegmentedTrack(restricted);
            foreach (var (name, tyres) in settings)
                cases.Add(new($"{layout}-{fieldName}-fresh-{name}", layout, fieldName, "fresh-payoff", name,
                    Configuration(tyres), drivers, new([], []), TargetStop(4)));
            var legal = new StrategyPlan(drivers.Select(d => new ScheduledDecision(4, d.Id,
                new(PitCompound: TyreCompound.Hard))).ToArray(), []);
            foreach (var phase in new[] { 3, 5 })
                cases.Add(new($"{layout}-{fieldName}-window-{phase}", layout, fieldName, "pit-window", $"phase-{phase}",
                    Configuration(defaults), drivers, legal, TargetStop(phase)));
            RaceConfiguration Configuration(TyreTuning tyres) => new(40, track, LapVariation: .2,
                Tyres: tyres, StopVariation: .6, EstimateError: .04);
        }
        var completed = new TyreEvidence[cases.Count];
        Parallel.For(0, cases.Count, new ParallelOptions { MaxDegreeOfParallelism = 2 }, index =>
        {
            var c = cases[index];
            var report = StrategyComparison.Compare(new(c.Configuration, c.Drivers, "driver-06", c.Baseline, c.Alternative, seeds));
            var directory = Path.Combine(output, c.Id);
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "comparison-replay.json"), JsonSerializer.Serialize(report.Replay, json));
            var sample = report.Pairs[0];
            File.WriteAllText(Path.Combine(directory, "sample-history.json"), JsonSerializer.Serialize(new {
                sample.Seed, Replay = report.Replay with { Seeds = [sample.Seed] },
                Baseline = new { sample.Baseline.Result.Events, sample.Baseline.Diagnostics },
                Alternative = new { sample.Alternative.Result.Events, sample.Alternative.Diagnostics } }, json));
            var rawTime = Stats(report.Pairs.Where(p => p.ComparableTimeDifference.HasValue).Select(p => p.ComparableTimeDifference!.Value));
            var low = rawTime.Mean is null ? null : rawTime.Mean - 1.96 * rawTime.StandardDeviation / Math.Sqrt(rawTime.Count);
            var high = rawTime.Mean is null ? null : rawTime.Mean + 1.96 * rawTime.StandardDeviation / Math.Sqrt(rawTime.Count);
            var repaid = new List<double>();
            var unrepaid = new List<int>();
            var outLaps = new List<double>();
            foreach (var pair in report.Pairs)
            {
                var events = pair.Alternative.Result.Events;
                var entry = events.Single(e => e.DriverId == "driver-06" && e.Kind == RaceEventKind.PitEntry);
                var exit = events.Single(e => e.DriverId == "driver-06" && e.Kind == RaceEventKind.PitExit);
                var outLap = events.FirstOrDefault(e => e.DriverId == "driver-06" && e.Kind == RaceEventKind.LapCompleted && e.Lap == entry.Lap + 1);
                if (outLap is not null) outLaps.Add(outLap.Time - exit.Time);
                if (c.Purpose != "fresh-payoff") continue;
                var baselineCrossings = pair.Baseline.Result.Events.Where(e => e.DriverId == "driver-06" && e.Kind == RaceEventKind.LapCompleted)
                    .ToDictionary(e => e.Lap, e => e.Time);
                var paid = events.FirstOrDefault(e => e.DriverId == "driver-06" && e.Kind == RaceEventKind.LapCompleted
                    && e.Lap > entry.Lap && baselineCrossings.TryGetValue(e.Lap, out var baselineTime) && e.Time <= baselineTime);
                if (paid is null) unrepaid.Add(pair.Seed); else repaid.Add(paid.Lap);
            }
            completed[index] = new(c.Id, c.Layout, c.Field, c.Purpose, c.Setting, report.Replay.Configuration, report.Metadata!,
                report.Replay.Baseline, report.Replay.Alternative, rawTime, report.EligibleEqualDistanceTimeDifference,
                report.EligiblePositionDifference, report.Groups, Counters(report.Pairs.Select(p => p.Baseline).ToArray(), true),
                Counters(report.Pairs.Select(p => p.Alternative).ToArray(), true),
                Stats(report.Pairs.Select(p => TargetLoss(p.Baseline))), Stats(report.Pairs.Select(p => TargetLoss(p.Alternative))),
                Stats(outLaps), repaid.Count, unrepaid, Stats(repaid), low, high,
                high < 0 ? "Alternative faster in this sample" : low > 0 ? "Alternative slower in this sample" : "Inconclusive",
                $"{c.Id}/comparison-replay.json", $"{c.Id}/sample-history.json");
            Console.WriteLine($"Tyre evidence completed: {c.Id}; raw equal-distance delta {Format(rawTime)}s; {completed[index].Verdict}");
        });
        var notes = new StringBuilder("# Segment tyre and pit-window sensitivity\n\n");
        notes.AppendLine($"40-lap, 22-car dry races, paired seeds {string.Join(", ", seeds)}. 36 preselected cases; no coefficients selected from favourable outcomes. Two 90-second/5000-metre layouts and the unchanged equal-all/mixed-car-fit fixtures from ticket 07. Mixed-car-fit includes both alternate car strengths and up to ±0.2 base driver Pace; other driver attributes are unchanged.\n");
        notes.AppendLine("Existing tyre curve only: default degradation per equivalent lap 0.025, exhaustion penalty 15 seconds/lap, warm-up magnitude 0.8 seconds. One-factor bounds: degradation 0.0125/0.05, exhaustion 0/30, warm-up 0/2. All other settings stay default and are explicit in machine output. Lap variation 0.2s, stop variation 0.6s, estimate error 0.04, form zero, passing variation one. Passing chance/difficulty stay 0.4/0.4.\n");
        notes.AppendLine("Fresh-payoff controls compare all-Medium/no-stop baseline against driver-06's phase-4 Hard stop; baseline is ineligible and every opponent remains on its fixed no-stop script. These are diagnostic raw equal-distance comparisons, not eligible strategy wins. Pit-window cases use legal all-driver phase-4 Hard stops, moving only driver-06 to phase 3 or 5. The instruction takes effect at the next lap-end after the phase boundary; actual entry laps and spatial rejoin encounters appear in histories.\n");
        notes.AppendLine("| Layout/field | Purpose/setting | Raw equal-distance delta mean (SD; range), seconds | Approx. 95% mean interval | Verdict | Target traffic loss mean B/A, seconds | Alternative out-lap mean, seconds | Repaid seeds / first repaid lap mean | Eligible time samples |\n| --- | --- | --- | --- | --- | --- | --- | --- | ---: |");
        foreach (var e in completed)
            notes.AppendLine($"| {e.Layout}/{e.Field} | {e.Purpose}/{e.Setting} | {Format(e.RawEqualDistanceTimeDifference)} | [{e.MeanIntervalLow:F2}, {e.MeanIntervalHigh:F2}] | {e.Verdict} | {Mean(e.TargetTrafficLossBaseline)}/{Mean(e.TargetTrafficLossAlternative)} | {Mean(e.AlternativeOutLapSeconds)} | {(e.Purpose == "fresh-payoff" ? $"{e.RepaidCount}/{runs}; {Mean(e.FirstRepaidLap)}" : "n/a")} | {e.EligibleEqualDistanceTimeDifference.Count} |");
        notes.AppendLine("\nAlternative minus baseline: negative time is faster. Intervals are descriptive paired normal approximations, not final balance estimates. Every eligibility/distance group and both eligible and raw distributions are published, including empty eligible controls. Repaid means the first post-stop crossing with alternative time no later than the baseline at the same completed lap; unrepaid seeds are explicit. Out-lap excludes pit time and includes spatial traffic/warm-up. Mean repay lap includes repaid seeds only. It does not imply eventual finishing superiority.\n");
        notes.AppendLine("## Spatial interpretation and limits\n\nBoth layouts have distance-weighted TyreDemand 1.0, with different local demands. The restricted layout has fewer passing visits; fresh-tyre gains can be trapped in queues after the start/finish rejoin. Target traffic loss, whole-field attempts/overtakes/blue flags and segment-labelled sample histories separate that mechanism from stationary Pit Loss. Warm-up is distributed once over the out-lap; its penalty can change encounter timing and opponents' outcomes. Identical stop scripts preserve scoped service draws across tuning cases. Changed events need not produce monotonic position results. The evidence measures these existing controls without adding curve families or final tuning.\n");
        notes.AppendLine("Exhaustion-off/high at default degradation has no useful exhausted running before the 40-lap finish: condition reaches zero at the final crossing. The neutral result does not establish exhaustion strength; that sensitivity is inconclusive in this workload. High degradation does exercise earlier exhaustion, but this bounded suite does not cross it with every exhaustion setting. Warm-up responses can be non-monotonic because both initial tyres and rejoin encounters change; compare paired time variability and spatial histories rather than infer a universal payoff curve.\n");
        notes.AppendLine($"Reproduce: `dotnet run -c Release --project tools/SimulationRunner -- --suite tyre-pit --runs 20 --output reports/segment-tyre-pit`. Each case exports replay inputs and seed {seeds[0]}'s final segment/encounter history. Regenerate other histories with --replay. Two cases execute concurrently; published case and seed order remains deterministic.\n");
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "tyre-pit-evidence.md"), notes.ToString());
        File.WriteAllText(Path.Combine(output, "tyre-pit-summary.json"), JsonSerializer.Serialize(new {
            Laps = 40, Seeds = seeds, Fields = fields, Settings = settings.Select(s => new { s.Name, s.Tyres }),
            PitWindowPhases = new[] { 3, 4, 5 }, TargetDriver = "driver-06", Scenarios = completed }, json));
    }

    private static StrategyPlan TargetStop(int phase) => new([new(phase, "driver-06", new(PitCompound: TyreCompound.Hard))], []);
    private static double TargetLoss(RaceRun run) => run.Result.Events.Where(e => e.DriverId == "driver-06" && e.Kind == RaceEventKind.TrafficLoss).Sum(e => e.Value);
}
