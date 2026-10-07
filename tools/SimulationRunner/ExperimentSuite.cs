using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Game.Domain;
using Game.Simulation;

namespace SimulationRunner;

internal static class ExperimentSuite
{
    public static void Run(string output, int seed, int runs, JsonSerializerOptions json)
    {
        var seeds = Enumerable.Range(seed, runs).ToArray();
        var summaries = new List<object>();
        var notes = new StringBuilder("# Dry-race strategy evidence\n\nNumerical coefficients are initial hypotheses.\n\n");
        notes.AppendLine($"Seeds: {string.Join(", ", seeds)}. Differences are alternative minus baseline.\n");
        var equal = LaboratoryFixtures.Field(false);
        var standard = new StrategyPlan([], []);
        var push = new StrategyPlan([], [], PaceInstruction.Push);
        var clock = Stopwatch.StartNew();
        var raceCount = 0;
        Compare("pace-short", new(5, new Track("Control", 100), Traffic: new(Enabled: false)), equal, standard, push);
        Compare("pace-long", new(40, new Track("Control", 100), Traffic: new(Enabled: false)), equal, standard, push);
        notes.AppendLine("Pace controls deliberately omit stops and are ineligible; they isolate the tyre trade-off and are diagnostic evidence.\n");
        var fresh = new StrategyPlan([new ScheduledDecision(4, "driver-06", new(PitCompound: TyreCompound.Hard))], []);
        Compare("fresh-tyres", new(40, new Track("Control", 100), Traffic: new(Enabled: false)), equal, standard, fresh);
        notes.AppendLine("Fresh-tyre control compares a legal stop against an ineligible no-stop control. Raw equal-distance times establish whether tyre gains repay the stop.\n");

        foreach (var mixed in new[] { false, true })
        foreach (var technical in new[] { false, true })
        {
            var track = LaboratoryFixtures.Track(technical);
            var field = LaboratoryFixtures.Field(mixed);
            var baseline = new StrategyPlan(field.Select(d => new ScheduledDecision(4, d.Id,
                new(PitCompound: TyreCompound.Hard))).ToArray(), []);
            var early = new StrategyPlan([new ScheduledDecision(3, "driver-06", new(PitCompound: TyreCompound.Hard))], []);
            var name = $"pit-window-{(mixed ? "mixed" : "equal")}-{track.Name.ToLowerInvariant()}";
            Compare(name, new(40, track, LapVariation: .2), field, baseline, early);
            var run = StrategyComparison.Run(new(40, track, LapVariation: .2), field, baseline, seed);
            ReportWriter.Race(Path.Combine(output, name), new(40, track, LapVariation: .2), field, baseline, run, json);
            raceCount++;
        }
        var phaseTimes = new List<double>();
        var benchmarkField = LaboratoryFixtures.Field(true);
        var benchmarkPlan = new StrategyPlan(benchmarkField.Select(d => new ScheduledDecision(4, d.Id,
            new(PitCompound: TyreCompound.Hard))).ToArray(), []);
        for (var index = 0; index < 10; index++)
        {
            var engine = new RaceEngine(new(40, LaboratoryFixtures.Track(true), LapVariation: .2), benchmarkField, seeds[index % seeds.Length]);
            PhaseResult result;
            do
            {
                var phaseClock = Stopwatch.StartNew();
                result = engine.ResolvePhase(benchmarkPlan);
                phaseClock.Stop();
                phaseTimes.Add(phaseClock.Elapsed.TotalMilliseconds);
            } while (!result.Finished);
            raceCount++;
        }
        var sorted = phaseTimes.Order().ToArray();
        notes.AppendLine($"Warm local simulation-only benchmark: {sorted.Length} phases across 10 mixed-field technical-track races; mean {sorted.Average():F3}ms, p95 {sorted[(int)Math.Ceiling(sorted.Length * .95) - 1]:F3}ms, maximum {sorted[^1]:F3}ms. No report I/O is included in these phase timings.\n");
        clock.Stop();
        notes.AppendLine($"Simulation plus report writing: {raceCount} races in {clock.Elapsed.TotalSeconds:F3}s. This is a local workload measurement, not a server capacity guarantee.\n");
        notes.AppendLine("## Interpretation limits\n\nThe model uses weighted whole-lap car performance and linear travel between events. It has no individual corners, sectors or location-specific passing zones. Catch events, crossings and pit exits recompute trajectories. Blue flags and battle wear are simplified. Weather, incidents, reliability and the season economy are excluded. These results establish model behaviour, not final balance or family playtest enjoyment.\n");
        notes.AppendLine("Position and elapsed-time advantages can disagree: the early-stop strategy can improve its position while taking longer itself, because changed traffic also changes opponents' times. Opponent scripts are fixed; their physical outcomes are not. Evaluate both metrics rather than calling every position gain a time gain.\n");
        notes.AppendLine("## Next modelling questions\n\nCheck whether sector-specific pace and passing zones change strategy conclusions; investigate tyre curves, passing frequency, correlated driver performance and pit-window sensitivity. Measure simulation-only latency and report memory separately before choosing server budgets. Browser playtesting should assess decision clarity and the 20–25 minute race target.\n");
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "evidence.md"), notes.ToString());
        File.WriteAllText(Path.Combine(output, "evidence-summary.json"), JsonSerializer.Serialize(new
        {
            Seeds = seeds, Experiments = summaries,
            Benchmark = new { Phases = sorted.Length, MeanMilliseconds = sorted.Average(),
                P95Milliseconds = sorted[(int)Math.Ceiling(sorted.Length * .95) - 1], MaximumMilliseconds = sorted[^1] },
            RaceCount = raceCount, TotalSecondsWithReportWriting = clock.Elapsed.TotalSeconds
        }, json));

        void Compare(string name, RaceConfiguration configuration, Driver[] drivers, StrategyPlan baseline, StrategyPlan alternative)
        {
            var report = StrategyComparison.Compare(new ComparisonRequest(configuration, drivers, "driver-06", baseline, alternative, seeds));
            ReportWriter.Comparison(Path.Combine(output, name), report, json);
            summaries.Add(new { Name = name, report.Replay, report.EligiblePositionDifference,
                report.EligibleEqualDistanceTimeDifference, report.DiagnosticPositionDifference, report.Groups });
            raceCount += runs * 2;
            notes.AppendLine($"## {name}\n\nEligible position difference: {report.EligiblePositionDifference}.\n\nEligible equal-distance time difference: {report.EligibleEqualDistanceTimeDifference}.\n\nDiagnostic position difference: {report.DiagnosticPositionDifference}.\n");
            foreach (var group in report.Groups) notes.AppendLine($"- Eligibility {group.BaselineEligible}/{group.AlternativeEligible}; laps {group.BaselineLaps}/{group.AlternativeLaps}; position {group.PositionDifference}; time {group.EqualDistanceTimeDifference}");
            if (name == "fresh-tyres")
            {
                var example = report.Pairs[0];
                var stop = example.Alternative.Result.Events.Single(e => e.DriverId == "driver-06" && e.Kind == RaceEventKind.PitEntry);
                var baselineLaps = example.Baseline.Result.Events.Where(e => e.DriverId == "driver-06" && e.Kind == RaceEventKind.LapCompleted)
                    .ToDictionary(e => e.Lap, e => e.Time);
                var payback = example.Alternative.Result.Events.FirstOrDefault(e => e.DriverId == "driver-06"
                    && e.Kind == RaceEventKind.LapCompleted && e.Lap > stop.Lap
                    && baselineLaps.TryGetValue(e.Lap, out var baselineTime) && e.Time <= baselineTime);
                notes.AppendLine($"\nSeed {example.Seed}: stop at end of lap {stop.Lap}, Pit Loss {stop.Value:F3}s; first subsequent matching lap crossing to repay the loss: {(payback is null ? "none" : payback.Lap.ToString())}. This is evidence from a paired control, not an inference from finishing position alone.");
            }
            notes.AppendLine();
            Console.WriteLine($"Completed {name}: {runs} paired seeds");
        }
    }

}
