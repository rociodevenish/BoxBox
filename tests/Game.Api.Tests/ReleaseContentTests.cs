using Xunit.Abstractions;
using System.Text;
using Game.Domain;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Collections.Immutable;
using Game.Simulation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;
using Game.Application;
using Xunit;
namespace Game.Api.Tests;

public sealed class ReleaseContentTests(ITestOutputHelper output)
{
 [Fact]
 public async Task Unsupported_content_version_is_rejected_before_reserving_campaign_creation()
 {
  var token=await LobbyTests.Identity(false);
  await using var server=Server();using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var command=Guid.NewGuid().ToString();
  var rejected=await client.PostAsJsonAsync("/api/campaigns",new {commandId=command,name="Host",contentVersion="unsupported-season-v99"});
  Assert.Equal(HttpStatusCode.BadRequest,rejected.StatusCode);
  Assert.Contains("Unsupported content version",await rejected.Content.ReadAsStringAsync());
  var valid=await client.PostAsJsonAsync("/api/campaigns",new CreateCampaignInput(command,"Host"));valid.EnsureSuccessStatusCode();
 }
 [Fact]
 public async Task Reviewed_candidate_creates_readable_immutable_two_event_campaign_and_recovers_after_restart()
 {
  var token=await LobbyTests.Identity(false);
  var input=new {commandId=Guid.NewGuid().ToString(),name="Host",contentVersion="release-2026-pilot-v1"};
  string first;
  await using(var server=Server())
  {
   using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
   var result=await client.PostAsJsonAsync("/api/campaigns",input);Assert.True(result.IsSuccessStatusCode,await result.Content.ReadAsStringAsync());first=await result.Content.ReadAsStringAsync();
   var joined=(await result.Content.ReadFromJsonAsync<CampaignJoined>())!;
   Assert.Equal("release-2026-pilot-v1",joined.Campaign.ContentVersion);
   Assert.Equal(new[]{"track-monza","track-monaco"},joined.Campaign.Calendar.Select(e=>e.TrackId));
   Assert.Equal("McLaren",joined.Campaign.Teams[0].ConstructorName);
   Assert.Equal("Lando Norris",joined.Campaign.Teams[0].Drivers[0].Name);
   using var measured=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")!);
   output.WriteLine("Initial frozen pilot record: "+Encoding.UTF8.GetByteCount(measured.Read("campaign-"+joined.Campaign.CampaignId)!.Payload)+" UTF-8 bytes (2 events, 11 constructors, 22 drivers, construction/source/asset snapshot).");
  }
  await using var recovered=Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton(new CampaignContentCatalog([])))));using var retry=recovered.CreateClient();retry.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var repeated=await retry.PostAsJsonAsync("/api/campaigns",input);repeated.EnsureSuccessStatusCode();Assert.Equal(first,await repeated.Content.ReadAsStringAsync());
 }
 [Theory]
 [InlineData("reference")][InlineData("units")][InlineData("bounds")][InlineData("travel")][InlineData("consumer")]
 [InlineData("missing-consumer")][InlineData("pool")][InlineData("budget")][InlineData("source")][InlineData("asset")][InlineData("reserve-consumer")][InlineData("staff-consumer")][InlineData("groups")][InlineData("constructor")][InlineData("package")][InlineData("opportunity")][InlineData("inactive-package")][InlineData("length-unit-conversion")]
 public async Task Invalid_candidate_is_rejected_before_creation_and_same_command_can_retry_corrected_pack(string fault)
 {
  var original=ReleaseContent.Pilot();var circuit=original.Circuits[0];
  var broken=fault switch
  {
   "constructor"=>original with{Constructors=original.Constructors.SetItem(0,original.Constructors[0] with{Id="unmapped-constructor"}),Assets=original.Assets.Select(a=>a.EntityId=="mclaren"?a with{EntityId="unmapped-constructor"}:a).ToImmutableArray()},
   "inactive-package"=>original with{Packages=original.Packages.Add(original.Packages[0] with{PackageId="authored-invalid-package",Strengths=ImmutableDictionary<string,double>.Empty.Add("engine.telepathy",3)})},
   "package"=>original with{Packages=original.Packages.SetItem(0,original.Packages[0] with{Strengths=ImmutableDictionary<string,double>.Empty.Add("engine.telepathy",3)})},
   "opportunity"=>original with{Circuits=original.Circuits.SetItem(0,circuit with{Track=circuit.Track with{Segments=circuit.Track.Segments!.Select(s=>s with{PassingZoneId="shared"}).ToArray()}})},
   "groups"=>original with{AttributeGroups=["Acceleration Stars"]},
   "reference"=>original with{Calendar=["track-missing","track-monaco"]},
   "length-unit-conversion"=>original with{Circuits=original.Circuits.SetItem(0,circuit with{Track=circuit.Track with{Segments=circuit.Track.Segments!.Select(s=>s with{Length=s.Length/1000}).ToArray()}})},
   "units"=>original with{LengthUnit="kilometres"},
   "bounds"=>original with{Circuits=original.Circuits.SetItem(0,circuit with{Demands=circuit.Demands.SetItem("topSpeed",101)})},
   "travel"=>original with{Circuits=original.Circuits.SetItem(0,circuit with{Track=circuit.Track with{Segments=new[]{circuit.Track.Segments![0] with{BaselineTime=0},circuit.Track.Segments![1]}}})},
   "consumer"=>original with{ActiveConsumers=original.ActiveConsumers.Add("driver.headlineStars")},
   "missing-consumer"=>original with{ActiveConsumers=[]},
   "pool"=>original with{Personnel=original.Personnel with{People=original.Personnel.People.Where(p=>p.Role!="Manager").ToImmutableArray()}},
   // Every individual price is affordable at 95; the global unique completion envelope requires 96.
   "budget"=>original with{Personnel=original.Personnel with{StartingCredit=95}},
   "reserve-consumer"=>original with{Personnel=original.Personnel with{People=original.Personnel.People.Select(p=>p.Id=="driver-yuki-tsunoda"?p with{Driver=p.Driver! with{Preferences=[new("telepathy","Telepathy")]}}:p).ToImmutableArray()}},
   "staff-consumer"=>original with{Personnel=original.Personnel with{People=original.Personnel.People.Select(p=>p.Role=="Manager"?p with{Attributes=p.Attributes!.Add("Telepathy",99)}:p).ToImmutableArray()}},
   "source"=>original with{Sources=original.Sources.SetItem(0,original.Sources[0] with{DataVersion="unreviewed-v999"})},
   _=>original with{Assets=original.Assets.Add(new("asset-missing","unmapped-person",null,"Illustrative"))}
  };
  var token=await LobbyTests.Identity(false);var input=new CreateCampaignInput(Guid.NewGuid().ToString(),"Host",ContentVersion:ReleaseContent.PilotVersion);
  await using(var server=Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton(new CampaignContentCatalog([broken]))))))
  {
   using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
   Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/campaigns",input)).StatusCode);
  }
  await using var corrected=Server();using var retry=corrected.CreateClient();retry.DefaultRequestHeaders.Authorization=new("Bearer",token);
  (await retry.PostAsJsonAsync("/api/campaigns",input)).EnsureSuccessStatusCode();
 }
 private static WebApplicationFactory<Program> Server()=>LobbyTests.Server().WithWebHostBuilder(b=>b.UseSetting("Wakeups:Mode","manual").ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")!)))));
 [Fact]
 public async Task Unsupported_saved_candidate_version_preserves_saved_inputs_and_supports_restoring_compatible_deployment()
 {
  var token=await LobbyTests.Identity(false);var durable=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")!);
  await using var server=Server();using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var response=await client.PostAsJsonAsync("/api/campaigns",new CreateCampaignInput(Guid.NewGuid().ToString(),"Host",ContentVersion:ReleaseContent.PilotVersion));response.EnsureSuccessStatusCode();
  var joined=(await response.Content.ReadFromJsonAsync<CampaignJoined>())!;var id=joined.Campaign.CampaignId;
  var saved=durable.Read("campaign-"+id)!;var future=JsonNode.Parse(saved.Payload)!;future["input"]!["contentVersion"]="release-2027-v99";future["contentVersion"]="release-2027-v99";
  var futureJson=future.ToJsonString();var version=durable.Write("campaign-"+id,saved.Version,futureJson,null);
  var rejected=await client.GetAsync($"/api/campaigns/{id}?deviceId={joined.Weekend.Membership.DeviceId}");Assert.Equal(HttpStatusCode.Conflict,rejected.StatusCode);
  Assert.Equal(futureJson,durable.Read("campaign-"+id)!.Payload);
  durable.Write("campaign-"+id,version,saved.Payload,null);
  var restored=await client.GetFromJsonAsync<CampaignSnapshot>($"/api/campaigns/{id}?deviceId={joined.Weekend.Membership.DeviceId}");Assert.Equal(ReleaseContent.PilotVersion,restored!.ContentVersion);
 }
 [Fact]
 public async Task Public_preparation_keeps_identical_twenty_two_car_full_race_inputs_after_content_catalogue_removal()
 {
  var token=await LobbyTests.Identity(false);CampaignJoined joined;string before;
  await using(var server=Server())
  {
   using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
   var created=await client.PostAsJsonAsync("/api/campaigns",new CreateCampaignInput(Guid.NewGuid().ToString(),"Host",Mode:"Family",ContentVersion:ReleaseContent.PilotVersion));created.EnsureSuccessStatusCode();joined=(await created.Content.ReadFromJsonAsync<CampaignJoined>())!;
   foreach(var team in joined.Campaign.Teams)
   {
    var added=await client.PostAsJsonAsync($"/api/lobbies/{joined.Campaign.WeekendId}/participants",new SharedParticipantInput(team.TeamId,joined.Weekend.Membership.DeviceId));added.EnsureSuccessStatusCode();
    var participant=(await added.Content.ReadFromJsonAsync<LobbySnapshot>())!.Players.Single(p=>p.Name==team.TeamId);
    (await client.PostAsJsonAsync($"/api/lobbies/{joined.Campaign.WeekendId}/assignments",new AssignTeamInput(team.TeamId,participant.PlayerId,joined.Weekend.Membership.DeviceId,"Open"))).EnsureSuccessStatusCode();
   }
   (await client.PostAsJsonAsync($"/api/campaigns/{joined.Campaign.CampaignId}/prepare",new{})).EnsureSuccessStatusCode();
   before=await FullRace(client,joined);
  }
  await using var recovered=Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton(new CampaignContentCatalog([])))));
  using var retry=recovered.CreateClient();retry.DefaultRequestHeaders.Authorization=new("Bearer",token);
  Assert.Equal(before,await FullRace(retry,joined));
 }
 private static async Task<string> FullRace(HttpClient client,CampaignJoined joined)
 {
  var prepared=new List<EffectiveWeekend>();
  foreach(var team in joined.Campaign.Teams)
  {
   var own=(await client.GetFromJsonAsync<PreparationTeamSnapshot>($"/api/lobbies/{joined.Campaign.WeekendId}/preparation/open/{team.TeamId}"))!;
   prepared.Add(WeekendPreparation.Prepare(own.Preset,6,new()));
  }
  var engine=new RaceEngine(prepared[0].Configuration,prepared.SelectMany(p=>p.Cars.Select(c=>c.RaceDriver)).ToArray(),123);
  while(!engine.GetState().Finished)engine.ResolvePhase();
  var result=engine.GetState();Assert.Equal(22,result.Classification.Count);Assert.All(result.Classification,row=>Assert.Equal(6,row.Laps));Assert.True(double.IsFinite(result.Time)&&result.Time>0);
  return JsonSerializer.Serialize(result);
 }
 [Fact]
 public async Task Lost_creation_acknowledgement_retries_the_reserved_candidate_after_deployment_catalogue_removal()
 {
  var token=await LobbyTests.Identity(false);var input=new CreateCampaignInput(Guid.NewGuid().ToString(),"Host",ContentVersion:ReleaseContent.PilotVersion);
  var durable=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")!);
  await using(var server=Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ICampaignStore>(new LostCreationAcknowledgement(new CampaignStore(durable)))))))
  {
   using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
   Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.PostAsJsonAsync("/api/campaigns",input)).StatusCode);
  }
  await using var recovered=Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton(new CampaignContentCatalog([])))));
  using var retry=recovered.CreateClient();retry.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var first=await retry.PostAsJsonAsync("/api/campaigns",input);first.EnsureSuccessStatusCode();var again=await retry.PostAsJsonAsync("/api/campaigns",input);again.EnsureSuccessStatusCode();
  Assert.Equal(await first.Content.ReadAsStringAsync(),await again.Content.ReadAsStringAsync());
  Assert.Equal(ReleaseContent.PilotVersion,(await again.Content.ReadFromJsonAsync<CampaignJoined>())!.Campaign.ContentVersion);
 }
 private sealed class LostCreationAcknowledgement(ICampaignStore inner):ICampaignStore
 {
  private bool fail=true;
  public StoredCampaign? Read(string id)=>inner.Read(id);
  public string Write(string id,string? expectedVersion,CampaignRecord record)
  {
   var version=inner.Write(id,expectedVersion,record);
   if(fail){fail=false;throw new IOException("Committed creation acknowledgement lost");}
   return version;
  }
 }
}
