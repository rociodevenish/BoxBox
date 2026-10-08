using Game.Api;
using Game.Application;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddSingleton<ISessionNotifications, SessionNotifications>();
builder.Services.AddSingleton<SessionCoordinator>();
var app = builder.Build();
app.Use(async (context, next) =>
{
    try { await next(context); }
    catch (SessionFault fault)
    {
        context.Response.StatusCode = fault.Failure switch
        { SessionFailure.Forbidden => 403, SessionFailure.Unavailable => 404, SessionFailure.Conflict => 409, _ => 400 };
        await context.Response.WriteAsJsonAsync(new { code = fault.Failure.ToString(), message = fault.Message });
    }
});
app.MapPost("/api/sessions", (CreateSessionInput input, SessionCoordinator sessions) => sessions.Create(input));
app.MapGet("/api/sessions/{id}", (string id, SessionCoordinator sessions) => sessions.Public(id));
app.MapGet("/api/sessions/{id}/team", (string id, HttpRequest request, SessionCoordinator sessions) => sessions.Team(id, Capability(request)));
app.MapPost("/api/sessions/{id}/plans", (string id, TeamCommand command, HttpRequest request, SessionCoordinator sessions) => sessions.Submit(id, Capability(request), command));
app.MapPost("/api/sessions/{id}/continue", (string id, ContinueCommand command, HttpRequest request, SessionCoordinator sessions) => sessions.Continue(id, Capability(request), command));
app.MapPost("/api/sessions/{id}/restart", (string id, HttpRequest request, SessionCoordinator sessions) => sessions.Restart(id, Capability(request)));
app.MapHub<RaceHub>("/hubs/race");
app.MapGet("/health", () => Results.Ok(new { status = "ready" }));
app.Run();
static string? Capability(HttpRequest request) => request.Headers.Authorization.FirstOrDefault() is { } value && value.StartsWith("Bearer ", StringComparison.Ordinal) ? value[7..] : null;
public partial class Program;
