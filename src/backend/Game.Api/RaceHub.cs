using Game.Application;
using Microsoft.AspNetCore.SignalR;
namespace Game.Api;
public sealed class RaceHub(SessionCoordinator sessions) : Hub
{
    public async Task<PublicSnapshot> SubscribePublic(string id)
    {
        try { var snapshot = sessions.Public(id); await Groups.AddToGroupAsync(Context.ConnectionId, "public:" + id); return snapshot; }
        catch (SessionFault fault) { throw new HubException(fault.Message); }
    }
    public async Task<TeamSnapshot> SubscribeTeam(string id, string capability)
    {
        try { var snapshot = sessions.Team(id, capability); await Groups.AddToGroupAsync(Context.ConnectionId, "team:" + id); return snapshot; }
        catch (SessionFault fault) { throw new HubException(fault.Message); }
    }
}
public sealed class SessionNotifications(IHubContext<RaceHub> hub, ILogger<SessionNotifications> logger) : ISessionNotifications
{
    public async Task Publish(PublicSnapshot display, TeamSnapshot team)
    {
        try { await Task.WhenAll(hub.Clients.Group("public:" + display.SessionId).SendAsync("publicSnapshot", display),
            hub.Clients.Group("team:" + team.SessionId).SendAsync("teamSnapshot", team)); }
        catch (Exception exception) { logger.LogWarning(exception, "Session notification failed; authoritative state remains available by snapshot."); }
    }
}
