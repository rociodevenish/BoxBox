using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Game.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace Game.Api.Tests;
public sealed class PersonnelAcquisitionTests
{
 [Theory]
 [InlineData(false)]
 [InlineData(true)]
 public async Task Reserved_first_offer_and_atomic_hire_survive_restart_and_uncertain_acknowledgement(bool committed)
 {
  var token=await LobbyTests.Identity(false);
  var durable=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")??"127.0.0.1:21980");
  var interrupted=new InterruptedStore(durable,committed);
  string id,weekend,device;JsonElement offer;object command;
  await using(var server=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(interrupted)))))
  {
   using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
   var created=await client.PostAsJsonAsync("/api/campaigns",new{commandId=Guid.NewGuid().ToString(),name="Host",personnelMode="acquisition"});created.EnsureSuccessStatusCode();
   var joined=(await created.Content.ReadFromJsonAsync<CampaignJoined>())!;
   Assert.Equal("acquiring",joined.Campaign.Lifecycle);
   id=joined.Campaign.CampaignId;weekend=joined.Campaign.WeekendId;device=joined.Weekend.Membership.DeviceId;
   (await client.PostAsJsonAsync($"/api/lobbies/{weekend}/claims",new ClaimTeamInput("team-01",device))).EnsureSuccessStatusCode();
   var path=$"/api/campaigns/{id}/acquisition/teams/team-01?deviceId={device}";
   offer=await client.GetFromJsonAsync<JsonElement>(path);
   Assert.Equal(100,offer.GetProperty("balance").GetInt32());Assert.Equal(3,offer.GetProperty("offers").GetArrayLength());
   Assert.Equal(offer.ToString(),(await client.GetFromJsonAsync<JsonElement>(path)).ToString());
   var person=offer.GetProperty("offers")[0];
   command=new{commandId=Guid.NewGuid().ToString(),revision=offer.GetProperty("revision").GetInt64(),teamId="team-01",personId=person.GetProperty("id").GetString()};
   Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync($"/api/campaigns/{id}/prepare",new{})).StatusCode);
   using var outsider=server.CreateClient();outsider.DefaultRequestHeaders.Authorization=new("Bearer",await LobbyTests.Identity(true));
   Assert.Equal(HttpStatusCode.Forbidden,(await outsider.GetAsync(path)).StatusCode);
   Assert.Equal(HttpStatusCode.Forbidden,(await outsider.PostAsJsonAsync(path.Replace("?","/hire?"),command)).StatusCode);
   Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(path.Replace("?","/hire?"),new{commandId=Guid.NewGuid().ToString(),revision=0,teamId="team-01",personId=person.GetProperty("id").GetString()})).StatusCode);
   Assert.Equal(offer.ToString(),(await client.GetFromJsonAsync<JsonElement>(path)).ToString());
   Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync($"/api/lobbies/{weekend}/preparation",new BeginPreparationInput("track-monza"))).StatusCode);
   interrupted.Armed=true;
   Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.PostAsJsonAsync(path.Replace("?","/hire?"),command)).StatusCode);
  }
  await using var recovered=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(durable))));
  using var retry=recovered.CreateClient();retry.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var uri=$"/api/campaigns/{id}/acquisition/teams/team-01?deviceId={device}";
  var purchased=await retry.PostAsJsonAsync(uri.Replace("?","/hire?"),command);purchased.EnsureSuccessStatusCode();
  var after=(await purchased.Content.ReadFromJsonAsync<JsonElement>())!;
  Assert.Equal(100-offer.GetProperty("offers")[0].GetProperty("price").GetInt32(),after.GetProperty("balance").GetInt32());
  Assert.Single(after.GetProperty("hires").EnumerateArray());Assert.Equal(2,after.GetProperty("ledger").GetArrayLength());
  var duplicate=await retry.PostAsJsonAsync(uri.Replace("?","/hire?"),command);duplicate.EnsureSuccessStatusCode();Assert.Equal(after.ToString(),(await duplicate.Content.ReadFromJsonAsync<JsonElement>()).ToString());
  var publicSnapshot=await retry.GetStringAsync($"/api/campaigns/{id}?deviceId={device}");Assert.DoesNotContain("\"balance\":",publicSnapshot);Assert.DoesNotContain("\"offers\":",publicSnapshot);
 }
 private sealed class InterruptedStore(ISessionStore inner,bool committed):ISessionStore
 {
  public bool Armed {get;set;}
  public StoredSession? Read(string id)=>inner.Read(id);
  public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);
  public string Write(string id,string? version,string payload,string? code)
  {
   if(Armed&&id.StartsWith("campaign-",StringComparison.Ordinal)){Armed=false;if(committed)inner.Write(id,version,payload,code);throw new IOException("Save failed or acknowledgement lost");}return inner.Write(id,version,payload,code);
  }
 }
}
