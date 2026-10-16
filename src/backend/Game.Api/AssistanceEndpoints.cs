using Game.Application;
using Microsoft.AspNetCore.SignalR;
namespace Game.Api;
public static class AssistanceEndpoints
{
    public static void MapAssistanceEndpoints(this WebApplication app)=>app.MapPost("/api/lobbies/{id}/players/{playerId}/assistance",
        async(string id,string playerId,PlayerAssistanceInput input,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions,IHubContext<RaceHub> hub)=>
        {
            var identity=await auth.Verify(LobbyEndpoints.Token(request));
            var state=await sessions.SetPlayerAssistance(id,identity,playerId,input);
            try{await hub.Clients.Group("lobby:"+id).SendAsync("lobbySnapshot",state);}catch(Exception error){app.Logger.LogWarning(error,"Assistance setting notification failed; refresh the saved lobby.");}
            try
            {
                foreach(var team in sessions.PreparationControllerUpdates(id,identity))
                    await PreparationNotifications.PublishTeam(hub,sessions,team);
            }
            catch(SessionFault fault) when(fault.Failure==SessionFailure.Conflict) { }
            catch(Exception error){app.Logger.LogWarning(error,"Preparation guidance notification failed; recover the saved snapshot.");}
            return state;
        });
}
