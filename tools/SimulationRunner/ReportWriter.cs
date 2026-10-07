using System.Text;
using System.Text.Json;
using Game.Domain;

namespace SimulationRunner;

internal static class ReportWriter
{
    public static void Race(string directory, RaceConfiguration configuration, Driver[] drivers,
        StrategyPlan plan, RaceRun run, JsonSerializerOptions json)
    {
        configuration = configuration.WithExplicitTuning();
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "race.json"), JsonSerializer.Serialize(new { configuration, drivers, plan, run }, json));
        var text = new StringBuilder($"Initial numerical hypotheses. Seed {run.Seed}\n");
        text.AppendLine("Configuration: " + JsonSerializer.Serialize(configuration, json));
        text.AppendLine("Starting field: " + JsonSerializer.Serialize(drivers, json));
        text.AppendLine("Strategy: " + JsonSerializer.Serialize(plan, json));
        foreach (var phase in run.Phases)
        {
            text.AppendLine($"Leader lap {phase.LeaderLap}; clock {phase.Time:F3}s");
            foreach (var car in phase.Classification)
                text.AppendLine($"P{car.Position} {car.DriverId}: {car.Laps}+{car.Progress:F4} laps; gap {car.GapSeconds:F3}s / {car.GapLaps} laps");
            foreach (var tyre in phase.Tyres) text.AppendLine($"{tyre.DriverId}: {tyre.Compound}, age {tyre.Age}, actual condition {tyre.Condition:F4}, pace {tyre.Pace}");
        }
        foreach (var decision in run.Result.Decisions)
            text.AppendLine($"Phase {decision.Phase} {decision.DriverId}: estimate {decision.EstimatedCondition:F4}, {decision.Source}, {decision.Decision}");
        foreach (var e in run.Result.Events.Where(e => e.Kind != RaceEventKind.LapCompleted && e.Kind != RaceEventKind.TyreCondition))
            text.AppendLine($"{e.Time:F3}s {e.DriverId} lap {e.Lap}: {e.Kind}, value {e.Value:F4}, other {e.OtherDriverId}, compound {e.Compound}");
        foreach (var car in run.Result.Classification)
        {
            var traffic = run.Result.Events.Where(e => e.DriverId == car.DriverId && e.Kind == RaceEventKind.TrafficLoss).Sum(e => e.Value);
            var pits = run.Result.Events.Where(e => e.DriverId == car.DriverId && e.Kind == RaceEventKind.PitEntry).Sum(e => e.Value);
            text.AppendLine($"Final P{car.Position} {car.DriverId}: {car.Laps} laps, {car.FinishTime:F3}s; eligible={run.Result.Eligibility[car.DriverId]}; traffic loss={traffic:F3}s; pit loss={pits:F3}s");
        }
        File.WriteAllText(Path.Combine(directory, "race.txt"), text.ToString());
    }

    public static void Comparison(string directory, ComparisonReport report, JsonSerializerOptions json)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "comparison.json"), JsonSerializer.Serialize(report, json));
        var text = new StringBuilder("Paired comparison: alternative minus baseline; negative differences favour the alternative.\n");
        text.AppendLine("Replay inputs: " + JsonSerializer.Serialize(report.Replay, json));
        text.AppendLine("Opponent scripts are fixed; conditional actions may respond to changed conditions.");
        text.AppendLine($"Eligible position difference: {report.EligiblePositionDifference}");
        text.AppendLine($"Eligible equal-distance time difference: {report.EligibleEqualDistanceTimeDifference}");
        text.AppendLine($"Raw diagnostic position difference (includes ineligible runs): {report.DiagnosticPositionDifference}");
        foreach (var group in report.Groups) text.AppendLine($"Eligibility/distance group: {group}");
        foreach (var pair in report.Pairs)
            text.AppendLine($"Seed {pair.Seed}: position delta {pair.PositionDifference}, laps {pair.BaselineLaps}/{pair.AlternativeLaps}, eligible {pair.BaselineEligible}/{pair.AlternativeEligible}, equal-distance time delta {pair.ComparableTimeDifference}");
        File.WriteAllText(Path.Combine(directory, "comparison.txt"), text.ToString());
    }
}
