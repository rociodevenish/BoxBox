using Game.Api;
using Game.Application;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
builder.Services.AddSignalR();
builder.Services.AddSingleton<FirebaseIdentity>();
builder.Services.AddSingleton<HouseholdSubscriptions>();
builder.Services.AddSingleton<ISessionNotifications, SessionNotifications>();
builder.Services.AddSingleton<IRacePresets,DryRacePresets>();
builder.Services.AddSingleton<SessionCoordinator>();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var app = builder.Build();
app.Use(async (context, next) =>
{
    // Private observations and controller authority must never enter browser/offline caches.
    if(context.Request.Path.StartsWithSegments("/api/lobbies") || context.Request.Path.StartsWithSegments("/hubs/race") || context.Request.Path.Value?.EndsWith("/team",StringComparison.Ordinal)==true)
        context.Response.Headers.CacheControl="no-store";
    try { await next(context); }
    catch (SessionFault fault)
    {
        context.Response.StatusCode = fault.Failure switch
        { SessionFailure.Forbidden => 403, SessionFailure.Unavailable => 404, SessionFailure.Conflict => 409, _ => 400 };
        await context.Response.WriteAsJsonAsync(new { code = fault.Failure.ToString(), message = fault.Message });
    }
});
app.MapLobbyEndpoints();
app.MapPost("/api/sessions", (CreateSessionInput input, SessionCoordinator sessions) => sessions.Create(input));
app.MapGet("/api/sessions/{id}", (string id, SessionCoordinator sessions) => sessions.Public(id));
app.MapGet("/api/sessions/{id}/team", (string id, HttpRequest request, SessionCoordinator sessions) => sessions.Team(id, Capability(request)));
app.MapPost("/api/sessions/{id}/plans", (string id, TeamCommand command, HttpRequest request, SessionCoordinator sessions) => sessions.Submit(id, Capability(request), command));
app.MapPost("/api/sessions/{id}/restart", (string id, HttpRequest request, SessionCoordinator sessions) => sessions.Restart(id, Capability(request)));
app.MapPost("/api/sessions/{id}/presentation-timing", (string id, PresentationTimingInput input, SessionCoordinator sessions) =>
    Results.Ok(new { recorded = sessions.RecordPresentationTiming(id, input) }));
app.MapGet("/api/designer/sessions/{id}/record", (string id, HttpContext context, IConfiguration configuration, SessionCoordinator sessions) =>
{
    RequireDesigner(context, configuration);
    return sessions.DesignerRecord(id);
});
app.MapPost("/api/designer/replay/verify", (JsonElement input, HttpContext context, IConfiguration configuration) =>
{
    RequireDesigner(context, configuration);
    try { return Results.Ok(BrowserReplay.Verify(BrowserReplay.Read(input))); }
    catch (Exception error) when (error is ArgumentException or JsonException)
    { return Results.BadRequest(new { code = "InvalidReplay", message = error.Message }); }
});
app.MapHub<RaceHub>("/hubs/race");
app.MapGet("/health", () => Results.Ok(new { status = "ready" }));
app.Run();
static string? Capability(HttpRequest request) => request.Headers.Authorization.FirstOrDefault() is { } value && value.StartsWith("Bearer ", StringComparison.Ordinal) ? value[7..] : null;
static void RequireDesigner(HttpContext context, IConfiguration configuration)
{
    var expected = configuration["Designer:Key"];
    var supplied = context.Request.Headers["X-Designer-Key"].FirstOrDefault();
    var remote = context.Connection.RemoteIpAddress;
    // TestServer has no remote socket. Real diagnostics require a local request and independent configured key.
    if (string.IsNullOrWhiteSpace(expected) || supplied is null || remote is not null && !System.Net.IPAddress.IsLoopback(remote)
        || !CryptographicOperations.FixedTimeEquals(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(expected)),
            SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(supplied))))
        throw new SessionFault(SessionFailure.Forbidden, "Designer diagnostics are disabled or not authorized on this host.");
}
public partial class Program;
