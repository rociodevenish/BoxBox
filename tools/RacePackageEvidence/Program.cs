using System.Collections.Immutable;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Game.Domain;
using Game.Simulation;

var json=new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase};
const string schema="race-package-evidence-v1",policy="legal-resources-fixed-six-arms-v1",product="5b8d08892550bf0aba38797f1a871e7f668c0e16";
string[] teams=Enumerable.Range(1,11).Select(i=>$"team-{i:00}").ToArray();
string[] personnelPolicies=["economical","expensive-legal"],arms=["Save","permanent","small-library","large-library","refinement","Rush"],scopes=["production-short","synthetic-alternating-six"];
var computedRaces=0;
RuleSnapshot Rules()=>new(new Dictionary<string,string>{{"permanent",PermanentDevelopment.Version},{"permanentOutcome","permanent-rich-outcome-v1"},{"package",PackageDevelopment.RichVersion},{"packageOutcome",PackageDevelopment.RichOutcomeVersion},{"rush",PackageDevelopment.RushVersion},{"selection",PackageSelectionDecisionPolicy.Version},{"staff",DevelopmentPersonnelMapping.Version},{"effective",WeekendPreparation.ModelVersion},{"card",WeekendPreparation.CardVersion},{"car",WeekendPreparation.CarVersion},{"scoring",ChampionshipScoring.Version},{"opening",AutomaticRaceStart.ModelVersion},{"incidentSynthetic","disruption-live-opt-in-v1"}},PermanentDevelopment.Options,PackageDevelopment.Templates,
 new IncidentTuning(ErrorRate:.0003,ContactProbability:.02,FailureRate:.00003,ComponentWearPerSecond:.00001,InterruptMajorIncidents:true));
string Enc<T>(T value)=>JsonSerializer.Serialize(value,json);
Dictionary<string,string> ToolSources()=>new[]{"Program.cs","RacePackageEvidence.csproj","evidence.test.mjs"}.ToDictionary(n=>n,n=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(File.ReadAllText(Path.Combine("tools","RacePackageEvidence",n)).Replace("\r\n","\n")))));
if(args.Length==2&&args[0]=="--replay")
{
 using var file=File.OpenRead(args[1]);using var gzip=new GZipStream(file,CompressionMode.Decompress);
 var saved=JsonSerializer.Deserialize<Archive>(gzip,json)??throw new Exception("Missing archive.");
 if(saved.Schema!=schema||saved.PolicyVersion!=policy||saved.ProductCommit!=product||Enc(saved.Rules)!=Enc(Rules())
  ||saved.Seeds.Length is not(1 or 8)||!saved.Seeds.SequenceEqual(Enumerable.Range(101,saved.Seeds.Length))||saved.Runs.Length!=saved.Seeds.Length*24
  ||Enc(saved.ToolSources)!=Enc(ToolSources())||saved.Controls!=PermanentDevelopment.DefaultControls||saved.Risk!=DevelopmentRiskControls.Default||saved.Income!=SeasonIncome.Default)
  throw new Exception("Unsupported, incomplete or reinterpreted archive contract.");
 var i=0;
 foreach(var rebuilt in Experiment(saved.Pool,saved.Catalogue,saved.Templates,saved.Seeds,saved.Income,saved.Controls,saved.Risk,saved.Rules))
 {
  if(Enc(rebuilt)!=Enc(saved.Runs[i]))throw new Exception($"Frozen public replay differs at row {i}: {rebuilt.Scope}/{rebuilt.Seed}/{rebuilt.PersonnelPolicy}/{rebuilt.Arm}.");
  i++;
 }
 if(i!=saved.Runs.Length)throw new Exception("Missing matrix rows.");
 Console.WriteLine($"Exact replay: {i} logical runs, {saved.Runs.Sum(r=>r.Events.Length)} event-races; {computedRaces} actual race computations, complete frozen public evidence, no catalogue reads.");return;
}
var pilot=args.Length==2&&args[0]=="--pilot";
if(!pilot&&args.Length!=1)throw new ArgumentException("Pass new output directory, --pilot directory, or --replay archive.json.gz.");
var output=args[^1];
if(Directory.Exists(output)&&Directory.EnumerateFileSystemEntries(output).Any())throw new ArgumentException("Refusing to overwrite evidence.");
Directory.CreateDirectory(output);
var pool=PersonnelCatalog.Open();var catalogue=ConstructionCatalog.Open();var templates=new[]{WeekendPresetCatalog.Open("track-monza"),WeekendPresetCatalog.Open("track-monaco")};
var seeds=Enumerable.Range(101,pilot?1:8).ToArray();var rules=Rules();
var sources=ToolSources();
var runs=Experiment(pool,catalogue,templates,seeds,SeasonIncome.Default,PermanentDevelopment.DefaultControls,DevelopmentRiskControls.Default,rules).ToArray();
var archive=new Archive(schema,policy,product,sources,rules,seeds,pool,catalogue,templates,SeasonIncome.Default,PermanentDevelopment.DefaultControls,DevelopmentRiskControls.Default,runs);
var path=Path.Combine(output,"race-packages.json.gz");
using(var file=File.Create(path))using(var gzip=new GZipStream(file,CompressionLevel.SmallestSize))JsonSerializer.Serialize(gzip,archive,json);
PublishSummary(runs,output);
File.WriteAllText(Path.Combine(output,"manifest.json"),JsonSerializer.Serialize(new{archive.Schema,archive.PolicyVersion,archive.ProductCommit,archive.ToolSources,seeds,scopes,arms,personnelPolicies,logicalRuns=runs.Length,logicalEventRaces=runs.Sum(r=>r.Events.Length),actualRaceComputations=computedRaces,laps=6,sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))},new JsonSerializerOptions(json){WriteIndented=true}));
Console.WriteLine($"Published {runs.Length} logical runs, {runs.Sum(r=>r.Events.Length)} event-races, {computedRaces} actual race computations; every pending/rejected/retired row retained.");

IEnumerable<Run> Experiment(PersonnelPool frozen,ConstructionCatalogue menu,WeekendPreset[] source,int[] experimentSeeds,SeasonIncomeConfiguration income,DevelopmentOutcomeControls controls,DevelopmentRiskControls risk,RuleSnapshot rules)
{
 PersonnelAcquisition.ValidatePool(frozen,11);CarConstruction.ValidateCatalogue(menu);SeasonIncome.ValidateConfiguration(income);PermanentDevelopment.ValidateControls(controls);risk.Validate();
 if(source.Length!=2||source[0].Circuit.Id!="track-monza"||source[1].Circuit.Id!="track-monaco")throw new Exception("Only frozen authored Monza/Monaco inputs are supported.");
 foreach(var scope in scopes)foreach(var seed in experimentSeeds)foreach(var personnelPolicy in personnelPolicies)
 {
  var calendar=Enumerable.Range(1,scope=="production-short"?2:6).Select(i=>"event-"+i).ToArray();
  var acquired=PersonnelAcquisition.Open(frozen,teams,seed);
  while(acquired.Offer is {} offer)
  {
   var legal=offer.PersonIds.Where(id=>PersonnelAcquisition.HireUnavailable(acquired,offer.TeamId,id) is null).Select(id=>frozen.People.Single(p=>p.Id==id));
   var expensive=offer.TeamId==teams[0]&&personnelPolicy=="expensive-legal";
   var person=(expensive?legal.OrderByDescending(p=>p.Price):legal.OrderBy(p=>p.Price)).ThenBy(p=>p.Id,StringComparer.Ordinal).First();
   acquired=PersonnelAcquisition.Hire(acquired,new(Command($"hire/{seed}/{personnelPolicy}/{acquired.Revision}"),acquired.Revision,offer.TeamId,person.Id));
  }
  PersonnelAcquisition.Validate(acquired);
  var construction=CarConstruction.Open(menu,teams);var state=acquired;
  foreach(var team in teams)
  {
   var bought=CarConstruction.Confirm(construction,state,new(Command($"car-buy/{seed}/{personnelPolicy}/{team}"),construction.Drafts[team].Revision,team,menu.Version));construction=bought.Construction;state=bought.Acquisition;
  }
  var baselines=source.Select(template=>teams.Select(team=>{
   var preset=template with{Cars=template.Cars.Select((c,i)=>c with{TeamId=team,EntryId=team+"-car-"+(i+1)}).ToImmutableArray()};
   var roster=PersonnelWeekendMapping.Apply(state,team,preset);var car=construction.Confirmed![team];
   return RacePackages.Migrate(ConstructedBaseCar.Apply(roster,car.Id,menu.Version,car.Ratings,car.MechanicalAxis));
  }).ToArray()).ToArray();
  var incidents=source.Select(p=>WeekendPresetCatalog.IncidentInputs(WeekendPreparation.Prepare(p,6,new()),scope=="production-short"?new IncidentTuning():rules.SyntheticIncidents).Configuration.Incidents!).ToArray();
  var empty=new DevelopmentState(PermanentDevelopment.Version,[],Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"package-evidence-development-v1/{seed}"))),[],[],[]);
  var first=MakeEvent(baselines[0],empty,0,calendar,seed,incidents[0],scope!="production-short");
  foreach(var arm in arms)
  {
   var ledger=state.Ledger;var development=empty;var events=new List<Event>();var steps=new List<Step>();
   for(var eventIndex=0;eventIndex<calendar.Length;eventIndex++)
   {
    var race=eventIndex==0?first:MakeEvent(baselines[eventIndex%2],development,eventIndex,calendar,seed+eventIndex*10000,incidents[eventIndex%2],scope!="production-short");
    events.Add(race);ledger=SeasonIncome.Grant(ledger,teams,calendar[eventIndex],income);
    if(eventIndex==calendar.Length-1)continue;
    var op=PermanentDevelopment.Open("op-"+(eventIndex+1),calendar[eventIndex],calendar[eventIndex+1],teams);
    var library=new[]{RacePackages.Baseline}.Concat(development.PackageOutcomes.Select(o=>o.Package).OfType<RacePackageVersion>()).ToArray();
    var planned=Plan(arm,eventIndex,library);var before=ledger.Where(e=>e.TeamId==teams[0]).Sum(e=>e.Credits);string? failure=planned.Reason;int? attemptedCost=null;string? attemptedQuote=null;
    foreach(var team in teams)
    {
     var roster=state.Hires.Where(h=>h.TeamId==team).Select(h=>frozen.People.Single(p=>p.Id==h.PersonId)).ToArray();
     var chosen=team==teams[0]?planned:new PlanChoice("Save",null,null,null,false,null);
     var staff=DevelopmentPersonnelMapping.Map(roster,frozen.Version,chosen.OptionId??"speed");
     if(chosen.Action!="Save")
     {
      try
      {
       var quote=chosen.Action=="PermanentRefinement"?PermanentDevelopment.Quote(chosen.OptionId!,staff,controls,risk):null;
       var package=chosen.Action=="PackageCreation"?PackageDevelopment.QuoteRich(chosen.OptionId!,staff,risk):chosen.Action=="PackageRefinement"?PackageDevelopment.QuoteRefinement(library.Single(p=>p.PackageId==chosen.Package!.PackageId&&p.Version==chosen.Package.Version),chosen.Refinement!,staff,risk,library.Where(p=>p.PackageId==chosen.Package!.PackageId).Max(p=>p.Version)+1):null;
       if(chosen.Rush)package=PackageDevelopment.Rush(package!);
       attemptedCost=quote?.Cost??package!.Cost;attemptedQuote=quote?.Id??package!.Id;
       op=PermanentDevelopment.Edit(op,team,new(Command($"draft/{seed}/{eventIndex}/{team}"),op.Id,op.Revision,chosen.Action,chosen.OptionId,attemptedQuote,Package:chosen.Package,Refinement:chosen.Refinement,Rush:chosen.Rush),staff,controls,risk,library);
       var paid=PermanentDevelopment.Commit(op,ledger,team,new(Command($"commit/{seed}/{eventIndex}/{team}"),op.Id,op.Revision,op.Drafts[team].Revision,attemptedQuote),staff);op=paid.State;ledger=paid.Ledger;continue;
      }
      catch(Exception error) when(error is InvalidOperationException or ArgumentException){failure=error.Message;}
     }
     op=PermanentDevelopment.Edit(op,team,new(Command($"save-draft/{seed}/{eventIndex}/{team}"),op.Id,op.Revision,"Save",null,null),staff,controls,risk,library);
     var saved=PermanentDevelopment.Commit(op,ledger,team,new(Command($"save-commit/{seed}/{eventIndex}/{team}"),op.Id,op.Revision,op.Drafts[team].Revision,null),staff);op=saved.State;ledger=saved.Ledger;
    }
    development=PermanentDevelopment.Close(development with{Opportunities=development.Opportunities.Add(op)},op.Id,calendar);
    steps.Add(new(eventIndex+1,planned,attemptedQuote,attemptedCost,failure,before,ledger.Where(e=>e.TeamId==teams[0]).Sum(e=>e.Credits),library.ToImmutableArray()));
   }
   if(ledger.GroupBy(e=>e.TeamId).Any(g=>g.Sum(e=>e.Credits)<0))throw new Exception("Negative balance.");
   yield return new(scope,seed,personnelPolicy,arm,scope=="production-short"&&arm is "large-library" or "refinement"?"small-library":null,calendar,state,construction,ledger,development,steps.ToArray(),events.ToArray());
  }
  Console.WriteLine($"Completed {scope}/{seed}/{personnelPolicy} (six paired arms).");
 }
}
PlanChoice Plan(string arm,int opportunity,IReadOnlyList<RacePackageVersion> library)
{
 if(arm=="Save"||arm=="small-library"&&opportunity>=2)return new("Save",null,null,null,false,null);
 if(arm=="permanent")return new("PermanentRefinement",opportunity%2==0?"handling":"speed",null,null,false,null);
 if(arm=="refinement"&&opportunity>0)
 {
  var source=library.Where(p=>p.PackageId=="op-1/team-01/package"&&p.Completed).OrderByDescending(p=>p.Version).FirstOrDefault();
  if(source is null)return new("Save",null,null,null,false,"No completed owned refinement parent; explicit Save.");
  string[] options=["strengthen","reduce-compromise","broaden-usefulness"];
  return new("PackageRefinement","downforce",new(source.PackageId,source.Version),options[(opportunity-1)%3],false,null);
 }
 string[] templates=["downforce","low-drag","tyre-management","mechanical-grip","cooling-reliability"];
 return new("PackageCreation",templates[opportunity],null,null,arm=="Rush",null);
}
Event MakeEvent(WeekendPreset[] baseline,DevelopmentState development,int eventIndex,string[] calendar,int seed,IncidentTuning incidents,bool disruptions)
{
 var eligible=development.Modifiers.Where(m=>Array.IndexOf(calendar,m.CompletionEventId)<=eventIndex);
 var completed=development.PackageOutcomes.Where(o=>o.Package is {Completed:true}&&o.CompletionEventId is not null&&Array.IndexOf(calendar,o.CompletionEventId)<=eventIndex);
 var observations=new List<RecordedPackageSelection>();
 var selected=baseline.Select(p=>{
  var available=RacePackages.Complete(DevelopedCar.Apply(p,eligible),completed);
  var observation=new PackageSelectionObservation(available.Circuit.Id,available.Circuit.Demands,available.Cars[0].Car.Ratings,available.PackageLibrary,incidents.FailureRate>0);
  var choice=PackageSelectionDecisionPolicy.Evaluate(observation);observations.Add(new("evidence-fixed-policy",observation,choice,choice.Selection));
  return RacePackages.Select(available,choice.Selection);
 }).ToArray();
 return Race(selected,seed,incidents,disruptions) with{Selections=observations.ToArray()};
}
void PublishSummary(Run[] runs,string output)
{
 var pairs=runs.Select(r=>{
  var baseline=runs.Single(s=>s.Scope==r.Scope&&s.Seed==r.Seed&&s.PersonnelPolicy==r.PersonnelPolicy&&s.Arm=="Save");
  var tables=Enumerable.Range(1,r.Events.Length).Select(n=>Table(r.Events.Take(n))).ToArray();var control=Table(baseline.Events);
  var leader=tables[0].Constructors[0].Id;var leadPoints=tables[0].Constructors[0].Points;
  var seconds=r.Events.Select((e,i)=>Mean(e)-Mean(baseline.Events[i])).ToArray();
  var outcomes=r.Development.Outcomes.Select(o=>o.Kind).Concat(r.Development.PackageOutcomes.Select(o=>o.Kind)).ToArray();
  var ownPackages=r.Development.PackageOutcomes.Where(o=>o.TeamId==teams[0]&&o.Package is {Completed:true}).ToArray();
  var pending=r.Development.Outcomes.Count(o=>!r.Development.Modifiers.Any(m=>m.ProjectId==o.ProjectId))+r.Development.PackageOutcomes.Count(o=>o.Package is not {Completed:true});
  var balance=r.Ledger.Where(e=>e.TeamId==teams[0]).Sum(e=>e.Credits);var spending=-r.Ledger.Where(e=>e.TeamId==teams[0]&&e.Reason is "permanent-development" or "race-package-development").Sum(e=>e.Credits);
  var selections=r.Events.Select(e=>e.Selections![0].Selection).ToArray();
  return new Pair(r.Scope,r.Seed,r.PersonnelPolicy,r.Arm,r.AliasOf,r.Acquisition.Ledger.Where(e=>e.TeamId==teams[0]).Sum(e=>e.Credits),balance,spending,r.Steps.Count(s=>s.AttemptedCost.HasValue),r.Steps.Count(s=>s.Failure is not null),r.Steps.Select(s=>s.Failure).Where(s=>s is not null).ToArray(),
   r.Development.Opportunities.Count(o=>o.Decisions.Single(d=>d.TeamId==teams[0]).ActionKind=="Save"),ownPackages.Select(o=>o.Package!.PackageId).Distinct().Count(),ownPackages.Length,
   new[]{"normal","partial","breakthrough","compromised","delay"}.ToDictionary(k=>k,k=>outcomes.Count(o=>o==k)),pending,
   r.Events.Sum(e=>e.Classification.Count(c=>c.RetirementTime.HasValue)),r.Events.Sum(e=>e.Classification.Count(c=>c.DriverId.StartsWith(teams[0]+"-",StringComparison.Ordinal)&&c.RetirementTime.HasValue)),seconds,seconds.All(s=>s.HasValue)?seconds.Sum(s=>s!.Value):null,
   tables.Select(t=>Margin(t,teams[0])).ToArray(),Margin(tables[^1],teams[0])-Margin(control,teams[0]),leader,tables[0].Constructors.Where(c=>c.Points==leadPoints).Select(c=>c.Id).ToArray(),tables.Select(t=>Margin(t,leader)).ToArray(),Margin(tables[^1],leader)-Margin(control,leader),tables[^1].Constructors.Single(c=>c.Id==teams[0]).Points,selections);
 }).ToArray();
 var summary=new{
  schema="race-package-summary-v1",archiveSchema=schema,policyVersion=policy,seeds=runs.Select(r=>r.Seed).Distinct(),logicalRuns=runs.Length,logicalEventRaces=runs.Sum(r=>r.Events.Length),
  limits="Production-short is the supported two-event shape with inactive incidents. Its small/large/refinement arms share the same first creation and are observational aliases. Synthetic-alternating-six is a separately authored repeated Monza/Monaco design experiment with existing opt-in incident coefficients/routes, not a production/full-calendar campaign. Pair only within same scope/resources/seed. Eight paired seeds are exploratory, not safety estimates. Higher staff price is not strength; own acquisition can change later market/opponent rosters. No injected credits, free purchased packages, forced outcomes, optimal strategy, runaway safety, human understanding or full-calendar claim. #74 remains unmeasured; S16 owns validated full-calendar release evidence.",
  groups=pairs.GroupBy(p=>(p.Scope,p.PersonnelPolicy,p.Arm)).Select(g=>new{g.Key.Scope,g.Key.PersonnelPolicy,g.Key.Arm,n=g.Count(),attempts=g.Sum(p=>p.Attempts),failures=g.Sum(p=>p.Failures),balance=Stats(g.Select(p=>(double)p.FinalBalance)),spending=Stats(g.Select(p=>(double)p.DevelopmentSpending)),completedPackages=Stats(g.Select(p=>(double)p.CompletedPackages)),completedVersions=Stats(g.Select(p=>(double)p.CompletedVersions)),pending=g.Sum(p=>p.Pending),outcomes=new[]{"normal","partial","breakthrough","compromised","delay"}.ToDictionary(k=>k,k=>g.Sum(p=>p.Outcomes[k])),retirements=g.Sum(p=>p.AllRetirements),targetRetirements=g.Sum(p=>p.TargetRetirements),pairedTotalSeconds=Stats(g.Where(p=>p.TotalSecondsVsSave.HasValue).Select(p=>p.TotalSecondsVsSave!.Value)),nonComparable=g.Count(p=>!p.TotalSecondsVsSave.HasValue),faster=g.Count(p=>p.TotalSecondsVsSave<0),slower=g.Count(p=>p.TotalSecondsVsSave>0),unchanged=g.Count(p=>p.TotalSecondsVsSave==0),targetFinalPointsMarginDeltaVsSave=Stats(g.Select(p=>(double)p.TargetFinalMarginDeltaVsSave)),actualOriginalLeaderMarginDeltaVsSave=Stats(g.Select(p=>(double)p.OriginalLeaderFinalMarginDeltaVsSave)),targetStartingGroups=new[]{"leading","trailing","tied"}.Select(status=>new{status,n=g.Count(p=>(p.TargetMargins[0]>0?"leading":p.TargetMargins[0]<0?"trailing":"tied")==status)})}),
  hypotheses=new[]{"Larger libraries need not beat small libraries: compare complete seedwise costs, actual sizes and selected versions, including unused purchases.","Refinement and Rush need not outperform creation: retain delay, compromise, cap/affordability rejection and slower results.","A lap-time difference is not a points gain: report non-comparable retirements and actual standings margins.","Short/repeated circuits cannot establish season-long runaway safety; actual original leader and empty subgroups remain visible."},pairs};
 File.WriteAllText(Path.Combine(output,"summary.json"),JsonSerializer.Serialize(summary,new JsonSerializerOptions(json){WriteIndented=true}));
}
Event Race(WeekendPreset[] presets,int seed,IncidentTuning incidents, bool disruptions)
{
 var prepared=presets.Select(p=>WeekendPreparation.Prepare(p,6,new())).ToArray();
 computedRaces++;
 var configuration=prepared[0].Configuration with{Incidents=incidents};
 if(disruptions)configuration=configuration with{Track=configuration.Track with{PitRoute=presets[0].Circuit.Id=="track-monza"?new PitRoute(.2,38):new PitRoute(.15,32)}};
 var grid=presets.SelectMany(p=>p.Cars).Select((c,i)=>new StartGridSlot(i+1,c.EntryId)).ToArray();
 var drivers=AutomaticRaceStart.StartingDrivers(new(configuration,prepared,grid,seed,new())).Select((d,i)=>d with{Gap=i*.8}).ToArray();
 var plan=new StrategyPlan(drivers.Select(d=>new ScheduledDecision(1,d.Id,new(PitCompound:TyreCompound.Hard))).ToArray(),[]);
 var replay=SimulationReplay.Capture(configuration,drivers,plan,seed);SimulationReplay.RequireVersion(replay.Configuration,replay.ModelVersion,replay.RandomnessVersion);
 var engine=new RaceEngine(replay.Configuration,replay.Drivers,replay.Seed);PhaseResult finish;var phases=0;
 do{finish=engine.ResolvePhase(replay.Plan);if(++phases>100)throw new Exception("Race exceeded bound.");}while(!finish.Finished);
 var cars=presets.SelectMany(p=>p.Cars).ToArray();var entrants=cars.Select((c,i)=>new ChampionshipEntrant(c.Driver.Id,c.TeamId,i)).ToArray();
 var scoring=ChampionshipScoring.Score(ChampionshipScoring.Default,entrants,finish.Classification.Select(c=>new ChampionshipFinish(cars.Single(car=>car.EntryId==c.DriverId).Driver.Id,c.Position,finish.Eligibility?.GetValueOrDefault(c.DriverId)??true,c.RetirementTime.HasValue)).ToArray());
 return new(presets,prepared,replay,finish.Classification.ToArray(),scoring,phases,finish.Time,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(finish,json)))));
}
ChampionshipTable Table(IEnumerable<Event> events)
{
 var all=events.ToArray();var entrants=all[0].Awards.Select((a,i)=>new ChampionshipEntrant(a.PersonId,a.TeamId,i)).ToArray();
 return ChampionshipScoring.Rebuild(entrants,all.Select(e=>(IReadOnlyList<ChampionshipAward>)e.Awards));
}
int Margin(ChampionshipTable table,string team)=>table.Constructors.Single(t=>t.Id==team).Points-table.Constructors.Where(t=>t.Id!=team).Max(t=>t.Points);
double? Mean(Event race){var target=race.Classification.Where(c=>c.DriverId.StartsWith(teams[0]+"-",StringComparison.Ordinal)).ToArray();return target.All(c=>c.FinishTime.HasValue)?target.Average(c=>c.FinishTime!.Value):null;}
object Stats(IEnumerable<double> source){var values=source.ToArray();return new{n=values.Length,min=values.Length==0?(double?)null:values.Min(),max=values.Length==0?(double?)null:values.Max(),mean=values.Length==0?(double?)null:values.Average()};}
string Command(string purpose)=>new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(schema+"/"+purpose))[..16]).ToString("D");
record RuleSnapshot(Dictionary<string,string> Versions,ImmutableArray<DevelopmentOption> PermanentOptions,ImmutableArray<RacePackageTemplate> PackageTemplates,IncidentTuning SyntheticIncidents);
record Archive(string Schema,string PolicyVersion,string ProductCommit,Dictionary<string,string> ToolSources,RuleSnapshot Rules,int[] Seeds,PersonnelPool Pool,ConstructionCatalogue Catalogue,WeekendPreset[] Templates,SeasonIncomeConfiguration Income,DevelopmentOutcomeControls Controls,DevelopmentRiskControls Risk,Run[] Runs);
record Run(string Scope,int Seed,string PersonnelPolicy,string Arm,string? AliasOf,string[] Calendar,AcquisitionState Acquisition,ConstructionState Construction,ImmutableArray<CreditEntry> Ledger,DevelopmentState Development,Step[] Steps,Event[] Events);
record PlanChoice(string Action,string? OptionId,RacePackageSelection? Package,string? Refinement,bool Rush,string? Reason);
record Step(int OpportunityIndex,PlanChoice Planned,string? AttemptedQuoteId,int? AttemptedCost,string? Failure,int BalanceBefore,int BalanceAfter,ImmutableArray<RacePackageVersion> ObservedLibrary);
record Event(WeekendPreset[] Presets,EffectiveWeekend[] Effective,RaceReplay Replay,ClassificationEntry[] Classification,ChampionshipAward[] Awards,int Phases,double Time,string Hash,IReadOnlyList<RecordedPackageSelection>? Selections=null);
record Pair(string Scope,int Seed,string PersonnelPolicy,string Arm,string? AliasOf,int PreseasonBalance,int FinalBalance,int DevelopmentSpending,int Attempts,int Failures,string?[] FailureReasons,int SaveDecisions,int CompletedPackages,int CompletedVersions,Dictionary<string,int> Outcomes,int Pending,int AllRetirements,int TargetRetirements,double?[] EventSecondsVsSave,double? TotalSecondsVsSave,int[] TargetMargins,int TargetFinalMarginDeltaVsSave,string OriginalLeaderByStandingsTiebreak,string[] OriginalPointLeaders,int[] OriginalLeaderMargins,int OriginalLeaderFinalMarginDeltaVsSave,int TargetPoints,RacePackageSelection[] Selections);
