using Game.Application;
using Microsoft.AspNetCore.SignalR;
namespace Game.Api;
public static class QualifyingEndpoints
{
    public static void MapQualifyingEndpoints(this WebApplication app)
    {
        app.MapPost("/api/lobbies/{id}/qualifying",async(string id,StartQualifyingCommand input,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions,IHubContext<RaceHub> hub)=>
        {
            var identity=await auth.Verify(LobbyEndpoints.Token(request));var ack=sessions.StartQualifying(id,identity,input);
            await sessions.PublishQualifying(id);
            try
            {
                await hub.Clients.Group("lobby:"+id).SendAsync("lobbySnapshot",sessions.LobbyPublic(id));
                await hub.Clients.Group("preparation:"+id).SendAsync("preparationSnapshot",sessions.PreparationPublic(id));
                foreach(var team in sessions.PreparationControllerUpdates(id,identity))await PreparationNotifications.PublishTeam(hub,sessions,team);
            }
            catch(Exception error){app.Logger.LogWarning(error,"Qualifying start notification failed; recover the saved state.");}
            return ack;
        });
        app.MapPost("/api/lobbies/{id}/qualifying/continue",async(string id,DueTransitionInput input,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions)=>
            await sessions.ContinueQualifying(id,await auth.Verify(LobbyEndpoints.Token(request)),input));
        app.MapGet("/api/lobbies/{id}/qualifying",(string id,SessionCoordinator sessions)=>sessions.QualifyingPublic(id));
        app.MapGet("/api/lobbies/{id}/qualifying/teams/{teamId}",async(string id,string teamId,string deviceId,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions)=>
            sessions.HouseholdQualifying(id,await auth.Verify(LobbyEndpoints.Token(request)),deviceId,teamId));
        app.MapGet("/api/lobbies/{id}/qualifying/open/{teamId}",(string id,string teamId,SessionCoordinator sessions)=>sessions.OpenQualifying(id,teamId));
        app.MapPost("/api/lobbies/{id}/qualifying/teams/{teamId}/choices",async(string id,string teamId,string deviceId,QualifyingCommand input,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions)=>
        {
            var ack=sessions.SubmitQualifying(id,await auth.Verify(LobbyEndpoints.Token(request)),deviceId,teamId,input);
            await sessions.PublishQualifying(id);return ack;
        });
    }
}
