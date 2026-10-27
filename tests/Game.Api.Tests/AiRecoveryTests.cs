using Game.Application;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;
namespace Game.Api.Tests;
public sealed class AiRecoveryTests
{
    [Fact]
    public async Task Restart_reuses_frozen_automatic_choices_and_records_same_policy()
    {
        WebApplicationFactory<Program> Server()=>new WebApplicationFactory<Program>().WithWebHostBuilder(b=>b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>
        {{"Designer:Key","ai-evidence"},{"Persistence:ProjectId","demo-f1game"},{"Persistence:EmulatorHost",Environment.GetEnvironmentVariable("F1_AI_RECOVERY_HOST")??Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")??"127.0.0.1:19100"}})));
        async Task<(SessionCreated,string)> Run(bool restart)
        {
            SessionCreated created;
            await using(var first=Server())
            {
                using var client=first.CreateClient();
                created=(await(await client.PostAsJsonAsync("/api/sessions",new CreateSessionInput("open-equal",6,7))).Content.ReadFromJsonAsync<SessionCreated>())!;
                if(!restart)return(created,await Complete(client,created));
            }
            await using var recovered=Server();using var read=recovered.CreateClient();
            return(created,await Complete(read,created));
        }
        async Task<string> Complete(HttpClient client,SessionCreated created)
        {
            client.DefaultRequestHeaders.Authorization=new("Bearer",created.ControllerCapability);
            // Refreshing and repeated observations must not change the phase-start automatic choices.
            for(var read=0;read<3;read++)(await client.GetAsync($"/api/sessions/{created.SessionId}/team")).EnsureSuccessStatusCode();
            (await client.PostAsJsonAsync($"/api/sessions/{created.SessionId}/plans",new TeamCommand(Guid.NewGuid().ToString(),0,1,
                [new("driver-01","Standard","Stay Out",null,"Neutral"),new("driver-02","Standard","Stay Out",null,"Neutral")]))).EnsureSuccessStatusCode();
            client.DefaultRequestHeaders.Add("X-Designer-Key","ai-evidence");
            var record=await client.GetFromJsonAsync<JsonElement>($"/api/designer/sessions/{created.SessionId}/record");
            return record.GetProperty("automaticPlans").GetRawText();
        }
        Assert.Equal((await Run(false)).Item2,(await Run(true)).Item2);
    }
}
