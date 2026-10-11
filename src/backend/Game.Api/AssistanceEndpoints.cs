using Game.Application;
using Microsoft.AspNetCore.SignalR;
namespace Game.Api;
public static class AssistanceEndpoints
{
    public static void MapAssistanceEndpoints(this WebApplication app)=>app.MapPost("/api/lobbies/{id}/players/{playerId}/assistance",
        async(string id,string playerId,PlayerAssistanceInput input,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions,IHubContext<RaceHub> hub)=>
        {
            var state=await sessions.SetPlayerAssistance(id,await auth.Verify(LobbyEndpoints.Token(request)),playerId,input);
            try{await hub.Clients.Group("lobby:"+id).SendAsync("lobbySnapshot",state);}catch(Exception error){app.Logger.LogWarning(error,"Assistance setting notification failed; refresh the saved lobby.");}
            return state;
        });
}
