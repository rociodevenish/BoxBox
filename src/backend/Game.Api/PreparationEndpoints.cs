using Game.Application;
using Microsoft.AspNetCore.SignalR;
namespace Game.Api;

public static class PreparationEndpoints
{
    public static void MapPreparationEndpoints(this WebApplication app)
    {
        foreach(var operation in new[]{"confirm","reopen"})
        {
            var reopen=operation=="reopen";
            app.MapPost("/api/lobbies/{id}/preparation/teams/{teamId}/"+operation,async(string id,string teamId,string deviceId,PracticeCommand input,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions,IHubContext<RaceHub> hub)=>
            {
                var identity=await auth.Verify(LobbyEndpoints.Token(request));
                var ack=reopen?sessions.ReopenPreparation(id,identity,deviceId,teamId,input):sessions.ConfirmPreparation(id,identity,deviceId,teamId,input);
                try
                {
                    await PreparationNotifications.PublishControllerChange(hub,sessions,id,identity,deviceId,teamId);
                }
                catch(Exception error){app.Logger.LogWarning(error,"Readiness notification failed; recover the saved snapshot.");}
                return ack;
            });
        }
        app.MapPost("/api/lobbies/{id}/preparation/freeze",async(string id,FreezePreparationCommand input,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions,IHubContext<RaceHub> hub)=>
        {
            var identity=await auth.Verify(LobbyEndpoints.Token(request));
            var ack=sessions.FreezePreparationForQualifying(id,identity,input);
            try
            {
                var snapshot=sessions.PreparationPublic(id);
                await hub.Clients.Group("preparation:"+id).SendAsync("preparationSnapshot",snapshot);
                foreach(var team in sessions.PreparationControllerUpdates(id,identity))
                    await PreparationNotifications.PublishTeam(hub,sessions,team);
            }
            catch(Exception error){app.Logger.LogWarning(error,"Freeze notification failed; recover the saved snapshot.");}
            return ack;
        });
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
                await PreparationNotifications.PublishControllerChange(hub,sessions,id,identity,deviceId,teamId);
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
                await PreparationNotifications.PublishControllerChange(hub,sessions,id,identity,deviceId,teamId);
            }
            catch(Exception error) { app.Logger.LogWarning(error,"Practice notification failed; recover the saved snapshot."); }
            return ack;
        });
    }
}
