using Game.Application;
using Microsoft.AspNetCore.SignalR;
namespace Game.Api;
public sealed class RaceHub(SessionCoordinator sessions,FirebaseIdentity auth,HouseholdSubscriptions subscriptions) : Hub
{
    public async Task<QualifyingPublicSnapshot> SubscribeQualifying(string id)
    {
        try{_=sessions.QualifyingPublic(id);await Groups.AddToGroupAsync(Context.ConnectionId,"qualifying:"+id);return sessions.QualifyingPublic(id);}
        catch(SessionFault fault){throw new HubException(fault.Message);}
    }
    public async Task<QualifyingTeamSnapshot> SubscribeHouseholdQualifying(string id,string deviceId,string teamId)
    {
        try
        {
            var request=Context.GetHttpContext()!.Request;
            var identity=await auth.Verify(LobbyEndpoints.Token(request)??request.Query["access_token"].FirstOrDefault());
            var own=sessions.HouseholdQualifying(id,identity,deviceId,teamId);var group=$"qualifying-household:{id}:{teamId}:{own.AssignmentRevision}";
            subscriptions.Add(Context.ConnectionId,id,teamId,own.AssignmentRevision);await Groups.AddToGroupAsync(Context.ConnectionId,group);
            try{var current=sessions.HouseholdQualifying(id,identity,deviceId,teamId);if(current.AssignmentRevision!=own.AssignmentRevision)throw new SessionFault(SessionFailure.Forbidden,"Assignment changed.");return current;}
            catch{await Groups.RemoveFromGroupAsync(Context.ConnectionId,group);throw;}
        }
        catch(SessionFault fault){throw new HubException(fault.Message);}
    }
    public async Task<QualifyingTeamSnapshot> SubscribeOpenQualifying(string id,string teamId)
    {
        try
        {
            var own=sessions.OpenQualifying(id,teamId);var group=$"qualifying-open:{id}:{teamId}:{own.AssignmentRevision}";
            await Groups.AddToGroupAsync(Context.ConnectionId,group);
            try{var current=sessions.OpenQualifying(id,teamId);if(current.AssignmentRevision!=own.AssignmentRevision)throw new SessionFault(SessionFailure.Forbidden,"Open assignment changed.");return current;}
            catch{await Groups.RemoveFromGroupAsync(Context.ConnectionId,group);throw;}
        }
        catch(SessionFault fault){throw new HubException(fault.Message);}
    }
    public Task UnsubscribeHouseholdQualifying(string id,string teamId,long revision)
    {subscriptions.Remove(Context.ConnectionId,id,teamId,revision);return Groups.RemoveFromGroupAsync(Context.ConnectionId,$"qualifying-household:{id}:{teamId}:{revision}");}
    public async Task<PreparationPublicSnapshot> SubscribePreparation(string id)
    {
        try { _=sessions.PreparationPublic(id); await Groups.AddToGroupAsync(Context.ConnectionId,"preparation:"+id); return sessions.PreparationPublic(id); }
        catch(SessionFault fault) { throw new HubException(fault.Message); }
    }
    public async Task<PreparationTeamSnapshot> SubscribeHouseholdPreparation(string id,string deviceId,string teamId)
    {
        try
        {
            var request=Context.GetHttpContext()!.Request;
            var identity=await auth.Verify(LobbyEndpoints.Token(request)??request.Query["access_token"].FirstOrDefault());
            var own=sessions.HouseholdPreparation(id,identity,deviceId,teamId);
            var group=$"preparation-household:{id}:{teamId}:{own.AssignmentRevision}";
            subscriptions.Add(Context.ConnectionId,id,teamId,own.AssignmentRevision);
            await Groups.AddToGroupAsync(Context.ConnectionId,group);
            try { var current=sessions.HouseholdPreparation(id,identity,deviceId,teamId);
                if(current.AssignmentRevision!=own.AssignmentRevision)throw new SessionFault(SessionFailure.Forbidden,"Controller assignment changed.");
                return current; }
            catch { await Groups.RemoveFromGroupAsync(Context.ConnectionId,group); throw; }
        }
        catch(SessionFault fault) { throw new HubException(fault.Message); }
    }
    public async Task<PreparationTeamSnapshot> SubscribeOpenPreparation(string id,string teamId)
    {
        try
        {
            var own=sessions.OpenPreparation(id,teamId);
            var group=$"preparation-open:{id}:{teamId}:{own.AssignmentRevision}";
            await Groups.AddToGroupAsync(Context.ConnectionId,group);
            try { var current=sessions.OpenPreparation(id,teamId);
                if(current.AssignmentRevision!=own.AssignmentRevision)throw new SessionFault(SessionFailure.Forbidden,"Open assignment changed."); return current; }
            catch { await Groups.RemoveFromGroupAsync(Context.ConnectionId,group); throw; }
        }
        catch(SessionFault fault) { throw new HubException(fault.Message); }
    }
    public async Task UnsubscribeHouseholdPreparation(string id,string teamId,long revision)
    { subscriptions.Remove(Context.ConnectionId,id,teamId,revision); await Groups.RemoveFromGroupAsync(Context.ConnectionId,$"preparation-household:{id}:{teamId}:{revision}"); }
    public override Task OnDisconnectedAsync(Exception? exception){subscriptions.RemoveConnection(Context.ConnectionId);return base.OnDisconnectedAsync(exception);}
    public async Task<OpenTeamSnapshot> SubscribeOpenTeam(string id,string teamId)
    {
        try {var own=sessions.OpenTeam(id,teamId);await Groups.AddToGroupAsync(Context.ConnectionId,"open:"+id+":"+teamId+":"+own.AssignmentRevision);return sessions.OpenTeam(id,teamId);}
        catch(SessionFault fault){throw new HubException(fault.Message);}
    }
    public Task UnsubscribeHouseholdTeam(string id,string teamId,long assignmentRevision)
        {subscriptions.Remove(Context.ConnectionId,id,teamId,assignmentRevision);return Groups.RemoveFromGroupAsync(Context.ConnectionId,"household:"+id+":"+teamId+":"+assignmentRevision);}
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
    public async Task<TeamSnapshot> SubscribeHouseholdTeam(string id,string deviceId,string teamId)
    {
        try {var request=Context.GetHttpContext()!.Request;var identity=await auth.Verify(LobbyEndpoints.Token(request)??request.Query["access_token"].FirstOrDefault());
            var own=sessions.HouseholdTeam(id,identity,deviceId,teamId);
            var group="household:"+id+":"+teamId+":"+own.AssignmentRevision;subscriptions.Add(Context.ConnectionId,id,teamId,own.AssignmentRevision);await Groups.AddToGroupAsync(Context.ConnectionId,group);try{var current=sessions.HouseholdTeam(id,identity,deviceId,teamId);if(current.AssignmentRevision!=own.AssignmentRevision)throw new SessionFault(SessionFailure.Forbidden,"Controller assignment changed. Refresh current authority.");return current;}catch{await Groups.RemoveFromGroupAsync(Context.ConnectionId,group);throw;}
        }catch(SessionFault fault){throw new HubException(fault.Message);}
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
    public async Task PublishQualifying(QualifyingPublicSnapshot display,IReadOnlyList<QualifyingTeamSnapshot> teams,IReadOnlyList<QualifyingTeamSnapshot> openTeams)
    {
        await hub.Clients.Group("qualifying:"+display.SessionId).SendAsync("qualifyingSnapshot",display);
        foreach(var team in teams)await hub.Clients.Group($"qualifying-household:{team.SessionId}:{team.TeamId}:{team.AssignmentRevision}").SendAsync("qualifyingTeamSnapshot",team);
        foreach(var team in openTeams)await hub.Clients.Group($"qualifying-open:{team.SessionId}:{team.TeamId}:{team.AssignmentRevision}").SendAsync("qualifyingTeamSnapshot",team);
    }
    public async Task PublishHousehold(PublicSnapshot display,IReadOnlyList<TeamSnapshot> teams,IReadOnlyList<OpenTeamSnapshot> openTeams)
    {
        await hub.Clients.Group("public:"+display.SessionId).SendAsync("publicSnapshot",display);
        foreach(var team in teams) await hub.Clients.Group("household:"+team.SessionId+":"+team.TeamId+":"+team.AssignmentRevision).SendAsync("teamSnapshot",team);
        foreach(var team in openTeams) await hub.Clients.Group("open:"+team.SessionId+":"+team.TeamId+":"+team.AssignmentRevision).SendAsync("openTeamSnapshot",team);
    }
    public async Task Publish(PublicSnapshot display, TeamSnapshot team)
    {
        try { await Task.WhenAll(hub.Clients.Group("public:" + display.SessionId).SendAsync("publicSnapshot", display),
            hub.Clients.Group("team:" + team.SessionId).SendAsync("teamSnapshot", team)); }
        catch (Exception exception) { logger.LogWarning(exception, "Session notification failed; authoritative state remains available by snapshot."); }
    }
}
