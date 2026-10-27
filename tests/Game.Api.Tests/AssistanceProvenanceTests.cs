using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Application;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Game.Api.Tests;
public sealed class AssistanceProvenanceTests
{
    [Fact]
    public async Task Human_help_history_keeps_the_concrete_phase_advice_and_its_policy_across_legacy_ai_restart()
    {
        var clock=new DeadlineTests.ControlledClock();using var underlying=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST")??"127.0.0.1:19100");var store=new CompatibleOldRecord(underlying);string id,device,token,advice;
        await using(var first=Server(clock,store))
        {
            using var host=first.CreateClient();var setup=await AssistanceTests.Setup(host);(id,device)=(setup.Id,setup.Device);token=host.DefaultRequestHeaders.Authorization!.Parameter!;
            (await host.PostAsJsonAsync($"/api/lobbies/{id}/players/{setup.Player}/assistance",new{helpLevel="Guided",autoAcceptRecommendation=false})).EnsureSuccessStatusCode();
            (await host.PostAsJsonAsync($"/api/lobbies/{id}/start",new{})).EnsureSuccessStatusCode();
            var own=await host.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}/teams/team-01?deviceId={device}");advice=own.GetProperty("assistance").GetProperty("recommendation").GetRawText();
            Assert.Equal(ObservationDecisionPolicy.Version,own.GetProperty("assistance").GetProperty("recommendation").GetProperty("policyVersion").GetString());
            (await host.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/plans?deviceId={device}",new TeamCommand(Guid.NewGuid().ToString(),0,1,[new("driver-01","Conserve","Stay Out",null,"Neutral"),new("driver-02","Standard","Stay Out",null,"Neutral")]))).EnsureSuccessStatusCode();
            (await host.PostAsJsonAsync($"/api/lobbies/{id}/players/{setup.Player}/assistance",new{helpLevel="Assisted",autoAcceptRecommendation=false})).EnsureSuccessStatusCode();
        }
        store.LegacyAi=true;
        await using(var second=Server(clock,store))
        {
            using var host=second.CreateClient();host.DefaultRequestHeaders.Authorization=new("Bearer",token);
            (await host.PostAsJsonAsync($"/api/lobbies/{id}/resume",new{})).EnsureSuccessStatusCode();
            (await host.PostAsJsonAsync($"/api/lobbies/{id}/continue",new DueTransitionInput(Guid.NewGuid().ToString(),0,1))).EnsureSuccessStatusCode();
        }
        await using var third=Server(clock,store);using var read=third.CreateClient();read.DefaultRequestHeaders.Authorization=new("Bearer",token);read.DefaultRequestHeaders.Add("X-Designer-Key","provenance-evidence");
        var record=await read.GetFromJsonAsync<JsonElement>($"/api/designer/sessions/{id}/record");Assert.Equal("dry-opponents-v1",record.GetProperty("opponentScriptVersion").GetString());
        var history=record.GetProperty("assistanceHistory").EnumerateArray().ToArray();Assert.Equal(2,history.Length);
        Assert.Equal("Guided",history[0].GetProperty("helpLevel").GetString());Assert.Equal("team-01",history[0].GetProperty("teamId").GetString());Assert.Equal(0,history[0].GetProperty("phase").GetInt32());
        Assert.Equal(ObservationDecisionPolicy.Version,history[0].GetProperty("policyVersion").GetString());Assert.Equal(advice,history[0].GetProperty("recommendation").GetRawText());
        Assert.Equal("Assisted",history[1].GetProperty("helpLevel").GetString());Assert.Equal(1,history[1].GetProperty("phase").GetInt32());Assert.Equal(ObservationDecisionPolicy.Version,history[1].GetProperty("policyVersion").GetString());
        var actual=record.GetProperty("replay").GetProperty("plan").GetProperty("scheduled").EnumerateArray().Single(entry=>entry.GetProperty("phase").GetInt32()==0&&entry.GetProperty("driverId").GetString()=="driver-01");Assert.Equal("Conserve",actual.GetProperty("decision").GetProperty("pace").GetString());
        var player=await read.GetStringAsync($"/api/lobbies/{id}/teams/team-01?deviceId={device}");Assert.DoesNotContain("assistanceHistory",player);var display=await read.GetStringAsync($"/api/sessions/{id}");Assert.DoesNotContain("recommendation",display);Assert.DoesNotContain("assistanceHistory",display);
    }
    [Fact]
    public async Task Old_saved_advice_remains_readable_without_inventing_historical_policy_provenance()
    {
        var clock=new DeadlineTests.ControlledClock();using var underlying=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST")??"127.0.0.1:19100");var store=new CompatibleOldRecord(underlying);string id,device,token;
        await using(var first=Server(clock,store))
        {
            using var host=first.CreateClient();var setup=await AssistanceTests.Setup(host);(id,device)=(setup.Id,setup.Device);token=host.DefaultRequestHeaders.Authorization!.Parameter!;
            (await host.PostAsJsonAsync($"/api/lobbies/{id}/players/{setup.Player}/assistance",new{helpLevel="Assisted",autoAcceptRecommendation=false})).EnsureSuccessStatusCode();(await host.PostAsJsonAsync($"/api/lobbies/{id}/start",new{})).EnsureSuccessStatusCode();
        }
        store.OmitProvenance=true;await using var restart=Server(clock,store);using var read=restart.CreateClient();read.DefaultRequestHeaders.Authorization=new("Bearer",token);read.DefaultRequestHeaders.Add("X-Designer-Key","provenance-evidence");
        var own=await read.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}/teams/team-01?deviceId={device}");Assert.NotEmpty(own.GetProperty("assistance").GetProperty("recommendation").GetProperty("cars").EnumerateArray());
        var record=await read.GetFromJsonAsync<JsonElement>($"/api/designer/sessions/{id}/record");Assert.True(!record.TryGetProperty("assistanceHistory",out var history)||history.ValueKind==JsonValueKind.Null);
    }
    private static WebApplicationFactory<Program> Server(TimeProvider clock,ISessionStore store)=>AssistanceTests.Server().WithWebHostBuilder(builder=>{
        builder.ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(new Dictionary<string,string?>{["Wakeups:Mode"]="manual",["Designer:Key"]="provenance-evidence"}));builder.ConfigureServices(services=>{services.AddSingleton(clock);services.AddSingleton(store);});
    });
    private sealed class CompatibleOldRecord(ISessionStore inner):ISessionStore
    {
        public bool LegacyAi{get;set;}public bool OmitProvenance{get;set;}
        public StoredSession? Read(string id)
        {
            var record=inner.Read(id);if(record is null)return null;var json=JsonNode.Parse(record.Payload)!;
            if(LegacyAi)json["policy"]="dry-opponents-v1";
            if(OmitProvenance){var modules=json["lobby"]!["race"]!["window"]!["moduleState"]!.AsObject();modules.Remove("assistance-history");foreach(var pair in modules.ToArray().Where(pair=>pair.Key.StartsWith("assistance:",StringComparison.Ordinal))){var advice=JsonNode.Parse(pair.Value!.GetValue<string>())!;advice["recommendation"]?.AsObject().Remove("policyVersion");modules[pair.Key]=advice.ToJsonString();}}
            return record with{Payload=json.ToJsonString()};
        }
        public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);
        public IReadOnlyList<string> ListSessions()=>inner.ListSessions();
        public string Write(string id,string? version,string payload,string? code)=>inner.Write(id,version,payload,code);
        public string Write(string id,string? version,string payload,string? code,ScheduledWakeup? wakeup)=>inner.Write(id,version,payload,code,wakeup);
    }
}
