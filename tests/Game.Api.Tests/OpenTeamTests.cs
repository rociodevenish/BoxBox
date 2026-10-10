using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http.Connections;
using System.Threading.Channels;
using Xunit;
namespace Game.Api.Tests;
public sealed class OpenTeamTests
{
 [Fact]
 public async Task Mixed_open_spectator_sees_only_designated_team_and_cannot_submit()
 {
  await using var server=LobbyTests.Server();using var host=server.CreateClient();var token=await LobbyTests.Identity(false);host.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var created=await(await host.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=6,seed=123,mode="Mixed",name="Host"})).Content.ReadFromJsonAsync<JsonElement>();
  var id=created.GetProperty("lobby").GetProperty("sessionId").GetString();var member=created.GetProperty("membership");var device=member.GetProperty("deviceId").GetString();
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-01",deviceId=device})).EnsureSuccessStatusCode();
  var added=await(await host.PostAsJsonAsync($"/api/lobbies/{id}/participants",new{name="Junior",deviceId=device})).Content.ReadFromJsonAsync<JsonElement>();var child=added.GetProperty("players").EnumerateArray().Single(p=>p.GetProperty("name").GetString()=="Junior").GetProperty("playerId").GetString();
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/assignments",new{teamId="team-02",playerId=child,deviceId=device,control="Open"})).EnsureSuccessStatusCode();
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/start",new{})).EnsureSuccessStatusCode();
  using var spectator=server.CreateClient();var response=await spectator.GetAsync($"/api/lobbies/{id}/open/team-02");response.EnsureSuccessStatusCode();var open=await response.Content.ReadFromJsonAsync<JsonElement>();
  Assert.Equal("team-02",open.GetProperty("teamId").GetString());Assert.Equal(new[]{"driver-03","driver-04"},open.GetProperty("cars").EnumerateArray().Select(c=>c.GetProperty("driverId").GetString()));
  Assert.Contains("estimatedTyreCondition",open.GetRawText());Assert.Equal(new[]{"assignmentRevision","cars","confirmedPlan","lifecycle","phase","revision","sessionId","teamId"},open.EnumerateObject().Select(p=>p.Name).Order());
  foreach(var car in open.GetProperty("cars").EnumerateArray())Assert.Equal(new[]{"battleOptions","compound","driverId","estimatedTyreCondition","inPits","stopQueued","tyreAge"},car.EnumerateObject().Select(p=>p.Name).Order());
  Assert.DoesNotContain("driver-01",open.GetRawText());
  Assert.Equal(HttpStatusCode.Forbidden,(await spectator.GetAsync($"/api/lobbies/{id}/open/team-01")).StatusCode);
  await using var hub=new HubConnectionBuilder().WithUrl("http://localhost/hubs/race",options=>{options.Transports=HttpTransportType.LongPolling;options.HttpMessageHandlerFactory=_=>server.Server.CreateHandler();}).Build();
  var updates=Channel.CreateUnbounded<JsonElement>();hub.On<JsonElement>("openTeamSnapshot",value=>updates.Writer.TryWrite(value));await hub.StartAsync();
  var initial=await hub.InvokeAsync<JsonElement>("SubscribeOpenTeam",id,"team-02");Assert.Equal(open.GetRawText(),initial.GetRawText());await Assert.ThrowsAsync<HubException>(()=>hub.InvokeAsync<JsonElement>("SubscribeOpenTeam",id,"team-01"));
  object Plan(int a,int b)=>new{commandId=Guid.NewGuid().ToString(),phase=0,revision=1,cars=new[]{new{driverId=$"driver-{a:00}",pace="Push",pit="Stay Out",compound=(string?)null,battle="Neutral"},new{driverId=$"driver-{b:00}",pace="Standard",pit="Stay Out",compound=(string?)null,battle="Neutral"}}};
  Assert.Equal(HttpStatusCode.Forbidden,(await spectator.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-02/plans?deviceId={device}",Plan(3,4))).StatusCode);
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/plans?deviceId={device}",Plan(1,2))).EnsureSuccessStatusCode();using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(10));var waiting=await updates.Reader.ReadAsync(timeout.Token);Assert.Equal(JsonValueKind.Null,waiting.GetProperty("confirmedPlan").ValueKind);Assert.DoesNotContain("driver-01",waiting.GetRawText());
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-02/plans?deviceId={device}",Plan(3,4))).EnsureSuccessStatusCode();var locked=await updates.Reader.ReadAsync(timeout.Token);Assert.Equal("Push",locked.GetProperty("confirmedPlan").GetProperty("cars")[0].GetProperty("pace").GetString());
  var publicState=await spectator.GetStringAsync($"/api/sessions/{id}");Assert.DoesNotContain("confirmedPlan",publicState);Assert.DoesNotContain("estimatedTyreCondition",publicState);
 }
 [Theory]
 [InlineData("Competitive","Private",false)]
 [InlineData("Family","Open",true)]
 [InlineData("Family","Private",false)]
 [InlineData("Mixed","SharedTablet",false)]
 public async Task Only_approved_designated_open_assignments_expose_team_observations(string mode,string control,bool open)
 {
  await using var server=LobbyTests.Server();using var host=server.CreateClient();host.DefaultRequestHeaders.Authorization=new("Bearer",await LobbyTests.Identity(false));
  var created=await(await host.PostAsJsonAsync("/api/lobbies",new{preset="restricted-equal",laps=6,seed=123,mode,name="Host"})).Content.ReadFromJsonAsync<JsonElement>();var id=created.GetProperty("lobby").GetProperty("sessionId").GetString();var member=created.GetProperty("membership");
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/assignments",new{teamId="team-04",playerId=member.GetProperty("playerId").GetString(),deviceId=member.GetProperty("deviceId").GetString(),control})).EnsureSuccessStatusCode();
  if(mode=="Competitive")Assert.Equal(HttpStatusCode.BadRequest,(await host.PostAsJsonAsync($"/api/lobbies/{id}/assignments",new{teamId="team-04",playerId=member.GetProperty("playerId").GetString(),deviceId=member.GetProperty("deviceId").GetString(),control="Open"})).StatusCode);
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/start",new{})).EnsureSuccessStatusCode();using var spectator=server.CreateClient();Assert.Equal(open?HttpStatusCode.OK:HttpStatusCode.Forbidden,(await spectator.GetAsync($"/api/lobbies/{id}/open/team-04")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await spectator.GetAsync($"/api/lobbies/{id}/open/team-01")).StatusCode);
 }
}

