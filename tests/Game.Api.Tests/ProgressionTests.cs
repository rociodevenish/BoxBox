using System.Net.Http.Json;
using System.Text.Json;
using Game.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;
using Game.Api;
using Google.Apis.Auth;
namespace Game.Api.Tests;
public sealed class ProgressionTests
{
    [Theory]
    [InlineData("wrong-subject")]
    [InlineData("wrong-email")]
    [InlineData("wrong-audience")]
    [InlineData("wrong-issuer")]
    [InlineData("unverified-email")]
    [InlineData("expired")]
    [InlineData("sweep-token")]
    public async Task Valid_signature_still_requires_exact_task_principal_and_audience(string token)
    {
        var clock=new DeadlineTests.ControlledClock();await using var server=Server(clock);using var callback=server.CreateClient();callback.DefaultRequestHeaders.Authorization=new("Bearer",token);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden,(await callback.PostAsJsonAsync("/internal/wakeups",new WakeupInput("unknown",Guid.NewGuid().ToString(),0,1))).StatusCode);
    }
    [Fact]
    public async Task Stale_callback_after_restart_cannot_close_a_newer_overdue_window()
    {
        var clock=new DeadlineTests.ControlledClock();var delivery=new CapturedDelivery();string id;ScheduledWakeup obsolete;long revision;
        await using(var server=Server(clock,delivery,true))
        {
            using var host=server.CreateClient();var (session,device)=await DeadlineTests.StartShared(host);id=session;obsolete=delivery.Latest(id);await LockBoth(host,id,device);
            var next=await host.PostAsJsonAsync($"/api/lobbies/{id}/continue",new DueTransitionInput(Guid.NewGuid().ToString(),0,1));next.EnsureSuccessStatusCode();revision=(await next.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("revision").GetInt64();
        }
        clock.Advance(TimeSpan.FromDays(1));await using var restarted=Server(clock,delivery,true);using var callback=restarted.CreateClient();callback.DefaultRequestHeaders.Authorization=new("Bearer","task-token");
        var state=await Send(callback,obsolete);Assert.Equal(revision,state.GetProperty("revision").GetInt64());Assert.Equal(5,state.GetProperty("leaderLap").GetInt32());
    }
    [Fact]
    public async Task Presentation_pause_freezes_remaining_duration_and_resume_uses_a_new_delivery_generation()
    {
        var clock=new DeadlineTests.ControlledClock();var delivery=new CapturedDelivery();await using var server=Server(clock,delivery);using var host=server.CreateClient();var (id,device)=await DeadlineTests.StartShared(host);await LockBoth(host,id,device);var old=delivery.Latest(id);
        clock.Advance(TimeSpan.FromSeconds(3));(await host.PostAsJsonAsync($"/api/lobbies/{id}/pause",new{})).EnsureSuccessStatusCode();
        clock.Advance(TimeSpan.FromHours(1));(await host.PostAsJsonAsync($"/api/lobbies/{id}/resume",new{})).EnsureSuccessStatusCode();
        var resumed=await host.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");Assert.Equal(clock.GetUtcNow().AddSeconds(5),resumed.GetProperty("presentationDeadlineUtc").GetDateTimeOffset());
        using var callback=server.CreateClient();callback.DefaultRequestHeaders.Authorization=new("Bearer","task-token");Assert.Equal(0,(await Send(callback,old)).GetProperty("phase").GetInt32());
        clock.Advance(TimeSpan.FromSeconds(5));Assert.Equal(1,(await Send(callback,delivery.Latest(id))).GetProperty("phase").GetInt32());
    }
    [Fact]
    public async Task Failed_enqueue_preserves_committed_outcome_and_independent_authenticated_sweep_repairs_progression()
    {
        var clock=new DeadlineTests.ControlledClock();var delivery=new CapturedDelivery{Fail=true};await using var server=Server(clock,delivery,true);using var host=server.CreateClient();
        var (id,device)=await DeadlineTests.StartShared(host);await LockBoth(host,id,device);
        var committed=await host.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");Assert.Equal(5,committed.GetProperty("leaderLap").GetInt32());
        clock.Advance(TimeSpan.FromSeconds(8));delivery.Fail=false;using var sweep=server.CreateClient();sweep.DefaultRequestHeaders.Authorization=new("Bearer","sweep-token");
        (await sweep.PostAsJsonAsync("/internal/wakeups/sweep",new{})).EnsureSuccessStatusCode();
        var repaired=await host.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");Assert.Equal(1,repaired.GetProperty("phase").GetInt32());Assert.Equal(5,repaired.GetProperty("leaderLap").GetInt32());
    }
    [Fact]
    public async Task Trusted_delivery_advances_without_display_and_duplicate_or_paused_generations_do_not_advance()
    {
        var clock=new DeadlineTests.ControlledClock();var delivery=new CapturedDelivery();await using var server=Server(clock,delivery);using var host=server.CreateClient();
        var (id,device)=await DeadlineTests.StartShared(host);await LockBoth(host,id,device);var intent=delivery.Latest(id);
        await using var subscriber=new Microsoft.AspNetCore.SignalR.Client.HubConnectionBuilder().WithUrl("http://localhost/hubs/race",options=>{options.Transports=Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;options.HttpMessageHandlerFactory=_=>server.Server.CreateHandler();}).Build();
        var updates=System.Threading.Channels.Channel.CreateUnbounded<JsonElement>();subscriber.On<JsonElement>("publicSnapshot",state=>updates.Writer.TryWrite(state));await subscriber.StartAsync();await subscriber.InvokeAsync<JsonElement>("SubscribePublic",id);
        clock.Advance(TimeSpan.FromSeconds(8));using var callback=server.CreateClient();callback.DefaultRequestHeaders.Authorization=new("Bearer","task-token");
        var result=await Send(callback,intent);Assert.Equal("awaiting-decisions",result.GetProperty("lifecycle").GetString());Assert.Equal(1,result.GetProperty("phase").GetInt32());
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(5));var broadcast=await updates.Reader.ReadAsync(timeout.Token);Assert.Equal(result.GetProperty("revision").GetInt64(),broadcast.GetProperty("revision").GetInt64());Assert.DoesNotContain("estimatedTyreCondition",broadcast.GetRawText());
        Assert.Equal(result.GetProperty("revision").GetInt64(),(await Send(callback,intent)).GetProperty("revision").GetInt64());
        var decision=delivery.Latest(id);(await host.PostAsJsonAsync($"/api/lobbies/{id}/pause",new{})).EnsureSuccessStatusCode();clock.Advance(TimeSpan.FromMinutes(10));
        var paused=await Send(callback,decision);Assert.True(paused.GetProperty("paused").GetBoolean());Assert.Equal(5,paused.GetProperty("leaderLap").GetInt32());
        (await host.PostAsJsonAsync($"/api/lobbies/{id}/resume",new{})).EnsureSuccessStatusCode();
        Assert.Equal(5,(await Send(callback,decision)).GetProperty("leaderLap").GetInt32());
    }
    [Fact]
    public async Task Failed_window_save_returns_retryable_failure_without_publishing_an_unsaved_next_window()
    {
        var clock=new DeadlineTests.ControlledClock();var delivery=new CapturedDelivery();using var underlying=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")??"127.0.0.1:19100");var writes=new InterruptedWrites(underlying);
        await using var server=Server(clock,delivery).WithWebHostBuilder(builder=>builder.ConfigureServices(services=>services.AddSingleton<ISessionStore>(writes)));using var host=server.CreateClient();
        var (id,device)=await DeadlineTests.StartShared(host);await LockBoth(host,id,device);var intent=delivery.Latest(id);clock.Advance(TimeSpan.FromSeconds(8));writes.FailNext=true;
        using var callback=server.CreateClient();callback.DefaultRequestHeaders.Authorization=new("Bearer","task-token");
        var response=await callback.PostAsJsonAsync("/internal/wakeups",new WakeupInput(id,intent.Generation,intent.Phase,intent.WindowRevision));Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable,response.StatusCode);
        var saved=await host.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");Assert.Equal(0,saved.GetProperty("phase").GetInt32());Assert.True(saved.GetProperty("paused").GetBoolean());
        var retry=await Send(callback,intent);Assert.Equal(1,retry.GetProperty("phase").GetInt32());Assert.Equal(5,retry.GetProperty("leaderLap").GetInt32());
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Trusted_presentation_callback_coldstart_advances_but_ordinary_restart_pauses(bool trusted)
    {
        var clock=new DeadlineTests.ControlledClock();var delivery=new CapturedDelivery();string id;ScheduledWakeup intent;
        await using(var server=Server(clock,delivery,true))
        {
            using var host=server.CreateClient();var started=await DeadlineTests.StartShared(host);id=started.Id;await LockBoth(host,id,started.Device);intent=delivery.Latest(id);
        }
        clock.Advance(TimeSpan.FromSeconds(8));await using var restarted=Server(clock,delivery,true);using var client=restarted.CreateClient();
        if(trusted)client.DefaultRequestHeaders.Authorization=new("Bearer","task-token");
        var recovered=trusted?await Send(client,intent):await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");
        Assert.Equal(trusted?1:0,recovered.GetProperty("phase").GetInt32());Assert.Equal(!trusted,recovered.GetProperty("paused").GetBoolean());
    }
    [Fact]
    public async Task Decision_callback_coldstart_closes_only_one_overdue_window_and_pauses()
    {
        var clock=new DeadlineTests.ControlledClock();var delivery=new CapturedDelivery();string id;ScheduledWakeup intent;
        await using(var server=Server(clock,delivery,true))
        {
            using var host=server.CreateClient();var started=await DeadlineTests.StartShared(host);id=started.Id;intent=delivery.Latest(id);
        }
        clock.Advance(TimeSpan.FromDays(3));await using var restarted=Server(clock,delivery,true);using var callback=restarted.CreateClient();callback.DefaultRequestHeaders.Authorization=new("Bearer","task-token");
        var state=await Send(callback,intent);Assert.Equal(5,state.GetProperty("leaderLap").GetInt32());Assert.True(state.GetProperty("paused").GetBoolean());
        Assert.Equal(state.GetProperty("revision").GetInt64(),(await Send(callback,intent)).GetProperty("revision").GetInt64());
    }
    [Fact]
    public async Task Player_identity_cannot_deliver_internal_wakeups_or_sweep()
    {
        await using var server=Server(new DeadlineTests.ControlledClock());using var player=server.CreateClient();
        player.DefaultRequestHeaders.Authorization=new("Bearer",await LobbyTests.Identity(false));
        Assert.Equal(System.Net.HttpStatusCode.Forbidden,(await player.PostAsJsonAsync("/internal/wakeups",new{sessionId="unknown",generation=Guid.NewGuid().ToString(),phase=0,windowRevision=1})).StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden,(await player.PostAsJsonAsync("/internal/wakeups/sweep",new{})).StatusCode);
    }
    [Fact]
    public async Task Disconnected_display_does_not_own_bounded_presentation_and_host_can_continue_exactly_once()
    {
        var clock=new DeadlineTests.ControlledClock();await using var server=Server(clock);using var host=server.CreateClient();
        var (id,device)=await DeadlineTests.StartShared(host);
        await LockBoth(host,id,device);
        var result=await host.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");
        Assert.Equal("phase-result",result.GetProperty("lifecycle").GetString());Assert.Equal(0,result.GetProperty("phase").GetInt32());
        Assert.Equal(clock.GetUtcNow().AddSeconds(8),result.GetProperty("presentationDeadlineUtc").GetDateTimeOffset());
        var command=new{commandId=Guid.NewGuid().ToString(),phase=0,windowRevision=1};
        var continued=await host.PostAsJsonAsync($"/api/lobbies/{id}/continue",command);continued.EnsureSuccessStatusCode();
        var next=await continued.Content.ReadFromJsonAsync<JsonElement>();Assert.Equal(1,next.GetProperty("phase").GetInt32());
        var duplicate=await host.PostAsJsonAsync($"/api/lobbies/{id}/continue",command);duplicate.EnsureSuccessStatusCode();
        Assert.Equal(next.GetProperty("revision").GetInt64(),(await duplicate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("revision").GetInt64());
    }
    private static WebApplicationFactory<Program> Server(TimeProvider clock,CapturedDelivery? delivery=null,bool durable=false)=>new WebApplicationFactory<Program>().WithWebHostBuilder(builder=>{
        builder.ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(new Dictionary<string,string?>{["Firebase:ProjectId"]="demo-f1game",["Wakeups:Mode"]="manual",["Persistence:ProjectId"]=durable?"demo-f1game":null,["Persistence:EmulatorHost"]=Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")??"127.0.0.1:19100",
            ["Wakeups:Audience"]="https://service.example.test",["Wakeups:ServiceAccountEmail"]="tasks@example.iam.gserviceaccount.com",["Wakeups:ServiceAccountSubject"]="123456789",["Wakeups:SweepServiceAccountEmail"]="sweep@example.iam.gserviceaccount.com",["Wakeups:SweepServiceAccountSubject"]="987654321"}));
        builder.ConfigureServices(services=>{services.AddSingleton(clock);services.AddSingleton<IGoogleWakeupTokens>(new IssuedTokens(clock));if(delivery is not null)services.AddSingleton<IScheduledWakeups>(delivery);});
    });
    private sealed class CapturedDelivery:IScheduledWakeups
    {
        private readonly Dictionary<string,ScheduledWakeup> saved=[];
        public bool Fail{get;set;}
        public void Enqueue(ScheduledWakeup wakeup){if(Fail)throw new IOException("Queue unavailable");saved[wakeup.SessionId]=wakeup;}
        public ScheduledWakeup Latest(string id)=>saved[id];
    }
    private sealed class InterruptedWrites(ISessionStore inner):ISessionStore
    {
        public bool FailNext{get;set;}
        public StoredSession? Read(string id)=>inner.Read(id);
        public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);
        public IReadOnlyList<string> ListSessions()=>inner.ListSessions();
        public string Write(string id,string? version,string payload,string? code)=>Write(id,version,payload,code,null);
        public string Write(string id,string? version,string payload,string? code,ScheduledWakeup? wakeup){if(FailNext){FailNext=false;throw new IOException("Commit unavailable");}return inner.Write(id,version,payload,code,wakeup);}
    }
    private sealed class IssuedTokens(TimeProvider clock):IGoogleWakeupTokens
    {
        public Task<GoogleJsonWebSignature.Payload> Validate(string token,string audience)
        {
            if(token is not ("task-token" or "sweep-token" or "wrong-subject" or "wrong-email" or "wrong-audience" or "wrong-issuer" or "unverified-email" or "expired"))throw new InvalidJwtException("No valid signature");
            return Task.FromResult(new GoogleJsonWebSignature.Payload{Issuer=token=="wrong-issuer"?"https://untrusted.example":"https://accounts.google.com",Audience=token=="wrong-audience"?"https://other.example":audience,
                Subject=token=="sweep-token"?"987654321":token=="wrong-subject"?"000":"123456789",Email=token=="sweep-token"?"sweep@example.iam.gserviceaccount.com":token=="wrong-email"?"other@example.iam.gserviceaccount.com":"tasks@example.iam.gserviceaccount.com",EmailVerified=token!="unverified-email",ExpirationTimeSeconds=clock.GetUtcNow().AddHours(token=="expired"?-1:1).ToUnixTimeSeconds()});
        }
    }
    private static async Task<JsonElement> Send(HttpClient client,ScheduledWakeup wakeup)
    {
        var response=await client.PostAsJsonAsync("/internal/wakeups",new WakeupInput(wakeup.SessionId,wakeup.Generation,wakeup.Phase,wakeup.WindowRevision));response.EnsureSuccessStatusCode();return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
    private static async Task LockBoth(HttpClient host,string id,string device)
    {
        foreach(var team in new[]{"team-01","team-02"})
        {
            var own=(await host.GetFromJsonAsync<TeamSnapshot>($"/api/lobbies/{id}/teams/{team}?deviceId={device}"))!;
            var command=new TeamCommand(Guid.NewGuid().ToString(),own.Phase,own.CommandRevision,own.Cars.Select(car=>new CarCommand(car.DriverId,"Standard","Stay Out",null,"Neutral")).ToArray());
            (await host.PostAsJsonAsync($"/api/lobbies/{id}/teams/{team}/plans?deviceId={device}",command)).EnsureSuccessStatusCode();
        }
    }
}
