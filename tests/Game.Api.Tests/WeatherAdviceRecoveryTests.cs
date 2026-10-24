using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Game.Application;
using Game.TestFixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
namespace Game.Api.Tests;

public sealed partial class QualifyingTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Weather_start_advice_forecasts_and_partial_locks_survive_real_Firestore_write_failure(bool commitFirst)
    {
        var store=new FailingStore(Store(),commitFirst);var clock=new Clock();var token=await LobbyTests.Identity(false);
        WebApplicationFactory<Program> Host()=>Server(store,clock).WithWebHostBuilder(b=>b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{["Wakeups:Mode"]="manual"})));
        await using var server=Host().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.AddSingleton<IRacePresets>(new WetTyreRacePresets())));
        using var client=server.CreateClient();var(id,device)=await Ready(client,token,true);
        var prep=$"/api/lobbies/{id}/preparation/teams/team-01?deviceId={device}";
        var preparation=await client.GetFromJsonAsync<JsonElement>(prep);
        Assert.True(preparation.TryGetProperty("forecast",out var preparationForecast));
        var lobby=(await client.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}"));
        foreach(var team in lobby.GetProperty("teams").EnumerateArray().Where(t=>t.GetProperty("teamId").GetString() is "team-01" or "team-02"))
            (await client.PostAsJsonAsync($"/api/lobbies/{id}/players/{team.GetProperty("playerId").GetString()}/assistance",new{helpLevel="Guided",autoAcceptRecommendation=true})).EnsureSuccessStatusCode();
        var root=$"/api/lobbies/{id}/qualifying";
        (await client.PostAsJsonAsync(root,await StartCommand(client,id))).EnsureSuccessStatusCode();
        for(var stage=1;stage<=3;stage++)
        {
            clock.Advance(60);await server.Services.GetRequiredService<SessionCoordinator>().SweepWakeups();
            var result=await client.GetFromJsonAsync<JsonElement>(root);
            if(stage<3)(await client.PostAsJsonAsync(root+"/continue",new{commandId=Guid.NewGuid().ToString(),phase=stage,windowRevision=result.GetProperty("windowRevision").GetInt64()})).EnsureSuccessStatusCode();
        }
        var uri=root+$"/teams/team-01?deviceId={device}";var own=(await client.GetFromJsonAsync<QualifyingTeamSnapshot>(uri))!;
        Assert.Equal("Guided",own.StartingGuidance!.HelpLevel);Assert.Equal(2,own.StartingGuidance.Suggested!.Count);
        var advice=JsonSerializer.Serialize(own.StartingGuidance);var forecast=JsonSerializer.Serialize(own.Forecast);
        var command=new StartingTyresCommand(Guid.NewGuid().ToString(),own.Revision,own.AssignmentRevision,own.EligibleDrivers.Select((d,i)=>new StartingTyreChoice(d.EntryId,i==0?"Wet":"Intermediate")).ToArray());
        var endpoint=root+$"/teams/team-01/starting-tyres?deviceId={device}";store.Armed=true;
        Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.PostAsJsonAsync(endpoint,command)).StatusCode);
        await using var restarted=Host();using var recovered=restarted.CreateClient();recovered.DefaultRequestHeaders.Authorization=new("Bearer",token);
        Assert.Equal(preparationForecast.GetRawText(),(await recovered.GetFromJsonAsync<JsonElement>(prep)).GetProperty("forecast").GetRawText());
        var restored=(await recovered.GetFromJsonAsync<QualifyingTeamSnapshot>(uri))!;
        Assert.Equal(advice,JsonSerializer.Serialize(restored.StartingGuidance));Assert.Equal(forecast,JsonSerializer.Serialize(restored.Forecast));
        var accepted=await recovered.PostAsJsonAsync(endpoint,command);accepted.EnsureSuccessStatusCode();
        Assert.Equal(command.Choices,(await recovered.GetFromJsonAsync<QualifyingTeamSnapshot>(uri))!.StartingCompounds);
        Assert.False((await recovered.GetFromJsonAsync<JsonElement>(root)).TryGetProperty("forecast",out _));
        clock.Advance(60);await restarted.Services.GetRequiredService<SessionCoordinator>().SweepWakeups();
        var race=(await recovered.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{id}"))!;
        Assert.True(race.Weather!.Time>=540);Assert.NotNull(race.Opening);
        var retry=await recovered.PostAsJsonAsync(endpoint,command);retry.EnsureSuccessStatusCode();Assert.Equal(await accepted.Content.ReadAsStringAsync(),await retry.Content.ReadAsStringAsync());
        await using var second=Host();using var reader=second.CreateClient();
        var again=(await reader.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{id}"))!;
        Assert.Equal(race.RaceTime,again.RaceTime);Assert.Equal(JsonSerializer.Serialize(race.Classification),JsonSerializer.Serialize(again.Classification));
    }
}


