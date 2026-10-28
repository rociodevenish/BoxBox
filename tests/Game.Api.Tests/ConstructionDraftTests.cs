using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Json;
using Game.Application;
using Game.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace Game.Api.Tests;
public sealed class ConstructionDraftTests
{
 [Theory]
 [InlineData(false)]
 [InlineData(true)]
 public async Task Free_authorised_draft_survives_uncertain_save_restart_and_retry_without_spending(bool committed)
 {
  var token=await LobbyTests.Identity(false);
  var durable=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")!);
  var interrupted=new DraftFailure(durable,committed);
  string id,device,weekend,path;ConstructionTeamSnapshot own;ConstructionEdit edit;AcquisitionTeamSnapshot before;
  await using(var server=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(interrupted)))))
  {
   using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
   var response=await client.PostAsJsonAsync("/api/campaigns",new{commandId=Guid.NewGuid().ToString(),name="Construction host",personnelMode="acquisition",seed=1});response.EnsureSuccessStatusCode();
   var joined=(await response.Content.ReadFromJsonAsync<CampaignJoined>())!;id=joined.Campaign.CampaignId;device=joined.Weekend.Membership.DeviceId;weekend=joined.Campaign.WeekendId;
   for(var n=1;n<=11;n++)
   {
    var name="Shared constructor "+n;var participants=await client.PostAsJsonAsync($"/api/lobbies/{weekend}/participants",new{name,deviceId=device});participants.EnsureSuccessStatusCode();
    var lobby=(await participants.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>());
    var player=lobby.GetProperty("players").EnumerateArray().Single(p=>p.GetProperty("name").GetString()==name).GetProperty("playerId").GetString();
    (await client.PostAsJsonAsync($"/api/lobbies/{weekend}/assignments",new{teamId=$"team-{n:00}",playerId=player,deviceId=device,control="SharedTablet"})).EnsureSuccessStatusCode();
   }
   for(var pick=0;pick<77;pick++)
   {
    var campaign=(await client.GetFromJsonAsync<CampaignSnapshot>($"/api/campaigns/{id}?deviceId={device}"))!;var team=campaign.Acquisition!.ActiveTeamId!;
    var personnel=(await client.GetFromJsonAsync<AcquisitionTeamSnapshot>($"/api/campaigns/{id}/acquisition/teams/{team}?deviceId={device}"))!;
    var legal=personnel.HireOptions.Where(o=>o.CanHire).OrderByDescending(o=>personnel.Offers.Single(p=>p.Id==o.PersonId).Price).First();
    (await client.PostAsJsonAsync($"/api/campaigns/{id}/acquisition/teams/{team}/hire?deviceId={device}",new PersonnelHire(Guid.NewGuid().ToString(),personnel.Revision,team,legal.PersonId))).EnsureSuccessStatusCode();
   }
   Assert.Equal("constructing",(await client.GetFromJsonAsync<CampaignSnapshot>($"/api/campaigns/{id}?deviceId={device}"))!.Lifecycle);
   Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync($"/api/campaigns/{id}/prepare",new{})).StatusCode);
   Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync($"/api/lobbies/{weekend}/preparation",new BeginPreparationInput("track-monza"))).StatusCode);
   path=$"/api/campaigns/{id}/construction/teams/team-01?deviceId={device}";
   own=(await client.GetFromJsonAsync<ConstructionTeamSnapshot>(path))!;
   var other=(await client.GetFromJsonAsync<ConstructionTeamSnapshot>(path.Replace("team-01","team-02")))!;
   Assert.Equal(System.Text.Json.JsonSerializer.Serialize(own.Catalogue),System.Text.Json.JsonSerializer.Serialize(other.Catalogue));Assert.Equal(18,own.Preview.Cost);
   before=(await client.GetFromJsonAsync<AcquisitionTeamSnapshot>($"/api/campaigns/{id}/acquisition/teams/team-01?deviceId={device}"))!;
   using var outsider=server.CreateClient();outsider.DefaultRequestHeaders.Authorization=new("Bearer",await LobbyTests.Identity(true));Assert.Equal(HttpStatusCode.Forbidden,(await outsider.GetAsync(path)).StatusCode);
   edit=new(Guid.NewGuid().ToString(),own.Draft.Revision,"team-01",own.Catalogue.Version,own.Draft.OptionIds);
   var invalid=await client.PostAsJsonAsync(path.Replace("?","/draft?"),edit with{OptionIds=["unknown"]});Assert.Equal(HttpStatusCode.BadRequest,invalid.StatusCode);
   var expensive=own.Catalogue.Categories.Select(c=>c.Options.OrderByDescending(o=>o.Cost).First().Id).ToImmutableArray();
   if(before.Balance<24)Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(path.Replace("?","/draft?"),edit with{CommandId=Guid.NewGuid().ToString(),OptionIds=expensive})).StatusCode);
   interrupted.Armed=true;Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.PostAsJsonAsync(path.Replace("?","/draft?"),edit)).StatusCode);
  }
  await using var recovered=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(durable))));using var retry=recovered.CreateClient();retry.DefaultRequestHeaders.Authorization=new("Bearer",token);
  (await retry.PostAsJsonAsync(path.Replace("?","/draft?"),edit)).EnsureSuccessStatusCode();
  var saved=(await retry.GetFromJsonAsync<ConstructionTeamSnapshot>(path))!;Assert.Equal(2,saved.Draft.Revision);Assert.Equal(own.Preview.Balance,saved.Preview.Balance);
  (await retry.PostAsJsonAsync(path.Replace("?","/draft?"),edit)).EnsureSuccessStatusCode();Assert.Equal(2,(await retry.GetFromJsonAsync<ConstructionTeamSnapshot>(path))!.Draft.Revision);
  Assert.Equal(HttpStatusCode.Conflict,(await retry.PostAsJsonAsync(path.Replace("?","/draft?"),edit with{CommandId=Guid.NewGuid().ToString()})).StatusCode);
  var after=(await retry.GetFromJsonAsync<AcquisitionTeamSnapshot>($"/api/campaigns/{id}/acquisition/teams/team-01?deviceId={device}"))!;Assert.Equal(before.Balance,after.Balance);Assert.Equal(before.Ledger,after.Ledger);
  Assert.Equal(HttpStatusCode.Conflict,(await retry.PostAsJsonAsync($"/api/lobbies/{weekend}/preparation",new BeginPreparationInput("track-monza"))).StatusCode);
 }
 private sealed class DraftFailure(ISessionStore inner,bool committed):ISessionStore
 {
  public bool Armed;
  public StoredSession? Read(string id)=>inner.Read(id);
  public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);
  public IReadOnlyList<string> ListSessions()=>inner.ListSessions();
  public string Write(string id,string? version,string payload,string? code)=>Write(id,version,payload,code,null);
  public string Write(string id,string? version,string payload,string? code,ScheduledWakeup? wakeup)
  {
   if(!Armed||!id.StartsWith("campaign-"))return inner.Write(id,version,payload,code,wakeup);
   Armed=false;if(committed)inner.Write(id,version,payload,code,wakeup);throw new IOException("Interrupted draft acknowledgement");
  }
 }
}
