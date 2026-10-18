using System.Net.Http.Json;
using Game.Application;
using Game.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Game.Api.Tests;
public sealed partial class QualifyingTests
{
    [Fact]
    public async Task Opening_incident_opens_special_window_before_normal_choices_and_recovers_exact_frozen_start()
    {
        var store=Store();var clock=new Clock();var token=await LobbyTests.Identity(false);
        Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> Host()=>Server(store,clock).WithWebHostBuilder(builder=>builder.ConfigureServices(services=>services.AddSingleton<IOpeningDisruptionPolicy>(new OpeningLabPolicy())));
        await using var server=Host();using var client=server.CreateClient();var(id,device)=await Ready(client,token);
        var root=$"/api/lobbies/{id}/qualifying";
        (await client.PostAsJsonAsync(root,await StartCommand(client,id))).EnsureSuccessStatusCode();
        for(var stage=1;stage<=3;stage++)
        {
            clock.Advance(30);await server.Services.GetRequiredService<SessionCoordinator>().SweepWakeups();
            if(stage<3){var state=(await client.GetFromJsonAsync<QualifyingPublicSnapshot>(root))!;(await client.PostAsJsonAsync(root+"/continue",new DueTransitionInput(Guid.NewGuid().ToString(),stage,state.WindowRevision))).EnsureSuccessStatusCode();}
        }
        var route=$"/api/sessions/{id}";var opening=(await client.GetFromJsonAsync<PublicSnapshot>(route))!;
        Assert.NotNull(opening.SafetyCar);Assert.Equal("awaiting-decisions",opening.Lifecycle);Assert.Equal(0,opening.Phase);Assert.Equal(0,opening.LeaderLap);
        Assert.StartsWith("Safety Car",opening.Opening!.Boundary);Assert.Equal(22,opening.Opening.Cars.Count);
        Assert.Contains(opening.Events,e=>e.Kind is "MechanicalFailure" or "Retirement");
        await using var recovered=Host();using var restored=recovered.CreateClient();restored.DefaultRequestHeaders.Authorization=client.DefaultRequestHeaders.Authorization;
        var saved=(await restored.GetFromJsonAsync<PublicSnapshot>(route))!;
        Assert.Equal(opening.RaceTime,saved.RaceTime);Assert.Equal(System.Text.Json.JsonSerializer.Serialize(opening.Classification),System.Text.Json.JsonSerializer.Serialize(saved.Classification));
        Assert.Equal(opening.Opening.Boundary,saved.Opening!.Boundary);
        var own=(await restored.GetFromJsonAsync<TeamSnapshot>($"/api/lobbies/{id}/teams/team-01?deviceId={device}"))!;
        Assert.Equal("SafetyCar",own.WindowKind);
        var command=new SafetyCarTeamCommand(Guid.NewGuid().ToString(),own.Phase,own.CommandRevision,own.Cars.Where(car=>car.SafetyCar?.Status=="available").Select(car=>new SafetyCarCarCommand(car.DriverId,"Stay Out",null)).ToArray());
        (await restored.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/safety-car-plans?deviceId={device}",command)).EnsureSuccessStatusCode();
        var result=(await restored.GetFromJsonAsync<PublicSnapshot>(route))!;
        Assert.True(result.RaceTime>opening.RaceTime);Assert.True(BrowserReplay.Verify(recovered.Services.GetRequiredService<SessionCoordinator>().DesignerRecord(id)).Matches);
    }
    private sealed class OpeningLabPolicy:IOpeningDisruptionPolicy
    {
        public OpeningDisruptionConfiguration? ForCircuit(string circuitId)=>new(new(.2,38),new(FailureRate:.01,ComponentWearPerSecond:0,InterruptMajorIncidents:true));
    }
}
