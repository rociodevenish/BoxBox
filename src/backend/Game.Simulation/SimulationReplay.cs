using Game.Domain;
namespace Game.Simulation;
public static class SimulationReplay
{
    public static RaceEngine CreateEngine(RaceReplay replay)
    {
        if (replay.DiagnosticScenario is { } scenario)
        {
            if (replay.Opening is not null) throw new ArgumentException("Diagnostic scenarios use the explicit lab race constructor.");
            return new(replay.Configuration, replay.Drivers, replay.Seed, scenario);
        }
        if(replay.Opening is not {} opening)return new(replay.Configuration,replay.Drivers,replay.Seed);
        if(opening.Input.Seed!=replay.Seed||System.Text.Json.JsonSerializer.Serialize(opening.Input.Configuration.WithExplicitTuning())!=System.Text.Json.JsonSerializer.Serialize(replay.Configuration.WithExplicitTuning()))
            throw new ArgumentException("Opening replay must match the race configuration and seed.");
        var result=AutomaticRaceStart.Replay(opening);
        var field=AutomaticRaceStart.StartingDrivers(opening.Input);
        if(!field.SequenceEqual(replay.Drivers))throw new ArgumentException("Opening replay field does not match the race.");
        return result.Engine;
    }
    public static string Model(RaceConfiguration config) => (config.Track.Segments is null
        ? SimulationVersions.AggregateModel : SimulationVersions.SegmentedModel) + (config.Incidents is null ? "" : "+local-incidents-v1")
        + (config.Track.PitRoute is null ? "" : "+pit-route-v1")
        + (config.Weather is null ? "" : "+circuit-weather-v1")
        + (config.WetTyres is null ? "" : "+wet-tyres-v1+actual-race-wet-use-v1")
        + (config.WeatherReactions is null ? "" : "+weather-reactions-v1");
    public static string Randomness(RaceConfiguration config) => (config.Track.Segments is null
        ? SimulationVersions.AggregateRandomness : SimulationVersions.SegmentedRandomness) + (config.Incidents is null ? "" : "+incident-sha256-v1")
        + (config.Weather is null ? "" : "+weather-sha256-v1");
    public static RaceReplay CaptureDiagnostic(RaceConfiguration config, IReadOnlyList<Driver> drivers,
        StrategyPlan plan, int seed, IncidentScenario scenario) => Capture(config, drivers, plan, seed) with
        { DiagnosticScenario = new(scenario.Events.ToArray()) };
    public static void RequireVersion(RaceConfiguration config, string? model, string? randomness)
    {
        if (model is not null && model != Model(config) || randomness is not null && randomness != Randomness(config))
            throw new ArgumentException("Replay model or randomness contract is incompatible with its layout or this implementation.");
    }
    public static StrategyPlan Snapshot(StrategyPlan plan) => plan with
    { Scheduled = plan.Scheduled.ToArray(), Conditional = plan.Conditional.ToArray(), DriverPaces = plan.DriverPaces?.ToDictionary() };
    public static RaceReplay Capture(RaceConfiguration config, IReadOnlyList<Driver> drivers, StrategyPlan plan, int seed)
    {
        if (config is null || config.Track is null || drivers is null || plan is null
            || plan.Scheduled is null || plan.Conditional is null)
            throw new ArgumentException("Replay configuration, field and strategy are required.");
        return new(Model(config), Randomness(config), config.WithExplicitTuning(), drivers.ToArray(), Snapshot(plan), seed);
    }
    public static ReplayMetadata Metadata(RaceConfiguration config) => new(Model(config), Randomness(config),
    [
        new("race-form", config.RaceFormVariation, "seconds/lap full range", "uniform centred", "driver/race"),
        new("lap-variation", config.LapVariation, "seconds/lap full range", "uniform centred", "driver/lap; cached once"),
        new("passing", config.PassingVariation, "threshold full range", "uniform around 0.5; zero fixes threshold 0.5",
            config.Track.Segments is null ? "attacker/opponent/lap" : "attacker/defender/zone visit"),
        new("pit-service", config.StopVariation, "seconds full range", "uniform centred", config.Track.Segments is null ? "driver/completed stop lap" : "driver/ordinal stop"),
        new("estimate-error", config.EstimateError, "condition plus/minus", "uniform centred; result clamped", "driver/Race Phase"),
        .. IncidentChannels(config)
    ], DistanceUnit: config.Track.Segments is null ? "normalized laps; physical length unspecified" : "metres");
    private static RandomChannelMetadata[] IncidentChannels(RaceConfiguration config) => config.Incidents is not { } t ? [] :
    [
        new("local-incidents-v1/error", t.ErrorRate, "hazard/running second", "integrated exponential", "driver/ordinal; lap-entry context"),
        new("local-incidents-v1/mechanical", t.FailureRate, "hazard/running second", "integrated exponential", "driver/ordinal; lap-entry context"),
        new("local-incidents-v1/contact", t.ContactProbability, "probability/encounter", "uniform", "actual attacker/defender encounter"),
        new("local-incidents-v1/severity", .25, "spin probability/error", "uniform", "driver/error ordinal")
    ];
}
