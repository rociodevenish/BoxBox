using Game.Application;
using Game.TestFixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
namespace Game.Api.Tests;
public sealed class StopBoundaryTests
{
    [Theory]
    [InlineData(600,true,false)]
    [InlineData(350,false,true)]
    public async Task Real_pending_stop_projects_through_authorised_subscriptions_and_replacement_rejects_both_cars_atomically(double gap,bool queued,bool inPits)
    {
        await using var server=new WebApplicationFactory<Program>().WithWebHostBuilder(builder=>builder.ConfigureServices(services=>services.AddSingleton<IRacePresets>(new BoundaryRacePresets(gap))));
        using var client=server.CreateClient();
        var created=await(await client.PostAsJsonAsync("/api/sessions",new{preset="open-equal",laps=10,seed=123})).Content.ReadFromJsonAsync<JsonElement>();
        var id=created.GetProperty("sessionId").GetString()!;var capability=created.GetProperty("controllerCapability").GetString()!;var route="/api/sessions/"+id;
        client.DefaultRequestHeaders.Authorization=new("Bearer",capability);
        var first=Plan(0,1,"Standard","Pit","Hard");(await client.PostAsJsonAsync(route+"/plans",first)).EnsureSuccessStatusCode();
        var ready=await client.GetFromJsonAsync<JsonElement>(route);
        await using var hub=new HubConnectionBuilder().WithUrl(new Uri(server.Server.BaseAddress,"/hubs/race"),options=>{options.Transports=HttpTransportType.LongPolling;options.HttpMessageHandlerFactory=_=>server.Server.CreateHandler();}).Build();
        await hub.StartAsync();
        await Assert.ThrowsAsync<HubException>(()=>hub.InvokeAsync<JsonElement>("SubscribeTeam",id,id));
        var own=await hub.InvokeAsync<JsonElement>("SubscribeTeam",id,capability);
        var slow=own.GetProperty("cars").EnumerateArray().Single(car=>car.GetProperty("driverId").GetString()=="driver-02");
        Assert.Equal(queued,slow.GetProperty("stopQueued").GetBoolean());Assert.Equal(inPits,slow.GetProperty("inPits").GetBoolean());
        var display=await hub.InvokeAsync<JsonElement>("SubscribePublic",id);
        Assert.Equal(inPits?"in-pit-lane":"on-track",display.GetProperty("classification").EnumerateArray().Single(car=>car.GetProperty("driverId").GetString()=="driver-02").GetProperty("pitState").GetString());
        var beforePublic=await client.GetStringAsync(route);var beforeTeam=await client.GetStringAsync(route+"/team");
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync(route+"/plans",Plan(1,own.GetProperty("revision").GetInt64(),"Push","Pit","Soft"))).StatusCode);
        PublicSnapshotAssertions.EqualState(beforePublic,await client.GetStringAsync(route));Assert.Equal(beforeTeam,await client.GetStringAsync(route+"/team"));
        var unchanged=await hub.InvokeAsync<JsonElement>("SubscribeTeam",id,capability);Assert.Equal(JsonValueKind.Null,unchanged.GetProperty("confirmedPlan").ValueKind);
        (await client.PostAsJsonAsync(route+"/plans",Plan(1,own.GetProperty("revision").GetInt64(),"Push","Stay Out",null))).EnsureSuccessStatusCode();
    }
    private static object Plan(int phase,long revision,string firstPace,string secondPit,string? secondCompound)=>new{commandId=Guid.NewGuid().ToString(),phase,revision,cars=new[]{
        new{driverId="driver-01",pace=firstPace,pit="Stay Out",compound=(string?)null,battle="Neutral"},
        new{driverId="driver-02",pace="Standard",pit=secondPit,compound=secondCompound,battle="Neutral"}}};
}
