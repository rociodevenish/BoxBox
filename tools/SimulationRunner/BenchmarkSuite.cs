using System.Text;
using System.Text.Json;
using Game.Domain;
using Game.Simulation;

namespace SimulationRunner;

internal record PreviousBenchmark(JsonElement Environment, BenchmarkResult Result);
internal static class BenchmarkSuite
{
    public static void Run(string output, int seed, int runs, string? previousFile, JsonSerializerOptions json)
    {
        if (runs < 20) throw new ArgumentException("Principal segment evidence requires at least 20 paired seeds.");
#if DEBUG
        throw new ArgumentException("Benchmark requires a Release build.");
#else
        if (previousFile is null) throw new ArgumentException("Benchmark requires --baseline-benchmark from the pinned previous executable harness.");
        var previous = JsonSerializer.Deserialize<PreviousBenchmark>(File.ReadAllText(previousFile), json)
            ?? throw new ArgumentException("Previous executable benchmark is required.");
        var seeds = Enumerable.Range(seed, runs).ToArray();
        var drivers = BenchmarkMeasurement.Field(); var plan = BenchmarkMeasurement.Plan(drivers);
        var aggregate = new RaceConfiguration(40, new("OpenSegment", 90, .5, PassingChance: .4), LapVariation: .2);
        if (previous.Result.SourceRevision != BenchmarkMeasurement.PreviousRevision || previous.Result.SegmentCount != 1
            || !previous.Result.Seeds.SequenceEqual(seeds) || !previous.Result.Drivers.SequenceEqual(drivers)
            || previous.Result.Configuration != aggregate.WithExplicitTuning()
            || !previous.Result.Plan.Scheduled.SequenceEqual(plan.Scheduled)
            || previous.Environment.GetProperty("BuildConfiguration").GetString() != "Release")
            throw new ArgumentException("Previous benchmark must use the pinned revision and matching Release workload/seeds.");
        var results = new List<BenchmarkResult> { previous.Result };
        var source = typeof(RaceEngine).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .Cast<System.Reflection.AssemblyInformationalVersionAttribute>().Single().InformationalVersion;
        results.Add(BenchmarkMeasurement.Measure("current-aggregate", source, 1, aggregate, drivers, plan, seeds,
            Path.Combine(output, "current-aggregate"), json));
        foreach (var factor in new[] { 1, 2, 4 })
        {
            var track = LaboratoryFixtures.SegmentedTrack(false);
            track = track with { Segments = track.Segments!.SelectMany(s => Enumerable.Range(1, factor)
                .Select(part => s with { Id = $"{s.Id}-{part}", Length = s.Length / factor,
                    BaselineTime = s.BaselineTime / factor, PassingZoneId = s.PassingZoneId ?? s.Id })).ToArray() };
            var name = $"current-segments-{track.Segments.Count}";
            results.Add(BenchmarkMeasurement.Measure(name, source, track.Segments.Count, aggregate with { Track = track }, drivers, plan,
                seeds, Path.Combine(output, name), json));
            Console.WriteLine($"Benchmark completed: {name}, {runs} timed full-distance races");
        }
        var notes = new StringBuilder("# Segment laboratory benchmark\n\n");
        notes.AppendLine($"Serial Release workload: 22 mixed-car-fit cars, 40 laps, paired seeds {string.Join(", ", seeds)}; two complete warm-up races, then {runs} timed races per model. Eight five-leader-lap phases per race. Warm-up excluded. Each model also has three separately sampled memory/report runs using the first three seeds. No benchmark overlaps an experiment sweep.\n");
        notes.AppendLine("Actual previous executable is compiled against pinned commit `d22ceedc9548d3cfce6672812a31faf0f6a1bcf6`, using its RaceEngine and ReportWriter; current aggregate is a separate control. Current workloads use 4/8/16 equivalent subdivisions of the same four-zone layout. Shared PassingZoneId preserves one opportunity per pair/eligible visit; subdivision does not add passing zones. These models have different semantics/seed contracts, so equal workload is not a claim of identical historical outcomes.\n");
        notes.AppendLine("| Model | Segments | Phase mean/p95/max ms | Phase allocated bytes mean/max | Mean race phase-resolution ms | Engine + run + phase snapshots retained heap delta mean bytes | Final history JSON mean bytes | Warm report mean ms / allocated bytes / output bytes |\n| --- | ---: | --- | --- | --- | --- | --- | --- |");
        foreach (var r in results)
            notes.AppendLine($"| {r.Name} | {r.SegmentCount} | {r.PhaseMilliseconds.Mean:F3}/{r.PhaseP95Milliseconds:F3}/{r.PhaseMilliseconds.Maximum:F3} | {r.PhaseAllocatedBytes.Mean:F0}/{r.PhaseAllocatedBytes.Maximum:F0} | {r.RacePhaseResolutionMilliseconds.Mean:F3} | {r.MemoryReportSamples.Average(s => s.RetainedHeapDeltaBytes):F0} | {r.MemoryReportSamples.Average(s => s.FinalHistoryJsonBytes):F0} | {r.MemoryReportSamples.Average(s => s.ReportMilliseconds):F3}/{r.MemoryReportSamples.Average(s => s.ReportAllocatedBytes):F0}/{r.MemoryReportSamples.Average(s => s.ReportBytes):F0} |");
        notes.AppendLine("\n## Measurement boundary\n\nPhase timings surround only ResolvePhase(plan), including its state/history snapshots; initialization is separately sampled. Phase allocation uses current-thread allocated bytes. Scalar sample recording and all serialization/file I/O lie outside that interval. Calls execute serially. Results include per-phase observations, mean/sample SD/p95/max and initialization costs.\n");
        notes.AppendLine("Retained heap is the incremental managed live-heap estimate after full GC for isolated roots holding the engine, RaceRun, all eight cumulative phase snapshots and designer diagnostics where supported. NoInlining boundaries and GC.KeepAlive prevent a previous sample's locals or premature collection from defining that lifetime. It remains a process-level GC estimate, not exact graph-size accounting. Final history JSON bytes and on-disk report bytes are separate sizes. Warm report timing/allocation uses each revision's actual ReportWriter after one untimed writer warm-up per sample, includes JSON/text/replay where supported, and includes file I/O. Reports contain cumulative phase histories, so output cost grows with richer contracts and retained events. No forced GC or report work enters phase timings.\n");
        notes.AppendLine("Runtime, CPU identifier/name, architecture, logical processors, GC configuration and available-memory limit are published for both executables. Host scheduling, JIT and GC affect tails; three report samples do not establish stable disk-performance percentiles. These local measurements do not establish multiplayer capacity or tight wall-clock test thresholds.\n");
        notes.AppendLine("Reproduce with `tools/verify-benchmark.ps1` (Release, pinned baseline checkout and external harness), then `--suite benchmark --runs 20 --baseline-benchmark <previous-benchmark.json> --output <directory>`. Generated full reports stay ignored; this summary and the machine data are concise published artifacts.\n");
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "benchmark-evidence.md"), notes.ToString());
        File.WriteAllText(Path.Combine(output, "benchmark-summary.json"), JsonSerializer.Serialize(new {
            CurrentEnvironment = BenchmarkMeasurement.EnvironmentInfo(), PreviousEnvironment = previous.Environment,
            TimingOrder = results.Select(r => r.Name), Results = results }, json));
#endif
    }
}
