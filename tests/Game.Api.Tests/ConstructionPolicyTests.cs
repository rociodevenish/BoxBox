using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Game.Application;
using Game.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace Game.Api.Tests;
public sealed class ConstructionPolicyTests
{
 [Theory]
 [InlineData(false)]
 [InlineData(true)]
 public async Task Recorded_policy_recovers_atomic_automatic_builds_and_preserves_private_manual_choices(bool committed)
 {
  var token=await LobbyTests.Identity(false);
  var durable=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")!);
  var interrupted=new PolicyFailure(durable,committed);
  string id,device,weekend,path,manualOptionsJson;ConstructionAutomaticRequest automatic=new(Guid.NewGuid().ToString());ConstructionConfirm accepted;
  await using(var server=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(interrupted)))))
  {
   using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
   var created=await client.PostAsJsonAsync("/api/campaigns",new{commandId=Guid.NewGuid().ToString(),name="Policy host",personnelMode="acquisition",seed=1});created.EnsureSuccessStatusCode();
   var joined=(await created.Content.ReadFromJsonAsync<CampaignJoined>())!;id=joined.Campaign.CampaignId;device=joined.Weekend.Membership.DeviceId;weekend=joined.Campaign.WeekendId;
   for(var n=1;n<=11;n++)
   {
    var name="Policy constructor "+n;var participants=await client.PostAsJsonAsync($"/api/lobbies/{weekend}/participants",new{name,deviceId=device});participants.EnsureSuccessStatusCode();
    var lobby=(await participants.Content.ReadFromJsonAsync<JsonElement>());var player=lobby.GetProperty("players").EnumerateArray().Single(p=>p.GetProperty("name").GetString()==name).GetProperty("playerId").GetString();
    (await client.PostAsJsonAsync($"/api/lobbies/{weekend}/assignments",new{teamId=$"team-{n:00}",playerId=player,deviceId=device,control="SharedTablet"})).EnsureSuccessStatusCode();
   }
   for(var pick=0;pick<77;pick++)
   {
    var campaign=(await client.GetFromJsonAsync<CampaignSnapshot>($"/api/campaigns/{id}?deviceId={device}"))!;var team=campaign.Acquisition!.ActiveTeamId!;
    var personnel=(await client.GetFromJsonAsync<AcquisitionTeamSnapshot>($"/api/campaigns/{id}/acquisition/teams/{team}?deviceId={device}"))!;
    var legal=personnel.HireOptions.Where(o=>o.CanHire).OrderByDescending(o=>personnel.Offers.Single(p=>p.Id==o.PersonId).Price).First();
    (await client.PostAsJsonAsync($"/api/campaigns/{id}/acquisition/teams/{team}/hire?deviceId={device}",new PersonnelHire(Guid.NewGuid().ToString(),personnel.Revision,team,legal.PersonId))).EnsureSuccessStatusCode();
   }
   path=$"/api/campaigns/{id}/construction/teams/team-01?deviceId={device}";
   var own=(await client.GetFromJsonAsync<ConstructionTeamSnapshot>(path))!;var balance=own.Preview.Balance;
   var advice=new ConstructionRecommendationRequest(Guid.NewGuid().ToString(),own.Draft.Revision,"team-01",own.Catalogue.Version);
   using var outsider=server.CreateClient();outsider.DefaultRequestHeaders.Authorization=new("Bearer",await LobbyTests.Identity(true));
   Assert.Equal(HttpStatusCode.Forbidden,(await outsider.PostAsJsonAsync(path.Replace("?","/recommend?"),advice)).StatusCode);
   Assert.Equal(HttpStatusCode.Forbidden,(await outsider.PostAsJsonAsync($"/api/campaigns/{id}/construction/automatic",automatic)).StatusCode);
   (await client.PostAsJsonAsync(path.Replace("?","/recommend?"),advice)).EnsureSuccessStatusCode();
   (await client.PostAsJsonAsync(path.Replace("?","/recommend?"),advice)).EnsureSuccessStatusCode();
   own=(await client.GetFromJsonAsync<ConstructionTeamSnapshot>(path))!;
   Assert.Single(own.PolicyChoices!);Assert.Equal(balance,own.Preview.Balance);Assert.Equal(2,own.Draft.Revision);
   Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(path.Replace("?","/recommend?"),advice with{CommandId=Guid.NewGuid().ToString()})).StatusCode);
   var touched=(await client.GetFromJsonAsync<ConstructionTeamSnapshot>(path.Replace("team-01","team-03")))!;
   var alternative=touched.Catalogue.Categories.SelectMany((category,index)=>category.Options.Where(o=>o.Id!=touched.Draft.OptionIds[index]&&o.Cost==category.Options.Single(selected=>selected.Id==touched.Draft.OptionIds[index]).Cost).Select(o=>(index,o.Id))).First();
   var privateManual=touched.Draft.OptionIds.SetItem(alternative.index,alternative.Id);
   manualOptionsJson=JsonSerializer.Serialize(privateManual);
   (await client.PostAsJsonAsync(path.Replace("team-01","team-03").Replace("?","/draft?"),new ConstructionEdit(Guid.NewGuid().ToString(),1,"team-03",touched.Catalogue.Version,privateManual))).EnsureSuccessStatusCode();
   Assert.Equal(JsonSerializer.Serialize(own.Recommendation),JsonSerializer.Serialize((await client.GetFromJsonAsync<ConstructionTeamSnapshot>(path))!.Recommendation));
   accepted=new(Guid.NewGuid().ToString(),own.Draft.Revision,"team-01",own.Catalogue.Version);
   (await client.PostAsJsonAsync(path.Replace("?","/confirm?"),accepted)).EnsureSuccessStatusCode();
   for(var n=2;n<=11;n++)(await client.PostAsJsonAsync($"/api/lobbies/{weekend}/assignments",new{teamId=$"team-{n:00}",playerId=(string?)null,deviceId=device,control="Private"})).EnsureSuccessStatusCode();
   interrupted.Armed=true;
   Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.PostAsJsonAsync($"/api/campaigns/{id}/construction/automatic",automatic)).StatusCode);
  }
  await using var recovered=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(durable))));using var retry=recovered.CreateClient();retry.DefaultRequestHeaders.Authorization=new("Bearer",token);
  (await retry.PostAsJsonAsync($"/api/campaigns/{id}/construction/automatic",automatic)).EnsureSuccessStatusCode();
  (await retry.PostAsJsonAsync($"/api/campaigns/{id}/construction/automatic",automatic)).EnsureSuccessStatusCode();
  var campaignAfter=(await retry.GetFromJsonAsync<CampaignSnapshot>($"/api/campaigns/{id}?deviceId={device}"))!;
  Assert.Equal("constructing",campaignAfter.Lifecycle);Assert.Equal(9,campaignAfter.ConstructionReadiness!.Count(t=>t.Source=="unclaimed-ai"));Assert.False(campaignAfter.ConstructionReadiness!.Single(t=>t.TeamId=="team-03").Ready);
  Assert.Equal(HttpStatusCode.Conflict,(await retry.PostAsJsonAsync($"/api/campaigns/{id}/prepare",new{})).StatusCode);
  var ownSaved=(await retry.GetFromJsonAsync<ConstructionTeamSnapshot>(path))!;Assert.Equal(accepted.CommandId,ownSaved.Confirmed!.CommandId);Assert.Single(ownSaved.PolicyChoices!);Assert.Equal(0,ownSaved.PolicyChoices!.Count(c=>c.Source=="unclaimed-ai"));
  var publicText=await retry.GetStringAsync($"/api/campaigns/{id}?deviceId={device}");Assert.DoesNotContain("optionIds",publicText);Assert.DoesNotContain("observation",publicText);
  Assert.Equal(HttpStatusCode.Forbidden,(await retry.GetAsync(path.Replace("team-01","team-02"))).StatusCode);
  var policyLobby=(await retry.GetFromJsonAsync<LobbySnapshot>($"/api/lobbies/{weekend}"))!;
  (await retry.PostAsJsonAsync($"/api/lobbies/{weekend}/assignments",new{teamId="team-02",playerId=policyLobby.Players.Single(p=>p.Name=="Policy constructor 2").PlayerId,deviceId=device,control="SharedTablet"})).EnsureSuccessStatusCode();
  var automaticCar=(await retry.GetFromJsonAsync<ConstructionTeamSnapshot>(path.Replace("team-01","team-02")))!;Assert.Equal("unclaimed-ai",Assert.Single(automaticCar.PolicyChoices!).Source);Assert.Equal(18,automaticCar.Confirmed!.Cost);
  var automaticLedger=(await retry.GetFromJsonAsync<AcquisitionTeamSnapshot>($"/api/campaigns/{id}/acquisition/teams/team-02?deviceId={device}"))!;Assert.Single(automaticLedger.Ledger,e=>e.Reason=="base-car-construction");Assert.Equal(automaticCar.PolicyChoices![0].Observation.Balance-18,automaticLedger.Balance);
  var lobbyNow=await retry.GetFromJsonAsync<LobbySnapshot>($"/api/lobbies/{weekend}");var playerNow=lobbyNow!.Players.First(p=>p.Name=="Policy constructor 3").PlayerId;
  (await retry.PostAsJsonAsync($"/api/lobbies/{weekend}/assignments",new{teamId="team-03",playerId=playerNow,deviceId=device,control="SharedTablet"})).EnsureSuccessStatusCode();
  var manual=(await retry.GetFromJsonAsync<ConstructionTeamSnapshot>(path.Replace("team-01","team-03")))!;Assert.Equal(2,manual.Draft.Revision);Assert.Null(manual.Confirmed);
  Assert.Equal(manualOptionsJson,JsonSerializer.Serialize(manual.Draft.OptionIds));
  (await retry.PostAsJsonAsync(path.Replace("team-01","team-03").Replace("?","/confirm?"),new ConstructionConfirm(Guid.NewGuid().ToString(),2,"team-03",manual.Catalogue.Version))).EnsureSuccessStatusCode();
  Assert.Equal("created",(await retry.GetFromJsonAsync<CampaignSnapshot>($"/api/campaigns/{id}?deviceId={device}"))!.Lifecycle);
  (await retry.PostAsJsonAsync($"/api/campaigns/{id}/prepare",new{})).EnsureSuccessStatusCode();
  (await retry.PostAsJsonAsync(path.Replace("?","/confirm?"),accepted)).EnsureSuccessStatusCode();
  var ledger=(await retry.GetFromJsonAsync<AcquisitionTeamSnapshot>($"/api/campaigns/{id}/acquisition/teams/team-01?deviceId={device}"))!;Assert.Single(ledger.Ledger,e=>e.Reason=="base-car-construction");
  for(var n=1;n<=3;n++)(await retry.PostAsJsonAsync($"/api/campaigns/{id}/absent/prepare",new CampaignAbsentInput($"team-{n:00}"))).EnsureSuccessStatusCode();
  (await retry.PostAsJsonAsync($"/api/campaigns/{id}/start",new CampaignStartInput(Guid.NewGuid().ToString()))).EnsureSuccessStatusCode();
  Assert.Equal(HttpStatusCode.Conflict,(await retry.PostAsJsonAsync($"/api/campaigns/{id}/construction/automatic",new ConstructionAutomaticRequest(Guid.NewGuid().ToString()))).StatusCode);
  (await retry.PostAsJsonAsync($"/api/campaigns/{id}/construction/automatic",automatic)).EnsureSuccessStatusCode();
  (await retry.PostAsJsonAsync(path.Replace("?","/confirm?"),accepted)).EnsureSuccessStatusCode();
  Assert.Equal(automaticCar.Confirmed.Id,(await retry.GetFromJsonAsync<ConstructionTeamSnapshot>(path.Replace("team-01","team-02")))!.Confirmed!.Id);
 }
 private sealed class PolicyFailure(ISessionStore inner,bool committed):ISessionStore
 {
  public bool Armed;
  public StoredSession? Read(string id)=>inner.Read(id);
  public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);
  public IReadOnlyList<string> ListSessions()=>inner.ListSessions();
  public string Write(string id,string? version,string payload,string? code)=>Write(id,version,payload,code,null);
  public string Write(string id,string? version,string payload,string? code,ScheduledWakeup? wakeup)
  {
   if(!Armed||!id.StartsWith("campaign-"))return inner.Write(id,version,payload,code,wakeup);
   Armed=false;if(committed)inner.Write(id,version,payload,code,wakeup);throw new IOException("Interrupted automatic construction acknowledgement");
  }
 }
}
