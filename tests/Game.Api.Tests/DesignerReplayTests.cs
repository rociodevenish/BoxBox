using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;
namespace Game.Api.Tests;
public sealed class DesignerReplayTests
{
    private const string TestDesignerKey = "independent-local-test-designer-key";
    [Theory]
    [InlineData(false, TestDesignerKey)]
    [InlineData(true, "invalid-key")]
    public async Task Missing_configuration_or_invalid_designer_key_disables_access(bool configured,string supplied)
    {
        await using var server=new WebApplicationFactory<Program>().WithWebHostBuilder(builder=>builder.ConfigureAppConfiguration((_,config)=>
            config.AddInMemoryCollection(new Dictionary<string,string?> { ["Designer:Key"]=configured?TestDesignerKey:null })));
        using var client=server.CreateClient();client.DefaultRequestHeaders.Add("X-Designer-Key",supplied);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/designer/sessions/unknown/record")).StatusCode);
    }
    [Fact]
    public async Task Designer_record_is_separate_from_player_grants_and_replays_every_accepted_phase_exactly()
    {
        await using var server = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Designer:Key"] = TestDesignerKey })));
        using var client = server.CreateClient();
        var created = await (await client.PostAsJsonAsync("/api/sessions", new { preset = "open-equal", laps = 6, seed = 123 })).Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("sessionId").GetString();
        var route = $"/api/sessions/{id}"; var designer = $"/api/designer/sessions/{id}/record";
        Assert.DoesNotContain("designer", created.GetRawText(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(designer)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", created.GetProperty("controllerCapability").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(designer)).StatusCode);
        for (var phase = 0; phase < 2; phase++)
        {
            var state = await client.GetFromJsonAsync<JsonElement>(route);
            (await client.PostAsJsonAsync(route + "/plans", new { commandId = Guid.NewGuid().ToString(), phase, revision = state.GetProperty("revision").GetInt64(), cars = new[] {
                new {driverId="driver-01",pace="Push",pit="Stay Out",compound=(string?)null,battle="Neutral"},
                new {driverId="driver-02",pace="Standard",pit=phase==0?"Pit":"Stay Out",compound=phase==0?"Hard":null,battle="Neutral"}
            }})).EnsureSuccessStatusCode();

        }
        foreach (var playerRoute in new[] {route,route+"/team"}) {
            var payload=await client.GetStringAsync(playerRoute);
            foreach(var hidden in new[]{"seed","modelVersion","randomnessVersion","opponentScriptVersion","outcomeHash","decisionMilliseconds","resolutionMilliseconds"}) Assert.DoesNotContain(hidden,payload,StringComparison.OrdinalIgnoreCase);
        }
        client.DefaultRequestHeaders.Add("X-Designer-Key", TestDesignerKey);
        var record = await client.GetFromJsonAsync<JsonElement>(designer);
        Assert.Equal("browser-dry-race-replay-v1",record.GetProperty("formatVersion").GetString());
        Assert.Equal(123,record.GetProperty("replay").GetProperty("seed").GetInt32());
        Assert.Equal("dry-observation-ai-v2",record.GetProperty("opponentScriptVersion").GetString());
        Assert.Equal(44,record.GetProperty("replay").GetProperty("plan").GetProperty("scheduled").GetArrayLength());
        Assert.Equal(2,record.GetProperty("phases").GetArrayLength());
        var response = await client.PostAsJsonAsync("/api/designer/replay/verify",record);
        response.EnsureSuccessStatusCode();
        var verified=await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(verified.GetProperty("matches").GetBoolean());
        Assert.True(verified.GetProperty("finished").GetBoolean());
        Assert.Equal(2,verified.GetProperty("phases").GetArrayLength());
        var incompatible=JsonNode.Parse(record.GetRawText())!;
        incompatible["replay"]!["modelVersion"]="unsupported-model";
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/designer/replay/verify",incompatible)).StatusCode);
        var incomplete=JsonNode.Parse(record.GetRawText())!;
        incomplete["replay"]!.AsObject().Remove("seed");
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/designer/replay/verify",incomplete)).StatusCode);
    }
    [Fact]
    public async Task Timings_are_designer_only_and_client_presentation_samples_are_write_only_diagnostics()
    {
        await using var server = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string,string?> { ["Designer:Key"] = TestDesignerKey })));
        using var client=server.CreateClient();
        var created=await (await client.PostAsJsonAsync("/api/sessions",new{preset="open-equal",laps=6,seed=123})).Content.ReadFromJsonAsync<JsonElement>();
        var id=created.GetProperty("sessionId").GetString();var route="/api/sessions/"+id;
        client.DefaultRequestHeaders.Authorization=new("Bearer",created.GetProperty("controllerCapability").GetString());
        var cars=new[]{new{driverId="driver-01",pace="Standard",pit="Stay Out",compound=(string?)null,battle="Neutral"},new{driverId="driver-02",pace="Standard",pit="Stay Out",compound=(string?)null,battle="Neutral"}};
        (await client.PostAsJsonAsync(route+"/plans",new{commandId=Guid.NewGuid().ToString(),phase=0,revision=1,cars})).EnsureSuccessStatusCode();
        var state=await client.GetFromJsonAsync<JsonElement>(route);var cursor=state.GetProperty("presentation").GetProperty("cursor").GetString();
        using var display=server.CreateClient();
        var sample=new{sampleId=Guid.NewGuid().ToString(),deviceId=Guid.NewGuid().ToString(),cursor,durationMilliseconds=42.5,clientElapsedMilliseconds=200.5,playbackKind="initial",completion="skipped"};
        var posted=await display.PostAsJsonAsync(route+"/presentation-timing",sample);posted.EnsureSuccessStatusCode();
        Assert.Equal(new[]{"recorded"},(await posted.Content.ReadFromJsonAsync<JsonElement>()).EnumerateObject().Select(field=>field.Name));
        (await display.PostAsJsonAsync(route+"/presentation-timing",sample)).EnsureSuccessStatusCode();
        PublicSnapshotAssertions.EqualState(state.GetRawText(),(await display.GetFromJsonAsync<JsonElement>(route)).GetRawText());
        client.DefaultRequestHeaders.Add("X-Designer-Key",TestDesignerKey);
        var record=await client.GetFromJsonAsync<JsonElement>($"/api/designer/sessions/{id}/record");
        var timing=record.GetProperty("timing");
        Assert.Equal("server-monotonic-window-clock",timing.GetProperty("phaseTimings")[0].GetProperty("source").GetString());
        Assert.True(timing.GetProperty("phaseTimings")[0].GetProperty("decisionMilliseconds").GetDouble()>=0);
        Assert.True(timing.GetProperty("phaseTimings")[0].GetProperty("resolutionMilliseconds").GetDouble()>0);
        var samples=timing.GetProperty("presentationSamples");Assert.Equal(1,samples.GetArrayLength());
        Assert.Equal("client-reported-monotonic-page-clock",samples[0].GetProperty("source").GetString());
        Assert.Equal(42.5,samples[0].GetProperty("durationMilliseconds").GetDouble());
        Assert.True(timing.GetProperty("elapsedMilliseconds").GetDouble()>0);
        Assert.Equal(JsonValueKind.Null,timing.GetProperty("totalToFinishMilliseconds").ValueKind);
    }
}
