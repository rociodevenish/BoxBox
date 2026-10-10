using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;
using Game.Application;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Game.Api.Tests;

public sealed class RecoveryTests
{
    [Fact]
    public async Task Concurrent_identical_commands_and_failed_network_notifications_preserve_one_result_and_acknowledgement()
    {
        await using var server = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.Replace(ServiceDescriptor.Singleton<ISessionNotifications, UnavailableNotifications>())));
        using var client = server.CreateClient();
        var session = await Create(client);
        var command = Plan();
        var responses = await Task.WhenAll(
            client.PostAsJsonAsync($"/api/sessions/{session.SessionId}/plans", command),
            client.PostAsJsonAsync($"/api/sessions/{session.SessionId}/plans", command));
        foreach (var response in responses) response.EnsureSuccessStatusCode();
        Assert.Equal(await responses[0].Content.ReadAsStringAsync(), await responses[1].Content.ReadAsStringAsync());
        var snapshot = (await client.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{session.SessionId}"))!;
        Assert.Equal(4, snapshot.Revision);
        Assert.Equal(5, snapshot.LeaderLap);
        var recovered = (await client.GetFromJsonAsync<TeamSnapshot>($"/api/sessions/{session.SessionId}/team"))!;
        Assert.Equal("awaiting-decisions", recovered.Lifecycle);
        Assert.Null(recovered.ConfirmedPlan); Assert.Equal(1,recovered.Phase);
    }
    // External realtime delivery is deliberately unavailable; physics and HTTP authority remain real.
    private sealed class UnavailableNotifications : ISessionNotifications
    {
        public Task Publish(PublicSnapshot display, TeamSnapshot team) => throw new IOException("Network delivery unavailable");
        public Task PublishHousehold(PublicSnapshot display, IReadOnlyList<TeamSnapshot> teams) => throw new IOException("Network delivery unavailable");
    }
    [Theory]
    [InlineData("{\"cars\":[null,null]}", HttpStatusCode.BadRequest)]
    [InlineData("{\"commandId\":\"9719c94c-c479-461a-a1a3-37f05f3a7b45\",\"phase\":0,\"revision\":1,\"cars\":[{\"driverId\":\"driver-03\"},{\"driverId\":\"driver-04\"}]}", HttpStatusCode.BadRequest)]
    [InlineData("{\"commandId\":\"9719c94c-c479-461a-a1a3-37f05f3a7b45\",\"phase\":0,\"revision\":1,\"cars\":[{\"driverId\":\"driver-01\",\"pace\":\"Standard\",\"pit\":\"Stay Out\",\"battle\":\"Attack\"},{\"driverId\":\"driver-02\",\"pace\":\"Standard\",\"pit\":\"Stay Out\",\"battle\":\"Neutral\"}]}", HttpStatusCode.BadRequest)]
    [InlineData("{\"commandId\":\"9719c94c-c479-461a-a1a3-37f05f3a7b45\",\"phase\":1,\"revision\":0,\"cars\":[{\"driverId\":\"driver-01\"},{\"driverId\":\"driver-02\"}]}", HttpStatusCode.Conflict)]
    public async Task Invalid_commands_leave_the_entire_decision_window_unchanged(string json, HttpStatusCode status)
    {
        await using var server = new WebApplicationFactory<Program>();
        using var client = server.CreateClient();
        var session = await Create(client);
        var before = await client.GetStringAsync($"/api/sessions/{session.SessionId}");
        using var body = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        Assert.Equal(status, (await client.PostAsync($"/api/sessions/{session.SessionId}/plans", body)).StatusCode);
        Assert.Equal(before, await client.GetStringAsync($"/api/sessions/{session.SessionId}"));
        var own = (await client.GetFromJsonAsync<TeamSnapshot>($"/api/sessions/{session.SessionId}/team"))!;
        Assert.Null(own.ConfirmedPlan);
        Assert.Equal("awaiting-decisions", own.Lifecycle);
    }
    [Fact]
    public async Task Fresh_private_subscription_recovers_the_next_window_and_concurrent_commands_resolve_once()
    {
        await using var server = new WebApplicationFactory<Program>();
        using var client = server.CreateClient();
        var session = await Create(client);
        var command = Plan();
        var competing = command with { CommandId = Guid.NewGuid().ToString() };
        var attempts = await Task.WhenAll(
            client.PostAsJsonAsync($"/api/sessions/{session.SessionId}/plans", command),
            client.PostAsJsonAsync($"/api/sessions/{session.SessionId}/plans", competing));
        Assert.Single(attempts, response => response.IsSuccessStatusCode);
        Assert.Single(attempts, response => response.StatusCode == HttpStatusCode.Conflict);
        var publicState = (await client.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{session.SessionId}"))!;
        Assert.Equal(4, publicState.Revision);
        Assert.Equal(5, publicState.LeaderLap);
        await using var connection = new Microsoft.AspNetCore.SignalR.Client.HubConnectionBuilder()
            .WithUrl(new Uri(server.Server.BaseAddress, "/hubs/race"), options =>
            { options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling; options.HttpMessageHandlerFactory = _ => server.Server.CreateHandler(); }).Build();
        await connection.StartAsync();
        var recovered = await connection.InvokeAsync<System.Text.Json.JsonElement>("SubscribeTeam", session.SessionId, session.ControllerCapability);
        Assert.Equal("awaiting-decisions", recovered.GetProperty("lifecycle").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null,recovered.GetProperty("confirmedPlan").ValueKind); Assert.Equal(1,recovered.GetProperty("phase").GetInt32());
    }
    [Fact]
    public async Task Accepted_command_retries_return_original_acknowledgement_without_reopening_or_advancing()
    {
        await using var server = new WebApplicationFactory<Program>();
        using var client = server.CreateClient();
        var session = await Create(client);
        var command = Plan();
        var first = await client.PostAsJsonAsync($"/api/sessions/{session.SessionId}/plans", command);
        first.EnsureSuccessStatusCode();
        var result = await client.GetStringAsync($"/api/sessions/{session.SessionId}");
        var repeated = await client.PostAsJsonAsync($"/api/sessions/{session.SessionId}/plans", command);
        repeated.EnsureSuccessStatusCode();
        Assert.Equal(await first.Content.ReadAsStringAsync(), await repeated.Content.ReadAsStringAsync());
        Assert.Equal(result, await client.GetStringAsync($"/api/sessions/{session.SessionId}"));
        var changed = command with { Cars = [command.Cars[0] with { Pace = "Conserve" }, command.Cars[1]] };
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/sessions/{session.SessionId}/plans", changed)).StatusCode);
    }

    private static async Task<SessionCreated> Create(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/sessions", new CreateSessionInput("open-equal", 6, 123));
        response.EnsureSuccessStatusCode();
        var session = (await response.Content.ReadFromJsonAsync<SessionCreated>())!;
        client.DefaultRequestHeaders.Authorization = new("Bearer", session.ControllerCapability);
        return session;
    }
    private static TeamCommand Plan() => new(Guid.NewGuid().ToString(), 0, 1,
        [new("driver-01", "Push", "Stay Out", null, "Neutral"), new("driver-02", "Standard", "Pit", "Hard", "Neutral")]);
}
