using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Game.Domain;
using Game.Simulation;

var json = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
json.Converters.Add(new JsonStringEnumConverter());
if(args.Length==2&&args[0]=="--replay")
{
    using var document=JsonDocument.Parse(File.ReadAllText(args[1]));
    var root=document.RootElement;
    var checkedRuns=0;
    Check(root.GetProperty("baselineReplay"),root.GetProperty("baselineRuns"));
    foreach(var comparison in root.GetProperty("comparisons").EnumerateArray())Check(comparison.GetProperty("replay"),comparison.GetProperty("runs"));
    Console.WriteLine($"Replayed {checkedRuns} saved full-field outcomes directly from disk without catalog access.");
    return;
    void Check(JsonElement saved,JsonElement outcomes)
    {
        var replay=saved.Deserialize<RaceReplay>(json)!;
        foreach(var outcome in outcomes.EnumerateArray())
        {
            var expected=outcome.Deserialize<Outcome>(json)!;
            if(JsonSerializer.Serialize(Run(replay with {Seed=expected.Seed}),json)!=JsonSerializer.Serialize(expected,json))
                throw new InvalidOperationException("Saved-file replay differs.");
            checkedRuns++;
        }
    }
}
if (args.Length != 1) throw new ArgumentException("Pass an evidence output directory, or --replay case.json.");
Directory.CreateDirectory(args[0]);
var seeds = Enumerable.Range(101, 12).ToArray();
var roster = new[] {
    ("driver-lando-norris","driver-oscar-piastri"), ("driver-charles-leclerc","driver-lewis-hamilton"),
    ("driver-max-verstappen","driver-isack-hadjar"), ("driver-george-russell","driver-andrea-kimi-antonelli"),
    ("driver-fernando-alonso","driver-lance-stroll"), ("driver-pierre-gasly","driver-franco-colapinto"),
    ("driver-esteban-ocon","driver-oliver-bearman"), ("driver-liam-lawson","driver-arvid-lindblad"),
    ("driver-alexander-albon","driver-carlos-sainz"), ("driver-nico-hulkenberg","driver-gabriel-bortoleto"),
    ("driver-sergio-perez","driver-valtteri-bottas") };
var variants = new Dictionary<string, WeekendSetup> {
    ["speed"] = new(-2,0,0), ["downforce"] = new(2,0,0),
    ["responsive"] = new(0,-2,0), ["stable"] = new(0,2,0),
    ["warm-up"] = new(0,0,-2), ["preservation"] = new(0,0,2) };
var summaries = new List<object>();
var replayChecks = new List<object>();
var files = new List<string>();
foreach (var circuit in new[] { "track-monza", "track-monaco" })
foreach (var equal in new[] { true, false })
foreach (var mode in new[] { "clear-zero", "traffic-zero", "traffic-noisy" })
{
    var presets = Field(circuit, equal);
    var neutral = presets.Select(p => WeekendPreparation.Prepare(p,20,new())).ToArray();
    var noisy = mode == "traffic-noisy";
    var configuration = neutral[0].Configuration with {
        Traffic = new(Enabled:mode != "clear-zero"), LapVariation = noisy ? .4 : 0,
        RaceFormVariation = noisy ? .4 : 0, StopVariation = noisy ? .6 : 0,
        PassingVariation = noisy ? 1 : 0, EstimateError = 0 };
    var baselineDrivers = Drivers(neutral);
    var plan = new StrategyPlan(baselineDrivers.Select(d => new ScheduledDecision(2,d.Id,new(PitCompound:TyreCompound.Hard))).ToArray(),[]);
    var baseline = SimulationReplay.Capture(configuration,baselineDrivers,plan,0);
    var runSeeds = noisy ? seeds : [101];
    var baselineRuns = runSeeds.Select(seed => Run(baseline with { Seed=seed })).ToArray();
    var comparisons = new List<object>();
    foreach (var (name,setup) in variants)
    {
        var target = WeekendPreparation.Prepare(presets[0],20,setup);
        var all = neutral.ToArray(); all[0] = target;
        var alternative = SimulationReplay.Capture(configuration,Drivers(all),plan,0);
        var runs = runSeeds.Select(seed => Run(alternative with { Seed=seed })).ToArray();
        var deltas = runs.Select((run,i) => new {
            seed=run.Seed,
            targetTeamMeanSeconds=TargetMean(run)-TargetMean(baselineRuns[i]),
            driverSeconds=run.Classification.Where(c=>c.DriverId.StartsWith("team-01-",StringComparison.Ordinal)).Select(c=>new {
                c.DriverId, seconds=c.FinishTime!.Value-baselineRuns[i].Classification.Single(b=>b.DriverId==c.DriverId).FinishTime!.Value,
                positionChange=c.Position-baselineRuns[i].Classification.Single(b=>b.DriverId==c.DriverId).Position }).ToArray()
        }).ToArray();
        var values = deltas.Select(d=>d.targetTeamMeanSeconds).ToArray();
        var statistics = Stats(values);
        comparisons.Add(new {name,setup,targetEffective=target,replay=alternative,pairedDeltas=deltas,statistics,runs});
        summaries.Add(new {circuit,field=equal?"equal":"mixed",mode,variant=name,statistics});
        // Compare actual replayed outcomes, not a copy of private mapping arithmetic.
        var saved=JsonSerializer.Deserialize<RaceReplay>(JsonSerializer.Serialize(alternative with {Seed=101},json),json)!;
        var recovered=Run(saved);
        var identical=JsonSerializer.Serialize(runs[0],json)==JsonSerializer.Serialize(recovered,json);
        if(!identical)throw new InvalidOperationException("Saved full-field replay changed outcomes.");
        replayChecks.Add(new {circuit,equal,mode,variant=name,seed=101,identical,
            saved.ModelVersion,saved.RandomnessVersion,outcomeSha256=Digest(recovered)});
    }
    var filename=$"{circuit}-{(equal?"equal":"mixed")}-{mode}.json";
    Write(filename,new {schema="weekend-setup-evidence-v1",circuit,field=equal?"equal":"mixed",mode,
        entryCount=22,teamCount=11,targetTeam="team-01",laps=20,sourcePresets=presets,
        neutralEffective=neutral,baselineReplay=baseline,metadata=SimulationReplay.Metadata(configuration),
        baselineRuns,comparisons});
}

// Practical first-lap/stint outcomes with all 22 entries retained and traffic/noise disabled.
var tyreProbes=new List<object>();
foreach(var circuit in new[]{"track-monza","track-monaco"})
foreach(var laps in new[]{1,20})
{
    var presets=Field(circuit,false);
    foreach(var (name,tyres) in new[]{("warm-up",-2),("neutral",0),("preservation",2)})
    {
        var weekends=presets.Select((p,i)=>WeekendPreparation.Prepare(p,laps,i==0?new(0,0,tyres):new())).ToArray();
        var configuration=weekends[0].Configuration with {Traffic=new(Enabled:false),LapVariation=0,RaceFormVariation=0,StopVariation=0,PassingVariation=0,EstimateError=0};
        var replay=SimulationReplay.Capture(configuration,Drivers(weekends),new([],[]),101);
        tyreProbes.Add(new {circuit,laps,name,sourcePresets=presets,targetEffective=weekends[0],replay,outcome=Run(replay),
            interpretation="Fixed-compound no-stop laboratory stint. Eligibility is recorded; not an official race strategy."});
    }
}

// Low/high Adaptability and information scores are independent, declared authored probes.
var personProbes=new List<object>();
var source=Field("track-monza",false);
foreach(var adaptability in new[]{10d,95d})
foreach(var information in new[]{10d,95d})
{
    var target=source[0] with {Engineer=source[0].Engineer with {Setup=information},Cars=source[0].Cars.Select((c,i)=>c with {
        Driver=c.Driver with {Ratings=c.Driver.Ratings.SetItem("adaptability",adaptability).SetItem("setupFeedback",information),
            Preferences=i==0?[new("responsive-front","Responsive front")]:[new("stable-rear","Stable rear")]}}).ToImmutableArray()};
    var assessment=WeekendPreparation.Assess(target,20,new(0,2,0),101);
    var all=source.Select((p,i)=>WeekendPreparation.Prepare(i==0?target:p,20,i==0?new(0,2,0):new())).ToArray();
    var config=all[0].Configuration with {Traffic=new(Enabled:false),LapVariation=0,RaceFormVariation=0,StopVariation=0,PassingVariation=0,EstimateError=0};
    var replay=SimulationReplay.Capture(config,Drivers(all),new([],[]),101);
    var result=Run(replay);
    var recreated=WeekendPreparation.Assess(target,20,new(0,2,0),101);
    if(JsonSerializer.Serialize(assessment,json)!=JsonSerializer.Serialize(recreated,json))throw new InvalidOperationException("Assessment rerolled.");
    personProbes.Add(new {adaptability,information,sourcePresets=source,targetSource=target,assessment,replay,outcome=result,
        repeatedAssessmentIdentical=true,outcomeSha256=Digest(result)});
}
Write("isolation-probes.json",new {tyreProbes,personProbes});
Write("summary.json",new {schema="weekend-setup-evidence-v1",preparationModel=WeekendPreparation.ModelVersion,
    setupModel=WeekendSetup.Version,practiceModel=WeekendPreparation.PracticeModelVersion,
    driverTrackContent=WeekendPreparation.CardVersion,carContent=WeekendPreparation.CarVersion,
    interpretation="Authored provisional 1–100 design inputs and representative geometry, not calibrated physics. Six axis endpoints, not all125 setups or an optimal-setup search. Signed delta is alternative minus neutral: negative is faster. No qualifying outcomes.",
    seeds,grid="Stable entry order; gaps 0.8s apart; target team occupies first two slots. Equal field clones first driver ratings/preferences but retains identities. Mixed field uses supplied detailed driver ratings/preferences. All base cars82. No card-star conversion.",
    mainStrategy="20 laps; every entry Standard, pit to Hard at Race Phase2; no conditional policy. Same plan, grid and non-target20 inputs per paired comparison.",
    noisyInterpretation="Paired scoped randomness may cancel in clear comparisons. Traffic changes can introduce non-linear sign variation; retain zero variance and contradictions rather than claiming universal advantage.",
    summaries,replayChecks,files});
Console.WriteLine($"Wrote {summaries.Count} comparisons, {replayChecks.Count} exact full-field replay checks and isolation probes.");

WeekendPreset[] Field(string circuit,bool equal)
{
    var opened=roster.Select(pair=>WeekendPresetCatalog.Open(circuit,pair.Item1,pair.Item2)).ToArray();
    return opened.Select((preset,index)=>preset with {Cars=preset.Cars.Select((car,slot)=>car with {
        TeamId=$"team-{index+1:00}",EntryId=$"team-{index+1:00}-car-{slot+1}",
        Driver=equal?car.Driver with {Ratings=opened[0].Cars[0].Driver.Ratings,Preferences=opened[0].Cars[0].Driver.Preferences}:car.Driver
    }).ToImmutableArray()}).ToArray();
}
Driver[] Drivers(EffectiveWeekend[] weekends)=>weekends.SelectMany(w=>w.Cars).Select((car,index)=>car.RaceDriver with {Gap=index*.8}).ToArray();
Outcome Run(RaceReplay replay)
{
    SimulationReplay.RequireVersion(replay.Configuration,replay.ModelVersion,replay.RandomnessVersion);
    if(replay.Drivers.Length!=22||replay.Drivers.Select(d=>d.Team).Distinct().Count()!=11)throw new InvalidOperationException("Expected full household field.");
    var engine=new RaceEngine(replay.Configuration,replay.Drivers,replay.Seed);
    PhaseResult result;var phases=0;
    do {result=engine.ResolvePhase(replay.Plan);if(++phases>100)throw new InvalidOperationException("Evidence race did not finish.");}while(!result.Finished);
    if(result.Classification.Count!=22||result.Classification.Any(c=>c.FinishTime is null))throw new InvalidOperationException("Incomplete full field.");
    var targetEvents=result.Events.Where(e=>e.DriverId.StartsWith("team-01-",StringComparison.Ordinal)&&e.Kind is RaceEventKind.LapCompleted or RaceEventKind.TyreChanged or RaceEventKind.PitEntry or RaceEventKind.PitExit).ToArray();
    return new(replay.Seed,result.Classification.ToArray(),result.Tyres.ToArray(),result.Eligibility,targetEvents,phases);
}
double TargetMean(Outcome outcome)=>outcome.Classification.Where(c=>c.DriverId.StartsWith("team-01-",StringComparison.Ordinal)).Average(c=>c.FinishTime!.Value);
object Stats(double[] values)
{
    var mean=values.Average();
    return new {meanSeconds=mean,minSeconds=values.Min(),maxSeconds=values.Max(),
        standardDeviationSeconds=Math.Sqrt(values.Average(x=>(x-mean)*(x-mean))),
        faster=values.Count(x=>x<-.0000001),tied=values.Count(x=>Math.Abs(x)<=.0000001),slower=values.Count(x=>x>.0000001),
        signChanges=values.Any(x=>x<-.0000001)&&values.Any(x=>x>.0000001)};
}
void Write(string name,object value){File.WriteAllText(Path.Combine(args[0],name),JsonSerializer.Serialize(value,json));files.Add(name);}
string Digest(object value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value,json))));
record Outcome(int Seed,ClassificationEntry[] Classification,TyreState[] Tyres,IReadOnlyDictionary<string,bool> Eligibility,RaceEvent[] TargetEvents,int Phases);
