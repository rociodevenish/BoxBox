using System.Text;
using System.Text.Json;
using Game.Domain;
using Game.Simulation;

namespace SimulationRunner;

internal record FormProfile(string Name, double FormSecondsFullRange, double LapSecondsFullRange);
internal record SeedPersistence(int Seed, int Drivers, int CompletedLapOffsets, double? PooledAdjacentOffsetCorrelation,
    double? DriverCenteredAdjacentCorrelation, double MeanDriverTemporalSDSeconds,
    double BetweenDriverMeanSDSeconds, double MeanDriverOffsetSeconds, double TargetElapsedLapSDSeconds);
internal record CorrespondingScopes(string Channel, int Matched, int BaselineOnly, int AlternativeOnly, int Mismatched);
internal record FormEvidence(string Id, string Layout, string Field, FormProfile Profile,
    RaceConfiguration Configuration, ReplayMetadata Metadata, StrategyPlan BaselinePlan, StrategyPlan AlternativePlan,
    Distribution PooledAdjacentOffsetCorrelation, Distribution DriverCenteredAdjacentCorrelation,
    Distribution MeanDriverTemporalSDSeconds, Distribution BetweenDriverMeanSDSeconds,
    Distribution TargetElapsedLapSDSeconds, IReadOnlyList<SeedPersistence> PersistenceBySeed,
    IReadOnlyList<CorrespondingScopes> Correspondence, Distribution EligiblePositionDifference,
    Distribution EligibleEqualDistanceTimeDifference, IReadOnlyList<ComparisonGroup> Groups,
    double? TimeMeanIntervalLow, double? TimeMeanIntervalHigh, string TimeVerdict,
    RaceCounters Baseline, RaceCounters Alternative, string ReplayFile, string SampleHistoryFile);

internal static partial class SegmentExperimentSuite
{
    public static void RunFormNoise(string output, int seed, int runs, JsonSerializerOptions json)
    {
        if (runs < 20) throw new ArgumentException("Principal segment evidence requires at least 20 paired seeds.");
        var seeds = Enumerable.Range(seed, runs).ToArray();
        var profiles = new FormProfile[] { new("zero", 0, 0), new("form-only", .4, 0),
            new("lap-only", 0, .2), new("combined", .4, .2), new("combined-high", .8, .4) };
        var fields = new Dictionary<string, Driver[]> { ["equal-all"] = LaboratoryFixtures.SegmentedField(false),
            ["mixed-car-fit"] = LaboratoryFixtures.SegmentedField(true) };
        var cases = (from restricted in new[] { false, true }
            from field in fields from profile in profiles
            select (Restricted: restricted, Field: field.Key, Drivers: field.Value, Profile: profile)).ToArray();
        var completed = new FormEvidence[cases.Length];
        Parallel.For(0, cases.Length, new ParallelOptions { MaxDegreeOfParallelism = 2 }, index =>
        {
            var c = cases[index];
            var layout = c.Restricted ? "restricted" : "open";
            var id = $"{layout}-{c.Field}-{c.Profile.Name}";
            var configuration = new RaceConfiguration(40, LaboratoryFixtures.SegmentedTrack(c.Restricted),
                LapVariation: c.Profile.LapSecondsFullRange, RaceFormVariation: c.Profile.FormSecondsFullRange,
                StopVariation: 0, EstimateError: 0, PassingVariation: 0);
            var baseline = new StrategyPlan(c.Drivers.Select(d => new ScheduledDecision(4, d.Id,
                new(PitCompound: TyreCompound.Hard))).ToArray(), []);
            var alternative = TargetStop(3);
            var report = StrategyComparison.Compare(new(configuration, c.Drivers, "driver-06", baseline, alternative, seeds));
            var persistence = report.Pairs.Select(p => Persistence(p.Baseline)).ToArray();
            var correspondence = new Dictionary<string, CorrespondingScopes>();
            foreach (var pair in report.Pairs)
            {
                var a = pair.Baseline.Diagnostics!.Where(d => d.Channel != "passing")
                    .ToDictionary(d => (d.Channel, d.DriverId, d.Scope));
                var b = pair.Alternative.Diagnostics!.Where(d => d.Channel != "passing")
                    .ToDictionary(d => (d.Channel, d.DriverId, d.Scope));
                foreach (var channel in a.Keys.Concat(b.Keys).Select(k => k.Channel).Distinct())
                {
                    var keys = a.Keys.Concat(b.Keys).Where(k => k.Channel == channel).Distinct().ToArray();
                    var matched = keys.Where(k => a.ContainsKey(k) && b.ContainsKey(k)).ToArray();
                    var mismatched = matched.Count(k => a[k].Value != b[k].Value);
                    if (mismatched != 0) throw new InvalidOperationException("Paired exogenous diagnostic draws changed.");
                    var old = correspondence.GetValueOrDefault(channel, new(channel, 0, 0, 0, 0));
                    correspondence[channel] = old with { Matched = old.Matched + matched.Length,
                        BaselineOnly = old.BaselineOnly + keys.Count(k => !b.ContainsKey(k)),
                        AlternativeOnly = old.AlternativeOnly + keys.Count(k => !a.ContainsKey(k)),
                        Mismatched = old.Mismatched + mismatched };
                }
            }
            var time = report.EligibleEqualDistanceTimeDifference;
            var radius = time.Mean is null ? null : 1.96 * time.StandardDeviation / Math.Sqrt(time.Count);
            var low = time.Mean - radius;
            var high = time.Mean + radius;
            var directory = Path.Combine(output, id);
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "comparison-replay.json"), JsonSerializer.Serialize(report.Replay, json));
            var sample = report.Pairs[0];
            File.WriteAllText(Path.Combine(directory, "sample-history.json"), JsonSerializer.Serialize(new {
                sample.Seed, Replay = report.Replay with { Seeds = [sample.Seed] },
                Baseline = new { sample.Baseline.Result.Events, sample.Baseline.Diagnostics },
                Alternative = new { sample.Alternative.Result.Events, sample.Alternative.Diagnostics } }, json));
            completed[index] = new(id, layout, c.Field, c.Profile, report.Replay.Configuration, report.Metadata!,
                report.Replay.Baseline, report.Replay.Alternative,
                Stats(persistence.Where(p => p.PooledAdjacentOffsetCorrelation.HasValue).Select(p => p.PooledAdjacentOffsetCorrelation!.Value)),
                Stats(persistence.Where(p => p.DriverCenteredAdjacentCorrelation.HasValue).Select(p => p.DriverCenteredAdjacentCorrelation!.Value)),
                Stats(persistence.Select(p => p.MeanDriverTemporalSDSeconds)), Stats(persistence.Select(p => p.BetweenDriverMeanSDSeconds)),
                Stats(persistence.Select(p => p.TargetElapsedLapSDSeconds)), persistence, correspondence.Values.OrderBy(v => v.Channel).ToArray(),
                report.EligiblePositionDifference, time, report.Groups, low, high,
                high < 0 ? "Earlier stop faster in this sample" : low > 0 ? "Earlier stop slower in this sample" : "Inconclusive",
                Counters(report.Pairs.Select(p => p.Baseline).ToArray(), true), Counters(report.Pairs.Select(p => p.Alternative).ToArray(), true),
                $"{id}/comparison-replay.json", $"{id}/sample-history.json");
            Console.WriteLine($"Form evidence completed: {id}; eligible time delta {Format(time)}s; pooled modifier correlation {Mean(completed[index].PooledAdjacentOffsetCorrelation)}");
        });
        var notes = new StringBuilder("# Persistent form and independent lap-noise evidence\n\n");
        notes.AppendLine($"40-lap, 22-car races, paired seeds {string.Join(", ", seeds)}; 20 preselected cases. Both 90-second layouts and unchanged equal-all/mixed-car-fit fields. Mixed-car-fit varies car strengths AND base driver Pace by up to ±0.2; other driver attributes are unchanged.\n");
        notes.AppendLine("Full ranges in seconds/lap: zero (0/0), form-only (0.4/0), lap-only (0/0.2), combined (0.4/0.2), combined-high (0.8/0.4), written form/lap. Form is one bounded zero-centred draw per driver/race; lap noise one independent scoped draw per driver/lap. Stop variation and estimate error are zero; passing variation zero uses the fixed 0.5 threshold, so current 0.4 chance/0.4 difficulty produces no ordinary passes. Tyres and spatial traffic remain active and fixed. No mutable driver attributes or shared team/field factor is introduced.\n");
        notes.AppendLine("Baseline legal stops: every driver to Hard after phase 4. Alternative moves driver-06 to phase 3. All corresponding exogenous diagnostic channel/driver/scope keys are compared exactly within each paired seed; mismatches fail the suite. Missing scopes caused by different distances/events are reported, not treated as mismatches. Passing scopes depend on actual encounters and are separate.\n");
        notes.AppendLine("## Sustained effective performance\n\nThe diagnostic modifier is resolved form + lap offset, before tyres, pace and spatial constraints. Each seed's correlation pools adjacent completed-lap modifier pairs from the same driver/race; it is not the correlation of an individual constant series. Driver-centering removes each driver's race mean first. Constant data has undefined correlation, shown as n/a (never fabricated zero). Means and sample SD below summarize 20 independent seeded races, rather than treating every lap pair as an independent trial.\n");
        notes.AppendLine("| Layout/field | Profile | Pooled adjacent correlation mean (SD) | Driver-centered correlation mean (SD) | Mean within-driver temporal SD seconds | Between-driver race-mean SD seconds | Target elapsed-lap SD seconds |\n| --- | --- | --- | --- | --- | --- | --- |");
        foreach (var e in completed)
            notes.AppendLine($"| {e.Layout}/{e.Field} | {e.Profile.Name} | {Moment(e.PooledAdjacentOffsetCorrelation)} | {Moment(e.DriverCenteredAdjacentCorrelation)} | {Moment(e.MeanDriverTemporalSDSeconds)} | {Moment(e.BetweenDriverMeanSDSeconds)} | {Moment(e.TargetElapsedLapSDSeconds)} |");
        notes.AppendLine("\nElapsed-lap duration includes tyre changes, traffic and Pit Loss. It is a confounded outcome, not an estimate of the injected random channels. Form-only produces sustained driver offsets with zero within-driver diagnostic variation; lap-only changes by lap; combined offsets retain a persistent component even after independent lap variation is added. Driver-centered results help distinguish that persistent component from independent temporal variation. The summary publishes every seed and sample count, including undefined correlations.\n");
        notes.AppendLine("## Strategy distributions and uncertainty\n\nAlternative minus baseline; negative time/position delta is favourable. Intervals are descriptive paired normal approximations for the mean, not balance or enjoyment claims.\n");
        notes.AppendLine("| Layout/field | Profile | Eligible time delta mean (SD; range), seconds | Approx. 95% mean interval | Verdict | Eligible position delta mean (SD) | Time samples / groups |\n| --- | --- | --- | --- | --- | --- | --- |");
        foreach (var e in completed)
            notes.AppendLine($"| {e.Layout}/{e.Field} | {e.Profile.Name} | {Format(e.EligibleEqualDistanceTimeDifference)} | [{e.TimeMeanIntervalLow:F3}, {e.TimeMeanIntervalHigh:F3}] | {e.TimeVerdict} | {Moment(e.EligiblePositionDifference)} | {e.EligibleEqualDistanceTimeDifference.Count}/{e.Groups.Count} |");
        notes.AppendLine("\nThe machine summary includes all eligibility/distance groups, actual on-track passes, attempts, traffic losses and blue flags, every magnitude/configuration and matched/unmatched draw counts. Increasing variability can change queues and encounters; strategy outcomes need not vary monotonically. A nonzero point estimate whose interval crosses zero is inconclusive. Corresponding draws stay stable even when strategy changes the event set.\n");
        notes.AppendLine("## Interpretation boundary\n\nPersistence exists within one driver's race only. The next seeded race samples another scope; this model supplies no latent factor shared by teammates/field and no form persistence across weekends. Finite sample correlations between different drivers do not establish a shared factor. Form diagnostics are designer replay data and do not enter team observations or player event flows. Coefficients remain bounded modelling hypotheses.\n");
        notes.AppendLine($"Reproduce: `dotnet run -c Release --project tools/SimulationRunner -- --suite form-noise --runs 20 --output reports/segment-form-noise`. Each case exports replay inputs and seed {seeds[0]}'s complete final spatial/diagnostic history. Regenerate other histories with --replay. Two cases execute concurrently; published case and seed order is deterministic.\n");
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "form-noise-evidence.md"), notes.ToString());
        File.WriteAllText(Path.Combine(output, "form-noise-summary.json"), JsonSerializer.Serialize(new {
            Laps = 40, Seeds = seeds, Fields = fields, Profiles = profiles, TargetDriver = "driver-06", Scenarios = completed }, json));
    }

    private static SeedPersistence Persistence(RaceRun run)
    {
        var x = new List<double>(); var y = new List<double>();
        var centeredX = new List<double>(); var centeredY = new List<double>();
        var temporalSD = new List<double>(); var driverMeans = new List<double>();
        var lapCount = 0;
        foreach (var driver in run.Replay!.Drivers)
        {
            var form = run.Diagnostics!.Single(d => d.DriverId == driver.Id && d.Channel == "race-form").Value;
            var noise = run.Diagnostics!.Where(d => d.DriverId == driver.Id && d.Channel == "lap-variation")
                .ToDictionary(d => int.Parse(d.Scope.AsSpan(4)), d => d.Value);
            var offsets = run.Result.Events.Where(e => e.DriverId == driver.Id && e.Kind == RaceEventKind.LapCompleted)
                .Select(e => form + noise[e.Lap]).ToArray();
            lapCount += offsets.Length;
            var distribution = Stats(offsets);
            temporalSD.Add(distribution.StandardDeviation!.Value);
            var mean = distribution.Mean!.Value;
            driverMeans.Add(mean);
            for (var lap = 1; lap < offsets.Length; lap++)
            {
                x.Add(offsets[lap - 1]); y.Add(offsets[lap]);
                centeredX.Add(offsets[lap - 1] - mean); centeredY.Add(offsets[lap] - mean);
            }
        }
        var target = run.Replay.Drivers.Single(d => d.Id == "driver-06");
        var crossings = run.Result.Events.Where(e => e.DriverId == target.Id && e.Kind == RaceEventKind.LapCompleted).Select(e => e.Time).ToArray();
        var durations = crossings.Zip(new[] { target.Gap }.Concat(crossings), (end, start) => end - start);
        return new(run.Seed, driverMeans.Count, lapCount, Correlation(x, y), Correlation(centeredX, centeredY),
            temporalSD.Average(), Stats(driverMeans).StandardDeviation!.Value, driverMeans.Average(), Stats(durations).StandardDeviation!.Value);
    }
    private static double? Correlation(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        var mx = x.Average(); var my = y.Average();
        var xx = x.Sum(v => (v - mx) * (v - mx)); var yy = y.Sum(v => (v - my) * (v - my));
        if (xx <= 1e-20 || yy <= 1e-20) return null;
        return Math.Clamp(x.Zip(y, (a, b) => (a - mx) * (b - my)).Sum() / Math.Sqrt(xx * yy), -1, 1);
    }
    private static string Moment(Distribution d) => d.Count == 0 ? "n/a" : $"{d.Mean:F3} (SD {d.StandardDeviation:F3})";
}
