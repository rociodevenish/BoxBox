using System.Collections.Immutable;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Application;
using Game.Domain;
using Game.Simulation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace Game.Api.Tests;
public sealed class PackageSelectionIntegrityTests
{
 private sealed class ChangedHistory(ISessionStore inner):ISessionStore
 {
  public string? Change;
  public StoredSession? Read(string id)
  {
   var saved=inner.Read(id);if(saved is null||Change is null)return saved;
   var json=JsonNode.Parse(saved.Payload)!;var modules=json["lobby"]?["moduleState"];
   if(modules?["weekend-preparation-v1"] is not {} module)return saved;
   var preparation=JsonNode.Parse(module.GetValue<string>())!;
   var choice=preparation["teams"]![Change=="omitted-version"?"team-02":"team-03"]!["packageChoices"]![0]!;
   var observation=choice["observation"]!;
   if(Change=="omitted-version")
   {
    var selected=choice["selection"]!["packageId"]!.GetValue<string>();var library=observation["library"]!.AsArray();
    var omitted=library.First(p=>p!["packageId"]!.GetValue<string>()!="neutral-package-v1"&&p["packageId"]!.GetValue<string>()!=selected);
    library.Remove(omitted);
   }
   else observation["reliabilityActive"]=true;
   // Recompute a self-consistent recommendation: replay alone must not legitimise a false observation.
   var observed=observation.Deserialize<PackageSelectionObservation>(BrowserReplay.JsonOptions)!;
   var recommendation=PackageSelectionDecisionPolicy.Evaluate(observed);
   choice["recommendation"]=JsonSerializer.SerializeToNode(recommendation,BrowserReplay.JsonOptions);
   choice["selection"]=JsonSerializer.SerializeToNode(recommendation.Selection,BrowserReplay.JsonOptions);
   modules!["weekend-preparation-v1"]=preparation.ToJsonString();
   return saved with{Payload=json.ToJsonString()};
  }
  public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);
  public IReadOnlyList<string> ListSessions()=>[];
  public string Write(string id,string? version,string payload,string? code)=>inner.Write(id,version,payload,code);
  public string Write(string id,string? version,string payload,string? code,ScheduledWakeup? wakeup)=>inner.Write(id,version,payload,code,wakeup);
 }
 [Theory][InlineData("omitted-version")][InlineData("invented-reliability")]
 public async Task Restart_rejects_self_consistent_selection_history_that_omits_owned_versions_or_invents_active_reliability(string change)
 {
  var store=new ChangedHistory(new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST")));
  Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> Server()=>LobbyTests.Server().WithWebHostBuilder(b=>b.UseSetting("Wakeups:Mode","manual").ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(store))));
  using var server=Server();using var client=server.CreateClient();var sessions=server.Services.GetRequiredService<SessionCoordinator>();var identity=new AuthIdentity("selection-integrity-host",false);
  var joined=sessions.CreateLobby(identity,new("open-equal",6,123,"Competitive","Host"));var id=joined.Lobby.SessionId;
  sessions.ClaimTeam(id,identity,new("team-01",joined.Membership.DeviceId));
  // A system-boundary fixture: obtain the specialised version from a paid public development operation.
  var staff=new DevelopmentPersonnel("fixture",0,0,0,0);var quote=PackageDevelopment.QuoteRich("downforce",staff,DevelopmentRiskControls.Only("normal"));
  var op=PermanentDevelopment.Open("purchase","one","two",["team-02"]);
  op=PermanentDevelopment.Edit(op,"team-02",new(Guid.NewGuid().ToString(),op.Id,op.Revision,"PackageCreation","downforce",quote.Id),staff,risk:quote.Risk);
  var paid=PermanentDevelopment.Commit(op,[new("start","team-02",100,"starting-credit")],"team-02",new(Guid.NewGuid().ToString(),op.Id,op.Revision,1,quote.Id),staff);
  var outcome=Assert.Single(PermanentDevelopment.Close(new(PermanentDevelopment.Version,[paid.State],new string('5',64)),op.Id,["one","two"]).PackageOutcomes);
  var otherQuote=PackageDevelopment.QuoteRich("cooling-reliability",staff,DevelopmentRiskControls.Only("normal"));
  var other=PermanentDevelopment.Open("other-purchase","one","two",["team-02"]);
  other=PermanentDevelopment.Edit(other,"team-02",new(Guid.NewGuid().ToString(),other.Id,other.Revision,"PackageCreation",otherQuote.Template.Id,otherQuote.Id),staff,risk:otherQuote.Risk);
  var otherPaid=PermanentDevelopment.Commit(other,[new("start","team-02",100,"starting-credit")],"team-02",new(Guid.NewGuid().ToString(),other.Id,other.Revision,1,otherQuote.Id),staff);
  var otherOutcome=Assert.Single(PermanentDevelopment.Close(new(PermanentDevelopment.Version,[otherPaid.State],new string('5',64)),other.Id,["one","two"]).PackageOutcomes);
  var presets=Enumerable.Range(0,11).Select(_=>RacePackages.Migrate(WeekendPresetCatalog.Open("track-monza"))).ToArray();
  presets[1]=presets[1] with{PackageLibrary=presets[1].PackageLibrary.Add(outcome.Package!).Add(otherOutcome.Package!)};
  sessions.BeginCampaignPreparation(id,identity,presets);
  var uri=$"/api/lobbies/{id}/preparation";var original=await client.GetStringAsync(uri);
  using(var restart=Server()){using var recovered=restart.CreateClient();Assert.Equal(original,await recovered.GetStringAsync(uri));}
  store.Change=change;
  using var altered=Server();using var rejected=altered.CreateClient();
  Assert.Equal(HttpStatusCode.Conflict,(await rejected.GetAsync(uri)).StatusCode);
 }
}

