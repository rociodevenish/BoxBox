using System.Net.Http.Json;
using Game.Application;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Domain;
using Game.Simulation;
namespace Game.Api.Tests;
public sealed partial class QualifyingTests
{
    [Fact]
    public async Task Legacy_opening_recovers_its_recorded_version_and_replays_the_first_race_phase()
    {
        var store=new LegacyOpeningStore(Store());var clock=new Clock();var token=await LobbyTests.Identity(false);
        await using var server=Server(store,clock);using var client=server.CreateClient();var(id,_)=await Ready(client,token);
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/qualifying",await StartCommand(client,id))).EnsureSuccessStatusCode();
        for(var stage=1;stage<=3;stage++)
        {
            clock.Advance(30);await server.Services.GetRequiredService<SessionCoordinator>().SweepWakeups();
            if(stage<3)
            {
                var state=(await client.GetFromJsonAsync<QualifyingPublicSnapshot>($"/api/lobbies/{id}/qualifying"))!;
                (await client.PostAsJsonAsync($"/api/lobbies/{id}/qualifying/continue",new DueTransitionInput(Guid.NewGuid().ToString(),stage,state.WindowRevision))).EnsureSuccessStatusCode();
            }
        }
        await using var restarted=Server(store,clock);using var recovered=restarted.CreateClient();recovered.DefaultRequestHeaders.Authorization=new("Bearer",token);
        var restored=(await recovered.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{id}"))!;
        Assert.Equal(AutomaticRaceStart.LegacyModelVersion,restored.Opening!.Version);
        Assert.Equal(store.Opening!.Opening.Time,restored.RaceTime);
        (await recovered.PostAsJsonAsync($"/api/lobbies/{id}/resume",new{})).EnsureSuccessStatusCode();
        var window=(await recovered.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{id}"))!;
        (await recovered.PostAsJsonAsync($"/api/lobbies/{id}/continue",new DueTransitionInput(Guid.NewGuid().ToString(),0,window.WindowRevision))).EnsureSuccessStatusCode();
        window=(await recovered.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{id}"))!;clock.Advance(30);
        (await recovered.PostAsJsonAsync($"/api/lobbies/{id}/advance-due",new DueTransitionInput(Guid.NewGuid().ToString(),0,window.WindowRevision))).EnsureSuccessStatusCode();
        var record=restarted.Services.GetRequiredService<SessionCoordinator>().DesignerRecord(id);
        Assert.Equal(AutomaticRaceStart.LegacyModelVersion,record.Replay.Opening!.Input.ModelVersion);
        var parsed=BrowserReplay.Read(JsonSerializer.SerializeToElement(record,BrowserReplay.JsonOptions));
        Assert.True(BrowserReplay.Verify(parsed).Matches);
        Assert.Equal(6,record.Phases.Single().LeaderLap);
    }
    // Model a stored pre-v2 opening at the persistence boundary, before any race decisions exist.
    private sealed class LegacyOpeningStore(ISessionStore inner):ISessionStore
    {
        public AutomaticStartResult? Opening{get;private set;}
        public StoredSession? Read(string id)=>inner.Read(id);
        public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);
        public IReadOnlyList<string> ListSessions()=>inner.ListSessions();
        public string Write(string id,string? expected,string payload,string? code)=>Write(id,expected,payload,code,null);
        public string Write(string id,string? expected,string payload,string? code,ScheduledWakeup? wakeup)
        {
            var document=JsonNode.Parse(payload)!;var race=document["lobby"]?["race"];
            if(Opening is null&&race?["opening"] is {} saved)
            {
                var replay=saved.Deserialize<AutomaticStartReplay>(BrowserReplay.JsonOptions)!;
                var input=replay.Input with{ModelVersion=AutomaticRaceStart.LegacyModelVersion};
                Opening=AutomaticRaceStart.Create(input);
                race["opening"]=JsonSerializer.SerializeToNode(replay with{Input=input,OutcomeHash=AutomaticRaceStart.OutcomeHash(Opening.Opening)},BrowserReplay.JsonOptions);
                // Historical opening saves predate the explicit presentation cursor.
                race["window"]?["moduleState"]?.AsObject().Remove("opening-presentation");
                payload=document.ToJsonString();
            }
            return inner.Write(id,expected,payload,code,wakeup);
        }
    }
}
