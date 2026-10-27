using System.Net;
using System.Net.Http.Json;
using Game.Application;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Game.Api.Tests;
public sealed partial class QualifyingTests
{
    [Fact]
    public async Task Opening_presentation_pause_and_stale_wakeups_cannot_start_two_first_windows()
    {
        var clock=new Clock();var wakeups=new CapturedWakeups();var token=await LobbyTests.Identity(false);
        await using var server=Server(Store(),clock,wakeups);using var client=server.CreateClient();var(id,_)=await Ready(client,token);
        var coordinator=server.Services.GetRequiredService<SessionCoordinator>();var root=$"/api/lobbies/{id}/qualifying";
        (await client.PostAsJsonAsync(root,await StartCommand(client,id))).EnsureSuccessStatusCode();
        for(var stage=1;stage<=3;stage++)
        {
            clock.Advance(30);await coordinator.SweepWakeups();
            if(stage<3){var state=(await client.GetFromJsonAsync<QualifyingPublicSnapshot>(root))!;(await client.PostAsJsonAsync(root+"/continue",new DueTransitionInput(Guid.NewGuid().ToString(),stage,state.WindowRevision))).EnsureSuccessStatusCode();}
        }
        var presentation=wakeups.Items.Last(w=>w.SessionId==id&&w.Kind=="presentation");
        var stale=new WakeupInput(id,presentation.Generation,presentation.Phase,presentation.WindowRevision);
        clock.Advance(7);Assert.Equal("phase-result",(await coordinator.DeliverWakeup(stale)).Lifecycle);
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/pause",new{})).EnsureSuccessStatusCode();clock.Advance(50);
        Assert.True((await coordinator.DeliverWakeup(stale)).Paused);
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/resume",new{})).EnsureSuccessStatusCode();
        var paused=(await client.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{id}"))!;Assert.Null(paused.DecisionDeadlineUtc);
        var resumed=wakeups.Items.Last(w=>w.SessionId==id);Assert.NotEqual(presentation.Generation,resumed.Generation);
        clock.Advance(1);var first=await coordinator.DeliverWakeup(new(id,resumed.Generation,resumed.Phase,resumed.WindowRevision));
        Assert.Equal("awaiting-decisions",first.Lifecycle);Assert.Equal(0,first.Phase);Assert.Equal(clock.GetUtcNow().AddSeconds(30),first.DecisionDeadlineUtc);
        Assert.Equal(first.WindowRevision,(await coordinator.DeliverWakeup(stale)).WindowRevision);
        Assert.Equal(first.WindowRevision,(await coordinator.DeliverWakeup(new(id,resumed.Generation,resumed.Phase,resumed.WindowRevision))).WindowRevision);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Opening_commits_once_recovers_and_first_window_begins_after_presentation(bool commitFirst)
    {
        var store=new FailingStore(Store(),commitFirst);var clock=new Clock();var wakeups=new CapturedWakeups();var token=await LobbyTests.Identity(false);
        await using var server=Server(store,clock,wakeups);using var client=server.CreateClient();var(id,device)=await Ready(client,token);
        var root=$"/api/lobbies/{id}/qualifying";
        (await client.PostAsJsonAsync(root,await StartCommand(client,id))).EnsureSuccessStatusCode();
        QualifyingCommand? last=null;
        for(var stage=1;stage<=3;stage++)
        {
            var own=await client.GetFromJsonAsync<System.Text.Json.JsonElement>(root+$"/teams/team-01?deviceId={device}");
            var command=Choice(own) with{Session=$"Q{stage}"};
            if(command.Choices!.Count==0){clock.Advance(60);await server.Services.GetRequiredService<SessionCoordinator>().SweepWakeups();}
            else
            {
                if(stage==3)store.Armed=true;
                var response=await client.PostAsJsonAsync(root+$"/teams/team-01/choices?deviceId={device}",command);
                if(stage==3){Assert.Equal(HttpStatusCode.ServiceUnavailable,response.StatusCode);response=await client.PostAsJsonAsync(root+$"/teams/team-01/choices?deviceId={device}",command);last=command;}
                response.EnsureSuccessStatusCode();
            }
            if(stage<3)
            {
                var state=(await client.GetFromJsonAsync<QualifyingPublicSnapshot>(root))!;
                (await client.PostAsJsonAsync(root+"/continue",new DueTransitionInput(Guid.NewGuid().ToString(),stage,state.WindowRevision))).EnsureSuccessStatusCode();
            }
        }
        var opening=(await client.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{id}"))!;
        Assert.Equal("phase-result",opening.Lifecycle);Assert.Equal(0,opening.Phase);Assert.Equal(1,opening.LeaderLap);
        Assert.Null(opening.DecisionDeadlineUtc);Assert.NotNull(opening.PresentationDeadlineUtc);Assert.Equal(22,opening.Opening!.Cars.Count);
        Assert.Contains(opening.Classification,c=>c.CompletedLaps==0&&c.Progress>0);Assert.All(opening.Classification,c=>Assert.True(c.TyreAge<=1));
        if(last is not null)(await client.PostAsJsonAsync(root+$"/teams/team-01/choices?deviceId={device}",last)).EnsureSuccessStatusCode();
        var before=await client.GetStringAsync($"/api/sessions/{id}");
        await using var restarted=Server(store,clock,new CapturedWakeups());using var recovered=restarted.CreateClient();recovered.DefaultRequestHeaders.Authorization=new("Bearer",token);
        var saved=(await recovered.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{id}"))!;
        Assert.True(saved.Paused);Assert.Equal(opening.RaceTime,saved.RaceTime);Assert.Equal(System.Text.Json.JsonSerializer.Serialize(opening.Classification),System.Text.Json.JsonSerializer.Serialize(saved.Classification));
        (await recovered.PostAsJsonAsync($"/api/lobbies/{id}/resume",new{})).EnsureSuccessStatusCode();
        var continuing=(await recovered.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{id}"))!;
        var skip=new DueTransitionInput(Guid.NewGuid().ToString(),0,continuing.WindowRevision);
        (await recovered.PostAsJsonAsync($"/api/lobbies/{id}/continue",skip)).EnsureSuccessStatusCode();
        var first=(await recovered.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{id}"))!;
        Assert.Equal("awaiting-decisions",first.Lifecycle);Assert.Equal(0,first.Phase);Assert.Equal(1,first.LeaderLap);Assert.NotNull(first.DecisionDeadlineUtc);
        Assert.Equal(opening.RaceTime,first.RaceTime);Assert.Equal(System.Text.Json.JsonSerializer.Serialize(opening.Classification),System.Text.Json.JsonSerializer.Serialize(first.Classification));
        (await recovered.PostAsJsonAsync($"/api/lobbies/{id}/continue",skip)).EnsureSuccessStatusCode();
        Assert.Equal(first.DecisionDeadlineUtc,(await recovered.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{id}"))!.DecisionDeadlineUtc);
        clock.Advance(30);
        (await recovered.PostAsJsonAsync($"/api/lobbies/{id}/advance-due",new DueTransitionInput(Guid.NewGuid().ToString(),0,first.WindowRevision))).EnsureSuccessStatusCode();
        var resolved=(await recovered.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{id}"))!;
        Assert.Equal(6,resolved.LeaderLap);Assert.Equal("finished",resolved.Lifecycle);
        Assert.True(BrowserReplay.Verify(restarted.Services.GetRequiredService<SessionCoordinator>().DesignerRecord(id)).Matches);
    }
}
