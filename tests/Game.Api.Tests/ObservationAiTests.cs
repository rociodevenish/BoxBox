using System.Net.Http.Json;
using System.Text.Json;
using Game.Application;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;
namespace Game.Api.Tests;
public sealed class ObservationAiTests
{
    [Fact]
    public async Task Automatic_choices_record_policy_source_and_contextual_legal_battles()
    {
        await using var server=new WebApplicationFactory<Program>().WithWebHostBuilder(b=>b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{{"Designer:Key","ai-evidence"}})));
        using var client=server.CreateClient();
        var created=(await(await client.PostAsJsonAsync("/api/sessions",new CreateSessionInput("open-equal",40,123))).Content.ReadFromJsonAsync<SessionCreated>())!;
        client.DefaultRequestHeaders.Authorization=new("Bearer",created.ControllerCapability);
        (await client.PostAsJsonAsync($"/api/sessions/{created.SessionId}/plans",new TeamCommand(Guid.NewGuid().ToString(),0,1,
            [new("driver-01","Standard","Stay Out",null,"Neutral"),new("driver-02","Standard","Stay Out",null,"Neutral")]))).EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Add("X-Designer-Key","ai-evidence");
        var record=await client.GetFromJsonAsync<JsonElement>($"/api/designer/sessions/{created.SessionId}/record");
        Assert.Equal("dry-observation-ai-v2",record.GetProperty("opponentScriptVersion").GetString());
        var automatic=record.GetProperty("automaticPlans")[0];
        Assert.Equal("unclaimed-ai",automatic.GetProperty("source").GetString());
        Assert.Equal(20,automatic.GetProperty("decisions").EnumerateObject().Count());
        Assert.Contains(automatic.GetProperty("decisions").EnumerateObject(),choice=>choice.Value.GetProperty("battle").GetString()=="Attack");
    }
}

