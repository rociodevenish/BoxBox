using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;
namespace Game.Api.Tests;
public sealed class HostedConfigurationTests
{
    [Theory]
    [InlineData("/api/auth/config")]
    [InlineData("/hubs/race/negotiate?negotiateVersion=1")]
    [InlineData("/hubs/race")]
    public async Task Unlisted_browser_origin_is_rejected_on_api_and_hub(string route)
    {
        await using var host=new WebApplicationFactory<Program>().WithWebHostBuilder(builder=>builder.UseSetting("Hosting:AllowedOrigins","https://family.example"));
        using var client=host.CreateClient();
        using var request=new HttpRequestMessage(HttpMethod.Post,route);
        request.Headers.Add("Origin","https://attacker.example");
        Assert.Equal(HttpStatusCode.Forbidden,(await client.SendAsync(request)).StatusCode);
    }
    [Fact]
    public async Task Listed_frontend_preflight_allows_bearer_and_json()
    {
        await using var host=new WebApplicationFactory<Program>().WithWebHostBuilder(builder=>builder.UseSetting("Hosting:AllowedOrigins","https://family.example"));
        using var client=host.CreateClient();using var request=new HttpRequestMessage(HttpMethod.Options,"/api/lobbies");
        request.Headers.Add("Origin","https://family.example");request.Headers.Add("Access-Control-Request-Method","POST");request.Headers.Add("Access-Control-Request-Headers","authorization,content-type");
        var response=await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent,response.StatusCode);Assert.Equal("https://family.example",response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }
    [Fact]
    public void Missing_production_configuration_fails_with_actionable_key()
    {
        using var host=new WebApplicationFactory<Program>().WithWebHostBuilder(builder=>builder.UseEnvironment("Production"));
        var error=Assert.Throws<InvalidOperationException>(()=>host.CreateClient());Assert.Contains("Firebase:ProjectId",error.Message);
    }
    private static readonly Dictionary<string,string> Production=new(){
        ["Firebase:ProjectId"]="family-project",["Firebase:ApiKey"]="public-client-key",["Persistence:ProjectId"]="family-project",
        ["Hosting:AllowedOrigins"]="https://family.example",["Wakeups:ProjectId"]="family-project",["Wakeups:Location"]="us-central1",["Wakeups:Queue"]="race-wakeups",
        ["Wakeups:CallbackUrl"]="https://race.example/internal/wakeups",["Wakeups:Audience"]="https://race.example",
        ["Wakeups:ServiceAccountEmail"]="task@family-project.iam.gserviceaccount.com",["Wakeups:ServiceAccountSubject"]="1234",
        ["Wakeups:SweepServiceAccountEmail"]="sweep@family-project.iam.gserviceaccount.com",["Wakeups:SweepServiceAccountSubject"]="5678"};
    [Fact]
    public async Task Complete_production_config_starts_and_exposes_only_public_auth_settings()
    {
        await using var host=Configured();using var client=host.CreateClient();
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/health")).StatusCode);
        var auth=await client.GetStringAsync("/api/auth/config");
        Assert.Contains("public-client-key",auth);Assert.DoesNotContain("iam.gserviceaccount",auth);Assert.DoesNotContain("1234",auth);Assert.Contains("\"emulatorUrl\":null",auth);
    }
    [Theory]
    [InlineData("Wakeups:Audience","https://wrong.example")]
    [InlineData("Wakeups:SweepServiceAccountSubject","1234")]
    [InlineData("Persistence:EmulatorHost","localhost:19330")]
    [InlineData("Hosting:AllowedOrigins","https://family.example/path")]
    [InlineData("Firebase:ApiKey","")]
    [InlineData("Wakeups:Queue","")]
    public void Unsafe_or_incomplete_production_config_fails_before_serving(string key,string value)
    {
        using var host=Configured(key,value);Assert.Throws<InvalidOperationException>(()=>host.CreateClient());
    }
    private static WebApplicationFactory<Program> Configured(string? key=null,string? value=null)=>new WebApplicationFactory<Program>().WithWebHostBuilder(builder=>{
        builder.UseEnvironment("Production");foreach(var setting in Production)builder.UseSetting(setting.Key,setting.Value);
        if(key is not null)builder.UseSetting(key,value);
    });
}
