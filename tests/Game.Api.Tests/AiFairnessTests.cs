using System.Net.Http.Json;
using System.Text.Json;
using Game.Domain;
using Game.Application;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace Game.Api.Tests;
public sealed class AiFairnessTests
{
    [Fact]
    public async Task Same_phase_start_observation_ignores_hidden_form_and_human_locked_choices()
    {
        async Task<string> Run(double form,string pace)
        {
            await using var server=Server(new Inputs(form));using var client=server.CreateClient();
            var created=(await(await client.PostAsJsonAsync("/api/sessions",new CreateSessionInput("open-equal",6,7))).Content.ReadFromJsonAsync<SessionCreated>())!;
            client.DefaultRequestHeaders.Authorization=new("Bearer",created.ControllerCapability);
            var observation=await client.GetStringAsync($"/api/sessions/{created.SessionId}/team");
            Assert.DoesNotContain("raceForm",observation,StringComparison.OrdinalIgnoreCase);
            (await client.PostAsJsonAsync($"/api/sessions/{created.SessionId}/plans",new TeamCommand(Guid.NewGuid().ToString(),0,1,
                [new("driver-01",pace,"Stay Out",null,"Neutral"),new("driver-02",pace,"Stay Out",null,"Neutral")]))).EnsureSuccessStatusCode();
            client.DefaultRequestHeaders.Add("X-Designer-Key","ai-evidence");
            var record=await client.GetFromJsonAsync<JsonElement>($"/api/designer/sessions/{created.SessionId}/record");
            return record.GetProperty("automaticPlans")[0].GetProperty("decisions").GetRawText();
        }
        Assert.Equal(await Run(0,"Standard"),await Run(4,"Push"));
    }
    [Fact]
    public async Task Short_race_ai_uses_a_second_compound_from_a_hard_start()
    {
        await using var server=Server(new Inputs(0,TyreCompound.Hard,true));using var client=server.CreateClient();
        var created=(await(await client.PostAsJsonAsync("/api/sessions",new CreateSessionInput("open-equal",6,7))).Content.ReadFromJsonAsync<SessionCreated>())!;
        client.DefaultRequestHeaders.Authorization=new("Bearer",created.ControllerCapability);
        PublicSnapshot state;
        do
        {
            state=(await client.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{created.SessionId}"))!;
            if(state.Lifecycle=="finished")break;
            (await client.PostAsJsonAsync($"/api/sessions/{created.SessionId}/plans",new TeamCommand(Guid.NewGuid().ToString(),state.Phase,state.Revision,
                [new("driver-01","Standard","Stay Out",null,"Neutral"),new("driver-02","Standard","Stay Out",null,"Neutral")]))).EnsureSuccessStatusCode();
        }while(true);
        Assert.All(state.Classification.Where(c=>c.TeamId!="team-01"),c=>Assert.True(c.EligibleAtFinish));
    }
    private static WebApplicationFactory<Program> Server(IRacePresets presets)=>new WebApplicationFactory<Program>().WithWebHostBuilder(b=>
    {b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{{"Designer:Key","ai-evidence"}}));b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton(presets)));});
    private sealed class Inputs(double form,TyreCompound compound=TyreCompound.Medium,bool humanBehind=false):IRacePresets
    {
        public RacePreset Get(string name,int laps)
        {var preset=new DryRacePresets().Get(name,laps);return preset with{Configuration=preset.Configuration with{RaceFormVariation=form},Drivers=preset.Drivers.Select(d=>d with{StartingCompound=compound,Gap=humanBehind&&d.Team=="team-01"?50:d.Gap}).ToArray()};}
    }
}

