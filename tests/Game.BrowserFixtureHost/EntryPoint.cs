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
                .ConfigureServices(services=>services.AddSingleton<IRacePresets>(new StopBoundaryPresets())));
        factory.UseKestrel(port);
        using var client=factory.CreateClient();
        Console.WriteLine($"Real fixture API running at {client.BaseAddress}");
        await Task.Delay(Timeout.Infinite);
    }
    private sealed class StopBoundaryPresets : IRacePresets
    {
        public RacePreset Get(string name,int laps)=>new BoundaryRacePresets(name=="restricted-equal"?350:600).Get(name,laps);
    }
}
