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
        await using var factory=new WebApplicationFactory<global::Program>().WithWebHostBuilder(builder=>
            builder.UseContentRoot(Path.GetFullPath("../../../../../src/backend/Game.Api",AppContext.BaseDirectory)).UseUrls($"http://127.0.0.1:{port}")
                .ConfigureServices(services=>{services.AddSingleton<IRacePresets>(new StopBoundaryPresets());if(Environment.GetEnvironmentVariable("F1_CONTROLLED_CLOCK")=="1"){services.AddSingleton<BrowserClock>();services.AddSingleton<TimeProvider>(sp=>sp.GetRequiredService<BrowserClock>());services.AddSingleton<IStartupFilter,ClockControlFilter>();}}));
        factory.UseKestrel(port);
        using var client=factory.CreateClient();
        Console.WriteLine($"Real fixture API running at {client.BaseAddress}");
        await Task.Delay(Timeout.Infinite);
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
