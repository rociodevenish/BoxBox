using System.Collections.Immutable;
using System.Net.Http.Json;
using System.Text.Json;
using Game.Application;
using Game.Simulation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace Game.Api.Tests;
public sealed class ContentCardTests
{
 [Theory]
 [InlineData("Manager")][InlineData("Race Engineer")][InlineData("Technical Engineer")][InlineData("Driver")]
 public async Task New_pack_rejects_card_effect_labels_that_disagree_with_supported_role(string role)
 {
  var original=ReleaseContent.Cards();var person=original.Personnel.People.First(p=>p.Role==role);
  var display=person.Presentation! with{ImplementedAttributes=person.Presentation!.ImplementedAttributes.Add("unsupported-effect")};
  var changed=person with{Presentation=display,Driver=person.Driver is {} driver?driver with{Presentation=display}:null};
  var broken=original with{Personnel=original.Personnel with{People=original.Personnel.People.Replace(person,changed)}};
  var token=await LobbyTests.Identity(false);
  await using var server=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton(new CampaignContentCatalog([broken])))));
  using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var result=await client.PostAsJsonAsync("/api/campaigns",new{commandId=Guid.NewGuid().ToString(),name="Effect host",calendar="full",contentVersion=ReleaseContent.CardVersion});
  Assert.Equal(System.Net.HttpStatusCode.BadRequest,result.StatusCode);
 }

 [Theory]
 [InlineData("https://example.test/a.webp",false)][InlineData("//example.test/a.webp",false)][InlineData("/content/../secret.webp",false)]
 [InlineData("/content/%2e%2e/secret.webp",false)][InlineData("/content/unmapped.webp",false)]
 [InlineData("/content/0000000000000000000000000000000000000000000000000000000000000000.webp",false)]
 [InlineData("/content/0000000000000000000000000000000000000000000000000000000000000000.webp",true)]
 public async Task New_pack_rejects_unsafe_or_unmapped_frozen_image_reference_before_creating_a_campaign(string url,bool synchronizeAsset)
 {
  var original=ReleaseContent.Cards();var person=original.Personnel.People.First(p=>p.Driver is not null);
  var display=person.Presentation! with{Image=person.Presentation!.Image! with{Url=url}};
  var changed=person with{Presentation=display,Driver=person.Driver! with{Presentation=display}};
  var broken=original with{Personnel=original.Personnel with{People=original.Personnel.People.Replace(person,changed)}};
  if(synchronizeAsset)broken=broken with{Assets=broken.Assets.Select(a=>a.Id==display.Image!.AssetId?a with{RuntimeUrl=url}:a).ToImmutableArray()};
  var token=await LobbyTests.Identity(false);
  await using var server=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton(new CampaignContentCatalog([broken])))));
  using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var result=await client.PostAsJsonAsync("/api/campaigns",new{commandId=Guid.NewGuid().ToString(),name="Image host",calendar="full",contentVersion=ReleaseContent.CardVersion});
  Assert.Equal(System.Net.HttpStatusCode.BadRequest,result.StatusCode);
 }
 [Fact]
 public async Task Full_release_driver_cards_keep_authored_groups_actual_prices_and_asset_identity_after_catalogue_update_and_restart()
 {
  var token=await LobbyTests.Identity(false);var command=Guid.NewGuid().ToString();
  Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> Server(bool empty=false)=>LobbyTests.Server().WithWebHostBuilder(b=>b.UseSetting("Wakeups:Mode","manual").UseSetting("Persistence:ProjectId","demo-f1game").UseSetting("Persistence:EmulatorHost",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST")).ConfigureServices(s=>{if(empty)s.Replace(ServiceDescriptor.Singleton(new CampaignContentCatalog([])));}));
  string uri,saved;
  await using(var server=Server())
  {
   using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
   var result=await client.PostAsJsonAsync("/api/campaigns",new{commandId=command,name="Card host",calendar="full",contentVersion="release-2026-cards-v1"});
   Assert.True(result.IsSuccessStatusCode,await result.Content.ReadAsStringAsync());
   var joined=(await result.Content.ReadFromJsonAsync<CampaignJoined>())!;var id=joined.Weekend.Lobby.SessionId;var device=joined.Weekend.Membership.DeviceId;
   Assert.Equal(23,joined.Campaign.Calendar.Count);
   (await client.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-04",deviceId=device})).EnsureSuccessStatusCode();
   (await client.PostAsJsonAsync($"/api/campaigns/{joined.Campaign.CampaignId}/prepare",new{commandId=Guid.NewGuid().ToString()})).EnsureSuccessStatusCode();
   uri=$"/api/lobbies/{id}/preparation/teams/team-04?deviceId={device}";
   saved=await client.GetStringAsync(uri);var own=JsonDocument.Parse(saved).RootElement;
   var antonelli=own.GetProperty("preset").GetProperty("cars").EnumerateArray().Select(c=>c.GetProperty("driver")).Single(d=>d.GetProperty("slug").GetString()=="andrea-kimi-antonelli");
   var display=antonelli.GetProperty("presentation");
   Assert.Equal("Andrea Kimi Antonelli",antonelli.GetProperty("name").GetString());
   Assert.Equal(5,display.GetProperty("simpleGroups").EnumerateObject().Count());
   Assert.Equal(24,display.GetProperty("price").GetInt32());
   Assert.StartsWith("/content/",display.GetProperty("image").GetProperty("url").GetString());
   Assert.Equal("real-driver",display.GetProperty("identityKind").GetString());
   Assert.DoesNotContain("provenance",display.GetRawText(),StringComparison.OrdinalIgnoreCase);
   var publicState=await client.GetStringAsync($"/api/lobbies/{id}/preparation");
   Assert.DoesNotContain("preferences",publicState);Assert.DoesNotContain("presentation",publicState);
  }
  await using var recovered=Server(true);using var retry=recovered.CreateClient();retry.DefaultRequestHeaders.Authorization=new("Bearer",token);
  Assert.Equal(saved,await retry.GetStringAsync(uri));
 }
}
