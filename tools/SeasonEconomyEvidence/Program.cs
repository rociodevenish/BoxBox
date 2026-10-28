using System.Collections.Immutable;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Game.Domain;
using Game.Simulation;

var json=new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase};
const string schema="short-season-economy-evidence-v1",policy="legal-offer-paired-refinements-v1";
string[] teams=Enumerable.Range(1,11).Select(i=>$"team-{i:00}").ToArray();
string[] policies=["economical","expensive-legal"],contexts=["minimum","speed","mechanical"],arms=["Save","reinforce","complement"];
if(args.Length==2&&args[0]=="--replay")
{
 using var file=File.OpenRead(args[1]);using var gzip=new GZipStream(file,CompressionMode.Decompress);
 var saved=JsonSerializer.Deserialize<Archive>(gzip,json)??throw new Exception("Missing archive.");
 if(saved.Schema!=schema||saved.PolicyVersion!=policy||saved.OutcomeVersion!=PermanentDevelopment.OutcomeVersion||saved.DevelopmentVersion!=PermanentDevelopment.Version||saved.StaffMappingVersion!=DevelopmentPersonnelMapping.Version||saved.EffectiveVersion!=WeekendPreparation.ModelVersion||saved.CardVersion!=WeekendPreparation.CardVersion||saved.ScoringVersion!=ChampionshipScoring.Version||!saved.Calendar.SequenceEqual(new[]{"track-monza","track-monaco"})||saved.Seeds.Length is not(1 or 24)||!saved.Seeds.SequenceEqual(Enumerable.Range(101,saved.Seeds.Length))||saved.Runs.Length!=saved.Seeds.Length*18)throw new Exception("Unsupported/incomplete archive.");
 var rebuilt=Experiment(saved.Pool,saved.Catalogue,saved.Templates,saved.Seeds,saved.Income,saved.Controls,saved.IncidentControls);
 if(JsonSerializer.Serialize(rebuilt,json)!=JsonSerializer.Serialize(saved.Runs,json))throw new Exception("Frozen public ledger, source, quote, outcome, modifier or race evidence changed.");
 Console.WriteLine($"Exact replay: {saved.Runs.Length} two-event counterfactuals, all failures retained, no catalogue reads.");return;
}
var pilot=args.Length==2&&args[0]=="--pilot";
if(!pilot&&args.Length!=1)throw new ArgumentException("Pass new output directory, --pilot directory, or --replay archive.json.gz.");
var output=args[^1];
if(Directory.Exists(output)&&Directory.EnumerateFileSystemEntries(output).Any())throw new ArgumentException("Refusing to overwrite evidence.");
Directory.CreateDirectory(output);
var pool=PersonnelCatalog.Open();var catalogue=ConstructionCatalog.Open();
var templates=new[]{WeekendPresetCatalog.Open("track-monza"),WeekendPresetCatalog.Open("track-monaco")};
var seeds=Enumerable.Range(101,pilot?1:24).ToArray();
var incidents=templates.Select(p=>WeekendPresetCatalog.IncidentInputs(WeekendPreparation.Prepare(p,6,new()),new IncidentTuning()).Configuration.Incidents!).ToArray();
var runs=Experiment(pool,catalogue,templates,seeds,SeasonIncome.Default,PermanentDevelopment.DefaultControls,incidents);
var archive=new Archive(schema,policy,PermanentDevelopment.Version,PermanentDevelopment.OutcomeVersion,DevelopmentPersonnelMapping.Version,WeekendPreparation.ModelVersion,WeekendPreparation.CardVersion,ChampionshipScoring.Version,seeds,["track-monza","track-monaco"],pool,catalogue,templates,SeasonIncome.Default,PermanentDevelopment.DefaultControls,incidents,runs);
using(var file=File.Create(Path.Combine(output,"season-economy.json.gz")))using(var gzip=new GZipStream(file,CompressionLevel.SmallestSize))JsonSerializer.Serialize(gzip,archive,json);
var pairs=runs.Select(r=>{
 var baseline=runs.Single(s=>s.Seed==r.Seed&&s.PersonnelPolicy==r.PersonnelPolicy&&s.Context==r.Context&&s.Arm=="Save");
 var before=Table(r.Events.Take(1));var after=Table(r.Events);var control=Table(baseline.Events);
 var lead=before.Constructors[0].Id;
 return new Pair(r.Seed,r.PersonnelPolicy,r.Context,r.Arm,r.BuildFailure,r.InvestmentFailure,r.PreseasonBalance,r.Ledger.Where(e=>e.TeamId==teams[0]).Sum(e=>e.Credits),r.Quote?.Cost,r.Development.Outcomes.FirstOrDefault()?.Kind??"Save",r.Development.Modifiers.Length,r.Events[1].Classification.Count(c=>c.DriverId.StartsWith(teams[0]+"-",StringComparison.Ordinal)&&c.RetirementTime.HasValue),Mean(r.Events[1])-Mean(baseline.Events[1]),Margin(before,teams[0]),Margin(after,teams[0]),Margin(after,teams[0])-Margin(control,teams[0]),lead,Margin(after,lead),Margin(after,lead)-Margin(control,lead),after.Constructors.Single(t=>t.Id==teams[0]).Points);
}).ToArray();
var summary=new{
 schema="short-season-economy-summary-v1",archiveSchema=archive.Schema,seeds,calendar=archive.Calendar,scenarios=runs.Length,
 limits="Synthetic two-event/one-opportunity experiments only. No human workload, understanding, full-calendar stacking or release-balance finding. Price is not roster strength. Target hiring can change later offers/opponent roster; philosophy build prices may differ. Unaffordable builds explicitly retain the minimum car; rejected investments explicitly commit Save, never receive free credits. Final-event delay cannot complete in this calendar.",
 groups=pairs.GroupBy(p=>(p.PersonnelPolicy,p.Context,p.Arm)).Select(g=>new{g.Key.PersonnelPolicy,g.Key.Context,g.Key.Arm,n=g.Count(),buildFailures=g.Count(p=>p.BuildFailure is not null),investmentFailures=g.Count(p=>p.InvestmentFailure is not null),preseason=Stats(g.Select(p=>(double)p.PreseasonBalance)),finalBalance=Stats(g.Select(p=>(double)p.FinalBalance)),outcomes=g.GroupBy(p=>p.Outcome).ToDictionary(x=>x.Key,x=>x.Count()),pairedSeconds=Stats(g.Where(p=>p.SecondsVsSave.HasValue).Select(p=>p.SecondsVsSave!.Value)),nonComparableFinishes=g.Count(p=>!p.SecondsVsSave.HasValue),retirements=g.Sum(p=>p.Retirements),earlyLeadAmplificationVsSave=Stats(g.Select(p=>(double)p.TargetMarginDeltaVsSave)),leaderSubgroup=g.GroupBy(p=>p.TargetMarginAfterFirst>0?"leading":p.TargetMarginAfterFirst<0?"trailing":"tied").Select(s=>new{status=s.Key,n=s.Count(),pointsMarginDeltaVsSave=Stats(s.Select(p=>(double)p.TargetMarginDeltaVsSave))}),faster=g.Count(p=>p.SecondsVsSave<0),slower=g.Count(p=>p.SecondsVsSave>0),tied=g.Count(p=>p.SecondsVsSave==0)}),
 hypotheses=new[]{"Expensive legal acquisition can leave less room for development: inspect rejected investment and retained credit rows; reject/inconclusive when data disagree.","Investment need not improve the final weekend: retain partial, delay, compromise, retirements and slower paired rows.","Reinforcement need not outperform complementary refinement at Monaco: compare complete seedwise rows, not a selected success.","One short opportunity cannot settle season-long runaway risk: report actual lead/margin changes and leave full-calendar claim inconclusive."},pairs};
File.WriteAllText(Path.Combine(output,"summary.json"),JsonSerializer.Serialize(summary,new JsonSerializerOptions(json){WriteIndented=true}));
File.WriteAllText(Path.Combine(output,"manifest.json"),JsonSerializer.Serialize(new{archive.Schema,archive.PolicyVersion,seeds,archive.Calendar,scenarios=runs.Length,sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(output,"season-economy.json.gz"))))},new JsonSerializerOptions(json){WriteIndented=true}));
Console.WriteLine($"Published {runs.Length} two-event counterfactuals ({seeds.Length} paired seeds); failed hypotheses and pending final delays retained.");

Run[] Experiment(PersonnelPool frozen,ConstructionCatalogue menu,WeekendPreset[] source,int[] experimentSeeds,SeasonIncomeConfiguration income,DevelopmentOutcomeControls controls,IncidentTuning[] incidents)
{
 PersonnelAcquisition.ValidatePool(frozen,11);CarConstruction.ValidateCatalogue(menu);SeasonIncome.ValidateConfiguration(income);PermanentDevelopment.ValidateControls(controls);
 if(source.Length!=2||source[0].Circuit.Id!="track-monza"||source[1].Circuit.Id!="track-monaco")throw new Exception("Only authored short calendar supported.");
 var result=new List<Run>();
 foreach(var seed in experimentSeeds)foreach(var personnelPolicy in policies)
 {
  var acquired=PersonnelAcquisition.Open(frozen,teams,seed);
  while(acquired.Offer is {} offer)
  {
   var legal=offer.PersonIds.Where(id=>PersonnelAcquisition.HireUnavailable(acquired,offer.TeamId,id) is null).Select(id=>frozen.People.Single(p=>p.Id==id));
   var expensive=offer.TeamId==teams[0]&&personnelPolicy=="expensive-legal";
   var person=(expensive?legal.OrderByDescending(p=>p.Price):legal.OrderBy(p=>p.Price)).ThenBy(p=>p.Id,StringComparer.Ordinal).First();
   acquired=PersonnelAcquisition.Hire(acquired,new(Command($"hire/{seed}/{personnelPolicy}/{acquired.Revision}"),acquired.Revision,offer.TeamId,person.Id));
  }
  PersonnelAcquisition.Validate(acquired);
  foreach(var context in contexts)
  {
   var construction=CarConstruction.Open(menu,teams);var state=acquired;string? buildFailure=null;
   foreach(var team in teams)
   {
    if(team==teams[0]&&context!="minimum")
    {
     ImmutableArray<string> choices=context=="speed"?["car-aero-low-drag-efficiency","car-chassis-balanced","car-power-top-end","car-thermal-low-drag"]:["car-aero-high-downforce","car-chassis-responsive","car-power-deployment","car-thermal-balanced"];
     try{construction=CarConstruction.Edit(construction,new(Command($"car-edit/{seed}/{personnelPolicy}/{context}"),1,team,menu.Version,choices),PersonnelAcquisition.Balance(state,team));}
     catch(InvalidOperationException error){buildFailure=error.Message;}
    }
    var bought=CarConstruction.Confirm(construction,state,new(Command($"car-buy/{seed}/{personnelPolicy}/{context}/{team}"),construction.Drafts[team].Revision,team,menu.Version));construction=bought.Construction;state=bought.Acquisition;
   }
   var presets=source.Select(template=>teams.Select(team=>{
    var preset=template with{Cars=template.Cars.Select((c,i)=>c with{TeamId=team,EntryId=team+"-car-"+(i+1)}).ToImmutableArray()};
    var roster=PersonnelWeekendMapping.Apply(state,team,preset);var car=construction.Confirmed![team];
    return ConstructedBaseCar.Apply(roster,car.Id,menu.Version,car.Ratings,car.MechanicalAxis);
   }).ToArray()).ToArray();
   var first=Race(presets[0],seed,incidents[0]);var initialLedger=SeasonIncome.Grant(state.Ledger,teams,"event-1",income);
   var targetRoster=state.Hires.Where(h=>h.TeamId==teams[0]).Select(h=>frozen.People.Single(p=>p.Id==h.PersonId)).ToArray();
   foreach(var arm in arms)
   {
    var op=PermanentDevelopment.Open("op-1","event-1","event-2",teams);var ledger=initialLedger;DevelopmentQuote? quote=null;string? investmentFailure=null;
    foreach(var team in teams)
    {
     DevelopmentPersonnel staff=new("preset-development-neutral-v1",0,0,0,0);
     if(team==teams[0]&&arm!="Save")
     {
      var option=(context=="mechanical")==(arm=="reinforce")?"handling":"speed";
      staff=DevelopmentPersonnelMapping.Map(targetRoster,frozen.Version,option);quote=PermanentDevelopment.Quote(option,staff,controls);
      op=PermanentDevelopment.Edit(op,team,new(Command($"draft/{seed}/{team}"),op.Id,op.Revision,"PermanentRefinement",option,quote.Id),staff,controls);
      try{var paid=PermanentDevelopment.Commit(op,ledger,team,new(Command($"commit/{seed}/{team}"),op.Id,op.Revision,op.Drafts[team].Revision,quote.Id),staff);op=paid.State;ledger=paid.Ledger;continue;}
      catch(InvalidOperationException error){investmentFailure=error.Message;}
     }
     op=PermanentDevelopment.Edit(op,team,new(Command($"save-draft/{seed}/{team}"),op.Id,op.Revision,"Save",null,null),staff,controls);
     var saved=PermanentDevelopment.Commit(op,ledger,team,new(Command($"save-commit/{seed}/{team}"),op.Id,op.Revision,op.Drafts[team].Revision,null),staff);op=saved.State;ledger=saved.Ledger;
    }
    var entropy=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"economy-development-channel-v1/{seed}")));
    var development=PermanentDevelopment.Close(new(PermanentDevelopment.Version,[op],entropy),op.Id,["event-1","event-2"]);
    var secondPresets=presets[1].Select(p=>DevelopedCar.Apply(p,development.Modifiers)).ToArray();
    var second=Race(secondPresets,seed+10000,incidents[1]);ledger=SeasonIncome.Grant(ledger,teams,"event-2",income);
    if(ledger.GroupBy(e=>e.TeamId).Any(g=>g.Sum(e=>e.Credits)<0))throw new Exception("Negative balance.");
    result.Add(new(seed,personnelPolicy,context,arm,buildFailure,investmentFailure,PersonnelAcquisition.Balance(state,teams[0]),state,construction,quote,development,ledger,[presets[0],secondPresets],[first,second]));
   }
  }
 }
 return result.ToArray();
}
Event Race(WeekendPreset[] presets,int seed,IncidentTuning incidents)
{
 var prepared=presets.Select(p=>WeekendPreparation.Prepare(p,6,new())).ToArray();
 var configuration=prepared[0].Configuration with{Incidents=incidents};
 var grid=presets.SelectMany(p=>p.Cars).Select((c,i)=>new StartGridSlot(i+1,c.EntryId)).ToArray();
 var drivers=AutomaticRaceStart.StartingDrivers(new(configuration,prepared,grid,seed,new())).Select((d,i)=>d with{Gap=i*.8}).ToArray();
 var plan=new StrategyPlan(drivers.Select(d=>new ScheduledDecision(1,d.Id,new(PitCompound:TyreCompound.Hard))).ToArray(),[]);
 var replay=SimulationReplay.Capture(configuration,drivers,plan,seed);SimulationReplay.RequireVersion(replay.Configuration,replay.ModelVersion,replay.RandomnessVersion);
 var engine=new RaceEngine(replay.Configuration,replay.Drivers,replay.Seed);PhaseResult finish;var phases=0;
 do{finish=engine.ResolvePhase(replay.Plan);if(++phases>100)throw new Exception("Race exceeded bound.");}while(!finish.Finished);
 var cars=presets.SelectMany(p=>p.Cars).ToArray();var entrants=cars.Select((c,i)=>new ChampionshipEntrant(c.Driver.Id,c.TeamId,i)).ToArray();
 var scoring=ChampionshipScoring.Score(ChampionshipScoring.Default,entrants,finish.Classification.Select(c=>new ChampionshipFinish(cars.Single(car=>car.EntryId==c.DriverId).Driver.Id,c.Position,finish.Eligibility?.GetValueOrDefault(c.DriverId)??true,c.RetirementTime.HasValue)).ToArray());
 return new(replay,finish.Classification.ToArray(),scoring,phases,finish.Time,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(finish,json)))));
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
record Archive(string Schema,string PolicyVersion,string DevelopmentVersion,string OutcomeVersion,string StaffMappingVersion,string EffectiveVersion,string CardVersion,string ScoringVersion,int[] Seeds,string[] Calendar,PersonnelPool Pool,ConstructionCatalogue Catalogue,WeekendPreset[] Templates,SeasonIncomeConfiguration Income,DevelopmentOutcomeControls Controls,IncidentTuning[] IncidentControls,Run[] Runs);
record Run(int Seed,string PersonnelPolicy,string Context,string Arm,string? BuildFailure,string? InvestmentFailure,int PreseasonBalance,AcquisitionState Acquisition,ConstructionState Construction,DevelopmentQuote? Quote,DevelopmentState Development,ImmutableArray<CreditEntry> Ledger,WeekendPreset[][] Presets,Event[] Events);
record Event(RaceReplay Replay,ClassificationEntry[] Classification,ChampionshipAward[] Awards,int Phases,double Time,string Hash);
record Pair(int Seed,string PersonnelPolicy,string Context,string Arm,string? BuildFailure,string? InvestmentFailure,int PreseasonBalance,int FinalBalance,int? QuotedCost,string Outcome,int Modifiers,int Retirements,double? SecondsVsSave,int TargetMarginAfterFirst,int TargetMarginAfterFinal,int TargetMarginDeltaVsSave,string OriginalLeader,int OriginalLeaderFinalMargin,int OriginalLeaderMarginDeltaVsSave,int FinalTargetPoints);
