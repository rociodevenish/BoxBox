using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
namespace Game.Api.Tests;
public sealed class CompleteRaceTests
{
    [Fact]
    public async Task Authorised_continue_opens_one_new_window_without_advancing_time_and_a_short_race_finishes()
    {
        await using var server = new WebApplicationFactory<Program>();
        using var client = server.CreateClient();
        var created = await (await client.PostAsJsonAsync("/api/sessions", new { preset = "open-equal", laps = 6, seed = 123 })).Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("sessionId").GetString();
        var route = $"/api/sessions/{id}";
        client.DefaultRequestHeaders.Authorization = new("Bearer", created.GetProperty("controllerCapability").GetString());
        var first = new { commandId = Guid.NewGuid().ToString(), phase = 0, revision = 1, cars = Cars(true) };
        (await client.PostAsJsonAsync(route + "/plans", first)).EnsureSuccessStatusCode();
        var result = await client.GetFromJsonAsync<JsonElement>(route);
        var time = result.GetProperty("raceTime").GetDouble();
        var next = new { commandId = Guid.NewGuid().ToString(), phase = 0, revision = result.GetProperty("revision").GetInt64() };
        var continued = await client.PostAsJsonAsync(route + "/continue", next);
        continued.EnsureSuccessStatusCode();
        var ack = await continued.Content.ReadFromJsonAsync<JsonElement>();
        var duplicate = await (await client.PostAsJsonAsync(route + "/continue", next)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ack.GetRawText(), duplicate.GetRawText());
        var open = await client.GetFromJsonAsync<JsonElement>(route);
        Assert.Equal(1, open.GetProperty("phase").GetInt32());
        Assert.Equal("awaiting-decisions", open.GetProperty("lifecycle").GetString());
        Assert.Equal(time, open.GetProperty("raceTime").GetDouble());
        var own = await client.GetFromJsonAsync<JsonElement>(route + "/team");
        Assert.Equal(JsonValueKind.Null, own.GetProperty("confirmedPlan").ValueKind);
        (await client.PostAsJsonAsync(route + "/plans", new { commandId = Guid.NewGuid().ToString(), phase = 1, revision = open.GetProperty("revision").GetInt64(), cars = Cars(false) })).EnsureSuccessStatusCode();
        var finish = await client.GetFromJsonAsync<JsonElement>(route);
        Assert.Equal("finished", finish.GetProperty("lifecycle").GetString());
        Assert.Equal(6, finish.GetProperty("leaderLap").GetInt32());
        var rows = finish.GetProperty("classification").EnumerateArray().ToArray();
        Assert.False(rows.Single(row => row.GetProperty("driverId").GetString() == "driver-01").GetProperty("eligibleAtFinish").GetBoolean());
        Assert.True(rows.Single(row => row.GetProperty("driverId").GetString() == "driver-02").GetProperty("eligibleAtFinish").GetBoolean());
        Assert.All(rows, row => Assert.InRange(row.GetProperty("completedLaps").GetInt32(), 1, 6));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(route + "/continue", new { commandId = Guid.NewGuid().ToString(), phase = 1, revision = finish.GetProperty("revision").GetInt64() })).StatusCode);
    }
    private static object[] Cars(bool stop) => [
        new { driverId = "driver-01", pace = "Standard", pit = "Stay Out", compound = (string?)null, battle = "Neutral" },
        new { driverId = "driver-02", pace = "Standard", pit = stop ? "Pit" : "Stay Out", compound = stop ? "Hard" : (string?)null, battle = "Neutral" }
    ];
    [Fact]
    public async Task Each_result_contains_only_bounded_new_events_with_opaque_stable_identity_and_public_counts()
    {
        await using var server = new WebApplicationFactory<Program>(); using var client = server.CreateClient();
        var created = await (await client.PostAsJsonAsync("/api/sessions", new { preset = "open-equal", laps = 6, seed = 123 })).Content.ReadFromJsonAsync<JsonElement>();
        var route = "/api/sessions/" + created.GetProperty("sessionId").GetString();
        client.DefaultRequestHeaders.Authorization = new("Bearer", created.GetProperty("controllerCapability").GetString());
        (await client.PostAsJsonAsync(route + "/plans", new { commandId = Guid.NewGuid().ToString(), phase = 0, revision = 1, cars = Cars(true) })).EnsureSuccessStatusCode();
        var first = await client.GetFromJsonAsync<JsonElement>(route);
        Assert.Equal("ready-next", first.GetProperty("lifecycle").GetString());
        var presentation = first.GetProperty("presentation");
        Assert.True(Guid.TryParse(presentation.GetProperty("cursor").GetString(), out _));
        Assert.Equal(1, presentation.GetProperty("pitEntries").GetInt32());
        Assert.Equal(1, presentation.GetProperty("pitExits").GetInt32());
        Assert.Equal(1, presentation.GetProperty("tyreChanges").GetInt32());
        var ids = first.GetProperty("events").EnumerateArray().Select(item => item.GetProperty("id").GetString()).ToArray();
        Assert.All(ids, value => Assert.True(Guid.TryParse(value, out _)));
        var again = await client.GetFromJsonAsync<JsonElement>(route);
        Assert.Equal(first.GetProperty("events").GetRawText(), again.GetProperty("events").GetRawText());
        using var stranger = server.CreateClient();
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.PostAsJsonAsync(route + "/continue", new { commandId = Guid.NewGuid().ToString(), phase = 0, revision = first.GetProperty("revision").GetInt64() })).StatusCode);
        (await client.PostAsJsonAsync(route + "/continue", new { commandId = Guid.NewGuid().ToString(), phase = 0, revision = first.GetProperty("revision").GetInt64() })).EnsureSuccessStatusCode();
        var next = await client.GetFromJsonAsync<JsonElement>(route);
        (await client.PostAsJsonAsync(route + "/plans", new { commandId = Guid.NewGuid().ToString(), phase = 1, revision = next.GetProperty("revision").GetInt64(), cars = Cars(false) })).EnsureSuccessStatusCode();
        var finish = await client.GetFromJsonAsync<JsonElement>(route);
        var events = finish.GetProperty("events").EnumerateArray().ToArray();
        Assert.InRange(events.Length, 1, 12);
        Assert.All(events, item => {
            Assert.DoesNotContain(item.GetProperty("id").GetString(), ids);
            Assert.True(item.GetProperty("time").GetDouble() >= first.GetProperty("raceTime").GetDouble());
            Assert.Equal(new[] { "id", "kind", "time", "lap", "driverId", "otherDriverId", "compound" }, item.EnumerateObject().Select(field => field.Name));
        });
        Assert.Equal(new[] { "cursor", "totalEvents", "overtakes", "pitEntries", "pitExits", "tyreChanges", "omittedEvents", "summary" }, finish.GetProperty("presentation").EnumerateObject().Select(field => field.Name));
        Assert.NotEqual(presentation.GetProperty("cursor").GetString(), finish.GetProperty("presentation").GetProperty("cursor").GetString());
        Assert.True(finish.GetProperty("presentation").GetProperty("omittedEvents").GetInt32() > 0);
    }
}
