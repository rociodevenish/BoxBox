using Game.Domain;
namespace Game.Simulation;
public static class SimulationReplay
{
    public static string Model(RaceConfiguration config) => config.Track.Segments is null
        ? SimulationVersions.AggregateModel : SimulationVersions.SegmentedModel;
    public static string Randomness(RaceConfiguration config) => config.Track.Segments is null
        ? SimulationVersions.AggregateRandomness : SimulationVersions.SegmentedRandomness;
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
        new("estimate-error", config.EstimateError, "condition plus/minus", "uniform centred; result clamped", "driver/Race Phase")
    ], DistanceUnit: config.Track.Segments is null ? "normalized laps; physical length unspecified" : "metres");
}
