using Game.Application;
namespace Game.Api;
public static class ControlEndpoints
{
    public static void MapControlEndpoints(this WebApplication app)
    {
        app.MapGet("/api/lobbies/{id}/control",(string id,SessionCoordinator sessions)=>sessions.ControlPublic(id));
        app.MapPost("/api/lobbies/{id}/teams/{teamId}/control",async(string id,string teamId,ControlRequest input,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions)=>await sessions.RequestTakeover(id,await auth.Verify(LobbyEndpoints.Token(request)),teamId,input));
        app.MapPost("/api/lobbies/{id}/teams/{teamId}/reclaim",async(string id,string teamId,string deviceId,ControlRequest input,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions)=>await sessions.RequestReclaim(id,await auth.Verify(LobbyEndpoints.Token(request)),deviceId,teamId,input));
    }
}
