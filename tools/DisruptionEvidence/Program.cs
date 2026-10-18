using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Game.Domain;
using Game.Simulation;

var json=new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase};
json.Converters.Add(new JsonStringEnumConverter());
if(args.Length==2&&args[0]=="--replay")
{
    using var file=File.OpenRead(args[1]);using var gzip=new GZipStream(file,CompressionMode.Decompress);
    var saved=JsonSerializer.Deserialize<Archive>(gzip,json)!;
    foreach(var run in saved.Runs)
        if(JsonSerializer.Serialize(Execute(run.Input),json)!=JsonSerializer.Serialize(run,json))throw new InvalidOperationException("Frozen replay differs: "+run.Input.Group);
    Console.WriteLine($"Exact replay of {saved.Runs.Length} frozen runs, no catalog reads.");return;
}
if(args.Length!=1)throw new ArgumentException("Output directory or --replay archive.json.gz required.");
if(Directory.Exists(args[0])&&Directory.EnumerateFileSystemEntries(args[0]).Any())throw new ArgumentException("Evidence output must be new or empty; saved archives are immutable.");
Directory.CreateDirectory(args[0]);
var runs=new List<Run>();var watch=Stopwatch.StartNew();
foreach(var circuit in new[]{"track-monza","track-monaco"})
{
    var weekend=WeekendPreparation.Prepare(WeekendPresetCatalog.Open(circuit),12);
    var mapped=WeekendPresetCatalog.IncidentInputs(weekend,new(ErrorRate:.0003,FailureRate:.00003,ContactProbability:.02));
    var config=mapped.Configuration with{Track=mapped.Configuration.Track with{PitRoute=circuit=="track-monza"?new(.2,38):new(.15,32)}};
    foreach(var seed in Enumerable.Range(101,64))
    foreach(var arm in new[]{"standard","conserve","push","reliability-1","reliability-100","battle"})
    {
        var drivers=mapped.Drivers.Select((d,i)=>d with{Team=i==0?"A":"B",Gap=i,Speed=i==1?d.Speed+10:d.Speed,
            IncidentProfile=d.IncidentProfile! with{Reliability=arm=="reliability-1"?1:arm=="reliability-100"?100:d.IncidentProfile.Reliability}}).ToArray();
        runs.Add(Execute(new(circuit,arm,seed,config,drivers,arm=="push"?PaceInstruction.Push:arm=="conserve"?PaceInstruction.Conserve:PaceInstruction.Standard,arm=="battle",false,false,null)));
    }
    foreach(var neutral in new[]{false,true})foreach(var pit in new[]{false,true})
        runs.Add(Execute(new(circuit,$"mechanism-{(neutral?"neutral":"green")}-{(pit?"pit":"stay")}",1,
            config with{Laps=20,AttributeSeconds=0,LapVariation=0,StopVariation=0,Traffic=new(Enabled:false),Tyres=new(DegradationPerLap:0,WarmupPenalty:0,HardWarmup:0,HardLoss:0),SafetyCar=new(DurationSeconds:600),Incidents=new(ComponentWearPerSecond:0,InterruptMajorIncidents:true)},
            [new("trigger","A",Gap:100),new("target","B",StartingCompound:TyreCompound.Hard)],PaceInstruction.Standard,false,neutral,pit,new([new("trigger",IncidentKind.MechanicalFailure,36)]))));
    foreach(var seed in Enumerable.Range(101,16))
    foreach(var neutral in new[]{false,true})foreach(var pit in new[]{false,true})
        runs.Add(Execute(new(circuit,$"strategy-{(neutral?"neutral":"green")}-{(pit?"pit":"stay")}",seed,
            config with{Incidents=new(ComponentWearPerSecond:0,InterruptMajorIncidents:true)},
            mapped.Drivers.Select((d,i)=>d with{Team=i==0?"A":"B",Gap=i*9}).ToArray(),PaceInstruction.Standard,false,neutral,pit,new([new(mapped.Drivers[0].Id,IncidentKind.Spin,36)]))));
}
var archive=new Archive("disruption-evidence-v1",runs.ToArray());
using(var file=File.Create(Path.Combine(args[0],"frozen.json.gz")))using(var gzip=new GZipStream(file,CompressionLevel.SmallestSize))JsonSerializer.Serialize(gzip,archive,json);
var summary=runs.GroupBy(r=>(r.Input.Circuit,r.Input.Group)).Select(g=>new{g.Key.Circuit,g.Key.Group,samples=g.Count(),events=g.Sum(r=>r.Final.Events.Count(e=>e.Kind is RaceEventKind.LockUp or RaceEventKind.Spin or RaceEventKind.Contact or RaceEventKind.MechanicalFailure)),retirements=g.Sum(r=>r.Final.Classification.Count(c=>c.RetirementTime.HasValue)),raceClockSeconds=g.Sum(r=>r.Final.Time),distanceEligibilityGroups=g.SelectMany(r=>r.Final.Classification.Select(c=>new{c.Laps,eligible=r.Final.Eligibility[c.DriverId],retired=c.RetirementTime.HasValue})).GroupBy(x=>x).Select(x=>new{x.Key.Laps,x.Key.eligible,x.Key.retired,count=x.Count()}),meanFinalTargetPosition=g.Average(r=>r.Final.Classification.Single(c=>c.DriverId==r.Input.Drivers[1].Id).Position)}).ToArray();
var pairs=new List<object>();
foreach(var circuit in runs.Select(r=>r.Input.Circuit).Distinct())
foreach(var arm in new[]{"conserve","push","reliability-1","reliability-100","battle"})
{
    var differences=Enumerable.Range(101,64).Select(seed=>{
        var a=runs.Single(r=>r.Input.Circuit==circuit&&r.Input.Group=="standard"&&r.Input.Seed==seed);var b=runs.Single(r=>r.Input.Circuit==circuit&&r.Input.Group==arm&&r.Input.Seed==seed);
        return new{seed,incidentDifference=b.Final.Events.Count(e=>e.Kind is RaceEventKind.LockUp or RaceEventKind.Spin or RaceEventKind.Contact or RaceEventKind.MechanicalFailure)-a.Final.Events.Count(e=>e.Kind is RaceEventKind.LockUp or RaceEventKind.Spin or RaceEventKind.Contact or RaceEventKind.MechanicalFailure),baseline=a.Final.Classification.Select(c=>new{c.DriverId,c.Laps,eligible=a.Final.Eligibility[c.DriverId],c.RetirementTime}),alternative=b.Final.Classification.Select(c=>new{c.DriverId,c.Laps,eligible=b.Final.Eligibility[c.DriverId],c.RetirementTime})};}).ToArray();
    pairs.Add(new{circuit,baseline="standard",alternative=arm,samples=64,differences});
}
var strategyPairs=runs.Where(r=>r.Input.Group.Contains("-pit")).Select(r=>{var stay=runs.Single(s=>s.Input.Circuit==r.Input.Circuit&&s.Input.Seed==r.Input.Seed&&s.Input.Group==r.Input.Group.Replace("-pit","-stay"));var id=r.Input.Drivers[1].Id;double Crossing(Run x)=>x.Final.Events.First(e=>e.DriverId==id&&e.Kind==RaceEventKind.LapCompleted&&e.Lap==2).Time;return new{r.Input.Circuit,r.Input.Group,r.Input.Seed,matchedLap2TimeCostSeconds=Crossing(r)-Crossing(stay),pitFinalPosition=r.Final.Classification.Single(c=>c.DriverId==id).Position,stayFinalPosition=stay.Final.Classification.Single(c=>c.DriverId==id).Position,pitEligible=r.Final.Eligibility[id],stayEligible=stay.Final.Eligibility[id]};}).ToArray();
File.WriteAllText(Path.Combine(args[0],"summary.json"),JsonSerializer.Serialize(new{schema=archive.Schema,runs=runs.Count,elapsedWallSeconds=watch.Elapsed.TotalSeconds,summary,pairs,strategyPairs},json));
Console.WriteLine($"Saved {runs.Count} frozen runs and paired groups.");

Run Execute(Input input)
{
    var engine=input.Scenario is {} scenario?new RaceEngine(input.Configuration,input.Drivers,input.Seed,scenario):new RaceEngine(input.Configuration,input.Drivers,input.Seed);var states=new List<PhaseResult>();
    var decisions=input.Drivers.Select((d,i)=>(d.Id,Decision:new DriverDecision(input.Pace,Battle:input.Battle?(i==0?BattleInstruction.Defend:BattleInstruction.Attack):BattleInstruction.Neutral))).ToDictionary(x=>x.Id,x=>x.Decision);
    Dictionary<string,DriverDecision> Legal()=>decisions.Where(p=>engine.GetSafetyCarOptions().Single(o=>o.DriverId==p.Key).Status!="retired").ToDictionary(p=>p.Key,p=>p.Value with{Battle=engine.GetState().BattleOptions[p.Key].Contains(p.Value.Battle)?p.Value.Battle:BattleInstruction.Neutral});
    var state=engine.ResolvePhase(Legal());states.Add(state);
    if(input.Scenario is not null)
    {
        if(state.Interruption is null)throw new InvalidOperationException("Missing diagnostic interruption.");
        var choices=engine.GetSafetyCarOptions().Where(o=>o.Status=="available").ToDictionary(o=>o.DriverId,o=>new SafetyCarDecision(input.Pit&&o.DriverId==input.Drivers[1].Id?TyreCompound.Hard:null));
        if(input.Neutralised)state=engine.ResolveSafetyCar(choices);
        else {engine.CommitSafetyCarChoices(choices);state=engine.ResolvePhase(Legal());}
        states.Add(state);
    }
    for(var i=0;!state.Finished&&i<50;i++){state=engine.ResolvePhase(Legal());states.Add(state);}
    if(!state.Finished)throw new InvalidOperationException("Bounded run failed to finish.");
    return new(input,SimulationReplay.Metadata(input.Configuration),states.ToArray(),state,engine.GetDesignerIncidentDiagnostics().ToArray());
}
record Input(string Circuit,string Group,int Seed,RaceConfiguration Configuration,Driver[] Drivers,PaceInstruction Pace,bool Battle,bool Neutralised,bool Pit,IncidentScenario? Scenario);
record Run(Input Input,ReplayMetadata Versions,PhaseResult[] States,PhaseResult Final,IncidentDiagnostic[] Diagnostics);
record Archive(string Schema,Run[] Runs);





