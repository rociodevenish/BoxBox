using System.Text.Json;
using System.Text.Json.Serialization;
using Game.Domain;
using SimulationRunner;

if (typeof(RaceConfiguration).GetProperty("RaceFormVariation") is not null || typeof(Track).GetProperty("Segments") is not null)
    throw new ArgumentException("Historical harness must be compiled against the pinned previous API, not the current aggregate model.");
if (args.Length != 3 || !int.TryParse(args[1], out var firstSeed) || !int.TryParse(args[2], out var runs) || runs < 20)
    throw new ArgumentException("Expected output directory, initial seed and at least 20 runs.");
var json = new JsonSerializerOptions { WriteIndented = true };
json.Converters.Add(new JsonStringEnumConverter());
var field = BenchmarkMeasurement.Field();
var configuration = new RaceConfiguration(40, new("OpenSegment", 90, .5, PassingChance: .4), LapVariation: .2);
var result = BenchmarkMeasurement.Measure("previous-aggregate", BenchmarkMeasurement.PreviousRevision, 1,
    configuration, field, BenchmarkMeasurement.Plan(field), Enumerable.Range(firstSeed, runs).ToArray(), args[0], json);
Directory.CreateDirectory(args[0]);
File.WriteAllText(Path.Combine(args[0], "previous-benchmark.json"), JsonSerializer.Serialize(new {
    Environment = BenchmarkMeasurement.EnvironmentInfo(), Result = result }, json));
Console.WriteLine($"Previous executable benchmark: {result.PhaseSamples.Count} phases; mean {result.PhaseMilliseconds.Mean:F3}ms");
