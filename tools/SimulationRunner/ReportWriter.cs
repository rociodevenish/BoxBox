using System.Text;
using System.Text.Json;
using Game.Domain;
using Game.Simulation;

namespace SimulationRunner;

internal static class ReportWriter
{
    public static void Race(string directory, RaceConfiguration configuration, Driver[] drivers,
        StrategyPlan plan, RaceRun run, JsonSerializerOptions json)
    {
        var replay = run.Replay ?? SimulationReplay.Capture(configuration, drivers, plan, run.Seed);
        configuration = replay.Configuration;
        drivers = replay.Drivers;
        plan = replay.Plan;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "race.json"), JsonSerializer.Serialize(new { configuration, drivers, plan, replay, metadata = run.Metadata, run }, json));
        File.WriteAllText(Path.Combine(directory, "race-replay.json"), JsonSerializer.Serialize(replay, json));
        var text = new StringBuilder($"Initial numerical hypotheses. Seed {run.Seed}\n");
        AppendMetadata(text, run.Metadata ?? SimulationReplay.Metadata(configuration));
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
            text.AppendLine(EventText(e));
        AppendDiagnostics(text, run);
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
        File.WriteAllText(Path.Combine(directory, "comparison-replay.json"), JsonSerializer.Serialize(report.Replay, json));
        var text = new StringBuilder("Paired comparison: alternative minus baseline; negative differences favour the alternative.\n");
        AppendMetadata(text, report.Metadata ?? SimulationReplay.Metadata(report.Replay.Configuration));
        text.AppendLine("Replay inputs: " + JsonSerializer.Serialize(report.Replay, json));
        text.AppendLine("Opponent scripts are fixed; conditional actions may respond to changed conditions.");
        text.AppendLine($"Eligible position difference: {report.EligiblePositionDifference}");
        text.AppendLine($"Eligible equal-distance time difference: {report.EligibleEqualDistanceTimeDifference}");
        text.AppendLine($"Raw diagnostic position difference (includes ineligible runs): {report.DiagnosticPositionDifference}");
        foreach (var group in report.Groups) text.AppendLine($"Eligibility/distance group: {group}");
        foreach (var pair in report.Pairs)
            text.AppendLine($"Seed {pair.Seed}: position delta {pair.PositionDifference}, laps {pair.BaselineLaps}/{pair.AlternativeLaps}, eligible {pair.BaselineEligible}/{pair.AlternativeEligible}, equal-distance time delta {pair.ComparableTimeDifference}");
        foreach (var pair in report.Pairs)
        {
            foreach (var (name, run) in new[] { ("baseline", pair.Baseline), ("alternative", pair.Alternative) })
            {
                text.AppendLine($"Seed {pair.Seed} {name} spatial/encounter history:");
                foreach (var e in run.Result.Events.Where(e => e.SegmentId is not null || e.EncounterId is not null))
                    text.AppendLine(EventText(e));
                AppendDiagnostics(text, run);
            }
        }
        File.WriteAllText(Path.Combine(directory, "comparison.txt"), text.ToString());
    }
    private static string EventText(RaceEvent e) => $"{e.Time:F6}s {e.DriverId} lap {e.Lap}: {e.Kind}, value {e.Value:F4}, other {e.OtherDriverId}, compound {e.Compound}, segment {e.SegmentId}, distance {e.DistanceMetres:F6}m, encounter {e.EncounterId}";
    private static void AppendMetadata(StringBuilder text, ReplayMetadata metadata)
    {
        text.AppendLine($"Model {metadata.ModelVersion}; randomness {metadata.RandomnessVersion}; units {metadata.TimeUnit}, {metadata.DistanceUnit}.");
        foreach (var channel in metadata.Channels)
            text.AppendLine($"Channel {channel.Name}: magnitude {channel.Magnitude}, {channel.Units}, {channel.Distribution}, scope {channel.Scope}.");
    }
    private static void AppendDiagnostics(StringBuilder text, RaceRun run)
    {
        text.AppendLine("Designer random diagnostics (excluded from team observations and player events):");
        foreach (var draw in run.Diagnostics ?? [])
            text.AppendLine($"{draw.Channel} {draw.DriverId} {draw.Scope}: {draw.Value:R}");
    }
}
