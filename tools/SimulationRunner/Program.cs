using System.Text.Json;
using System.Text.Json.Serialization;
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
var pace = args.Contains("push") ? PaceInstruction.Push : args.Contains("conserve")
    ? PaceInstruction.Conserve : PaceInstruction.Standard;
var decisions = drivers.ToDictionary(d => d.Id, _ => new DriverDecision(pace));
var jsonOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true };
jsonOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
jsonOptions.Converters.Add(new JsonStringEnumConverter());
var scriptIndex = Array.IndexOf(args, "--script");
if (scriptIndex >= 0 && scriptIndex + 1 >= args.Length) throw new ArgumentException("--script needs a file.");
var script = scriptIndex < 0 ? [] : JsonSerializer.Deserialize<ScheduledDecision[]>(
    File.ReadAllText(args[scriptIndex + 1]), jsonOptions) ?? throw new ArgumentException("Script must be an array.");
if (script.Any(s => s is null || s.Phase < 0 || s.Phase >= (laps + 4) / 5
        || drivers.All(d => d.Id != s.DriverId) || s.Decision is null || !Enum.IsDefined(s.Decision.Pace)
        || s.Decision.PitCompound is { } compound && !Enum.IsDefined(compound))
    || script.GroupBy(s => (s.Phase, s.DriverId)).Any(g => g.Count() > 1))
    throw new ArgumentException("Script contains an unknown driver, invalid phase or duplicate decision.");
PhaseResult result;
var phases = new List<PhaseResult>();
var phase = 0;
do
{
    var phaseDecisions = new Dictionary<string, DriverDecision>(decisions);
    foreach (var scheduled in script.Where(s => s.Phase == phase)) phaseDecisions[scheduled.DriverId] = scheduled.Decision;
    result = engine.ResolvePhase(phaseDecisions);
    phases.Add(result);
    phase++;
} while (!result.Finished);
Directory.CreateDirectory("reports");
File.WriteAllText("reports/race.json", JsonSerializer.Serialize(new { seed, configuration, drivers, decisions, script, phases, result }, jsonOptions));
var summary = $"Seed {seed}; {laps} laps; {track.Name}; mixed={mixed}\nFresh starting tyres: Medium\n"
    + string.Join(Environment.NewLine, phases.Select(p => $"Phase: leader lap {p.LeaderLap}, clock {p.Time:F3}s; "
        + string.Join(", ", p.Classification.Select(c => $"{c.DriverId} +{c.GapSeconds:F3}s / {c.GapLaps} laps"))))
    + Environment.NewLine + string.Join(Environment.NewLine, phases.SelectMany(p => p.Tyres.Select(t =>
        $"Lap {p.LeaderLap}: {t.DriverId} {t.Compound}, age {t.Age}, condition {t.Condition:F3}, pace {t.Pace}")))
    + Environment.NewLine + string.Join(Environment.NewLine, result.Classification.Select(c =>
    $"P{c.Position,2} {c.DriverId}: {c.Laps} laps, {c.FinishTime:F3}s, +{c.GapLaps} laps / {c.GapSeconds:F3}s, eligible={result.Eligibility[c.DriverId]}"))
    + Environment.NewLine + string.Join(Environment.NewLine, result.Events.Where(e => e.Kind.StartsWith("Pit")
        || e.Kind == "TyreChanged").Select(e => $"{e.Time:F3}s {e.DriverId} lap {e.Lap}: {e.Kind} {e.Value:F3}"));
File.WriteAllText("reports/race.txt", summary);
Console.WriteLine(summary);
