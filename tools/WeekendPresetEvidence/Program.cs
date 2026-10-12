using System.Collections.Immutable;
using System.Text.Json;
using Game.Domain;
using Game.Simulation;

if (args.Length != 1) throw new ArgumentException("Pass an evidence output directory.");
Directory.CreateDirectory(args[0]);
var reports = new List<object>();
foreach (var circuit in new[] { "track-monza", "track-monaco" })
foreach (var mixed in new[] { false, true })
foreach (var noise in new[] { false, true })
{
    var preset = WeekendPresetCatalog.Open(circuit);
    if (!mixed) preset = preset with { Cars = preset.Cars.Select(c => c with { Driver = preset.Cars[0].Driver }).ToImmutableArray() };
    EffectiveWeekend Variant(bool speed)
    {
        var keys = speed ? new[] { "topSpeed", "accelerationDeployment" }
            : new[] { "highSpeedAero", "lowSpeedAero", "mechanicalGrip", "braking", "traction" };
        var cars = preset.Cars.Select(c => c with { Car = c.Car with {
            Id = speed ? "authored-speed-probe-v1" : "authored-corner-probe-v1",
            Ratings = c.Car.Ratings.SetItems(keys.Select(k => new KeyValuePair<string, double>(k, 92))) } }).ToImmutableArray();
        var weekend = WeekendPreparation.Prepare(preset with { Cars = cars }, 12);
        return weekend with { Configuration = weekend.Configuration with { Traffic = new(Enabled: false),
            Tyres = new(DegradationPerLap: 0, WarmupPenalty: 0), LapVariation = noise ? .4 : 0, RaceFormVariation = noise ? .4 : 0 } };
    }
    var speed = Variant(true);
    var corners = Variant(false);
    double Time(EffectiveWeekend weekend, int seed)
    {
        var race = WeekendPreparation.CreateRace(weekend, seed);
        PhaseResult result;
        do { result = race.ResolvePhase(); } while (!result.Finished);
        return result.Classification.Single(c => c.DriverId == "car-1").FinishTime!.Value;
    }
    var deltas = Enumerable.Range(101, 20).Select(seed => new { seed, speedMinusCornersSeconds = Time(speed, seed) - Time(corners, seed) }).ToArray();
    reports.Add(new { circuit, mixed, noise, speed, corners, pairedSeeds = deltas,
        meanSpeedMinusCornersSeconds = deltas.Average(d => d.speedMinusCornersSeconds) });
}
File.WriteAllText(Path.Combine(args[0], "effective-weekend-v1.json"), JsonSerializer.Serialize(new {
    model = WeekendPreparation.ModelVersion, interpretation = "Authored hypotheses, not real-world calibration. Representative segmented geometry. Clear-air controls isolate circuit fit; no setup optimisation claim.",
    reports }, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Wrote {reports.Count} paired comparisons and self-contained effective inputs.");
