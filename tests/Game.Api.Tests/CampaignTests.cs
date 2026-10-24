using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Game.Api;
using Game.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace Game.Api.Tests;

public sealed class CampaignTests
{
    [Fact]
    public async Task Preparation_readiness_privacy_and_concurrent_start_use_one_lobby_weekend_after_restart()
    {
        var token=await LobbyTests.Identity(false);var guest=await LobbyTests.Identity(true);
        var store=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")??"127.0.0.1:19580");
        string id,weekend,device,context;
        await using(var server=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(store)))))
        {
            using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
            var created=await client.PostAsJsonAsync("/api/campaigns",new CreateCampaignInput(Guid.NewGuid().ToString(),"Host"));created.EnsureSuccessStatusCode();
            var joined=(await created.Content.ReadFromJsonAsync<CampaignJoined>())!;id=joined.Campaign.CampaignId;weekend=joined.Campaign.WeekendId;device=joined.Weekend.Membership.DeviceId;
            Assert.NotEqual(id,weekend);Assert.NotEqual(device,joined.Weekend.Membership.PlayerId);
            Assert.Equal(22,joined.Campaign.Teams.SelectMany(t=>t.Drivers).Select(d=>d.PersonId).Distinct().Count());
            (await client.PostAsJsonAsync($"/api/lobbies/{weekend}/claims",new ClaimTeamInput("team-01",device))).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync($"/api/lobbies/{weekend}/start",new{})).StatusCode);
            (await client.PostAsJsonAsync($"/api/campaigns/{id}/prepare",new{})).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync($"/api/lobbies/{weekend}/preparation",new BeginPreparationInput("track-monaco"))).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync($"/api/campaigns/{id}/start",new CampaignStartInput(Guid.NewGuid().ToString()))).StatusCode);
            var uri=$"/api/lobbies/{weekend}/preparation/teams/team-01?deviceId={device}";
            var own=(await client.GetFromJsonAsync<PreparationTeamSnapshot>(uri))!;
            Assert.Equal("driver-lando-norris",own.Preset.Cars[0].Driver.Id);Assert.All(own.Preset.Cars,c=>Assert.All(c.Car.Ratings.Values,r=>Assert.Equal(82,r)));
            (await client.PostAsJsonAsync(uri.Replace("?","/practice?"),new PracticeCommand(Guid.NewGuid().ToString(),own.Revision,own.AssignmentRevision))).EnsureSuccessStatusCode();
            own=(await client.GetFromJsonAsync<PreparationTeamSnapshot>(uri))!;
            (await client.PostAsJsonAsync(uri.Replace("?","/confirm?"),new PracticeCommand(Guid.NewGuid().ToString(),own.Revision,own.AssignmentRevision))).EnsureSuccessStatusCode();
            var snapshot=await client.GetStringAsync($"/api/campaigns/{id}?deviceId={device}");Assert.DoesNotContain("setup",snapshot);Assert.DoesNotContain("recommendation",snapshot);Assert.DoesNotContain("ratings",snapshot);
            using var outsider=server.CreateClient();outsider.DefaultRequestHeaders.Authorization=new("Bearer",guest);
            Assert.Equal(HttpStatusCode.Forbidden,(await outsider.GetAsync($"/api/campaigns/{id}?deviceId={device}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,(await outsider.PostAsJsonAsync($"/api/campaigns/{id}/start",new CampaignStartInput(Guid.NewGuid().ToString()))).StatusCode);
            var command=new CampaignStartInput(Guid.NewGuid().ToString());
            var starts=await Task.WhenAll(client.PostAsJsonAsync($"/api/campaigns/{id}/start",command),client.PostAsJsonAsync($"/api/campaigns/{id}/start",new CampaignStartInput(Guid.NewGuid().ToString())));
            foreach(var start in starts)start.EnsureSuccessStatusCode();
            context=(await client.GetFromJsonAsync<QualifyingPublicSnapshot>($"/api/lobbies/{weekend}/qualifying"))!.SessionId;
            foreach(var start in starts)Assert.Equal("active-weekend",(await start.Content.ReadFromJsonAsync<CampaignSnapshot>())!.Lifecycle);
        }
        await using var recovered=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(store))));using var retry=recovered.CreateClient();retry.DefaultRequestHeaders.Authorization=new("Bearer",token);
        (await retry.PostAsJsonAsync($"/api/campaigns/{id}/start",new CampaignStartInput(Guid.NewGuid().ToString()))).EnsureSuccessStatusCode();
        var after=(await retry.GetFromJsonAsync<CampaignSnapshot>($"/api/campaigns/{id}?deviceId={device}"))!;Assert.Equal("active-weekend",after.Lifecycle);Assert.Equal(weekend,after.WeekendId);
        Assert.Equal(context,(await retry.GetFromJsonAsync<QualifyingPublicSnapshot>($"/api/lobbies/{weekend}/qualifying"))!.SessionId);
    }
    [Theory]
    [InlineData(1,false)]
    [InlineData(1,true)]
    [InlineData(2,false)]
    [InlineData(2,true)]
    public async Task Uncertain_campaign_reservation_or_weekend_creation_retries_same_reserved_event(int write,bool committed)
    {
        var token=await LobbyTests.Identity(false);var input=new CreateCampaignInput(Guid.NewGuid().ToString(),"Host");
        var durable=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")??"127.0.0.1:19580");
        var interrupted=new InterruptedStore(durable,write,committed);
        await using(var server=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(interrupted)))))
        {
            using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
            Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.PostAsJsonAsync("/api/campaigns",input)).StatusCode);
        }
        await using var recovered=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(durable))));using var retry=recovered.CreateClient();retry.DefaultRequestHeaders.Authorization=new("Bearer",token);
        var first=await retry.PostAsJsonAsync("/api/campaigns",input);first.EnsureSuccessStatusCode();var joined=(await first.Content.ReadFromJsonAsync<CampaignJoined>())!;
        var repeated=await retry.PostAsJsonAsync("/api/campaigns",input);repeated.EnsureSuccessStatusCode();var again=(await repeated.Content.ReadFromJsonAsync<CampaignJoined>())!;
        Assert.Equal(joined.Campaign.CampaignId,again.Campaign.CampaignId);Assert.Equal(joined.Campaign.Calendar[0].EventId,again.Campaign.Calendar[0].EventId);
        Assert.Equal(joined.Weekend.Lobby.SessionId,again.Weekend.Lobby.SessionId);Assert.Equal(joined.Weekend.Membership,again.Weekend.Membership);
        Assert.Equal(joined.Campaign.Calendar[0].SessionId,joined.Weekend.Lobby.SessionId);
    }
    private sealed class InterruptedStore(ISessionStore inner,int failedWrite,bool committed):ISessionStore
    {
        private int writes;
        public bool Armed {get;set;}=true;
        public StoredSession? Read(string id)=>inner.Read(id);
        public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);
        public string Write(string id,string? version,string payload,string? code)
        {
            if(Armed&&++writes==failedWrite){if(committed)inner.Write(id,version,payload,code);throw new IOException("Save response lost");}
            return inner.Write(id,version,payload,code);
        }
    }
    [Theory]
    [InlineData(1,false)]
    [InlineData(1,true)]
    [InlineData(2,false)]
    [InlineData(2,true)]
    [InlineData(3,false)]
    [InlineData(3,true)]
    public async Task Restart_during_start_reservation_qualifying_or_campaign_publication_resumes_one_weekend(int write,bool committed)
    {
        var token=await LobbyTests.Identity(false);var command=new CampaignStartInput(Guid.NewGuid().ToString());
        var durable=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")??"127.0.0.1:19580");
        var interrupted=new InterruptedStore(durable,write,committed){Armed=false};CampaignJoined joined;
        await using(var server=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(interrupted)))))
        {
            using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
            var creation=await client.PostAsJsonAsync("/api/campaigns",new CreateCampaignInput(Guid.NewGuid().ToString(),"Host"));creation.EnsureSuccessStatusCode();joined=(await creation.Content.ReadFromJsonAsync<CampaignJoined>())!;
            var id=joined.Campaign.CampaignId;var weekend=joined.Campaign.WeekendId;var device=joined.Weekend.Membership.DeviceId;
            (await client.PostAsJsonAsync($"/api/lobbies/{weekend}/claims",new ClaimTeamInput("team-01",device))).EnsureSuccessStatusCode();
            (await client.PostAsJsonAsync($"/api/campaigns/{id}/prepare",new{})).EnsureSuccessStatusCode();
            var uri=$"/api/lobbies/{weekend}/preparation/teams/team-01?deviceId={device}";var own=(await client.GetFromJsonAsync<PreparationTeamSnapshot>(uri))!;
            (await client.PostAsJsonAsync(uri.Replace("?","/practice?"),new PracticeCommand(Guid.NewGuid().ToString(),own.Revision,own.AssignmentRevision))).EnsureSuccessStatusCode();own=(await client.GetFromJsonAsync<PreparationTeamSnapshot>(uri))!;
            (await client.PostAsJsonAsync(uri.Replace("?","/confirm?"),new PracticeCommand(Guid.NewGuid().ToString(),own.Revision,own.AssignmentRevision))).EnsureSuccessStatusCode();
            interrupted.Armed=true;
            Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.PostAsJsonAsync($"/api/campaigns/{id}/start",command)).StatusCode);
        }
        await using var recovered=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(durable))));using var retry=recovered.CreateClient();retry.DefaultRequestHeaders.Authorization=new("Bearer",token);
        var started=await retry.PostAsJsonAsync($"/api/campaigns/{joined.Campaign.CampaignId}/start",command);started.EnsureSuccessStatusCode();var first=await started.Content.ReadAsStringAsync();
        var again=await retry.PostAsJsonAsync($"/api/campaigns/{joined.Campaign.CampaignId}/start",command);again.EnsureSuccessStatusCode();Assert.Equal(first,await again.Content.ReadAsStringAsync());
        Assert.Equal("active-weekend",(await again.Content.ReadFromJsonAsync<CampaignSnapshot>())!.Lifecycle);
        var qualifying=(await retry.GetFromJsonAsync<QualifyingPublicSnapshot>($"/api/lobbies/{joined.Campaign.WeekendId}/qualifying"))!;Assert.Equal("Q1",qualifying.Session);Assert.Equal(joined.Campaign.WeekendId,qualifying.SessionId);
    }
    [Fact]
    public async Task Permanent_host_creates_frozen_short_calendar_and_recovers_same_creation_after_restart()
    {
        var token=await LobbyTests.Identity(false);
        var store=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")??"127.0.0.1:19580");
        var input=new {commandId=Guid.NewGuid().ToString(),name="Host",mode="Competitive",calendar="short",seed=123,laps=6};
        string id,first,device;
        await using(var server=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(store)))))
        {
            using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
            var response=await client.PostAsJsonAsync("/api/campaigns",input);response.EnsureSuccessStatusCode();first=await response.Content.ReadAsStringAsync();
            var state=JsonDocument.Parse(first).RootElement;id=state.GetProperty("campaign").GetProperty("campaignId").GetString()!;
            device=state.GetProperty("weekend").GetProperty("membership").GetProperty("deviceId").GetString()!;
            Assert.Equal("created",state.GetProperty("campaign").GetProperty("lifecycle").GetString());
            Assert.Equal(2,state.GetProperty("campaign").GetProperty("calendar").GetArrayLength());
            Assert.Equal("track-monza",state.GetProperty("campaign").GetProperty("calendar")[0].GetProperty("trackId").GetString());
            Assert.Equal("track-monaco",state.GetProperty("campaign").GetProperty("calendar")[1].GetProperty("trackId").GetString());
            Assert.DoesNotContain("ratings",first);Assert.DoesNotContain("authUid",first);Assert.DoesNotContain("setup",first);
            Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/campaigns",new{commandId=Guid.NewGuid().ToString(),name="Host",mode="Competitive",calendar="full",seed=123,laps=6})).StatusCode);
        }
        await using var recovered=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(store))));
        using var retry=recovered.CreateClient();retry.DefaultRequestHeaders.Authorization=new("Bearer",token);
        var repeated=await retry.PostAsJsonAsync("/api/campaigns",input);repeated.EnsureSuccessStatusCode();Assert.Equal(first,await repeated.Content.ReadAsStringAsync());
        var stateAfter=await retry.GetFromJsonAsync<JsonElement>($"/api/campaigns/{id}?deviceId={device}");Assert.Equal(id,stateAfter.GetProperty("campaignId").GetString());
    }
}

