using Game.Application;
using Microsoft.AspNetCore.SignalR;
namespace Game.Api;

// Call only after the authoritative mutation has persisted successfully.
internal static class PreparationNotifications
{
    public static async Task PublishControllerChange(IHubContext<RaceHub> hub,SessionCoordinator sessions,
        string id,AuthIdentity identity,string deviceId,string teamId)
    {
        await PublishTeam(hub,sessions,sessions.HouseholdPreparation(id,identity,deviceId,teamId));
        await hub.Clients.Group("preparation:"+id).SendAsync("preparationSnapshot",sessions.PreparationPublic(id));
    }

    public static async Task PublishTeam(IHubContext<RaceHub> hub,SessionCoordinator sessions,PreparationTeamSnapshot team)
    {
        await hub.Clients.Group($"preparation-household:{team.SessionId}:{team.TeamId}:{team.AssignmentRevision}")
            .SendAsync("preparationTeamSnapshot",team);
        try
        {
            var open=sessions.OpenPreparation(team.SessionId,team.TeamId);
            await hub.Clients.Group($"preparation-open:{team.SessionId}:{team.TeamId}:{open.AssignmentRevision}")
                .SendAsync("preparationTeamSnapshot",open);
        }
        catch(SessionFault fault) when(fault.Failure==SessionFailure.Forbidden) { }
    }
}
