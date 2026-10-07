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
    var track = options.Technical ? new Track("Technical", 95, .2, PassingChance: .08)
        : new Track("Fast", 85, .8, PassingChance: .8);
    var configuration = options.Configuration is null
        ? new RaceConfiguration(options.Laps, track, LapVariation: .2, Tyres: new(), Traffic: new())
        : Read<RaceConfiguration>(options.Configuration);
    var drivers = Enumerable.Range(1, 22).Select(i => new Driver($"driver-{i:00}", $"team-{(i + 1) / 2:00}",
        Speed: options.Mixed ? (22 - i) * .1 : 0, Cornering: options.Mixed ? (i % 5) * .1 : 0,
        Gap: (i - 1) * .5)).ToArray();
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

    T Read<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path), json)
        ?? throw new ArgumentException($"{path} contains a null input.");
}
catch (Exception error) when (error is ArgumentException or IOException or JsonException or FormatException or OverflowException)
{
    Console.Error.WriteLine($"Error: {error.Message}");
    return 1;
}
return 0;
