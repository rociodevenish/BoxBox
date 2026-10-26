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
 var saved=JsonSerializer.Deserialize<Archive>(gzip,json)??throw new Exception("Missing archive.");
 Verify(saved);Console.WriteLine("Verified frozen content, 48 schedules, all observed offers/actions, debits and completion summaries without catalog access.");return;
}
if(args.Length!=1)throw new ArgumentException("Pass a new/empty output directory or --replay acquisition.json.gz.");
if(Directory.Exists(args[0])&&Directory.EnumerateFileSystemEntries(args[0]).Any())throw new ArgumentException("Output must be new or empty.");
Directory.CreateDirectory(args[0]);
var pool=PersonnelCatalog.Open();var runs=new List<Run>();
foreach(var seed in Seeds())foreach(var policy in Policies())foreach(var compensation in new[]{0,1})runs.Add(Experiment(pool,seed,policy,compensation));
var archive=new Archive("acquisition-evidence-v1",PersonnelAcquisition.Version,PersonnelAcquisition.ReserveVersion,"legal-offer-experiments-v1",pool,runs.ToArray());
Verify(archive);
using(var file=File.Create(Path.Combine(args[0],"acquisition.json.gz")))using(var gzip=new GZipStream(file,CompressionLevel.SmallestSize))JsonSerializer.Serialize(gzip,archive,json);
var groups=runs.GroupBy(r=>(r.Policy,r.Compensation)).Select(g=>new{g.Key.Policy,g.Key.Compensation,runs=g.Count(),completedTeams=g.Sum(r=>r.Teams.Count(t=>t.Ready)),balances=Stats(g.SelectMany(r=>r.Teams.Select(t=>t.Balance))),scouts=g.Sum(r=>r.Actions.Count(a=>a.Kind=="scout")),blockedOffers=g.Sum(r=>r.Actions.Where(a=>a.Kind=="hire").Sum(a=>a.BlockedOffers)),minReserveMargin=g.Min(r=>r.Actions.Where(a=>a.Kind=="hire").Min(a=>a.Balance-pool.People.Single(p=>p.Id==a.PersonId).Price-a.ReserveAfterHire[a.PersonId!])),categoryPriority=PersonnelAcquisition.RequiredSlots.Select(slot=>new{slot,positions=Enumerable.Range(0,11).Select(position=>new{position=position+1,paidPrice=Stats(g.Select(r=>{var pick=r.Schedule.Where(p=>p.Slot==slot).ElementAt(position);var action=r.Actions.Single(a=>a.Kind=="hire"&&a.TeamId==pick.TeamId&&a.Slot==slot);return pool.People.Single(p=>p.Id==action.PersonId).Price;}))}).ToArray()}).ToArray(),priority=Enumerable.Range(0,11).Select(position=>new{position=position+1,balance=Stats(g.SelectMany(r=>r.Teams.Where(t=>t.InitialPosition==position+1).Select(t=>t.Balance)))}).ToArray()}).ToArray();
var paired=runs.Where(r=>r.Compensation==0).Select(before=>{var after=runs.Single(r=>r.Seed==before.Seed&&r.Policy==before.Policy&&r.Compensation==1);return new{before.Seed,before.Policy,teamDeltas=before.Teams.Select(t=>new{t.TeamId,t.InitialPosition,balanceDelta=after.Teams.Single(a=>a.TeamId==t.TeamId).Balance-t.Balance,scoutDelta=after.Teams.Single(a=>a.TeamId==t.TeamId).ScoutsUsed-t.ScoutsUsed}).ToArray()};}).ToArray();
File.WriteAllText(Path.Combine(args[0],"summary.json"),JsonSerializer.Serialize(new{schema="acquisition-summary-v1",seeds=Seeds(),groups,paired,limits="Synthetic acquisition only. Fixed provisional prices and remaining credits are not roster strength, market prices, race outcomes or fairness proof. Eight seeds, three deterministic policies; priority comparisons descriptive and inconclusive. Mixed-policy confounding remains. Compensation may change draws for later teams. No human preference or campaign/economy outcome measured."},new JsonSerializerOptions(json){WriteIndented=true}));
Console.WriteLine($"Saved {runs.Count} complete runs / {runs.Sum(r=>r.Teams.Length)} ready teams / {runs.Sum(r=>r.Actions.Count(a=>a.Kind=="hire"))} unique hires; paired compensation 0 versus default 1.");

int[] Seeds()=>[-1,0,1,7,42,72,123,2026];
string[] Policies()=>["low-cost","high-cost-legal","mixed-scout"];
object Stats(IEnumerable<int> values){var v=values.ToArray();var mean=v.Average();return new{n=v.Length,min=v.Min(),max=v.Max(),range=v.Max()-v.Min(),mean,standardDeviation=Math.Sqrt(v.Average(x=>Math.Pow(x-mean,2)))};}
Run Experiment(PersonnelPool frozen,int seed,string policy,int compensation)
{
 if(!Policies().Contains(policy)||compensation is <0 or >1)throw new Exception("Unsupported experiment policy.");
 var teams=Enumerable.Range(1,11).Select(i=>$"team-{i:00}").ToArray();
 var state=PersonnelAcquisition.Open(frozen,teams,seed,new("bounded-scouts-v1",2,compensation,3));
 var order=state.Carousel!.InitialOrder.ToArray();var schedule=state.Carousel.Picks.ToArray();var actions=new List<ActionRecord>();
 while(state.Offer is {} offer)
 {
  // Policies see only their real offer and their own legal-hire previews. They cannot select future pool cards.
  var wantsScout=policy=="mixed-scout";
  if(wantsScout&&PersonnelAcquisition.ScoutUnavailable(state,offer.TeamId) is null)
  {
   var id=Command(seed,policy,compensation,state.Revision);
   actions.Add(Observe("scout",null,id,state));state=PersonnelAcquisition.Scout(state,new(id,state.Revision,offer.TeamId));
  }
  offer=state.Offer!;
  var legal=offer.PersonIds.Where(id=>PersonnelAcquisition.HireUnavailable(state,offer.TeamId,id) is null).Select(id=>frozen.People.Single(p=>p.Id==id));
  // Mixed cohort rotates policy assignment by seed across stable team identities, not initial priority.
  var expensive=policy=="high-cost-legal"||policy=="mixed-scout"&&((Array.IndexOf(teams,offer.TeamId)+(seed%3+3)%3)%3!=0);
  var selected=(expensive?legal.OrderByDescending(p=>p.Price):legal.OrderBy(p=>p.Price)).ThenBy(p=>p.Id,StringComparer.Ordinal).First();
  var command=Command(seed,policy,compensation,state.Revision);actions.Add(Observe("hire",selected.Id,command,state));
  state=PersonnelAcquisition.Hire(state,new(command,state.Revision,offer.TeamId,selected.Id));
  PersonnelAcquisition.Validate(state);
 }
 var results=teams.Select(team=>new TeamResult(team,Array.IndexOf(order,team)+1,PersonnelAcquisition.Balance(state,team),PersonnelAcquisition.RosterReady(state,team),state.Draws.Count(d=>d.TeamId==team&&d.Scout),state.ScoutTokens![team],state.Hires.Where(h=>h.TeamId==team).ToArray(),state.Ledger.Where(e=>e.TeamId==team).ToArray(),state.Hires.Where(h=>h.TeamId==team).GroupBy(h=>PersonnelAcquisition.Role(h.Slot)).ToDictionary(g=>g.Key,g=>g.Sum(h=>frozen.People.Single(p=>p.Id==h.PersonId).Price)))).ToArray();
 if(results.Any(t=>!t.Ready)||state.Hires.Select(h=>h.PersonId).Distinct().Count()!=77)throw new Exception("Roster completion/ownership failed.");
 return new(seed,policy,compensation,state.Carousel!.Version,state.ScoutAllocation!,order,schedule,actions.ToArray(),results);
}
ActionRecord Observe(string kind,string? person,string command,AcquisitionState state)
{
 var offer=state.Offer!;
 return new(kind,command,state.Revision,offer.TeamId,offer.Slot,person,offer.PersonIds.ToArray(),PersonnelAcquisition.Balance(state,offer.TeamId),PersonnelAcquisition.CompletionReserve(state,offer.TeamId),state.ScoutTokens![offer.TeamId],PersonnelAcquisition.ScoutUnavailable(state,offer.TeamId),offer.PersonIds.Count(id=>PersonnelAcquisition.HireUnavailable(state,offer.TeamId,id) is not null),offer.PersonIds.ToDictionary(id=>id,id=>PersonnelAcquisition.ReserveAfterHire(state,offer.TeamId,id)));
}
string Command(int seed,string policy,int compensation,long revision)=>new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(FormattableString.Invariant($"acquisition-evidence-v1/{seed}/{policy}/{compensation}/{revision}")))[..16]).ToString("D");
void Verify(Archive saved)
{
 if(saved.Schema!="acquisition-evidence-v1"||saved.DomainVersion!=PersonnelAcquisition.Version||saved.ReserveVersion!=PersonnelAcquisition.ReserveVersion||saved.PolicyVersion!="legal-offer-experiments-v1"||saved.Runs.Length!=48)throw new Exception("Unsupported or incomplete archive.");
 PersonnelAcquisition.ValidatePool(saved.Pool,11);
 var index=0;
 foreach(var seed in Seeds())foreach(var policy in Policies())foreach(var compensation in new[]{0,1})
 {
  var expected=Experiment(saved.Pool,seed,policy,compensation);
  if(JsonSerializer.Serialize(expected,json)!=JsonSerializer.Serialize(saved.Runs[index++],json))throw new Exception("Saved schedule, observations, actions, ledger or result differ from frozen replay.");
 }
}
record Archive(string Schema,string DomainVersion,string ReserveVersion,string PolicyVersion,PersonnelPool Pool,Run[] Runs);
record Run(int Seed,string Policy,int Compensation,string ScheduleVersion,ScoutAllocation ScoutAllocation,string[] InitialOrder,PersonnelPick[] Schedule,ActionRecord[] Actions,TeamResult[] Teams);
record ActionRecord(string Kind,string CommandId,long Revision,string TeamId,string Slot,string? PersonId,string[] Offers,int Balance,int Reserve,int Tokens,string? ScoutUnavailable,int BlockedOffers,Dictionary<string,int> ReserveAfterHire);
record TeamResult(string TeamId,int InitialPosition,int Balance,bool Ready,int ScoutsUsed,int ScoutsRemaining,PersonnelAssignment[] Hires,CreditEntry[] Ledger,Dictionary<string,int> ExpenditureByRole);
