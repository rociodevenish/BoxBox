using System.Text;
using System.Text.Json;
using System.Globalization;
using Game.Domain;
using Game.Simulation;
namespace SimulationRunner;

internal record RaceCounters(Distribution Overtakes, Distribution? PassingAttempts,
    Distribution TrafficLossSeconds, Distribution BlueFlags);
internal record ScenarioEvidence(string Id, string Layout, string Field, string Mode, double PassingDifficulty,
    RaceConfiguration Configuration, ReplayMetadata Metadata, RaceCounters Baseline, RaceCounters Alternative,
    Distribution EligiblePositionDifference, Distribution EligibleEqualDistanceTimeDifference,
    IReadOnlyList<ComparisonGroup> Groups, string ReplayFile, string SampleHistoryFile);
internal record PairedClaim(string Claim, Distribution PairedDifference, double? MeanIntervalLow,
    double? MeanIntervalHigh, string Verdict);

internal static partial class SegmentExperimentSuite
{
    public static void RunTrackPassing(string output, int seed, int runs, JsonSerializerOptions json)
    {
        if (runs < 20) throw new ArgumentException("Principal segment evidence requires at least 20 paired seeds.");
        var seeds = Enumerable.Range(seed, runs).ToArray();
        var difficulties = new[] { 0d, .4, .8 };
        var fields = new Dictionary<string, Driver[]> { ["equal-all"] = LaboratoryFixtures.SegmentedField(false),
            ["mixed-car-fit"] = LaboratoryFixtures.SegmentedField(true) };
        var baseline = new StrategyPlan(fields["equal-all"].Select(d => new ScheduledDecision(4, d.Id,
            new(PitCompound: TyreCompound.Hard))).ToArray(), []);
        var alternative = new StrategyPlan([new(3, "driver-06", new(PitCompound: TyreCompound.Hard))], []);
        var scenarios = new List<ScenarioEvidence>();
        var countsByCase = new Dictionary<string, int[]>();
        var selected = new List<(bool Restricted, string Field, Driver[] Drivers, string Mode, double Difficulty)>();
        foreach (var restricted in new[] { false, true })
        foreach (var (fieldName, drivers) in fields)
        {
            foreach (var difficulty in difficulties) selected.Add((restricted, fieldName, drivers, "segmented", difficulty));
            selected.Add((restricted, fieldName, drivers, "zero-noise", .4));
            selected.Add((restricted, fieldName, drivers, "aggregate", .4));
        }
        // Runs share immutable inputs only. Bound memory to two comparisons and publish in selection order.
        var completed = new ScenarioEvidence[selected.Count];
        Parallel.For(0, selected.Count, new ParallelOptions { MaxDegreeOfParallelism = 2 }, index =>
        {
            var s = selected[index];
            completed[index] = Compare(s.Restricted, s.Field, s.Drivers, s.Mode, s.Difficulty);
        });
        scenarios.AddRange(completed);
        var claims = new List<PairedClaim>();
        foreach (var field in fields.Keys)
        {
            var open = countsByCase[$"open-{field}-segmented-0.4"];
            var restricted = countsByCase[$"restricted-{field}-segmented-0.4"];
            var differences = Stats(open.Zip(restricted, (a, b) => (double)a - b));
            var radius = 1.96 * differences.StandardDeviation!.Value / Math.Sqrt(differences.Count);
            var low = differences.Mean!.Value - radius;
            var high = differences.Mean.Value + radius;
            claims.Add(new($"Open minus restricted baseline on-track passes, {field}", differences, low, high,
                low > 0 ? "Supported in this sample" : high < 0 ? "Hypothesis failed in this sample" : "Inconclusive"));
        }
        var notes = new StringBuilder("# Segment track fit and passing evidence\n\n");
        notes.AppendLine($"40-lap, 22-car dry races; paired seeds {string.Join(", ", seeds)}. All selected difficulty values: 0, 0.4, 0.8. No tuning was selected after inspecting outcomes.\n");
        notes.AppendLine("Equal-all means identical drivers AND cars; mixed-car-fit alternates straight/corner car strengths and varies base driver Pace by up to 0.2 seconds/lap. Other driver attributes are unchanged. The first strength pair has equal overall clear-air pace. Both layouts have 90-second baseline laps, 5000 metres and integrated Speed/Cornering demand 0.5/0.5; open has two eligible visits and restricted one. PassingChance stays 0.4. Coefficients are hypotheses, not real-world calibration.\n");
        notes.AppendLine("## How many actual on-track overtakes with equal cars?\n");
        foreach (var scenario in scenarios.Where(s => s.Field == "equal-all" && s.Mode == "segmented" && s.PassingDifficulty == .4))
            notes.AppendLine($"- {scenario.Layout}: baseline {Format(scenario.Baseline.Overtakes)} overtakes/race; target early-stop alternative {Format(scenario.Alternative.Overtakes)}. These count Overtake events only; pit-cycle rank changes and blue flags are excluded.");
        notes.AppendLine("\nEach distribution shows mean, sample SD and range across all published paired seeds. Attempts and queue loss are separate outcomes; an attempt is not an overtake. Aggregate attempt counts are unavailable, rather than zero.\n");
        notes.AppendLine("| Layout | Field | Model/control | Difficulty | Baseline passes | Alternative passes | Attempts mean B/A | Queue loss seconds mean B/A | Blue flags mean B/A |");
        notes.AppendLine("| --- | --- | --- | ---: | --- | --- | --- | --- | --- |");
        foreach (var s in scenarios)
            notes.AppendLine($"| {s.Layout} | {s.Field} | {s.Mode} | {s.PassingDifficulty:F1} | {Format(s.Baseline.Overtakes)} | {Format(s.Alternative.Overtakes)} | {Mean(s.Baseline.PassingAttempts)}/{Mean(s.Alternative.PassingAttempts)} | {Mean(s.Baseline.TrafficLossSeconds)}/{Mean(s.Alternative.TrafficLossSeconds)} | {Mean(s.Baseline.BlueFlags)}/{Mean(s.Alternative.BlueFlags)} |");
        notes.AppendLine("\n## Hypotheses and uncertainty\n");
        foreach (var claim in claims)
            notes.AppendLine($"- {claim.Claim}: paired mean {claim.PairedDifference.Mean:F3}; sample SD {claim.PairedDifference.StandardDeviation:F3}; approximate 95% mean interval [{claim.MeanIntervalLow:F3}, {claim.MeanIntervalHigh:F3}]. {claim.Verdict}.");
        foreach (var layout in new[] { "open", "restricted" })
        foreach (var field in fields.Keys)
        {
            var sweep = scenarios.Where(s => s.Layout == layout && s.Field == field && s.Mode == "segmented").OrderBy(s => s.PassingDifficulty).ToArray();
            var ordered = sweep.Zip(sweep.Skip(1)).All(p => p.First.Baseline.Overtakes.Mean >= p.Second.Baseline.Overtakes.Mean
                && p.First.Alternative.Overtakes.Mean >= p.Second.Alternative.Overtakes.Mean);
            var spread = sweep.Select(s => s.Baseline.Overtakes.Mean).Distinct().Count() > 1;
            notes.AppendLine($"- Increasing difficulty reduces mean passes, {layout}/{field}: {(ordered && spread ? "supported descriptively" : spread ? "hypothesis failed or non-monotonic" : "inconclusive; no measurable spread")}. Dynamic encounter sets mean individual races need not be monotonic.");
        }
        notes.AppendLine("\nIntervals are descriptive normal approximations for paired count means, not final balance estimates. Twenty seeds can leave small effects inconclusive. Zero-noise also sets PassingVariation to zero: its fixed 0.5 threshold makes the current <=0.4 passing chances fail, while still exposing failed opportunities/queues. Blue flags remain explicit. Aggregate controls retain the same attributes, 90-second lap and 0.4 track chance but have no spatial passing zones; historical Fast/Technical tuning is a different workload.\n");
        notes.AppendLine("## Strategy outcomes and comparability\n");
        foreach (var s in scenarios)
            notes.AppendLine($"- {s.Id}: eligible position delta {s.EligiblePositionDifference.Mean:F3} (n={s.EligiblePositionDifference.Count}, SD={s.EligiblePositionDifference.StandardDeviation:F3}); eligible equal-distance time delta {s.EligibleEqualDistanceTimeDifference.Mean:F3}s (n={s.EligibleEqualDistanceTimeDifference.Count}, SD={s.EligibleEqualDistanceTimeDifference.StandardDeviation:F3}); {s.Groups.Count} eligibility/distance groups. Alternative minus baseline; machine summary publishes every group, including incompatible/ineligible groups.");
        notes.AppendLine("\nMoving the target stop from the next crossing after phase 4 to phase 3 preserves corresponding driver/lap draws and ordinal stop draws. It can change encounter identities and opponents' physical outcomes. Position changes are separate from on-track passes and equal-distance time gains.\n");
        notes.AppendLine("Reproduce: `dotnet run --project tools/SimulationRunner -- --suite track-passing --runs 20 --output reports/segment-track-passing`. Each case exports full comparison replay inputs and one seed's final spatial/encounter history; full histories can be regenerated with --replay. Summary includes explicit per-case tuning, shared fields/plans/seeds and all outcome groups.\n");
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "track-passing-evidence.md"), notes.ToString());
        File.WriteAllText(Path.Combine(output, "track-passing-summary.json"), JsonSerializer.Serialize(new
        {
            Laps = 40, Seeds = seeds, DifficultyValues = difficulties, Fields = fields, Baseline = baseline,
            Alternative = alternative, TargetDriver = "driver-06", Scenarios = scenarios, Claims = claims
        }, json));

        ScenarioEvidence Compare(bool restricted, string fieldName, Driver[] drivers, string mode, double difficulty)
        {
            var layout = restricted ? "restricted" : "open";
            var id = $"{layout}-{fieldName}-{mode}-{difficulty.ToString("0.0", CultureInfo.InvariantCulture)}";
            var track = LaboratoryFixtures.SegmentedTrack(restricted, difficulty);
            if (mode == "aggregate") track = track with { Segments = null };
            var zero = mode == "zero-noise";
            var config = new RaceConfiguration(40, track, LapVariation: zero ? 0 : .2,
                StopVariation: zero ? 0 : .6, EstimateError: zero ? 0 : .04, PassingVariation: zero ? 0 : 1);
            var report = StrategyComparison.Compare(new(config, drivers, "driver-06", baseline, alternative, seeds));
            var directory = Path.Combine(output, id);
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "comparison-replay.json"), JsonSerializer.Serialize(report.Replay, json));
            var sample = report.Pairs[0];
            File.WriteAllText(Path.Combine(directory, "sample-history.json"), JsonSerializer.Serialize(new
            {
                sample.Seed, Replay = report.Replay with { Seeds = [sample.Seed] },
                Baseline = new { sample.Baseline.Result.Events, sample.Baseline.Diagnostics },
                Alternative = new { sample.Alternative.Result.Events, sample.Alternative.Diagnostics }
            }, json));
            var baselineRuns = report.Pairs.Select(p => p.Baseline).ToArray();
            var alternativeRuns = report.Pairs.Select(p => p.Alternative).ToArray();
            lock (countsByCase) countsByCase[id] = baselineRuns.Select(r => r.Result.Events.Count(e => e.Kind == RaceEventKind.Overtake)).ToArray();
            var evidence = new ScenarioEvidence(id, layout, fieldName, mode, difficulty, report.Replay.Configuration, report.Metadata!,
                Counters(baselineRuns, mode != "aggregate"), Counters(alternativeRuns, mode != "aggregate"),
                report.EligiblePositionDifference, report.EligibleEqualDistanceTimeDifference, report.Groups,
                $"{id}/comparison-replay.json", $"{id}/sample-history.json");
            Console.WriteLine($"Evidence completed: {id}, {runs} paired seeds; baseline passes {Format(evidence.Baseline.Overtakes)}; alternative {Format(evidence.Alternative.Overtakes)}");
            return evidence;
        }
    }

    internal static RaceCounters Counters(IReadOnlyList<RaceRun> runs, bool segmented) => new(
        Stats(runs.Select(r => (double)r.Result.Events.Count(e => e.Kind == RaceEventKind.Overtake))),
        segmented ? Stats(runs.Select(r => (double)r.Result.Events.Count(e => e.Kind == RaceEventKind.PassingAttempt))) : null,
        Stats(runs.Select(r => r.Result.Events.Where(e => e.Kind == RaceEventKind.TrafficLoss).Sum(e => e.Value))),
        Stats(runs.Select(r => (double)r.Result.Events.Count(e => e.Kind == RaceEventKind.BlueFlag))));
    internal static Distribution Stats(IEnumerable<double> source)
    {
        var values = source.ToArray();
        if (values.Length == 0) return new(0, null, null, null, null);
        var mean = values.Average();
        return new(values.Length, mean, values.Length == 1 ? 0 : Math.Sqrt(values.Sum(v => Math.Pow(v - mean, 2)) / (values.Length - 1)), values.Min(), values.Max());
    }
    private static string Format(Distribution d) => $"{d.Mean:F2} (SD {d.StandardDeviation:F2}; {d.Minimum:F0}-{d.Maximum:F0})";
    private static string Mean(Distribution? d) => d is null ? "n/a" : $"{d.Mean:F2}";
}
