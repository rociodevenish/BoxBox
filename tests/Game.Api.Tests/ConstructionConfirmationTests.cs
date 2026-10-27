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
public sealed class ConstructionConfirmationTests
{
 [Theory]
 [InlineData(false,false)]
 [InlineData(false,true)]
 [InlineData(true,false)]
 [InlineData(true,true)]
 public async Task Confirmed_car_and_exact_debit_survive_uncertain_acknowledgement_and_open_only_after_all_teams(bool committed,bool handoff)
 {
  var token=await LobbyTests.Identity(false);
  var durable=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")!);
  var interrupted=new ConfirmationFailure(durable,committed);
  string id,device,weekend,path;ConstructionTeamSnapshot own;ConstructionConfirm edit;AcquisitionTeamSnapshot before;
  await using(var server=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(interrupted)))))
  {
   using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
   var response=await client.PostAsJsonAsync("/api/campaigns",new{commandId=Guid.NewGuid().ToString(),name="Construction host",personnelMode="acquisition",seed=1});response.EnsureSuccessStatusCode();
   var joined=(await response.Content.ReadFromJsonAsync<CampaignJoined>())!;id=joined.Campaign.CampaignId;device=joined.Weekend.Membership.DeviceId;weekend=joined.Campaign.WeekendId;interrupted.Target=weekend;
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
   before=(await client.GetFromJsonAsync<AcquisitionTeamSnapshot>($"/api/campaigns/{id}/acquisition/teams/team-01?deviceId={device}"))!;
   edit=new(Guid.NewGuid().ToString(),own.Draft.Revision,"team-01",own.Catalogue.Version);
   using var outsider=server.CreateClient();outsider.DefaultRequestHeaders.Authorization=new("Bearer",await LobbyTests.Identity(true));
   Assert.Equal(HttpStatusCode.Forbidden,(await outsider.PostAsJsonAsync(path.Replace("?","/confirm?"),edit)).StatusCode);
   Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(path.Replace("?","/confirm?"),edit with{Revision=0})).StatusCode);
   if(!handoff){interrupted.Armed=true;Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.PostAsJsonAsync(path.Replace("?","/confirm?"),edit)).StatusCode);}
  }
  await using var recovered=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(interrupted))));using var retry=recovered.CreateClient();retry.DefaultRequestHeaders.Authorization=new("Bearer",token);
  (await retry.PostAsJsonAsync(path.Replace("?","/confirm?"),edit)).EnsureSuccessStatusCode();
  var accepted=(await retry.GetFromJsonAsync<ConstructionTeamSnapshot>(path))!;Assert.NotNull(accepted.Confirmed);Assert.Equal(before.Balance-18,accepted.Preview.Balance);
  (await retry.PostAsJsonAsync(path.Replace("?","/confirm?"),edit)).EnsureSuccessStatusCode();
  var ledger=(await retry.GetFromJsonAsync<AcquisitionTeamSnapshot>($"/api/campaigns/{id}/acquisition/teams/team-01?deviceId={device}"))!;
  Assert.Single(ledger.Ledger,e=>e.Reason=="base-car-construction");Assert.True(ledger.RosterReady);
  Assert.Equal(HttpStatusCode.Conflict,(await retry.PostAsJsonAsync(path.Replace("?","/confirm?"),edit with{CommandId=Guid.NewGuid().ToString()})).StatusCode);
  Assert.Equal(HttpStatusCode.Conflict,(await retry.PostAsJsonAsync($"/api/lobbies/{weekend}/preparation",new BeginPreparationInput("track-monza"))).StatusCode);
  for(var n=2;n<=11;n++)
  {
   var team=$"team-{n:00}";var target=path.Replace("team-01",team);var next=(await retry.GetFromJsonAsync<ConstructionTeamSnapshot>(target))!;
   var confirmation=new ConstructionConfirm(Guid.NewGuid().ToString(),next.Draft.Revision,team,next.Catalogue.Version);
   if(n==11&&handoff){interrupted.Lobby=true;interrupted.Armed=true;Assert.Equal(HttpStatusCode.ServiceUnavailable,(await retry.PostAsJsonAsync(target.Replace("?","/confirm?"),confirmation)).StatusCode);}
   (await retry.PostAsJsonAsync(target.Replace("?","/confirm?"),confirmation)).EnsureSuccessStatusCode();
  }
  Assert.Equal("created",(await retry.GetFromJsonAsync<CampaignSnapshot>($"/api/campaigns/{id}?deviceId={device}"))!.Lifecycle);
  interrupted.Lobby=true;interrupted.Armed=true;
  Assert.Equal(HttpStatusCode.ServiceUnavailable,(await retry.PostAsJsonAsync($"/api/campaigns/{id}/prepare",new{})).StatusCode);
  await using(var restarted=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(durable)))))
  {
   using var restored=restarted.CreateClient();restored.DefaultRequestHeaders.Authorization=new("Bearer",token);
   (await restored.PostAsJsonAsync($"/api/campaigns/{id}/prepare",new{})).EnsureSuccessStatusCode();
   var prepared=(await restored.GetFromJsonAsync<PreparationTeamSnapshot>($"/api/lobbies/{weekend}/preparation/teams/team-01?deviceId={device}"))!;
   Assert.Equal(accepted.Confirmed!.Id,prepared.Preset.BaseCarId);
   Assert.All(prepared.Preset.Cars,car=>Assert.Equal(accepted.Confirmed.Ratings.OrderBy(p=>p.Key),car.Car.Ratings.OrderBy(p=>p.Key)));
   Assert.Equal(accepted.Confirmed.CommandId,(await restored.GetFromJsonAsync<ConstructionTeamSnapshot>(path))!.Confirmed!.CommandId);
  }
  (await retry.PostAsJsonAsync(path.Replace("?","/confirm?"),edit)).EnsureSuccessStatusCode();
  Assert.Equal(accepted.Confirmed!.Id,(await retry.GetFromJsonAsync<ConstructionTeamSnapshot>(path))!.Confirmed!.Id);
  if(!handoff)
  {
   var clock=new DeadlineTests.ControlledClock();
   await using(var racing=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>{s.Replace(ServiceDescriptor.Singleton<ISessionStore>(interrupted));s.Replace(ServiceDescriptor.Singleton<TimeProvider>(clock));})))
   {
    using var host=racing.CreateClient();host.DefaultRequestHeaders.Authorization=new("Bearer",token);
    for(var n=1;n<=11;n++)(await host.PostAsJsonAsync($"/api/campaigns/{id}/absent/prepare",new CampaignAbsentInput($"team-{n:00}"))).EnsureSuccessStatusCode();
    (await host.PostAsJsonAsync($"/api/campaigns/{id}/start",new CampaignStartInput(Guid.NewGuid().ToString()))).EnsureSuccessStatusCode();
    interrupted.Race=true;interrupted.Armed=true;var failed=false;
    for(var n=0;n<12&&!failed;n++)
    {
     clock.Advance(TimeSpan.FromHours(1));
     try{await racing.Services.GetRequiredService<SessionCoordinator>().SweepWakeups();}
     catch(SessionFault error) when(error.Failure==SessionFailure.StorageUnavailable){failed=true;}
    }
    Assert.True(failed,"The constructed race initialization must reach the injected persistence interruption.");
   }
   await using var raceRestart=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>{s.Replace(ServiceDescriptor.Singleton<ISessionStore>(interrupted));s.Replace(ServiceDescriptor.Singleton<TimeProvider>(clock));}));
   using var raceClient=raceRestart.CreateClient();raceClient.DefaultRequestHeaders.Authorization=new("Bearer",token);
   for(var n=0;n<12;n++){var state=await raceClient.GetAsync($"/api/sessions/{weekend}");if(state.IsSuccessStatusCode)break;clock.Advance(TimeSpan.FromHours(1));await raceRestart.Services.GetRequiredService<SessionCoordinator>().SweepWakeups();}
   (await raceClient.GetAsync($"/api/sessions/{weekend}")).EnsureSuccessStatusCode();
   Assert.Equal(accepted.Confirmed.Id,(await raceClient.GetFromJsonAsync<ConstructionTeamSnapshot>(path))!.Confirmed!.Id);
   var finalLedger=(await raceClient.GetFromJsonAsync<AcquisitionTeamSnapshot>($"/api/campaigns/{id}/acquisition/teams/team-01?deviceId={device}"))!;
   Assert.Single(finalLedger.Ledger,e=>e.Reason=="base-car-construction");Assert.Equal(before.Balance-18,finalLedger.Balance);
  }
 }
 private sealed class ConfirmationFailure(ISessionStore inner,bool committed):ISessionStore
 {
  public bool Armed;public bool Lobby;public bool Race;public string? Target;
  public StoredSession? Read(string id)=>inner.Read(id);
  public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);
  public IReadOnlyList<string> ListSessions()=>Target is null?inner.ListSessions():[Target];
  public string Write(string id,string? version,string payload,string? code)=>Write(id,version,payload,code,null);
  public string Write(string id,string? version,string payload,string? code,ScheduledWakeup? wakeup)
  {
   if(!Armed||(Lobby?id!=Target:!id.StartsWith("campaign-")))return inner.Write(id,version,payload,code,wakeup);
   if(Race&&System.Text.Json.JsonDocument.Parse(payload).RootElement.GetProperty("lobby").GetProperty("race").ValueKind!=System.Text.Json.JsonValueKind.Object)return inner.Write(id,version,payload,code,wakeup);
   Armed=false;if(committed)inner.Write(id,version,payload,code,wakeup);throw new IOException("Interrupted construction acknowledgement");
  }
 }
}
