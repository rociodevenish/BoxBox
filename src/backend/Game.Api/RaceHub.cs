using Game.Application;
using Microsoft.AspNetCore.SignalR;
namespace Game.Api;
public sealed class RaceHub(SessionCoordinator sessions,FirebaseIdentity auth) : Hub
{
    public async Task<JoinedLobby> SubscribeLobby(string id,string deviceId)
    {
        try {
            var request=Context.GetHttpContext()!.Request;
            var identity=await auth.Verify(LobbyEndpoints.Token(request)??request.Query["access_token"].FirstOrDefault());
            var membership=sessions.LobbyMembership(id,identity,deviceId);
            await Groups.AddToGroupAsync(Context.ConnectionId,"lobby:"+id);
            return new(sessions.LobbyPublic(id),membership);
        } catch(SessionFault fault){throw new HubException(fault.Message);}
    }
    public async Task<PublicSnapshot> SubscribePublic(string id)
    {
        try { _ = sessions.Public(id); await Groups.AddToGroupAsync(Context.ConnectionId, "public:" + id); return sessions.Public(id); }
        catch (SessionFault fault) { throw new HubException(fault.Message); }
    }
    public async Task<TeamSnapshot> SubscribeTeam(string id, string capability)
    {
        try { _ = sessions.Team(id, capability); await Groups.AddToGroupAsync(Context.ConnectionId, "team:" + id); return sessions.Team(id, capability); }
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
