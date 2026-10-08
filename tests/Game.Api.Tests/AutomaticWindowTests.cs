using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
namespace Game.Api.Tests;
public sealed class AutomaticWindowTests
{
 [Fact]
 public async Task Accepted_phase_opens_the_next_window_once_and_preserves_resolved_feedback_for_retry_and_display()
 {
  await using var server=new WebApplicationFactory<Program>();using var client=server.CreateClient();
  var created=await(await client.PostAsJsonAsync("/api/sessions",new{preset="open-equal",laps=6,seed=123})).Content.ReadFromJsonAsync<JsonElement>();
  var route="/api/sessions/"+created.GetProperty("sessionId").GetString();client.DefaultRequestHeaders.Authorization=new("Bearer",created.GetProperty("controllerCapability").GetString());
  var command=new{commandId=Guid.NewGuid().ToString(),phase=0,revision=1,cars=new[]{new{driverId="driver-01",pace="Push",pit="Stay Out",compound=(string?)null,battle="Neutral"},new{driverId="driver-02",pace="Standard",pit="Pit",compound=(string?)"Hard",battle="Neutral"}}};
  var response=await client.PostAsJsonAsync(route+"/plans",command);response.EnsureSuccessStatusCode();var acknowledgement=await response.Content.ReadAsStringAsync();
  var snapshot=await client.GetFromJsonAsync<JsonElement>(route);Assert.Equal("awaiting-decisions",snapshot.GetProperty("lifecycle").GetString());Assert.Equal(1,snapshot.GetProperty("phase").GetInt32());Assert.Equal(5,snapshot.GetProperty("leaderLap").GetInt32());
  Assert.Equal(0,snapshot.GetProperty("presentation").GetProperty("resolvedPhase").GetInt32());Assert.NotEmpty(snapshot.GetProperty("events").EnumerateArray());
  var own=await client.GetFromJsonAsync<JsonElement>(route+"/team");Assert.Equal(JsonValueKind.Null,own.GetProperty("confirmedPlan").ValueKind);
  var before=await client.GetStringAsync(route);Assert.Equal(acknowledgement,await(await client.PostAsJsonAsync(route+"/plans",command)).Content.ReadAsStringAsync());Assert.Equal(before,await client.GetStringAsync(route));
 }
}
