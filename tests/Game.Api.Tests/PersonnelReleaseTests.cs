using System.Net.Http.Json;
using System.Collections.Immutable;
using System.Text.Json;
using Game.Application;
using Game.Domain;
using Game.Simulation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace Game.Api.Tests;
public sealed class PersonnelReleaseTests
{
 [Theory]
 [InlineData("paid-missing")][InlineData("paid-price")][InlineData("unique-drivers")][InlineData("affordability")][InlineData("fictional-provenance")][InlineData("unsupported-effect")][InlineData("free-package")]
 public void Invalid_personnel_or_paid_options_fail_before_reserving_creation(string fault)
 {
  var good=ReleaseContent.PersonnelCandidate();var broken=fault switch
  {
   "paid-missing"=>good with{PaidPackageOptions=[]},
   "paid-price"=>good with{PaidPackageOptions=good.PaidPackageOptions.SetItem(0,good.PaidPackageOptions[0] with{Cost=1})},
   "unique-drivers"=>good with{Personnel=good.Personnel with{People=good.Personnel.People.Where(p=>p.Role!="Driver"||good.Constructors.SelectMany(t=>t.DriverIds).Contains(p.Id)).ToImmutableArray()},PersonnelDetails=good.PersonnelDetails.Where(p=>p.Role!="Driver"||good.Constructors.SelectMany(t=>t.DriverIds).Contains(p.PersonId)).ToImmutableArray()},
   "affordability"=>good with{Personnel=good.Personnel with{StartingCredit=95}},
   "fictional-provenance"=>good with{PersonnelDetails=good.PersonnelDetails.Select(p=>p.Role=="Manager"?p with{IdentityKind="real-staff"}:p).ToImmutableArray()},
   "unsupported-effect"=>good with{PersonnelDetails=good.PersonnelDetails.SetItem(0,good.PersonnelDetails[0] with{ImplementedAttributes=good.PersonnelDetails[0].ImplementedAttributes.Add("headlineStars")})},
   _=>good with{Packages=good.Packages.Add(PackageDevelopment.PreviewOutcome(good.PaidPackageOptions[0],"normal"))}
  };
  var sessions=new VolatileSessionStore();var store=new CampaignStore(sessions);var weekends=new SessionCoordinator(new SilentNotifications(),new DryRacePresets(),sessions);
  var input=new CreateCampaignInput(Guid.NewGuid().ToString(),"Host",ContentVersion:ReleaseContent.PersonnelVersion);
  var rejected=new CampaignCoordinator(weekends,store,contentCatalog:new([broken]));
  Assert.Equal(SessionFailure.InvalidInput,Assert.Throws<SessionFault>(()=>rejected.Create(new("host",false),input)).Failure);
  var corrected=new CampaignCoordinator(weekends,store,contentCatalog:new([good]));
  Assert.Equal(ReleaseContent.PersonnelVersion,corrected.Create(new("host",false),input).Campaign.ContentVersion);
 }
 private sealed class SilentNotifications:ISessionNotifications
 {
  public Task Publish(PublicSnapshot display,TeamSnapshot team)=>Task.CompletedTask;
  public Task PublishHousehold(PublicSnapshot display,IReadOnlyList<TeamSnapshot> teams,IReadOnlyList<OpenTeamSnapshot> openTeams)=>Task.CompletedTask;
 }
 [Fact]
 public async Task Personnel_candidate_opens_unique_affordable_rosters_with_truthful_role_labels()
 {
  var token=await LobbyTests.Identity(false);
  await using var server=LobbyTests.Server().WithWebHostBuilder(b=>b.UseSetting("Wakeups:Mode","manual").ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")!)))));
  using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var response=await client.PostAsJsonAsync("/api/campaigns",new CreateCampaignInput(Guid.NewGuid().ToString(),"Roster host",PersonnelMode:"acquisition",ContentVersion:"release-2026-personnel-rc1"));
  Assert.True(response.IsSuccessStatusCode,await response.Content.ReadAsStringAsync());
  var joined=(await response.Content.ReadFromJsonAsync<CampaignJoined>())!;
  Assert.Equal(11,joined.Campaign.Teams.Count);Assert.Equal(77,joined.Campaign.Acquisition!.RequiredSlots);
  var team=joined.Campaign.Acquisition.ActiveTeamId!;var device=joined.Weekend.Membership.DeviceId;
  (await client.PostAsJsonAsync($"/api/lobbies/{joined.Campaign.WeekendId}/claims",new ClaimTeamInput(team,device))).EnsureSuccessStatusCode();
  var own=(await client.GetFromJsonAsync<AcquisitionTeamSnapshot>($"/api/campaigns/{joined.Campaign.CampaignId}/acquisition/teams/{team}?deviceId={device}"))!;
  Assert.Equal(96,own.CompletionReserve);Assert.Equal(100,own.Balance);
  Assert.All(own.Offers,p=>Assert.Contains("Fictional",p.EffectLabel));
 }
 [Fact]
 public async Task Every_team_hires_seven_unique_people_retains_car_reserve_and_recovers_final_hire()
 {
  var token=await LobbyTests.Identity(false);var durable=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")!);
  var input=new CreateCampaignInput(Guid.NewGuid().ToString(),"Complete rosters",PersonnelMode:"acquisition",ContentVersion:ReleaseContent.PersonnelVersion);
  await using var server=LobbyTests.Server().WithWebHostBuilder(b=>b.UseSetting("Wakeups:Mode","manual").ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(durable))));
  using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var created=await client.PostAsJsonAsync("/api/campaigns",input);Assert.True(created.IsSuccessStatusCode,await created.Content.ReadAsStringAsync());
  var joined=(await created.Content.ReadFromJsonAsync<CampaignJoined>())!;var device=joined.Weekend.Membership.DeviceId;var id=joined.Campaign.CampaignId;
  foreach(var team in joined.Campaign.Teams)
  {
   var added=await client.PostAsJsonAsync($"/api/lobbies/{joined.Campaign.WeekendId}/participants",new SharedParticipantInput(team.TeamId,device));added.EnsureSuccessStatusCode();
   var player=(await added.Content.ReadFromJsonAsync<LobbySnapshot>())!.Players.Single(p=>p.Name==team.TeamId);
   (await client.PostAsJsonAsync($"/api/lobbies/{joined.Campaign.WeekendId}/assignments",new AssignTeamInput(team.TeamId,player.PlayerId,device,"SharedTablet"))).EnsureSuccessStatusCode();
  }
  PersonnelHire? last=null;string path="";var bought=new HashSet<string>();
  for(var pick=0;pick<77;pick++)
  {
   var campaign=(await client.GetFromJsonAsync<CampaignSnapshot>($"/api/campaigns/{id}?deviceId={device}"))!;var team=campaign.Acquisition!.ActiveTeamId!;
   path=$"/api/campaigns/{id}/acquisition/teams/{team}?deviceId={device}";var own=(await client.GetFromJsonAsync<AcquisitionTeamSnapshot>(path))!;
   var legal=own.HireOptions.Where(o=>o.CanHire).Select(o=>own.Offers.Single(p=>p.Id==o.PersonId)).OrderByDescending(p=>p.Price).ThenBy(p=>p.Id).First();
   Assert.True(bought.Add(legal.Id));last=new(Guid.NewGuid().ToString(),own.Revision,team,legal.Id);
   var response=await client.PostAsJsonAsync(path.Replace("?","/hire?"),last);response.EnsureSuccessStatusCode();
   var accepted=(await response.Content.ReadFromJsonAsync<AcquisitionTeamSnapshot>())!;Assert.True(accepted.Balance>=accepted.CompletionReserve);
  }
  Assert.Equal(77,bought.Count);
  await using var recovered=LobbyTests.Server().WithWebHostBuilder(b=>b.UseSetting("Wakeups:Mode","manual").ConfigureServices(s=>{s.Replace(ServiceDescriptor.Singleton<ISessionStore>(durable));s.Replace(ServiceDescriptor.Singleton(new CampaignContentCatalog([])));}));
  using var retry=recovered.CreateClient();retry.DefaultRequestHeaders.Authorization=new("Bearer",token);
  (await retry.PostAsJsonAsync(path.Replace("?","/hire?"),last)).EnsureSuccessStatusCode();
  foreach(var team in joined.Campaign.Teams)
  {
   var own=(await retry.GetFromJsonAsync<AcquisitionTeamSnapshot>($"/api/campaigns/{id}/acquisition/teams/{team.TeamId}?deviceId={device}"))!;
   Assert.True(own.RosterReady);Assert.Empty(own.RemainingSlots);Assert.Equal(7,own.Hires.Count);Assert.Equal(18,own.CompletionReserve);Assert.True(own.Balance>=18);
   Assert.Single(own.Roster,p=>p.Role=="Manager");Assert.Equal(2,own.Roster.Count(p=>p.Role=="Driver"));Assert.Equal(2,own.Roster.Count(p=>p.Role=="Race Engineer"));Assert.Equal(2,own.Roster.Count(p=>p.Role=="Technical Engineer"));
   Assert.All(own.Roster.Where(p=>p.Role!="Driver"),p=>Assert.Contains("Fictional",p.EffectLabel));
   Assert.DoesNotContain(own.Roster,p=>p.Role=="Pit Crew");
   var constructionPath=$"/api/campaigns/{id}/construction/teams/{team.TeamId}?deviceId={device}";
   var construction=(await retry.GetFromJsonAsync<ConstructionTeamSnapshot>(constructionPath))!;Assert.Equal(18,construction.Preview.Cost);
   var confirmation=new ConstructionConfirm(Guid.NewGuid().ToString(),construction.Draft.Revision,team.TeamId,construction.Catalogue.Version);
   var boughtCar=await retry.PostAsJsonAsync(constructionPath.Replace("?","/confirm?"),confirmation);boughtCar.EnsureSuccessStatusCode();
   var confirmed=(await boughtCar.Content.ReadFromJsonAsync<ConstructionTeamSnapshot>())!;Assert.Equal(18,confirmed.Confirmed!.Cost);
   (await retry.PostAsJsonAsync(constructionPath.Replace("?","/confirm?"),confirmation)).EnsureSuccessStatusCode();
  }
  var campaignAfter=(await retry.GetFromJsonAsync<CampaignSnapshot>($"/api/campaigns/{id}?deviceId={device}"))!;Assert.True(campaignAfter.Acquisition!.AllRostersReady);Assert.Equal("created",campaignAfter.Lifecycle);
  (await retry.PostAsJsonAsync($"/api/campaigns/{id}/prepare",new{})).EnsureSuccessStatusCode();
  var preparation=(await retry.GetFromJsonAsync<PreparationTeamSnapshot>($"/api/lobbies/{joined.Campaign.WeekendId}/preparation/teams/team-01?deviceId={device}"))!;
  Assert.Equal("neutral-package-v1",Assert.Single(preparation.Preset.PackageLibrary).PackageId);
  Assert.All(preparation.Preset.Cars,car=>Assert.Contains(car.Driver.Id,bought));
 }
}
