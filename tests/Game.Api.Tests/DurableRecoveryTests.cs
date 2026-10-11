using System.Net.Http.Json;
using Game.Application;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;
using Game.Api;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;
namespace Game.Api.Tests;
public sealed class DurableRecoveryTests(Xunit.Abstractions.ITestOutputHelper output)
{
    [Fact]
    public async Task Restart_recovers_committed_result_and_original_retry_acknowledgement()
    {
        SessionCreated created; string acknowledgement; PublicSnapshot before;
        var command = new TeamCommand(Guid.NewGuid().ToString(),0,1,
            [new("driver-01","Push","Stay Out",null,"Neutral"),new("driver-02","Standard","Pit","Hard","Neutral")]);
        await using (var server=Server())
        {
            using var client=server.CreateClient();
            var creation=await client.PostAsJsonAsync("/api/sessions",new CreateSessionInput("open-equal",6,123));
            Assert.True(creation.IsSuccessStatusCode,await creation.Content.ReadAsStringAsync());
            created=(await creation.Content.ReadFromJsonAsync<SessionCreated>())!;
            client.DefaultRequestHeaders.Authorization=new("Bearer",created.ControllerCapability);
            var response=await client.PostAsJsonAsync($"/api/sessions/{created.SessionId}/plans",command);response.EnsureSuccessStatusCode();
            acknowledgement=await response.Content.ReadAsStringAsync();
            before=(await client.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{created.SessionId}"))!;
        }
        await using var recovered=Server();using var reconnect=recovered.CreateClient();
        reconnect.DefaultRequestHeaders.Authorization=new("Bearer",created.ControllerCapability);
        var stopwatch=System.Diagnostics.Stopwatch.StartNew();
        var stateResponse=await reconnect.GetAsync($"/api/sessions/{created.SessionId}");stateResponse.EnsureSuccessStatusCode();
        var after=(await stateResponse.Content.ReadFromJsonAsync<PublicSnapshot>())!;
        output.WriteLine("Restart snapshot recovery {0:F2}ms",stopwatch.Elapsed.TotalMilliseconds);
        reconnect.DefaultRequestHeaders.Add("X-Designer-Key","recovery-diagnostics-only");
        var record=await reconnect.GetFromJsonAsync<JsonElement>($"/api/designer/sessions/{created.SessionId}/record");
        output.WriteLine("Engine initialization/replay {0:F2}ms",record.GetProperty("timing").GetProperty("recoveryReplayMilliseconds").GetDouble());
        Assert.Equal(before.Revision,after.Revision);Assert.Equal(before.RaceTime,after.RaceTime);
        Assert.Equal(before.Classification.Select(c=>c.DriverId),after.Classification.Select(c=>c.DriverId));
        await using var hub=Connection(recovered);await hub.StartAsync();
        var hubSnapshot=await hub.InvokeAsync<PublicSnapshot>("SubscribePublic",created.SessionId);Assert.Equal(after.Revision,hubSnapshot.Revision);
        var privateSnapshot=await hub.InvokeAsync<TeamSnapshot>("SubscribeTeam",created.SessionId,created.ControllerCapability);Assert.Equal(after.Phase,privateSnapshot.Phase);
        var retry=await reconnect.PostAsJsonAsync($"/api/sessions/{created.SessionId}/plans",command);retry.EnsureSuccessStatusCode();
        Assert.Equal(acknowledgement,await retry.Content.ReadAsStringAsync());
    }
    [Theory]
    [InlineData(2,false,false)]
    [InlineData(2,true,false)]
    [InlineData(3,false,false)]
    [InlineData(3,false,true)]
    [InlineData(3,true,false)]
    public async Task Failed_save_never_acknowledges_and_restart_reuses_durable_choices(int failedWrite,bool failedRead,bool committedBeforeFailure)
    {
        SessionCreated created;
        var command=new TeamCommand(Guid.NewGuid().ToString(),0,1,
            [new("driver-01","Push","Stay Out",null,"Neutral"),new("driver-02","Standard","Pit","Hard","Neutral")]);
        var store=new InterruptingStore(new FirestoreSessionStore("demo-f1game","127.0.0.1:19100"),failedWrite,failedRead,committedBeforeFailure);
        var delivery=new ObservedDelivery();
        await using(var server=Server().WithWebHostBuilder(builder=>builder.ConfigureServices(services=>{services.Replace(ServiceDescriptor.Singleton<ISessionStore>(store));services.Replace(ServiceDescriptor.Singleton<ISessionNotifications>(delivery));})))
        {
            using var client=server.CreateClient();created=(await(await client.PostAsJsonAsync("/api/sessions",new CreateSessionInput("open-equal",6,123))).Content.ReadFromJsonAsync<SessionCreated>())!;
            client.DefaultRequestHeaders.Authorization=new("Bearer",created.ControllerCapability);
            var failure=await client.PostAsJsonAsync($"/api/sessions/{created.SessionId}/plans",command);
            Assert.Equal(HttpStatusCode.ServiceUnavailable,failure.StatusCode);
            Assert.Contains("StorageUnavailable",await failure.Content.ReadAsStringAsync());
            Assert.DoesNotContain(delivery.Snapshots,state=>state.LeaderLap>0);
            if(failedWrite==2)Assert.Empty(delivery.Snapshots);
        }
        await using var restored=Server();using var retryClient=restored.CreateClient();retryClient.DefaultRequestHeaders.Authorization=new("Bearer",created.ControllerCapability);
        var firstRetry=await retryClient.PostAsJsonAsync($"/api/sessions/{created.SessionId}/plans",command);firstRetry.EnsureSuccessStatusCode();
        var snapshot=(await retryClient.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{created.SessionId}"))!;
        Assert.Equal(5,snapshot.LeaderLap);
        await using(var uninterrupted=new WebApplicationFactory<Program>()){using var baseline=uninterrupted.CreateClient();var original=(await(await baseline.PostAsJsonAsync("/api/sessions",new CreateSessionInput("open-equal",6,123))).Content.ReadFromJsonAsync<SessionCreated>())!;baseline.DefaultRequestHeaders.Authorization=new("Bearer",original.ControllerCapability);(await baseline.PostAsJsonAsync($"/api/sessions/{original.SessionId}/plans",command)).EnsureSuccessStatusCode();var expected=(await baseline.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{original.SessionId}"))!;Assert.Equal(expected.RaceTime,snapshot.RaceTime);Assert.Equal(JsonSerializer.Serialize(expected.Classification),JsonSerializer.Serialize(snapshot.Classification));}
        var retry=await retryClient.PostAsJsonAsync($"/api/sessions/{created.SessionId}/plans",command);retry.EnsureSuccessStatusCode();
        Assert.Equal(await firstRetry.Content.ReadAsStringAsync(),await retry.Content.ReadAsStringAsync());
        Assert.Equal(snapshot.Revision,(await retryClient.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{created.SessionId}"))!.Revision);
    }
    private sealed class ObservedDelivery:ISessionNotifications
    {
        public List<PublicSnapshot> Snapshots {get;}=[];
        public Task Publish(PublicSnapshot display,TeamSnapshot team){Snapshots.Add(display);return Task.CompletedTask;}
        public Task PublishHousehold(PublicSnapshot display,IReadOnlyList<TeamSnapshot> teams,IReadOnlyList<OpenTeamSnapshot> openTeams){Snapshots.Add(display);return Task.CompletedTask;}
    }
    private sealed class InterruptingStore(ISessionStore inner,int failedWrite,bool failedRead,bool committedBeforeFailure):ISessionStore
    {
        private int writes;
        private bool outage;
        public StoredSession? Read(string id)=>outage&&failedRead?throw new IOException("Storage read unavailable"):inner.Read(id);
        public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);
        public string Write(string id,string? version,string payload,string? code)
        {
            if(++writes==failedWrite){if(committedBeforeFailure)inner.Write(id,version,payload,code);outage=true;throw new IOException("Storage write unavailable");}
            return inner.Write(id,version,payload,code);
        }
    }
    [Fact]
    public async Task Authenticated_household_restart_preserves_lock_and_revoked_device_cannot_read_private_team()
    {
        var token=await LobbyTests.Identity(false);string id,device,code;TeamCommand command;
        await using(var server=Server())
        {
            using var host=server.CreateClient();host.DefaultRequestHeaders.Authorization=new("Bearer",token);
            var response=await host.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=6,seed=123,mode="Competitive",name="Host"});response.EnsureSuccessStatusCode();
            var created=await response.Content.ReadFromJsonAsync<JsonElement>();id=created.GetProperty("lobby").GetProperty("sessionId").GetString()!;
            device=created.GetProperty("membership").GetProperty("deviceId").GetString()!;code=created.GetProperty("lobby").GetProperty("joinCode").GetString()!;
            (await host.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-01",deviceId=device})).EnsureSuccessStatusCode();
            var participant=(await(await host.PostAsJsonAsync($"/api/lobbies/{id}/participants",new{name="Junior",deviceId=device})).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("players").EnumerateArray().Single(p=>p.GetProperty("name").GetString()=="Junior").GetProperty("playerId").GetString();
            (await host.PostAsJsonAsync($"/api/lobbies/{id}/assignments",new{teamId="team-02",playerId=participant,deviceId=device,control="SharedTablet"})).EnsureSuccessStatusCode();
            (await host.PostAsJsonAsync($"/api/lobbies/{id}/start",new{})).EnsureSuccessStatusCode();
            command=new(Guid.NewGuid().ToString(),0,1,[new("driver-01","Push","Stay Out",null,"Neutral"),new("driver-02","Standard","Stay Out",null,"Neutral")]);
            (await host.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/plans?deviceId={device}",command)).EnsureSuccessStatusCode();
        }
        await using(var server=Server())
        {
            using var host=server.CreateClient();host.DefaultRequestHeaders.Authorization=new("Bearer",token);
            var joined=await host.PostAsJsonAsync("/api/lobbies/join",new{code,name="Host",deviceId=device});joined.EnsureSuccessStatusCode();
            var team=(await host.GetFromJsonAsync<TeamSnapshot>($"/api/lobbies/{id}/teams/team-01?deviceId={device}"))!;Assert.Equal(command.CommandId,team.ConfirmedPlan!.CommandId);
            var publicJson=await host.GetStringAsync($"/api/sessions/{id}");Assert.DoesNotContain("confirmedPlan",publicJson);Assert.DoesNotContain("estimatedTyreCondition",publicJson);
            (await host.PostAsJsonAsync($"/api/lobbies/{id}/devices/{device}/revoke",new{})).EnsureSuccessStatusCode();
        }
        await using var last=Server();using var revoked=last.CreateClient();revoked.DefaultRequestHeaders.Authorization=new("Bearer",token);
        Assert.Equal(HttpStatusCode.Forbidden,(await revoked.GetAsync($"/api/lobbies/{id}/teams/team-01?deviceId={device}")).StatusCode);
    }
    [Fact]
    public async Task Competing_application_instances_conditionally_commit_one_phase()
    {
        await using var first=Server();using var a=first.CreateClient();
        var created=(await(await a.PostAsJsonAsync("/api/sessions",new CreateSessionInput("open-equal",6,123))).Content.ReadFromJsonAsync<SessionCreated>())!;
        a.DefaultRequestHeaders.Authorization=new("Bearer",created.ControllerCapability);
        await using var second=Server();using var b=second.CreateClient();b.DefaultRequestHeaders.Authorization=new("Bearer",created.ControllerCapability);
        (await b.GetAsync($"/api/sessions/{created.SessionId}")).EnsureSuccessStatusCode();
        var firstPlan=new TeamCommand(Guid.NewGuid().ToString(),0,1,[new("driver-01","Push","Stay Out",null,"Neutral"),new("driver-02","Standard","Pit","Hard","Neutral")]);
        var otherPlan=firstPlan with{CommandId=Guid.NewGuid().ToString(),Cars=[firstPlan.Cars[0] with{Pace="Conserve"},firstPlan.Cars[1]]};
        var results=await Task.WhenAll(a.PostAsJsonAsync($"/api/sessions/{created.SessionId}/plans",firstPlan),b.PostAsJsonAsync($"/api/sessions/{created.SessionId}/plans",otherPlan));
        Assert.Single(results,r=>r.IsSuccessStatusCode);Assert.Single(results,r=>r.StatusCode==HttpStatusCode.Conflict);
        await using var restart=Server();using var read=restart.CreateClient();var state=(await read.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{created.SessionId}"))!;
        Assert.Equal(4,state.Revision);Assert.Equal(5,state.LeaderLap);
    }
    [Fact]
    public async Task Incompatible_saved_version_is_actionable_and_preserves_recoverable_inputs()
    {
        var store=new FirestoreSessionStore("demo-f1game","127.0.0.1:19100");SessionCreated created;
        await using(var initial=Server())
        {using var client=initial.CreateClient();created=(await(await client.PostAsJsonAsync("/api/sessions",new CreateSessionInput("open-equal",6,123))).Content.ReadFromJsonAsync<SessionCreated>())!;}
        await using(var incompatible=Server().WithWebHostBuilder(builder=>builder.ConfigureServices(services=>services.Replace(ServiceDescriptor.Singleton<ISessionStore>(new IncompatibleStore(store))))))
        {
            using var read=incompatible.CreateClient();var rejected=await read.GetAsync($"/api/sessions/{created.SessionId}");Assert.Equal(HttpStatusCode.Conflict,rejected.StatusCode);
            Assert.Contains("compatible version",await rejected.Content.ReadAsStringAsync());
        }
        await using var compatible=Server();using var restored=compatible.CreateClient();(await restored.GetAsync($"/api/sessions/{created.SessionId}")).EnsureSuccessStatusCode();
    }
    private sealed class IncompatibleStore(ISessionStore inner):ISessionStore
    {
        public StoredSession? Read(string id){var record=inner.Read(id);return record is null?null:record with{Payload=record.Payload.Replace("session-recovery-v1","future-recovery-v9")};}
        public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);
        public string Write(string id,string? version,string payload,string? code)=>inner.Write(id,version,payload,code);
    }
    private static HubConnection Connection(WebApplicationFactory<Program> server)=>new HubConnectionBuilder().WithUrl("http://localhost/hubs/race",options=>{options.Transports=HttpTransportType.LongPolling;options.HttpMessageHandlerFactory=_=>server.Server.CreateHandler();}).Build();
    internal static WebApplicationFactory<Program> Server()=>new WebApplicationFactory<Program>().WithWebHostBuilder(builder=>builder.ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(new Dictionary<string,string?>{
        ["Designer:Key"]="recovery-diagnostics-only",["Firebase:ProjectId"]="demo-f1game",["Persistence:ProjectId"]="demo-f1game",["Persistence:EmulatorHost"]="127.0.0.1:19100"})));
}
