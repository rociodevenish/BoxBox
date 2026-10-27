using System.Collections.Immutable;
using System.Text.Json;
using Game.Domain;

namespace Game.Simulation;

/// <summary>Versioned supplied content, packaged with this model rather than read from mutable runtime files.</summary>
public static class WeekendPresetCatalog
{
    // Explicit lab opt-in; ordinary prepared weekends retain the historical incident-free contract.
    public static (RaceConfiguration Configuration, Driver[] Drivers) IncidentInputs(EffectiveWeekend weekend, IncidentTuning tuning)
    {
        using var tracks = Read("weekend.tracks.json");
        var track = Find(tracks.RootElement.GetProperty("tracks"), weekend.Source.Circuit.Id);
        var ordinal = track.GetProperty("raceCharacteristics").GetProperty("mechanicalStress").GetDouble();
        if (ordinal is < 1 or > 5) throw new ArgumentException("Mechanical Stress must be an authored 1–5 ordinal.");
        // Authored hypothesis: ordinal endpoints map to 0.5–1.5 operating stress, not headline stars.
        var configured = tuning with { MechanicalStress = .5 + (ordinal - 1) / 4 };
        return (weekend.Configuration with { Incidents = configured }, weekend.Cars.Select(car => car.RaceDriver with {
            IncidentProfile = new(car.Source.Driver.Ratings["errorControl"], car.Source.Car.Ratings["reliability"],
                SourceVersion: $"local-profile-v1/{car.Source.Driver.Version}/{car.Source.Car.Version}/{Version(tracks)}")
        }).ToArray());
    }
    public static WeatherTuning WeatherInputs(EffectiveWeekend weekend, WeatherTuning tuning)
    {
        using var tracks = Read("weekend.tracks.json");
        var track = Find(tracks.RootElement.GetProperty("tracks"), weekend.Source.Circuit.Id);
        var ordinal = track.GetProperty("raceCharacteristics").GetProperty("weatherVolatility").GetInt32();
        // Explicit authored map for the supplied race characteristic, never headline stars.
        var volatility = ordinal switch { 1 => .08, 2 => .16, 3 => .24, 4 => .32, 5 => .40,
            _ => throw new ArgumentException("Weather Volatility must be an authored 1–5 ordinal.") };
        var result = tuning with { Volatility = volatility, SourceVersion = $"weather-track-v1/{Version(tracks)}/{weekend.Source.Circuit.Id}" };
        result.Validate();
        return result;
    }
    public static WeekendPreset Open(string circuitId, string firstDriverId = "driver-max-verstappen",
        string secondDriverId = "driver-george-russell")
    {
        using var drivers = Read("weekend.drivers.json");
        using var tracks = Read("weekend.tracks.json");
        var track = Find(tracks.RootElement.GetProperty("tracks"), circuitId);
        var demands = Ratings(track.GetProperty("detailedDemands"));
        var speed = (demands["topSpeed"] + demands["accelerationDeployment"]) / 2;
        var corner = new[] { "highSpeedCornering", "lowSpeedCornering", "mechanicalGrip", "braking", "traction" }.Average(k => demands[k]);
        var share = speed / (speed + corner);
        var length = track.GetProperty("trackLengthKm").GetDouble() * 1000;
        // Authored representative geometry, not imagery-derived circuit reconstruction.
        var layout = Array.AsReadOnly(new TrackSegment[] {
            new("representative-straight", length * share, 90 * share, 1, 0),
            new("representative-corners", length * (1 - share), 90 * (1 - share), 0, 1, PassingAllowed: false) });
        var circuit = new CircuitPreset(circuitId, track.GetProperty("circuitName").GetString()!,
            track.GetProperty("slug").GetString()!, Version(tracks), demands,
            new Track(track.GetProperty("circuitName").GetString()!, 90, share, Segments: layout));
        var car = new CarPreset("neutral-car-v1", WeekendPreparation.CarVersion,
            WeekendPreparation.CarKeys.ToImmutableDictionary(k => k, _ => 82d));
        DriverPreset Driver(string id)
        {
            var row = Find(drivers.RootElement.GetProperty("drivers"), id);
            return new(id, row.GetProperty("identity").GetProperty("displayName").GetString()!,
                row.GetProperty("slug").GetString()!, Version(drivers), Ratings(row.GetProperty("ratings").GetProperty("detailed")),
                row.GetProperty("preferences").EnumerateArray().Select(p => new DriverPreference(
                    p.GetProperty("key").GetString()!, p.GetProperty("label").GetString()!)).ToImmutableArray());
        }
        return new(WeekendPreparation.ModelVersion, circuit,
            [new("car-1", "team-1", car, Driver(firstDriverId)), new("car-2", "team-1", car, Driver(secondDriverId))],
            new("engineer-neutral-v1", "Preset Race Engineer", "authored-engineer-v1", 82));
    }

    private static JsonDocument Read(string name) => JsonDocument.Parse(
        typeof(WeekendPresetCatalog).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException("Missing packaged weekend content."));
    private static string Version(JsonDocument document) => document.RootElement.GetProperty("schemaVersion").GetString()
        + "/" + document.RootElement.GetProperty("dataVersion").GetString();
    private static JsonElement Find(JsonElement rows, string id) => rows.EnumerateArray().FirstOrDefault(row => row.GetProperty("id").GetString() == id)
        is var row && row.ValueKind != JsonValueKind.Undefined ? row : throw new ArgumentException($"Unknown supplied identity: {id}");
    private static ImmutableDictionary<string, double> Ratings(JsonElement values) => values.EnumerateObject()
        .ToImmutableDictionary(p => p.Name, p => p.Value.GetDouble());
}
