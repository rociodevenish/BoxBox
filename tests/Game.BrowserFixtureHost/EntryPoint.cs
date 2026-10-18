using Game.Application;
using Game.TestFixtures;
using Microsoft.AspNetCore.Mvc.Testing;
namespace Game.BrowserFixtureHost;
/// <summary>Test-only real API host; no alternative routes or physics.</summary>
public static class EntryPoint
{
    public static async Task Main()
    {
        var port=int.Parse(Environment.GetEnvironmentVariable("F1_API_PORT")??"4571");
        var repositoryRoot=Environment.GetEnvironmentVariable("F1_REPO_ROOT");
        var contentRoot=repositoryRoot is null
            ?Path.GetFullPath("../../../../../src/backend/Game.Api",AppContext.BaseDirectory)
            :Path.Combine(repositoryRoot,"src","backend","Game.Api");
        await using var factory=new WebApplicationFactory<global::Program>().WithWebHostBuilder(builder=>
            builder.UseContentRoot(contentRoot).UseUrls($"http://127.0.0.1:{port}")
                .ConfigureServices(services=>{services.AddSingleton<IRacePresets>(Environment.GetEnvironmentVariable("F1_WEATHER_REACTION_FIXTURE")=="1"?new WeatherReactionRacePresets():Environment.GetEnvironmentVariable("F1_WET_TYRE_FIXTURE")=="1"?new WetTyreRacePresets():Environment.GetEnvironmentVariable("F1_WEATHER_FIXTURE")=="1"?new WeatherRacePresets():Environment.GetEnvironmentVariable("F1_SAFETY_CAR_FIXTURE")=="1"?new SafetyCarWindowPresets():new StopBoundaryPresets());if(Environment.GetEnvironmentVariable("F1_OPENING_DISRUPTION_FIXTURE")=="1")services.AddSingleton<IOpeningDisruptionPolicy>(new OpeningLabPolicy());if(Environment.GetEnvironmentVariable("F1_CONTROLLED_CLOCK")=="1"){services.AddSingleton<BrowserClock>();services.AddSingleton<TimeProvider>(sp=>sp.GetRequiredService<BrowserClock>());services.AddSingleton<IStartupFilter,ClockControlFilter>();}}));
        factory.UseKestrel(port);
        using var client=factory.CreateClient();
        Console.WriteLine($"Real fixture API running at {client.BaseAddress}");
        await Task.Delay(Timeout.Infinite);
    }
    private sealed class OpeningLabPolicy:IOpeningDisruptionPolicy
    {
        public OpeningDisruptionConfiguration? ForCircuit(string circuitId)=>circuitId=="track-monza"
            ?new(new(.2,38),new(FailureRate:.01,ComponentWearPerSecond:0,InterruptMajorIncidents:true)):null;
    }
    private sealed class BrowserClock:TimeProvider
    {
        private DateTimeOffset now=DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow()=>now;
        public void Advance(int seconds)=>now=now.AddSeconds(seconds);
    }
    private sealed record ClockAdvance(int Seconds);
    private sealed class ClockControlFilter(BrowserClock clock):IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)=>app=>{
            app.Use(async(context,following)=>{
                if(context.Request.Path=="/__test/clock"&&context.Request.Method=="POST"){
                    var input=await context.Request.ReadFromJsonAsync<ClockAdvance>();
                    if(input is null||input.Seconds<0||input.Seconds>86400){context.Response.StatusCode=400;return;}
                    clock.Advance(input.Seconds);await context.Response.WriteAsJsonAsync(new{serverTimeUtc=clock.GetUtcNow()});return;
                }
                await following(context);
            });next(app);
        };
    }
    private sealed class StopBoundaryPresets : IRacePresets
    {
        public RacePreset Get(string name,int laps)=>new BoundaryRacePresets(name=="restricted-equal"?350:600).Get(name,laps);
    }
}
