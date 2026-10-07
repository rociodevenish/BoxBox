using System.Text.Json;
using Game.Domain;
using Game.Simulation;

var laps = args.Length > 0 ? int.Parse(args[0]) : 50;
var seed = args.Length > 1 ? int.Parse(args[1]) : 7;
var mixed = args.Contains("mixed");
var track = args.Contains("technical") ? new Track("Technical", 95, .2) : new Track("Fast", 85, .8);
var drivers = Enumerable.Range(1, 22).Select(i => new Driver($"driver-{i:00}", $"team-{(i + 1) / 2:00}",
    Speed: mixed ? (22 - i) * .1 : 0, Cornering: mixed ? (i % 5) * .1 : 0, Gap: (i - 1) * .5)).ToArray();
var configuration = new RaceConfiguration(laps, track, LapVariation: .2);
var engine = new RaceEngine(configuration, drivers, seed);
PhaseResult result;
var phases = new List<PhaseResult>();
do { result = engine.ResolvePhase(); phases.Add(result); } while (!result.Finished);
Directory.CreateDirectory("reports");
File.WriteAllText("reports/race.json", JsonSerializer.Serialize(new { seed, configuration, drivers, phases, result },
    new JsonSerializerOptions { WriteIndented = true }));
var summary = $"Seed {seed}; {laps} laps; {track.Name}; mixed={mixed}\nFresh starting tyres: Medium\n"
    + string.Join(Environment.NewLine, phases.Select(p => $"Phase: leader lap {p.LeaderLap}, clock {p.Time:F3}s; "
        + string.Join(", ", p.Classification.Select(c => $"{c.DriverId} +{c.GapSeconds:F3}s / {c.GapLaps} laps"))))
    + Environment.NewLine + string.Join(Environment.NewLine, result.Classification.Select(c =>
    $"P{c.Position,2} {c.DriverId}: {c.Laps} laps, {c.FinishTime:F3}s, +{c.GapLaps} laps / {c.GapSeconds:F3}s"));
File.WriteAllText("reports/race.txt", summary);
Console.WriteLine(summary);
