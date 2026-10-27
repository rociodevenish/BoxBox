using Game.Application;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Game.Api.Tests;

public sealed class IntegratedScreensTests
{
    [Fact]
    public async Task Running_public_ahead_gap_uses_recorded_timing_and_second_place_has_the_same_reference_as_leader_gap()
    {
        await using var server = new WebApplicationFactory<Program>();
        using var client = server.CreateClient();
        var created = (await (await client.PostAsJsonAsync("/api/sessions", new CreateSessionInput("open-equal", 6, 123))).Content.ReadFromJsonAsync<SessionCreated>())!;
        client.DefaultRequestHeaders.Authorization = new("Bearer", created.ControllerCapability);
        var command = new TeamCommand(Guid.NewGuid().ToString(), 0, 1,
            [new("driver-01", "Standard", "Stay Out", null, "Neutral"), new("driver-02", "Standard", "Stay Out", null, "Neutral")]);
        (await client.PostAsJsonAsync($"/api/sessions/{created.SessionId}/plans", command)).EnsureSuccessStatusCode();
        var state = (await client.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{created.SessionId}"))!;
        var second = state.Classification[1];
        Assert.Equal("time", second.GapToAhead.Kind);
        Assert.Equal(second.GapToLeader.Seconds, second.GapToAhead.Seconds);
        Assert.All(state.Classification.Skip(1), car => Assert.Equal("time", car.GapToAhead.Kind));
        Assert.All(state.Classification.Skip(1), car => Assert.True(car.GapToAhead.Seconds >= 0));
    }
    [Fact]
    public async Task Mixed_initial_classification_uses_known_rolling_order_and_delays_not_future_lap_pace()
    {
        await using var server = new WebApplicationFactory<Program>();
        using var client = server.CreateClient();
        var created = (await (await client.PostAsJsonAsync("/api/sessions", new CreateSessionInput("open-mixed", 6, 123))).Content.ReadFromJsonAsync<SessionCreated>())!;
        var state = (await client.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{created.SessionId}"))!;
        Assert.Equal(Enumerable.Range(1, 22).Select(number => $"driver-{number:00}"), state.Classification.Select(car => car.DriverId));
        Assert.Equal("leader", state.Classification[0].GapToLeader.Kind);
        Assert.All(state.Classification.Skip(1), car => Assert.Equal(.5, car.GapToAhead.Seconds));
    }
}
