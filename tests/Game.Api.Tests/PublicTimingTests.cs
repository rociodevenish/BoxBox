using Game.Application;
using Game.Domain;
using Game.Simulation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
namespace Game.Api.Tests;
public sealed class PublicTimingTests
{
    [Fact]
    public async Task Public_timing_is_bounded_recorded_and_next_distance_tracks_remaining_laps()
    {
        await using var server = new WebApplicationFactory<Program>();
        using var client = server.CreateClient();
        var created = (await (await client.PostAsJsonAsync("/api/sessions", new CreateSessionInput("open-equal", 10, 123))).Content.ReadFromJsonAsync<SessionCreated>())!;
        client.DefaultRequestHeaders.Authorization = new("Bearer", created.ControllerCapability);
        var route = $"/api/sessions/{created.SessionId}";
        var initial = await client.GetFromJsonAsync<JsonElement>(route);
        Assert.Equal(5, initial.GetProperty("nextDecisionLaps").GetInt32());
        Assert.All(initial.GetProperty("classification").EnumerateArray(), row => {
            Assert.Empty(row.GetProperty("recentLapTimes").EnumerateArray());
            Assert.Equal(0, row.GetProperty("completedPitStops").GetInt32());
            Assert.False(row.GetProperty("isFastestLap").GetBoolean());
        });
        for (var phase = 0; phase < 2; phase++)
        {
            var state = await client.GetFromJsonAsync<JsonElement>(route);
            (await client.PostAsJsonAsync(route + "/plans", new TeamCommand(Guid.NewGuid().ToString(), phase, state.GetProperty("revision").GetInt64(),
                [new("driver-01", "Standard", phase == 0 ? "Pit" : "Stay Out", phase == 0 ? "Hard" : null, "Neutral"), new("driver-02", "Standard", "Stay Out", null, "Neutral")]))).EnsureSuccessStatusCode();
        }
        var final = await client.GetFromJsonAsync<JsonElement>(route);
        Assert.Equal(0, final.GetProperty("nextDecisionLaps").GetInt32());
        var rows = final.GetProperty("classification").EnumerateArray().ToArray();
        Assert.Single(rows, row => row.GetProperty("isFastestLap").GetBoolean());
        Assert.Equal(1, rows.Single(row => row.GetProperty("driverId").GetString() == "driver-01").GetProperty("completedPitStops").GetInt32());
        Assert.All(rows, row => {
            var laps = row.GetProperty("recentLapTimes").EnumerateArray().ToArray();
            Assert.Equal(5, laps.Length);
            Assert.Equal(row.GetProperty("completedLaps").GetInt32(), laps[^1].GetProperty("lap").GetInt32());
            Assert.All(laps, lap => Assert.True(lap.GetProperty("seconds").GetDouble() > 0));
        });
    }
    private sealed class EqualTimingPresets : IRacePresets
    {
        public RacePreset Get(string name, int laps)
        {
            var preset = new DryRacePresets().Get(name, laps);
            return preset with { Configuration = preset.Configuration with { LapVariation = 0, StopVariation = 0, RaceFormVariation = 0, Traffic = new(Enabled: false),
                    // Exact timing ties must survive legitimate AI pace/compound choices.
                    Tyres = new(DegradationPerLap:0,WarmupPenalty:0,DegradationPenalty:0,ExhaustionPenalty:0,PushGain:0,ConserveLoss:0,SoftGain:0,HardLoss:0) },
                Drivers = preset.Drivers.Select(driver => driver with { Gap = 0 }).ToArray() };
        }
    }
    [Fact]
    public async Task Strict_fastest_lap_ties_choose_the_ordinal_driver_without_fabricating_initial_times()
    {
        await using var server = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(new Dictionary<string,string?>{{"Designer:Key","tie-fixture-evidence"}}));
                builder.ConfigureServices(services => { services.RemoveAll<IRacePresets>(); services.AddSingleton<IRacePresets, EqualTimingPresets>(); });
            });
        using var client = server.CreateClient();
        var created = (await (await client.PostAsJsonAsync("/api/sessions", new CreateSessionInput("open-equal", 6, 123))).Content.ReadFromJsonAsync<SessionCreated>())!;
        client.DefaultRequestHeaders.Authorization = new("Bearer", created.ControllerCapability);
        (await client.PostAsJsonAsync($"/api/sessions/{created.SessionId}/plans", new TeamCommand(Guid.NewGuid().ToString(), 0, 1,
            [new("driver-01", "Standard", "Stay Out", null, "Neutral"), new("driver-02", "Standard", "Stay Out", null, "Neutral")]))).EnsureSuccessStatusCode();
        var state = (await client.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{created.SessionId}"))!;
        Assert.Equal(1, state.NextDecisionLaps);
        Assert.Equal("driver-01", Assert.Single(state.Classification, row => row.IsFastestLap).DriverId);
        client.DefaultRequestHeaders.Add("X-Designer-Key","tie-fixture-evidence");
        var record=BrowserReplay.Read(await client.GetFromJsonAsync<JsonElement>($"/api/designer/sessions/{created.SessionId}/record"));
        var engine = new RaceEngine(record.Replay.Configuration,record.Replay.Drivers,record.Replay.Seed);
        var result=engine.ResolvePhase(record.Replay.Plan.Scheduled.Where(entry=>entry.Phase==0).ToDictionary(entry=>entry.DriverId,entry=>entry.Decision));
        var completed = result.Events.Where(item => item.DriverId == "driver-01" && item.Kind == RaceEventKind.LapCompleted).ToArray();
        Assert.Equal(completed.Select(lap=>lap.Time),result.Events.Where(item=>item.DriverId=="driver-02"&&item.Kind==RaceEventKind.LapCompleted).Select(lap=>lap.Time));
        Assert.Equal(state.Classification.Single(row=>row.DriverId=="driver-01").RecentLapTimes,state.Classification.Single(row=>row.DriverId=="driver-02").RecentLapTimes);
        var previous = completed[0].Time;
        var expected = completed.Skip(1).Select(item => { var duration = item.Time - previous; previous = item.Time; return new PublicLapTime(item.Lap, duration); }).ToArray();
        Assert.Equal(expected, state.Classification.Single(row => row.DriverId == "driver-01").RecentLapTimes);
        Assert.All(state.Classification, row => {
            Assert.Equal(row.CompletedLaps-1, row.RecentLapTimes.Count);
            Assert.Equal(2, row.RecentLapTimes[0].Lap);
            Assert.DoesNotContain(row.RecentLapTimes, lap => lap.Lap == 1);
        });
    }}
