using System.Net.Http.Json;
using System.Text.Json;
using Game.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace Game.Api.Tests;
public sealed class PersonnelDeadlineTests
{
 private sealed class Clock:TimeProvider
 {private DateTimeOffset now=DateTimeOffset.Parse("2026-10-26T00:00:00Z");public override DateTimeOffset GetUtcNow()=>now;public void Advance()=>now=now.AddMinutes(3);}
 private sealed class Queue:IScheduledWakeups
 {public ScheduledWakeup? Last;public void Enqueue(ScheduledWakeup wakeup)=>Last=wakeup;}
 private sealed class Interrupted(ISessionStore inner,bool committed):ISessionStore
 {
  public bool Armed;public StoredSession? Read(string id)=>inner.Read(id);public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);public IReadOnlyList<string> ListSessions()=>inner.ListSessions();
  public string Write(string id,string? version,string payload,string? code)=>Write(id,version,payload,code,null);
  public string Write(string id,string? version,string payload,string? code,ScheduledWakeup? wakeup)
  {if(Armed&&id.StartsWith("campaign-",StringComparison.Ordinal)){Armed=false;if(committed)inner.Write(id,version,payload,code,wakeup);throw new IOException("Deadline save failed or acknowledgement lost");}return inner.Write(id,version,payload,code,wakeup);}
 }
 [Theory]
 [InlineData(false)]
 [InlineData(true)]
 public async Task Saved_deadline_restarts_and_repeated_delivery_preserve_accepted_hires_and_offer_only_advice(bool committed)
 {
  var token=await LobbyTests.Identity(false);var clock=new Clock();var queue=new Queue();
  var durable=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")!);
  var interrupted=new Interrupted(durable,committed);
  string id,device,firstTeam;ScheduledWakeup first;
  await using(var server=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>{s.Replace(ServiceDescriptor.Singleton<ISessionStore>(interrupted));s.Replace(ServiceDescriptor.Singleton<TimeProvider>(clock));s.AddSingleton<IScheduledWakeups>(queue);})))
  {
   using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
   var created=await client.PostAsJsonAsync("/api/campaigns",new{commandId=Guid.NewGuid().ToString(),name="Deadline host",personnelMode="acquisition",seed=1});created.EnsureSuccessStatusCode();
   var joined=(await created.Content.ReadFromJsonAsync<CampaignJoined>())!;id=joined.Campaign.CampaignId;device=joined.Weekend.Membership.DeviceId;
   firstTeam=joined.Campaign.Acquisition!.ActiveTeamId!;Assert.Null(queue.Last);
   (await client.PostAsJsonAsync($"/api/lobbies/{joined.Campaign.WeekendId}/claims",new ClaimTeamInput(firstTeam,device))).EnsureSuccessStatusCode();
   (await client.PostAsJsonAsync($"/api/campaigns/{id}/acquisition/activate",new{})).EnsureSuccessStatusCode();first=queue.Last!;Assert.Equal("personnel",first.Kind);
   clock.Advance();interrupted.Armed=true;
   Assert.Equal(SessionFailure.StorageUnavailable,Assert.Throws<SessionFault>(()=>server.Services.GetRequiredService<CampaignCoordinator>().DeliverPersonnelWakeup(new(first.SessionId,first.Generation,first.Phase,first.WindowRevision))).Failure);
  }
  await using var recovered=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>{s.Replace(ServiceDescriptor.Singleton<ISessionStore>(durable));s.Replace(ServiceDescriptor.Singleton<TimeProvider>(clock));s.AddSingleton<IScheduledWakeups>(queue);}));
  using var retry=recovered.CreateClient();retry.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var campaigns=recovered.Services.GetRequiredService<CampaignCoordinator>();
  var before=await retry.GetFromJsonAsync<JsonElement>($"/api/campaigns/{id}?deviceId={device}");
  if(!committed)Assert.Equal(1,campaigns.SweepPersonnelWakeups());
  var after=await retry.GetFromJsonAsync<JsonElement>($"/api/campaigns/{id}?deviceId={device}");Assert.Equal(1,after.GetProperty("acquisition").GetProperty("filledSlots").GetInt32());
  var own=(await retry.GetFromJsonAsync<AcquisitionTeamSnapshot>($"/api/campaigns/{id}/acquisition/teams/{firstTeam}?deviceId={device}"));
  var choice=Assert.Single(own!.AutomaticChoices!);Assert.Equal("deadline-absence-ai",choice.Source);Assert.Equal("offer-only-low-cost-v1",choice.Recommendation.Version);Assert.Contains(choice.Observation.Offers,p=>p.PersonId==choice.Recommendation.PersonId&&p.CanHire);
  campaigns.DeliverPersonnelWakeup(new(first.SessionId,first.Generation,first.Phase,first.WindowRevision));
  Assert.Equal(after.ToString(),(await retry.GetFromJsonAsync<JsonElement>($"/api/campaigns/{id}?deviceId={device}")).ToString());
  // Every future actual offer uses the same legal policy and retained fixed pool until completion.
  for(var pick=1;pick<77;pick++){clock.Advance();Assert.Equal(1,campaigns.SweepPersonnelWakeups());}
  var final=await retry.GetFromJsonAsync<JsonElement>($"/api/campaigns/{id}?deviceId={device}");Assert.Equal("created",final.GetProperty("lifecycle").GetString());Assert.True(final.GetProperty("acquisition").GetProperty("allRostersReady").GetBoolean());
  (await retry.PostAsJsonAsync($"/api/campaigns/{id}/prepare",new{})).EnsureSuccessStatusCode();
 }
}


