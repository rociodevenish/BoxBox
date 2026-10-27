using System.Net.Http.Json;
using System.Text.Json;
using Game.Application;
using Game.TestFixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Game.Api.Tests;
public sealed partial class QualifyingTests
{
 [Fact] public async Task Wet_grid_precedes_simultaneous_starting_tyres_and_accepted_locks_survive_restart_timeout_and_pause()
 {
  var store=Store();var clock=new Clock();var token=await LobbyTests.Identity(false);
  await using var server=Server(store,clock).WithWebHostBuilder(b=>b.ConfigureServices(s=>s.AddSingleton<IRacePresets>(new WetTyreRacePresets())));
  using var client=server.CreateClient();var(id,device)=await Ready(client,token,true);
  var root=$"/api/lobbies/{id}/qualifying";
  (await client.PostAsJsonAsync(root,await StartCommand(client,id))).EnsureSuccessStatusCode();
  for(var stage=1;stage<=3;stage++)
  {
   var before=await client.GetFromJsonAsync<JsonElement>(root);
   Assert.Equal((stage-1)*180+90,before.GetProperty("weather").GetProperty("time").GetDouble());
   clock.Advance(60);await server.Services.GetRequiredService<SessionCoordinator>().SweepWakeups();
   var result=await client.GetFromJsonAsync<JsonElement>(root);
   if(stage<3)(await client.PostAsJsonAsync(root+"/continue",new{commandId=Guid.NewGuid().ToString(),phase=stage,windowRevision=result.GetProperty("windowRevision").GetInt64()})).EnsureSuccessStatusCode();
  }
  var grid=await client.GetFromJsonAsync<JsonElement>(root);Assert.Equal("starting-tyres",grid.GetProperty("lifecycle").GetString());Assert.Equal(22,grid.GetProperty("grid").GetArrayLength());
  Assert.Equal(System.Net.HttpStatusCode.NotFound,(await client.GetAsync($"/api/sessions/{id}")).StatusCode);
  var uri=root+$"/teams/team-01?deviceId={device}";var own=await client.GetFromJsonAsync<QualifyingTeamSnapshot>(uri);
  Assert.Equal("StartTyres",own!.Session);Assert.Equal(2,own.StartingFallback!.Count);
  var command=new StartingTyresCommand(Guid.NewGuid().ToString(),own.Revision,own.AssignmentRevision,own.EligibleDrivers.Select((d,i)=>new StartingTyreChoice(d.EntryId,i==0?"Wet":"Intermediate")).ToArray());
  var endpoint=root+$"/teams/team-01/starting-tyres?deviceId={device}";
  (await client.PostAsJsonAsync(endpoint,command)).EnsureSuccessStatusCode();
  var locked=await client.GetStringAsync(uri);
  (await client.PostAsJsonAsync($"/api/lobbies/{id}/pause",new{})).EnsureSuccessStatusCode();
  await using var restarted=Server(store,clock);using var recovered=restarted.CreateClient();recovered.DefaultRequestHeaders.Authorization=new("Bearer",token);
  Assert.Equal(locked,await recovered.GetStringAsync(uri));
  clock.Advance(600);await restarted.Services.GetRequiredService<SessionCoordinator>().SweepWakeups();
  Assert.Equal("starting-tyres",(await recovered.GetFromJsonAsync<JsonElement>(root)).GetProperty("lifecycle").GetString());
  (await recovered.PostAsJsonAsync(endpoint,command)).EnsureSuccessStatusCode();
  (await recovered.PostAsJsonAsync($"/api/lobbies/{id}/resume",new{})).EnsureSuccessStatusCode();clock.Advance(60);await restarted.Services.GetRequiredService<SessionCoordinator>().SweepWakeups();
  var race=await recovered.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{id}");Assert.NotNull(race!.Opening);Assert.True(race.Weather!.Time>=540);
  var accepted=await recovered.GetFromJsonAsync<QualifyingTeamSnapshot>(uri);Assert.Equal(command.Choices,accepted!.StartingCompounds);
  var fallback=await recovered.GetFromJsonAsync<QualifyingTeamSnapshot>(root+$"/teams/team-02?deviceId={device}");Assert.Equal("timeout-observation",fallback!.Source);Assert.Equal(fallback.StartingFallback,fallback.StartingCompounds);
  (await recovered.PostAsJsonAsync(endpoint,command)).EnsureSuccessStatusCode();
 }
}

