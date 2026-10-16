using Game.Application;
using Microsoft.AspNetCore.SignalR;
namespace Game.Api;
public static class LobbyEndpoints
{
 public static void MapLobbyEndpoints(this WebApplication app)
 {
  app.MapPost("/api/lobbies/{id}/continue",async(string id,DueTransitionInput input,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions)=>await sessions.ContinueHousehold(id,await auth.Verify(Token(request)),input));
  app.MapPost("/api/lobbies/{id}/advance-due",async(string id,DueTransitionInput input,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions)=>await sessions.AdvanceHouseholdDue(id,await auth.Verify(Token(request)),input));
  app.MapGet("/api/lobbies/{id}/controller-devices",async(string id,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions)=>sessions.ControllerDevices(id,await auth.Verify(Token(request))));
  app.MapPost("/api/lobbies/{id}/devices/{deviceId}/revoke",async(string id,string deviceId,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions,IHubContext<RaceHub> hub,HouseholdSubscriptions subscriptions)=>{var state=await sessions.RevokeDevice(id,await auth.Verify(Token(request)),deviceId);await subscriptions.CloseObsolete(hub.Groups,state);await Notify(hub,state,app.Logger);return state;});
  foreach(var action in new[]{"pause","resume"}){var paused=action=="pause";app.MapPost("/api/lobbies/{id}/"+action,async(string id,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions,IHubContext<RaceHub> hub)=>{var state=await sessions.SetHouseholdPaused(id,await auth.Verify(Token(request)),paused);await Notify(hub,state,app.Logger);if(sessions.HasQualifying(id))await sessions.PublishQualifying(id);return state;});}
  app.MapGet("/api/lobbies/{id}/controllers",async(string id,string deviceId,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions)=>sessions.HouseholdControllers(id,await auth.Verify(Token(request)),deviceId));
  app.MapGet("/api/auth/config",(IConfiguration config,IHostEnvironment environment)=>{
   var project=config["Firebase:ProjectId"];if(string.IsNullOrWhiteSpace(project))throw new SessionFault(SessionFailure.Unavailable,"Household sign-in is not configured on this server.");
   return new {projectId=project,apiKey=config["Firebase:ApiKey"]??"demo-local-key",emulatorUrl=environment.IsDevelopment()&&Environment.GetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST") is not null?config["Firebase:EmulatorUrl"]??"http://127.0.0.1:19099":null};
  });
  app.MapPost("/api/lobbies",async(CreateLobbyInput input,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions)=>sessions.CreateLobby(await auth.Verify(Token(request)),input));
  app.MapPost("/api/lobbies/join",async(JoinLobbyInput input,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions,IHubContext<RaceHub> hub)=>{
   var joined=sessions.JoinLobby(await auth.Verify(Token(request)),input);await Notify(hub,joined.Lobby,app.Logger);return joined;
  });
  app.MapPost("/api/lobbies/{id}/teams/{teamId}/controllers",async(string id,string teamId,ControllerGrantInput input,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions,IHubContext<RaceHub> hub)=>{var state=sessions.GrantController(id,await auth.Verify(Token(request)),teamId,input.DeviceId);await Notify(hub,state,app.Logger);return state;});
  app.MapGet("/api/lobbies/{id}/teams/{teamId}",async(string id,string teamId,string deviceId,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions)=>sessions.HouseholdTeam(id,await auth.Verify(Token(request)),deviceId,teamId));
  app.MapPost("/api/lobbies/{id}/teams/{teamId}/plans",async(string id,string teamId,string deviceId,TeamCommand input,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions)=>await sessions.SubmitHousehold(id,await auth.Verify(Token(request)),deviceId,teamId,input));
  app.MapGet("/api/lobbies/{id}",(string id,SessionCoordinator sessions)=>sessions.LobbyPublic(id));
  app.MapGet("/api/lobbies/{id}/open/{teamId}",(string id,string teamId,SessionCoordinator sessions)=>sessions.OpenTeam(id,teamId));
  app.MapGet("/api/lobbies/{id}/membership",async(string id,string deviceId,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions)=>new JoinedLobby(sessions.LobbyPublic(id),sessions.LobbyMembership(id,await auth.Verify(Token(request)),deviceId)));
  app.MapPost("/api/lobbies/{id}/claims",async(string id,ClaimTeamInput input,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions,IHubContext<RaceHub> hub)=>{
   var state=sessions.ClaimTeam(id,await auth.Verify(Token(request)),input);await Notify(hub,state,app.Logger);return state;
  });
  app.MapPost("/api/lobbies/{id}/participants",async(string id,SharedParticipantInput input,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions,IHubContext<RaceHub> hub)=>{
   var state=sessions.AddParticipant(id,await auth.Verify(Token(request)),input);await Notify(hub,state,app.Logger);return state;
  });
  app.MapPost("/api/lobbies/{id}/assignments",async(string id,AssignTeamInput input,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions,IHubContext<RaceHub> hub)=>{
   var state=sessions.AssignTeam(id,await auth.Verify(Token(request)),input);await Notify(hub,state,app.Logger);return state;
  });
  app.MapPost("/api/lobbies/{id}/start",async(string id,HttpRequest request,FirebaseIdentity auth,SessionCoordinator sessions,IHubContext<RaceHub> hub)=>{
   var state=sessions.StartLobby(id,await auth.Verify(Token(request)));await Notify(hub,state,app.Logger);return state;
  });
 }
 internal static string? Token(HttpRequest request)=>request.Headers.Authorization.FirstOrDefault() is {} value&&value.StartsWith("Bearer ",StringComparison.Ordinal)?value[7..]:null;
 private static async Task Notify(IHubContext<RaceHub> hub,LobbySnapshot snapshot,ILogger logger){try{await hub.Clients.Group("lobby:"+snapshot.SessionId).SendAsync("lobbySnapshot",snapshot);await hub.Clients.Group("public:"+snapshot.SessionId).SendAsync("lobbyRevision",snapshot.Revision);}catch(Exception error){logger.LogWarning(error,"Lobby notification failed; recover current state by snapshot.");}}
}
