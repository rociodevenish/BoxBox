using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using Game.Domain;
using Game.Simulation;

namespace SimulationRunner;

internal record PhaseCost(int Seed, int Phase, double Milliseconds, long AllocatedBytes);
internal record MemoryReportCost(int Seed, long RetainedHeapDeltaBytes, int FinalEvents, int RetainedPhaseSnapshots,
    long FinalHistoryJsonBytes, double ReportMilliseconds, long ReportAllocatedBytes, long ReportBytes);
internal record BenchmarkResult(string Name, string SourceRevision, int SegmentCount, int WarmupRaces,
    int[] Seeds, RaceConfiguration Configuration, Driver[] Drivers, StrategyPlan Plan,
    Distribution PhaseMilliseconds, double PhaseP95Milliseconds, Distribution PhaseAllocatedBytes,
    Distribution InitializationMilliseconds, Distribution InitializationAllocatedBytes,
    Distribution RacePhaseResolutionMilliseconds, IReadOnlyList<PhaseCost> PhaseSamples,
    IReadOnlyList<MemoryReportCost> MemoryReportSamples);

internal static class BenchmarkMeasurement
{
    public const string PreviousRevision = "d22ceedc9548d3cfce6672812a31faf0f6a1bcf6";
    public static object EnvironmentInfo() => new {
        Framework = RuntimeInformation.FrameworkDescription, OS = RuntimeInformation.OSDescription,
        Architecture = RuntimeInformation.ProcessArchitecture.ToString(), LogicalProcessors = Environment.ProcessorCount,
        ProcessorIdentifier = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"),
        ProcessorName = OperatingSystem.IsWindows() ? Microsoft.Win32.Registry.GetValue(
            @"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", null)?.ToString() : null,
        GCAvailableMemoryBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes, GCSettings.IsServerGC,
        GCMode = GCSettings.LatencyMode.ToString(), Stopwatch.Frequency,
#if DEBUG
        BuildConfiguration = "Debug"
#else
        BuildConfiguration = "Release"
#endif
    };
    public static Driver[] Field() => Enumerable.Range(1, 22).Select(i => new Driver($"driver-{i:00}", $"team-{(i + 1) / 2:00}",
        Speed: i % 2 == 1 ? 4 : 0, Cornering: i % 2 == 0 ? 4 : 0,
        Pace: i > 2 ? (i % 5 - 2) * .1 : 0, Gap: (i - 1) * .5)).ToArray();
    public static StrategyPlan Plan(Driver[] drivers) => new(drivers.Select(d => new ScheduledDecision(4, d.Id,
        new(PitCompound: TyreCompound.Hard))).ToArray(), []);

    public static BenchmarkResult Measure(string name, string revision, int segmentCount, RaceConfiguration configuration,
        Driver[] drivers, StrategyPlan plan, int[] seeds, string directory, JsonSerializerOptions json)
    {
        const int warmups = 2;
        for (var i = 0; i < warmups; i++) Warmup(configuration, drivers, plan, seeds[i]);
        var phases = new List<PhaseCost>();
        var initialMs = new List<double>(); var initialBytes = new List<double>(); var raceMs = new List<double>();
        foreach (var seed in seeds) TimedRace(configuration, drivers, plan, seed, phases, initialMs, initialBytes, raceMs);
        var memory = seeds.Take(3).Select(seed => MemoryAndReport(configuration, drivers, plan, seed,
            Path.Combine(directory, $"sample-{seed}"), json)).ToArray();
        var sorted = phases.Select(p => p.Milliseconds).Order().ToArray();
        return new(name, revision, segmentCount, warmups, seeds, configuration.WithExplicitTuning(), drivers, plan,
            Statistics(sorted), sorted[(int)Math.Ceiling(sorted.Length * .95) - 1],
            Statistics(phases.Select(p => (double)p.AllocatedBytes)), Statistics(initialMs), Statistics(initialBytes),
            Statistics(raceMs), phases, memory);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Warmup(RaceConfiguration configuration, Driver[] drivers, StrategyPlan plan, int seed)
    {
        var engine = new RaceEngine(configuration, drivers, seed);
        while (!engine.ResolvePhase(plan).Finished) { }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TimedRace(RaceConfiguration configuration, Driver[] drivers, StrategyPlan plan, int seed,
        List<PhaseCost> samples, List<double> initMs, List<double> initBytes, List<double> raceMs)
    {
        var bytes = GC.GetAllocatedBytesForCurrentThread(); var start = Stopwatch.GetTimestamp();
        var engine = new RaceEngine(configuration, drivers, seed);
        var initializationMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        var initializationBytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
        initMs.Add(initializationMs);
        initBytes.Add(initializationBytes);
        double total = 0;
        for (var phase = 0; ; phase++)
        {
            bytes = GC.GetAllocatedBytesForCurrentThread(); start = Stopwatch.GetTimestamp();
            var result = engine.ResolvePhase(plan);
            var ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            var allocated = GC.GetAllocatedBytesForCurrentThread() - bytes;
            samples.Add(new(seed, phase, ms, allocated)); total += ms;
            if (result.Finished)
            {
                if (result.LeaderLap != configuration.Laps || result.Classification.Count != drivers.Length || result.Eligibility.Values.Any(e => !e))
                    throw new InvalidOperationException("Benchmark race did not complete the legal full-field workload.");
                break;
            }
        }
        raceMs.Add(total);
    }
    private sealed record RetainedRoots(RaceEngine Engine, RaceRun Run);
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static RetainedRoots CreateRoots(RaceConfiguration configuration, Driver[] drivers, StrategyPlan plan, int seed)
    {
        var engine = new RaceEngine(configuration, drivers, seed); var phases = new List<PhaseResult>();
        PhaseResult result;
        do { result = engine.ResolvePhase(plan); phases.Add(result); } while (!result.Finished);
#if SEGMENT_MODEL
        var run = new RaceRun(seed, phases, result, SimulationReplay.Capture(configuration, drivers, plan, seed),
            SimulationReplay.Metadata(configuration), engine.GetDesignerDiagnostics());
#else
        var run = new RaceRun(seed, phases, result);
#endif
        return new(engine, run);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static MemoryReportCost MemoryAndReport(RaceConfiguration configuration, Driver[] drivers, StrategyPlan plan,
        int seed, string directory, JsonSerializerOptions json)
    {
        var before = GC.GetTotalMemory(true);
        var roots = CreateRoots(configuration, drivers, plan, seed);
        var retained = GC.GetTotalMemory(true) - before;
        // Warm serializer/writer separately. Neither full GC nor report I/O enters phase timings.
        ReportWriter.Race(Path.Combine(directory, "warmup"), configuration, drivers, plan, roots.Run, json);
        var bytes = GC.GetAllocatedBytesForCurrentThread(); var start = Stopwatch.GetTimestamp();
        ReportWriter.Race(Path.Combine(directory, "measured"), configuration, drivers, plan, roots.Run, json);
        var ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        var allocated = GC.GetAllocatedBytesForCurrentThread() - bytes;
        var reportBytes = Directory.EnumerateFiles(Path.Combine(directory, "measured")).Sum(p => new FileInfo(p).Length);
        var historyBytes = JsonSerializer.SerializeToUtf8Bytes(roots.Run.Result.Events, json).LongLength;
        GC.KeepAlive(roots);
        return new(seed, retained, roots.Run.Result.Events.Count, roots.Run.Phases.Count, historyBytes, ms, allocated, reportBytes);
    }
    private static Distribution Statistics(IEnumerable<double> source)
    {
        var values = source.ToArray(); var mean = values.Average();
        return new(values.Length, mean, values.Length == 1 ? 0 : Math.Sqrt(values.Sum(v => (v - mean) * (v - mean)) / (values.Length - 1)), values.Min(), values.Max());
    }
}
