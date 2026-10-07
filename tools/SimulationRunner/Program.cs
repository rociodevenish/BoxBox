using System.Text.Json;
using System.Text.Json.Serialization;
using Game.Domain;
using Game.Simulation;
using SimulationRunner;

try
{
    var options = RunnerOptions.Parse(args);
    var json = new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    json.Converters.Add(new JsonStringEnumConverter());
    if (options.Replay is not null)
    {
        using var replayDocument = JsonDocument.Parse(File.ReadAllText(options.Replay));
        if (replayDocument.RootElement.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Replay must be an object.");
        if (replayDocument.RootElement.EnumerateObject().Any(p => p.Name.Equals("seeds", StringComparison.OrdinalIgnoreCase)))
        {
            var request = JsonInput.Read<ComparisonRequest>(options.Replay, json);
            var report = StrategyComparison.Compare(request);
            ReportWriter.Comparison(options.Output, report, json);
        }
        else
        {
            var replay = JsonInput.Read<RaceReplay>(options.Replay, json);
            var run = StrategyComparison.Replay(replay);
            ReportWriter.Race(options.Output, replay.Configuration, replay.Drivers, replay.Plan, run, json);
        }
        Console.WriteLine($"Replay reports: {Path.GetFullPath(options.Output)}");
        return 0;
    }
    if (options.Experiments)
    {
        ExperimentSuite.Run(options.Output, options.Seed, options.Runs, json);
        Console.WriteLine($"Experiment evidence: {Path.GetFullPath(options.Output)}");
        return 0;
    }
    var track = LaboratoryFixtures.Track(options.Technical);
    var configuration = options.Configuration is null
        ? new RaceConfiguration(options.Laps, track, LapVariation: .2, Tyres: new(), Traffic: new())
        : Read<RaceConfiguration>(options.Configuration);
    var drivers = LaboratoryFixtures.Field(options.Mixed);
    var plan = options.Strategy is not null ? Read<StrategyPlan>(options.Strategy)
        : new StrategyPlan(options.Script is null ? [] : Read<ScheduledDecision[]>(options.Script), [],
            options.Pace);
    if (options.Alternative is not null)
    {
        var comparison = StrategyComparison.Compare(new ComparisonRequest(configuration, drivers,
            options.Target, plan, Read<StrategyPlan>(options.Alternative), Enumerable.Range(options.Seed, options.Runs).ToArray()));
        ReportWriter.Comparison(options.Output, comparison, json);
        Console.WriteLine($"Paired seeds: {comparison.Pairs.Count}; eligible time delta: {comparison.EligibleEqualDistanceTimeDifference}");
    }
    else
    {
        var run = StrategyComparison.Run(configuration, drivers, plan, options.Seed);
        ReportWriter.Race(options.Output, configuration, drivers, plan, run, json);
        foreach (var car in run.Result.Classification)
            Console.WriteLine($"P{car.Position,2} {car.DriverId}: {car.Laps} laps, {car.FinishTime:F3}s, eligible={run.Result.Eligibility[car.DriverId]}");
    }
    Console.WriteLine($"Reports: {Path.GetFullPath(options.Output)}");

    T Read<T>(string path) => JsonInput.Read<T>(path, json);
}
catch (Exception error) when (error is ArgumentException or IOException or JsonException or FormatException or OverflowException)
{
    Console.Error.WriteLine($"Error: {error.Message}");
    return 1;
}
return 0;
