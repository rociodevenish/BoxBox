using Game.Application;
using Game.TestFixtures;
using Microsoft.AspNetCore.Mvc.Testing;
namespace Game.BrowserFixtureHost;
/// <summary>Test-only real API host; no alternative routes or physics.</summary>
public static class EntryPoint
{
    public static async Task Main()
    {
        await using var factory=new WebApplicationFactory<global::Program>().WithWebHostBuilder(builder=>
            builder.UseContentRoot(Path.GetFullPath("../../../../../src/backend/Game.Api",AppContext.BaseDirectory)).UseUrls("http://127.0.0.1:4510")
                .ConfigureServices(services=>services.AddSingleton<IRacePresets>(new StopBoundaryPresets())));
        factory.UseKestrel(4510);
        using var client=factory.CreateClient();
        Console.WriteLine($"Real fixture API running at {client.BaseAddress}");
        await Task.Delay(Timeout.Infinite);
    }
    private sealed class StopBoundaryPresets : IRacePresets
    {
        public RacePreset Get(string name,int laps)=>new BoundaryRacePresets(name=="restricted-equal"?350:600).Get(name,laps);
    }
}
