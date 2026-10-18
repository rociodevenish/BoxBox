using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Application;
using Game.Domain;
using Game.Simulation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Game.Api.Tests;
public sealed class DisruptionRecoveryTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Unversioned_staging_session_migrates_once_then_second_restart_replays_typed_history_exactly(bool committedBeforeMigrationFailure)
    {
        using var durable=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")??Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST")??"127.0.0.1:19100");var legacy=new LegacyStaging(durable);
        WebApplicationFactory<Program> Host(ISessionStore store)=>DurableRecoveryTests.Server().WithWebHostBuilder(builder=>builder.ConfigureServices(services=>{services.AddSingleton<IRacePresets>(new Game.TestFixtures.SafetyCarWindowPresets());services.AddSingleton(store);}));
        await using var server=Host(legacy);using var client=server.CreateClient();
        var created=(await(await client.PostAsJsonAsync("/api/sessions",new CreateSessionInput("open-equal",10,123))).Content.ReadFromJsonAsync<SessionCreated>())!;
        var route="/api/sessions/"+created.SessionId;client.DefaultRequestHeaders.Authorization=new("Bearer",created.ControllerCapability);
        (await client.PostAsJsonAsync(route+"/plans",new TeamCommand(Guid.NewGuid().ToString(),0,1,[new("driver-01","Standard","Stay Out",null,"Neutral"),new("driver-02","Standard","Stay Out",null,"Neutral")]))).EnsureSuccessStatusCode();
        var incident=(await client.GetFromJsonAsync<PublicSnapshot>(route))!;var own=(await client.GetFromJsonAsync<TeamSnapshot>(route+"/team"))!;
        Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.PostAsJsonAsync(route+"/safety-car-plans",new SafetyCarTeamCommand(Guid.NewGuid().ToString(),own.Phase,own.Revision,[new("driver-01","Pit","Hard"),new("driver-02","Stay Out",null)]))).StatusCode);
        await using var recovered=Host(new FailedBoundary(durable,"migrated",committedBeforeMigrationFailure));using var read=recovered.CreateClient();
        Assert.Equal(HttpStatusCode.ServiceUnavailable,(await read.GetAsync(route)).StatusCode);
        var result=(await read.GetFromJsonAsync<PublicSnapshot>(route))!;Assert.Equal(incident.RaceTime+180,result.RaceTime,7);
        Assert.Equal("awaiting-decisions",result.Lifecycle);
        Assert.Equal("Hard",result.Classification.Single(car=>car.DriverId=="driver-01").Compound);
        await using var again=Host(durable);using var second=again.CreateClient();var repeated=(await second.GetFromJsonAsync<PublicSnapshot>(route))!;
        Assert.Equal(result.RaceTime,repeated.RaceTime);Assert.Equal(JsonSerializer.Serialize(result.Classification),JsonSerializer.Serialize(repeated.Classification));
        var replay=again.Services.GetRequiredService<SessionCoordinator>().DesignerRecord(created.SessionId);
        Assert.Equal(new[]{"Normal","SafetyCarStaging","SafetyCar"},replay.OperationKinds);Assert.True(BrowserReplay.Verify(replay).Matches);
    }
    private sealed class LegacyStaging(ISessionStore inner):ISessionStore
    {
        private bool converted;
        public StoredSession? Read(string id)=>inner.Read(id);
        public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);
        public IReadOnlyList<string> ListSessions()=>inner.ListSessions();
        public string Write(string id,string? expected,string payload,string? code)=>Write(id,expected,payload,code,null);
        public string Write(string id,string? expected,string payload,string? code,ScheduledWakeup? wakeup)
        {
            var document=JsonNode.Parse(payload)!;var race=document["race"];
            if(converted||race?["outcomes"]?.AsArray().Count!=2)return inner.Write(id,expected,payload,code,wakeup);
            converted=true;
            var configuration=race["configuration"]!.Deserialize<RaceConfiguration>(BrowserReplay.JsonOptions)!;
            var drivers=race["drivers"]!.Deserialize<Driver[]>(BrowserReplay.JsonOptions)!;var seed=race["input"]!["seed"]!.GetValue<int>();
            var plans=race["plans"]!.Deserialize<Dictionary<string,DriverDecision>[]>(BrowserReplay.JsonOptions)!;
            var engine=new RaceEngine(configuration,drivers,seed);engine.ResolvePhase(plans[0]);
            var staged=engine.CommitSafetyCarChoices(plans[1].ToDictionary(pair=>pair.Key,pair=>new SafetyCarDecision(pair.Value.PitCompound)));
            race["outcomes"]![1]=JsonSerializer.SerializeToNode(new RecordedReplayPhase(1,staged.LeaderLap,staged.Time,false,BrowserReplay.OutcomeHash(staged)),BrowserReplay.JsonOptions);
            race.AsObject().Remove("operationVersion");race["lifecycle"]="safety-car-ready";race["engineEventCursor"]=staged.Events.Count;
            inner.Write(id,expected,document.ToJsonString(),code,null);throw new IOException("Saved historical staging fixture");
        }
    }
    [Theory]
    [InlineData("incident",false)] [InlineData("incident",true)]
    [InlineData("special-pending",false)] [InlineData("special-pending",true)]
    [InlineData("neutralised",false)] [InlineData("neutralised",true)]
    public async Task Failed_disruption_write_and_duplicate_retry_preserve_one_incident_stop_and_neutralised_result(string boundary,bool committedFirst)
    {
        using var durable=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")??Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST")??"127.0.0.1:19100");
        var failures=new FailedBoundary(durable,boundary,committedFirst);
        var delivery=new ObservedDelivery();
        WebApplicationFactory<Program> Host(ISessionStore store)=>DurableRecoveryTests.Server().WithWebHostBuilder(builder=>builder.ConfigureServices(services=>{
            services.AddSingleton<IRacePresets>(new Game.TestFixtures.SafetyCarWindowPresets());services.AddSingleton(store);services.AddSingleton<ISessionNotifications>(delivery);
        }));
        await using var server=Host(failures);using var client=server.CreateClient();
        var created=(await(await client.PostAsJsonAsync("/api/sessions",new CreateSessionInput("open-equal",10,123))).Content.ReadFromJsonAsync<SessionCreated>())!;
        var route="/api/sessions/"+created.SessionId;client.DefaultRequestHeaders.Authorization=new("Bearer",created.ControllerCapability);
        var normal=new TeamCommand(Guid.NewGuid().ToString(),0,1,[new("driver-01","Standard","Stay Out",null,"Neutral"),new("driver-02","Standard","Stay Out",null,"Neutral")]);
        var response=await client.PostAsJsonAsync(route+"/plans",normal);
        if(boundary=="incident")
        {
            Assert.Equal(HttpStatusCode.ServiceUnavailable,response.StatusCode);Assert.All(delivery.Snapshots,state=>Assert.Equal(0,state.RaceTime));
        }
        else response.EnsureSuccessStatusCode();
        SafetyCarTeamCommand? special=null;
        if(boundary!="incident")
        {
            var own=(await client.GetFromJsonAsync<TeamSnapshot>(route+"/team"))!;
            var interruption=(await client.GetFromJsonAsync<PublicSnapshot>(route))!;delivery.Snapshots.Clear();
            special=new(Guid.NewGuid().ToString(),own.Phase,own.Revision,[new("driver-01","Pit","Hard"),new("driver-02","Stay Out",null)]);
            Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.PostAsJsonAsync(route+"/safety-car-plans",special)).StatusCode);
            Assert.All(delivery.Snapshots,state=>Assert.Equal(interruption.RaceTime,state.RaceTime));
        }
        await using var recovered=Host(durable);using var restored=recovered.CreateClient();restored.DefaultRequestHeaders.Authorization=client.DefaultRequestHeaders.Authorization;
        var retry=special is null?await restored.PostAsJsonAsync(route+"/plans",normal):await restored.PostAsJsonAsync(route+"/safety-car-plans",special);
        retry.EnsureSuccessStatusCode();
        var before=(await restored.GetFromJsonAsync<PublicSnapshot>(route))!;
        var repeated=special is null?await restored.PostAsJsonAsync(route+"/plans",normal):await restored.PostAsJsonAsync(route+"/safety-car-plans",special);
        Assert.Equal(await retry.Content.ReadAsStringAsync(),await repeated.Content.ReadAsStringAsync());
        await using var secondRestart=Host(durable);using var read=secondRestart.CreateClient();
        var after=(await read.GetFromJsonAsync<PublicSnapshot>(route))!;
        Assert.Equal(before.RaceTime,after.RaceTime);Assert.Equal(JsonSerializer.Serialize(before.Classification),JsonSerializer.Serialize(after.Classification));
        var replay=secondRestart.Services.GetRequiredService<SessionCoordinator>().DesignerRecord(created.SessionId);
        Assert.Equal(special is null?1:2,replay.Phases.Count);Assert.True(BrowserReplay.Verify(replay).Matches);
        if(special is not null)Assert.Equal("Hard",after.Classification.Single(car=>car.DriverId=="driver-01").Compound);
    }
    private sealed class ObservedDelivery:ISessionNotifications
    {
        public List<PublicSnapshot> Snapshots{get;}=[];
        public Task Publish(PublicSnapshot display,TeamSnapshot team){Snapshots.Add(display);return Task.CompletedTask;}
        public Task PublishHousehold(PublicSnapshot display,IReadOnlyList<TeamSnapshot> teams,IReadOnlyList<OpenTeamSnapshot> openTeams){Snapshots.Add(display);return Task.CompletedTask;}
    }
    private sealed class FailedBoundary(ISessionStore inner,string boundary,bool committedFirst):ISessionStore
    {
        private bool failed;
        public StoredSession? Read(string id)=>inner.Read(id);
        public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);
        public IReadOnlyList<string> ListSessions()=>inner.ListSessions();
        public string Write(string id,string? expected,string payload,string? code)=>Write(id,expected,payload,code,null);
        public string Write(string id,string? expected,string payload,string? code,ScheduledWakeup? wakeup)
        {
            var race=JsonNode.Parse(payload)?["race"];var outcomes=race?["outcomes"]?.AsArray().Count??0;
            var matches=boundary switch{"incident"=>outcomes==1,"neutralised"=>outcomes==2,"migrated"=>outcomes==3,_=>race?["windowKind"]?.GetValue<string>()=="SafetyCar"&&race?["pendingPlan"] is not null};
            if(failed||!matches)return inner.Write(id,expected,payload,code,wakeup);
            failed=true;if(committedFirst)inner.Write(id,expected,payload,code,wakeup);throw new IOException("Injected interruption at durable boundary");
        }
    }
}
