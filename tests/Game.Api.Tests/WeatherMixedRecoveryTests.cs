using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Game.Application;
using Game.TestFixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Game.Api.Tests;

public class WeatherMixedRecoveryTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Mixed_weather_result_save_failure_recovers_one_operation_and_preserves_neutralisation_deadline(bool commitFirst)
    {
        using var durable=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")??Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        var failures=new Failure(durable,commitFirst);var delivery=new Delivery();
        WebApplicationFactory<Program> Host(ISessionStore store)=>DurableRecoveryTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>{
            s.AddSingleton(store);s.AddSingleton<IRacePresets,WeatherAdviceRacePresets>();s.AddSingleton<ISessionNotifications>(delivery);
        }));
        await using var server=Host(failures);using var client=server.CreateClient();
        var created=(await(await client.PostAsJsonAsync("/api/sessions",new CreateSessionInput("open-mixed",10,123))).Content.ReadFromJsonAsync<SessionCreated>())!;
        client.DefaultRequestHeaders.Authorization=new("Bearer",created.ControllerCapability);var route="/api/sessions/"+created.SessionId;
        var own=(await client.GetFromJsonAsync<TeamSnapshot>(route+"/team"))!;
        TeamCommand Command(TeamSnapshot state,string pace,string? kind=null)=>new(Guid.NewGuid().ToString(),state.Phase,state.Revision,
            state.Cars.Where(c=>c.SafetyCar?.Status is not ("retired" or "finished")).Select(c=>new CarCommand(c.DriverId,pace,"Stay Out",null,"Neutral")).ToArray(),kind);
        (await client.PostAsJsonAsync(route+"/plans",Command(own,"Standard"))).EnsureSuccessStatusCode();
        var trigger=(await client.GetFromJsonAsync<PublicSnapshot>(route))!;
        Assert.Equal(new[]{"Weather","SafetyCar"},trigger.WeatherReaction!.Causes);
        var originalDeadline=trigger.WeatherReaction.SafetyCarUntil??trigger.RaceTime+180;
        own=(await client.GetFromJsonAsync<TeamSnapshot>(route+"/team"))!;var forecast=JsonSerializer.Serialize(own.Forecast);
        await using(var readHost=Host(durable)){using var read=readHost.CreateClient();read.DefaultRequestHeaders.Authorization=client.DefaultRequestHeaders.Authorization;
            var saved=(await read.GetFromJsonAsync<TeamSnapshot>(route+"/team"))!;Assert.Equal(forecast,JsonSerializer.Serialize(saved.Forecast));}
        var command=Command(own,"Conserve","Weather");delivery.Snapshots.Clear();failures.Remaining=2;
        Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.PostAsJsonAsync(route+"/plans",command)).StatusCode);
        Assert.All(delivery.Snapshots,snapshot=>Assert.Equal(trigger.RaceTime,snapshot.RaceTime));
        await using var recovered=Host(durable);using var restored=recovered.CreateClient();restored.DefaultRequestHeaders.Authorization=client.DefaultRequestHeaders.Authorization;
        var accepted=await restored.PostAsJsonAsync(route+"/plans",command);accepted.EnsureSuccessStatusCode();
        var after=(await restored.GetFromJsonAsync<PublicSnapshot>(route))!;
        if(after.WeatherReaction?.SafetyCarActive==true)Assert.Equal(originalDeadline,after.WeatherReaction.SafetyCarUntil);
        var retry=await restored.PostAsJsonAsync(route+"/plans",command);Assert.Equal(await accepted.Content.ReadAsStringAsync(),await retry.Content.ReadAsStringAsync());
        await using var second=Host(durable);using var reader=second.CreateClient();var again=(await reader.GetFromJsonAsync<PublicSnapshot>(route))!;
        Assert.Equal(after.RaceTime,again.RaceTime);Assert.Equal(JsonSerializer.Serialize(after.Classification),JsonSerializer.Serialize(again.Classification));
        var replay=second.Services.GetRequiredService<SessionCoordinator>().DesignerRecord(created.SessionId);
        Assert.Equal(new[]{"Normal","Weather"},replay.OperationKinds);Assert.True(BrowserReplay.Verify(replay).Matches);
    }
    private sealed class Delivery:ISessionNotifications
    {
        public List<PublicSnapshot> Snapshots{get;}=[];
        public Task Publish(PublicSnapshot display,TeamSnapshot own){Snapshots.Add(display);return Task.CompletedTask;}
        public Task PublishHousehold(PublicSnapshot display,IReadOnlyList<TeamSnapshot> teams,IReadOnlyList<OpenTeamSnapshot> openTeams){Snapshots.Add(display);return Task.CompletedTask;}
    }
    private sealed class Failure(ISessionStore inner,bool commitFirst):ISessionStore
    {
        public int Remaining{get;set;}
        public StoredSession? Read(string id)=>inner.Read(id);
        public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);
        public IReadOnlyList<string> ListSessions()=>inner.ListSessions();
        public string Write(string id,string? expected,string payload,string? code)=>Write(id,expected,payload,code,null);
        public string Write(string id,string? expected,string payload,string? code,ScheduledWakeup? wakeup)
        {
            if(Remaining<=0||--Remaining>0)return inner.Write(id,expected,payload,code,wakeup);
            if(commitFirst)inner.Write(id,expected,payload,code,wakeup);throw new IOException("Weather outcome storage failure");
        }
    }
}

