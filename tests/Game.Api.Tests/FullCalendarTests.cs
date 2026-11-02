using System.Text;
using System.Text.Json;
using Xunit.Abstractions;
using System.Net;
using System.Collections.Immutable;
using Game.Simulation;
using System.Net.Http.Json;
using Game.Application;
using Game.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace Game.Api.Tests;

public sealed class FullCalendarTests(ITestOutputHelper output)
{
 [Fact]
 public async Task Full_calendar_has_verified_twenty_three_event_order_and_physical_Sepang_mapping_after_restart()
 {
  var token=await LobbyTests.Identity(false);var input=new {commandId=Guid.NewGuid().ToString(),name="Host",calendar="full",contentVersion="release-2026-full-circuits-v1"};
  string first;
  await using(var server=Server())
  {
   using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
   var response=await client.PostAsJsonAsync("/api/campaigns",input);Assert.True(response.IsSuccessStatusCode,await response.Content.ReadAsStringAsync());first=await response.Content.ReadAsStringAsync();
   var joined=(await response.Content.ReadFromJsonAsync<CampaignJoined>())!;Assert.Equal(23,joined.Campaign.Calendar.Count);
   Assert.Equal(ExpectedOrder,joined.Campaign.Calendar.Select(e=>e.TrackId));
   Assert.Equal("event-bahrain-gp-malaysia-2026",joined.Campaign.Calendar[15].SourceEventId);Assert.Equal("Bahrain Grand Prix in Malaysia",joined.Campaign.Calendar[15].EventName);Assert.True(joined.Campaign.FullCalendarAvailable);
   using var measured=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")!);
   var logical=new CampaignStore(measured).Read(joined.Campaign.CampaignId)!;
   output.WriteLine("Initial full-calendar record: "+JsonSerializer.SerializeToUtf8Bytes(logical.Record,BrowserReplay.JsonOptions).Length+" logical UTF-8 bytes; "+Encoding.UTF8.GetByteCount(measured.Read("campaign-"+joined.Campaign.CampaignId)!.Payload)+" stored envelope UTF-8 bytes; 23 events / 253 frozen team presets.");
   Assert.Equal("track-albert-park",joined.Campaign.Calendar[0].TrackId);Assert.Equal("track-yas-marina",joined.Campaign.Calendar[^1].TrackId);Assert.Equal("track-sepang",joined.Campaign.Calendar[15].TrackId);
   Assert.DoesNotContain(joined.Campaign.Calendar,e=>e.TrackId=="track-jeddah"||e.TrackId=="track-sakhir");
  }
  await using var recovered=Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton(new CampaignContentCatalog([])))));using var retry=recovered.CreateClient();retry.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var again=await retry.PostAsJsonAsync("/api/campaigns",input);again.EnsureSuccessStatusCode();Assert.Equal(first,await again.Content.ReadAsStringAsync());
 }
 private static Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> Server()=>LobbyTests.Server().WithWebHostBuilder(b=>b.UseSetting("Wakeups:Mode","manual").ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")!)))));
 private static readonly string[] ExpectedOrder=["track-albert-park","track-shanghai","track-suzuka","track-miami","track-circuit-gilles-villeneuve","track-monaco","track-barcelona-catalunya","track-red-bull-ring","track-silverstone","track-spa-francorchamps","track-hungaroring","track-zandvoort","track-monza","track-madring","track-baku","track-sepang","track-marina-bay","track-cota","track-mexico-city","track-interlagos","track-las-vegas","track-lusail","track-yas-marina"];
 [Theory]
 [InlineData("route")][InlineData("order")][InlineData("missing-circuit")][InlineData("missing-profile")][InlineData("weather-bounds")][InlineData("Bahrain-venue")]
 public async Task Incomplete_or_invalid_full_coverage_cannot_reserve_a_campaign(string fault)
 {
  var pack=ReleaseContent.FullCalendar();var first=pack.Circuits[0];var withoutRoute=first.Track with{PitRoute=null};
  var broken=fault switch
  {
   "route"=>pack with{Circuits=pack.Circuits.SetItem(0,first with{Track=withoutRoute}),Conditions=pack.Conditions!.SetItem(first.Id,pack.Conditions[first.Id] with{Track=withoutRoute})},
   "order"=>pack with{Calendar=pack.Calendar.Reverse().ToImmutableArray(),Events=pack.Events.Reverse().ToImmutableArray()},
   "missing-circuit"=>pack with{Circuits=pack.Circuits.RemoveAt(0)},
   "missing-profile"=>pack with{Conditions=pack.Conditions!.Remove(first.Id)},
   "weather-bounds"=>pack with{Conditions=pack.Conditions!.SetItem(first.Id,pack.Conditions[first.Id] with{Weather=pack.Conditions[first.Id].Weather! with{MeanRain=2}})},
   _=>pack with{Events=pack.Events.SetItem(15,pack.Events[15] with{TrackId="track-monza"})}
  };
  var token=await LobbyTests.Identity(false);var input=new CreateCampaignInput(Guid.NewGuid().ToString(),"Host",Calendar:"full",ContentVersion:ReleaseContent.FullCalendarVersion);
  await using(var server=Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton(new CampaignContentCatalog([broken]))))))
  {
   using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
   Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/campaigns",input)).StatusCode);
  }
  if(fault=="route")
  {
   await using var corrected=Server();using var retry=corrected.CreateClient();retry.DefaultRequestHeaders.Authorization=new("Bearer",token);
   (await retry.PostAsJsonAsync("/api/campaigns",input)).EnsureSuccessStatusCode();
  }
 }
 [Fact]
 public async Task Unsupported_saved_full_profile_fails_without_overwriting_history_and_can_restore_compatible_inputs()
 {
  var token=await LobbyTests.Identity(false);using var durable=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")!);var store=new CampaignStore(durable);
  await using var server=Server();using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var created=await client.PostAsJsonAsync("/api/campaigns",new CreateCampaignInput(Guid.NewGuid().ToString(),"Host",Calendar:"full",ContentVersion:ReleaseContent.FullCalendarVersion));created.EnsureSuccessStatusCode();var joined=(await created.Content.ReadFromJsonAsync<CampaignJoined>())!;
  var id=joined.Campaign.CampaignId;var original=store.Read(id)!;var conditions=original.Record.EventConditions.ToDictionary();var eventId=original.Record.Calendar[0].EventId;
  conditions[eventId]=conditions[eventId] with{Weather=conditions[eventId].Weather! with{ModelVersion="unsupported-weather-v99"}};
  var damagedVersion=store.Write(id,original.Version,original.Record with{EventConditions=conditions});var damagedPayload=durable.Read("campaign-"+id)!.Payload;
  var result=await client.GetAsync($"/api/campaigns/{id}?deviceId={joined.Weekend.Membership.DeviceId}");Assert.Equal(HttpStatusCode.Conflict,result.StatusCode);Assert.Equal(damagedPayload,durable.Read("campaign-"+id)!.Payload);
  store.Write(id,damagedVersion,original.Record);
  var recovered=await client.GetFromJsonAsync<CampaignSnapshot>($"/api/campaigns/{id}?deviceId={joined.Weekend.Membership.DeviceId}");Assert.Equal(ReleaseContent.FullCalendarVersion,recovered!.ContentVersion);
 }
}
