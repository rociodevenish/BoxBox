using Game.Application;
using Microsoft.AspNetCore.SignalR;
namespace Game.Api;

public static class PreparationEndpoints
{
    public static void MapPreparationEndpoints(this WebApplication app)
    {
        app.MapPost("/api/lobbies/{id}/preparation",async(string id,BeginPreparationInput input,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions,IHubContext<RaceHub> hub)=>
        {
            var snapshot=sessions.BeginPreparation(id,await auth.Verify(LobbyEndpoints.Token(request)),input);
            try { await hub.Clients.Group("preparation:"+id).SendAsync("preparationSnapshot",snapshot);
                await hub.Clients.Group("lobby:"+id).SendAsync("lobbySnapshot",sessions.LobbyPublic(id)); }
            catch(Exception error) { app.Logger.LogWarning(error,"Preparation notification failed; recover the saved snapshot."); }
            return snapshot;
        });
        app.MapGet("/api/lobbies/{id}/preparation",(string id,SessionCoordinator sessions)=>sessions.PreparationPublic(id));
        app.MapGet("/api/lobbies/{id}/preparation/teams/{teamId}",async(string id,string teamId,string deviceId,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions)=>
            sessions.HouseholdPreparation(id,await auth.Verify(LobbyEndpoints.Token(request)),deviceId,teamId));
        app.MapGet("/api/lobbies/{id}/preparation/open/{teamId}",(string id,string teamId,SessionCoordinator sessions)=>sessions.OpenPreparation(id,teamId));
        app.MapPost("/api/lobbies/{id}/preparation/teams/{teamId}/setup",async(string id,string teamId,string deviceId,SetupCommand input,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions,IHubContext<RaceHub> hub)=>
        {
            var identity=await auth.Verify(LobbyEndpoints.Token(request));
            var ack=sessions.SaveSetup(id,identity,deviceId,teamId,input);
            try
            {
                var team=sessions.HouseholdPreparation(id,identity,deviceId,teamId);
                await hub.Clients.Group($"preparation-household:{id}:{teamId}:{team.AssignmentRevision}").SendAsync("preparationTeamSnapshot",team);
                try { var open=sessions.OpenPreparation(id,teamId);
                    await hub.Clients.Group($"preparation-open:{id}:{teamId}:{open.AssignmentRevision}").SendAsync("preparationTeamSnapshot",open); }
                catch(SessionFault fault) when(fault.Failure==SessionFailure.Forbidden) { }
                await hub.Clients.Group("preparation:"+id).SendAsync("preparationSnapshot",sessions.PreparationPublic(id));
            }
            catch(Exception error) { app.Logger.LogWarning(error,"Setup notification failed; recover the saved snapshot."); }
            return ack;
        });
        app.MapPost("/api/lobbies/{id}/preparation/teams/{teamId}/practice",async(string id,string teamId,string deviceId,PracticeCommand input,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions,IHubContext<RaceHub> hub)=>
        {
            var identity=await auth.Verify(LobbyEndpoints.Token(request));
            var ack=sessions.ConfirmPractice(id,identity,deviceId,teamId,input);
            try
            {
                var team=sessions.HouseholdPreparation(id,identity,deviceId,teamId);
                await hub.Clients.Group($"preparation-household:{id}:{teamId}:{team.AssignmentRevision}").SendAsync("preparationTeamSnapshot",team);
                try { var open=sessions.OpenPreparation(id,teamId);
                    await hub.Clients.Group($"preparation-open:{id}:{teamId}:{open.AssignmentRevision}").SendAsync("preparationTeamSnapshot",open); }
                catch(SessionFault fault) when(fault.Failure==SessionFailure.Forbidden) { }
                await hub.Clients.Group("preparation:"+id).SendAsync("preparationSnapshot",sessions.PreparationPublic(id));
            }
            catch(Exception error) { app.Logger.LogWarning(error,"Practice notification failed; recover the saved snapshot."); }
            return ack;
        });
    }
}
