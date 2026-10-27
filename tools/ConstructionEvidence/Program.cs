using System.Collections.Immutable;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Game.Domain;
using Game.Simulation;

var json=new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase};
if(args.Length==2&&args[0]=="--replay")
{
 using var file=File.OpenRead(args[1]);using var gzip=new GZipStream(file,CompressionMode.Decompress);
 var saved=JsonSerializer.Deserialize<Archive>(gzip,json)??throw new Exception("Missing archive");
 Verify(saved);Console.WriteLine("Exact replay of 1300 frozen public race outcomes; no catalogue reads.");return;
}
if(args.Length!=1)throw new ArgumentException("New output directory or --replay construction.json.gz required.");
if(Directory.Exists(args[0])&&Directory.EnumerateFileSystemEntries(args[0]).Any())throw new ArgumentException("Refusing to overwrite evidence.");
Directory.CreateDirectory(args[0]);
var catalogue=ConstructionCatalog.Open();var cases=new List<Case>();
string[][] roster=[ ["driver-lando-norris","driver-oscar-piastri"],["driver-charles-leclerc","driver-lewis-hamilton"],["driver-max-verstappen","driver-isack-hadjar"],["driver-george-russell","driver-andrea-kimi-antonelli"],["driver-fernando-alonso","driver-lance-stroll"],["driver-pierre-gasly","driver-franco-colapinto"],["driver-esteban-ocon","driver-oliver-bearman"],["driver-liam-lawson","driver-arvid-lindblad"],["driver-alexander-albon","driver-carlos-sainz"],["driver-nico-hulkenberg","driver-gabriel-bortoleto"],["driver-sergio-perez","driver-valtteri-bottas"] ];
var builds=new Dictionary<string,ImmutableArray<string>>{
 ["baseline"]=CarConstruction.Open(catalogue,["team-01"]).Drafts["team-01"].OptionIds,
 ["low-drag"]=["car-aero-low-drag-efficiency","car-chassis-balanced","car-power-top-end","car-thermal-low-drag"],
 ["cornering"]=["car-aero-high-downforce","car-chassis-responsive","car-power-deployment","car-thermal-balanced"],
 ["tyre"]=["car-aero-balanced-efficiency","car-chassis-tyre-friendly","car-power-smooth-delivery","car-thermal-long-run"],
 ["reliability"]=["car-aero-high-speed-stability","car-chassis-balanced","car-power-robust","car-thermal-hot-climate"]};
foreach(var circuit in new[]{"track-monza","track-monaco"})foreach(var diagnostic in new[]{false,true})foreach(var (name,choices) in builds)
{
 var presets=roster.Select((r,i)=>WeekendPresetCatalog.Open(circuit,r[0],r[1]) with{Cars=WeekendPresetCatalog.Open(circuit,r[0],r[1]).Cars.Select((c,s)=>c with{TeamId=$"team-{i+1:00}",EntryId=$"team-{i+1:00}-car-{s+1}"}).ToImmutableArray()}).ToArray();
 presets=presets.Select((p,i)=>Apply(p,i==0?name:"baseline",i==0?choices:builds["baseline"])).ToArray();
 var weekends=presets.Select(p=>WeekendPreparation.Prepare(p,20,new())).ToArray();
 var mapped=weekends.Select(w=>diagnostic?WeekendPresetCatalog.IncidentInputs(w,new(FailureRate:.00003)): (Configuration:w.Configuration,Drivers:w.Cars.Select(c=>c.RaceDriver).ToArray())).ToArray();
 var drivers=mapped.SelectMany(m=>m.Drivers).Select((d,i)=>d with{Gap=i*.8}).ToArray();
 var plan=new StrategyPlan(drivers.Select(d=>new ScheduledDecision(2,d.Id,new(PitCompound:TyreCompound.Hard))).ToArray(),[]);
 var replay=SimulationReplay.Capture(mapped[0].Configuration,drivers,plan,0);
 var outcomes=Enumerable.Range(101,diagnostic?100:30).Select(seed=>Run(replay with{Seed=seed})).ToArray();
 cases.Add(new(circuit,diagnostic,name,choices,CarConstruction.Cost(catalogue,choices),24-CarConstruction.Cost(catalogue,choices),presets,replay,outcomes));
}
var archive=new Archive("construction-evidence-v1",catalogue,WeekendPreparation.ModelVersion,WeekendPreparation.CardVersion,24,cases.ToArray());
using(var file=File.Create(Path.Combine(args[0],"construction.json.gz")))using(var gzip=new GZipStream(file,CompressionLevel.SmallestSize))JsonSerializer.Serialize(gzip,archive,json);
var summary=cases.Where(c=>c.Build!="baseline").Select(c=>{
 var baseline=cases.Single(b=>b.Circuit==c.Circuit&&b.Diagnostic==c.Diagnostic&&b.Build=="baseline");
 var pairs=c.Outcomes.Select((o,i)=>new{seed=o.Seed,seconds=Mean(o)-Mean(baseline.Outcomes[i]),position=Position(o)-Position(baseline.Outcomes[i]),retirements=Retired(o)-Retired(baseline.Outcomes[i]),o.Hash}).ToArray();
 var finite=pairs.Where(p=>p.seconds.HasValue).Select(p=>p.seconds!.Value).ToArray();
 return new{c.Circuit,c.Diagnostic,c.Build,c.Cost,c.RemainingCredits,samples=pairs.Length,comparableFinishes=finite.Length,meanSeconds=finite.Length>0?(double?)finite.Average():null,minSeconds=finite.Length>0?(double?)finite.Min():null,maxSeconds=finite.Length>0?(double?)finite.Max():null,faster=finite.Count(v=>v<0),slower=finite.Count(v=>v>0),retirementDifference=pairs.Sum(p=>p.retirements),pairs};});
File.WriteAllText(Path.Combine(args[0],"summary.json"),JsonSerializer.Serialize(summary,new JsonSerializerOptions(json){WriteIndented=true}));
Console.WriteLine("Published 20 frozen cases / 1300 public outcomes.");
WeekendPreset Apply(WeekendPreset preset,string name,ImmutableArray<string> choices)=>ConstructedBaseCar.Apply(preset,"evidence-base-v1/"+name,catalogue.Version,CarConstruction.Ratings(catalogue,choices),CarConstruction.MechanicalAxis(catalogue,choices));
Outcome Run(RaceReplay replay)
{
 SimulationReplay.RequireVersion(replay.Configuration,replay.ModelVersion,replay.RandomnessVersion);
 var engine=new RaceEngine(replay.Configuration,replay.Drivers,replay.Seed);PhaseResult result;var phases=0;
 do{result=engine.ResolvePhase(replay.Plan);if(++phases>100)throw new Exception("Race exceeded phase bound");}while(!result.Finished);
 var events=result.Events.Where(e=>e.Kind is RaceEventKind.LockUp or RaceEventKind.Spin or RaceEventKind.Contact or RaceEventKind.MechanicalFailure || e.DriverId.StartsWith("team-01-",StringComparison.Ordinal)&&e.Kind is RaceEventKind.LapCompleted or RaceEventKind.TyreChanged or RaceEventKind.PitEntry or RaceEventKind.PitExit).ToArray();
 return new(replay.Seed,result.Time,result.Classification.ToArray(),result.Tyres.ToArray(),result.Eligibility,events,phases,Digest(result));
}
string Digest(object value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value,json))));
void Verify(Archive saved)
{
 if(saved.Schema!="construction-evidence-v1"||saved.EffectiveModel!=WeekendPreparation.ModelVersion||saved.CardVersion!=WeekendPreparation.CardVersion||saved.StartingCredits!=24||saved.Cases.Length!=20)throw new Exception("Unsupported archive contract");
 CarConstruction.ValidateCatalogue(saved.Catalogue);
 foreach(var circuit in new[]{"track-monza","track-monaco"})foreach(var diagnostic in new[]{false,true})foreach(var name in new[]{"baseline","low-drag","cornering","tyre","reliability"})
 {
  var c=saved.Cases.Single(c=>c.Circuit==circuit&&c.Diagnostic==diagnostic&&c.Build==name);
  if(c.Cost!=CarConstruction.Cost(saved.Catalogue,c.Choices)||c.RemainingCredits!=24-c.Cost||c.Presets.Length!=11||c.Replay.Drivers.Length!=22||c.Outcomes.Length!=(diagnostic?100:30)|| (c.Replay.Configuration.Incidents is not null)!=diagnostic)throw new Exception("Incomplete evidence inputs");
  var baseline=CarConstruction.Open(saved.Catalogue,["team-01"]).Drafts["team-01"].OptionIds;
  foreach(var (preset,index) in c.Presets.Select((p,i)=>(p,i)))
  {
   var choices=index==0?c.Choices:baseline;
   var expected=CarConstruction.Ratings(saved.Catalogue,choices);
   if(preset.Circuit.Id!=circuit||preset.Cars.Length!=2||preset.Cars.Any(car=>car.Car.Ratings.Count!=expected.Count||expected.Any(p=>car.Car.Ratings.GetValueOrDefault(p.Key)!=p.Value)))throw new Exception("Frozen construction differs from materialized car");
  }
  var effective=c.Presets.Select(p=>WeekendPreparation.Prepare(p,20,new())).ToArray();
  var reconstructed=effective.SelectMany(w=>w.Cars).Select((car,i)=>car.RaceDriver with{Gap=i*.8,IncidentProfile=diagnostic?c.Replay.Drivers[i].IncidentProfile:null}).ToArray();
  if(diagnostic&&reconstructed.Where((d,i)=>d.IncidentProfile is null||d.IncidentProfile.Reliability!=effective.SelectMany(w=>w.Cars).ElementAt(i).Source.Car.Ratings["reliability"]).Any())throw new Exception("Incident profile differs from frozen car");
  var configuration=effective[0].Configuration with{Incidents=c.Replay.Configuration.Incidents};
  var expectedPlan=new StrategyPlan(reconstructed.Select(d=>new ScheduledDecision(2,d.Id,new(PitCompound:TyreCompound.Hard))).ToArray(),[]);
  var reconstructedReplay=SimulationReplay.Capture(configuration,reconstructed,expectedPlan,0);
  if(JsonSerializer.Serialize(reconstructedReplay,json)!=JsonSerializer.Serialize(c.Replay,json))throw new Exception("Frozen setup, grid, plan or effective inputs differ");
  foreach(var (o,i) in c.Outcomes.Select((o,i)=>(o,i)))if(o.Seed!=101+i||JsonSerializer.Serialize(Run(c.Replay with{Seed=o.Seed}),json)!=JsonSerializer.Serialize(o,json))throw new Exception("Frozen public outcome changed");
 }
}
IEnumerable<ClassificationEntry> Target(Outcome o)=>o.Classification.Where(c=>c.DriverId.StartsWith("team-01-",StringComparison.Ordinal));
double? Mean(Outcome o)=>Target(o).All(c=>c.FinishTime.HasValue)?Target(o).Average(c=>c.FinishTime!.Value):null;
double Position(Outcome o)=>Target(o).Average(c=>c.Position);
int Retired(Outcome o)=>Target(o).Count(c=>c.RetirementTime.HasValue);
record Archive(string Schema,ConstructionCatalogue Catalogue,string EffectiveModel,string CardVersion,int StartingCredits,Case[] Cases);
record Case(string Circuit,bool Diagnostic,string Build,ImmutableArray<string> Choices,int Cost,int RemainingCredits,WeekendPreset[] Presets,RaceReplay Replay,Outcome[] Outcomes);
record Outcome(int Seed,double Time,ClassificationEntry[] Classification,TyreState[] Tyres,IReadOnlyDictionary<string,bool> Eligibility,RaceEvent[] TargetAndIncidentEvents,int Phases,string Hash);
