namespace Game.Application;
public sealed partial class SessionCoordinator
{
 private sealed record Player(string Id,string Name,string? Uid);
 private sealed record Device(string Id,string Uid);
 private sealed record Assignment(string PlayerId,string DeviceId,string Control,bool Approved,long Revision);
 private sealed class Lobby(string id,string code,string host,CreateLobbyInput input)
 {
  public string Id {get;}=id;public string Code {get;}=code;public string Host {get;}=host;
  public CreateLobbyInput Input {get;}=input;public long Revision {get;set;}=1;
  public List<Player> Players {get;}=[];public List<Device> Devices {get;}=[];
  public string Lifecycle {get;set;}="lobby";
  public Session? Race {get;set;}
  public string[] RequiredTeams {get;set;}=[];
  public string[] ScriptedTeams {get;set;}=[];
  public Dictionary<string,Assignment> Assignments {get;}=[];
 }
 private readonly Dictionary<string,Lobby> lobbies=[];
 public JoinedLobby CreateLobby(AuthIdentity identity,CreateLobbyInput input)
 {
  if(identity.Guest) throw new SessionFault(SessionFailure.Forbidden,"Use a permanent account to host a household.");
  if(input is null || input.Preset is not ("open-equal" or "restricted-equal" or "open-mixed" or "restricted-mixed") || input.Laps is not (6 or 10 or 20 or 40 or 50) || input.Seed is null || input.Mode is not ("Competitive" or "Family" or "Mixed")) throw new SessionFault(SessionFailure.InvalidInput,"Choose a supported preset, distance and privacy mode.");
  var name=Name(input.Name);lock(gate){
   string code;do{code=Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(4));}while(lobbies.Values.Any(l=>l.Code==code));
   var lobby=new Lobby(Guid.NewGuid().ToString("N"),code,identity.Uid,input);
   var player=new Player(Guid.NewGuid().ToString("N"),name,identity.Uid);var device=new Device(Guid.NewGuid().ToString("N"),identity.Uid);
   lobby.Players.Add(player);lobby.Devices.Add(device);lobbies.Add(lobby.Id,lobby);
   return new(ProjectLobby(lobby),new(player.Id,device.Id,true));
  }
 }
 public JoinedLobby JoinLobby(AuthIdentity identity,JoinLobbyInput input)
 {
  var name=Name(input?.Name);lock(gate){
   var lobby=lobbies.Values.SingleOrDefault(l=>l.Code==input?.Code?.Trim().ToUpperInvariant()) ?? throw new SessionFault(SessionFailure.Unavailable,"Lobby unavailable. Check the join code.");
   var player=lobby.Players.SingleOrDefault(p=>p.Uid==identity.Uid);
   if(player is null){if(lobby.Lifecycle!="lobby")throw new SessionFault(SessionFailure.Conflict,"The participant set is frozen after start.");player=new(Guid.NewGuid().ToString("N"),name,identity.Uid);lobby.Players.Add(player);lobby.Revision++;}
   var device=lobby.Devices.SingleOrDefault(d=>d.Id==input?.DeviceId&&d.Uid==identity.Uid);
   if(device is null){device=new(Guid.NewGuid().ToString("N"),identity.Uid);lobby.Devices.Add(device);}
   return new(ProjectLobby(lobby),new(player.Id,device.Id,lobby.Host==identity.Uid));
  }
 }
 public LobbySnapshot LobbyPublic(string id){lock(gate)return ProjectLobby(RequireLobby(id));}
 public LobbyMember LobbyMembership(string id,AuthIdentity identity,string deviceId){lock(gate){var lobby=RequireLobby(id);var player=lobby.Players.SingleOrDefault(p=>p.Uid==identity.Uid);var device=lobby.Devices.SingleOrDefault(d=>d.Id==deviceId&&d.Uid==identity.Uid);if(player is null||device is null)throw new SessionFault(SessionFailure.Forbidden,"Join this lobby on this authenticated device first.");return new(player.Id,device.Id,lobby.Host==identity.Uid);}}
 public LobbySnapshot ClaimTeam(string id,AuthIdentity identity,ClaimTeamInput input)
 {
  lock(gate){var lobby=RequireLobby(id);var member=LobbyMembership(id,identity,input?.DeviceId??"");RequireLobbyOpen(lobby);
   if(input is null||!ConstructorCatalogue.Any(t=>t.TeamId==input.TeamId))throw new SessionFault(SessionFailure.InvalidInput,"Choose one of the eleven teams.");
   if(lobby.Assignments.TryGetValue(input.TeamId,out var previous)){if(previous.PlayerId==member.PlayerId)return ProjectLobby(lobby);throw new SessionFault(SessionFailure.Conflict,"Another player already claimed this team.");}
   if(lobby.Assignments.Values.Any(a=>a.PlayerId==member.PlayerId))throw new SessionFault(SessionFailure.Conflict,"Each participant can claim one team. Ask the host to correct an assignment.");
   lobby.Revision++;lobby.Assignments.Add(input.TeamId,new(member.PlayerId,member.DeviceId,lobby.Input.Mode=="Family"?"Open":"Private",member.IsHost,lobby.Revision));return ProjectLobby(lobby);
  }
 }
 private static void RequireLobbyOpen(Lobby lobby){if(lobby.Lifecycle!="lobby")throw new SessionFault(SessionFailure.Conflict,"Assignments are frozen after the race starts.");}
 private static readonly (string TeamId,string Id,string Name)[] ConstructorCatalogue=[
 ("team-01","mclaren","McLaren"),("team-02","ferrari","Ferrari"),("team-03","red-bull-racing","Red Bull Racing"),("team-04","mercedes","Mercedes"),("team-05","aston-martin","Aston Martin"),("team-06","alpine","Alpine"),("team-07","haas","Haas"),("team-08","racing-bulls","Racing Bulls"),("team-09","williams","Williams"),("team-10","audi","Audi"),("team-11","cadillac","Cadillac")];
 public LobbySnapshot AddParticipant(string id,AuthIdentity identity,SharedParticipantInput input)
 {
  var name=Name(input?.Name);lock(gate){var lobby=RequireLobby(id);RequireHost(lobby,identity);RequireLobbyOpen(lobby);
   _=LobbyMembership(id,identity,input?.DeviceId??"");if(lobby.Players.Count>=32)throw new SessionFault(SessionFailure.Conflict,"This household has reached its participant limit.");
   lobby.Players.Add(new(Guid.NewGuid().ToString("N"),name,null));lobby.Revision++;return ProjectLobby(lobby);
  }
 }
 public LobbySnapshot AssignTeam(string id,AuthIdentity identity,AssignTeamInput input)
 {
  lock(gate){var lobby=RequireLobby(id);RequireHost(lobby,identity);RequireLobbyOpen(lobby);
   if(input is null||!ConstructorCatalogue.Any(t=>t.TeamId==input.TeamId)||input.Control is not ("Private" or "SharedTablet" or "Open")||lobby.Input.Mode=="Competitive"&&input.Control=="Open")throw new SessionFault(SessionFailure.InvalidInput,"Choose a team and a control arrangement compatible with this privacy mode.");
   if(input.PlayerId is null){lobby.Assignments.Remove(input.TeamId);lobby.Revision++;return ProjectLobby(lobby);}
   var player=lobby.Players.SingleOrDefault(p=>p.Id==input.PlayerId)??throw new SessionFault(SessionFailure.InvalidInput,"Choose a participant from this household.");
   var device=input.DeviceId is null?lobby.Devices.FirstOrDefault(d=>d.Uid==player.Uid):lobby.Devices.SingleOrDefault(d=>d.Id==input.DeviceId);
   device=device??throw new SessionFault(SessionFailure.InvalidInput,"Choose a joined device.");
   if(device.Uid!=player.Uid && !(device.Uid==lobby.Host&&(player.Uid is null||input.Control=="SharedTablet"||input.Control=="Open")))throw new SessionFault(SessionFailure.Forbidden,"The selected device does not serve this participant.");
   if(player.Uid is null&&input.Control=="Private")throw new SessionFault(SessionFailure.InvalidInput,"A shared participant needs SharedTablet or Open control.");
   foreach(var previous in lobby.Assignments.Where(a=>a.Value.PlayerId==player.Id&&a.Key!=input.TeamId).Select(a=>a.Key).ToArray())lobby.Assignments.Remove(previous);
   lobby.Revision++;lobby.Assignments[input.TeamId]=new(player.Id,device.Id,input.Control,true,lobby.Revision);return ProjectLobby(lobby);
  }
 }
 public LobbySnapshot StartLobby(string id,AuthIdentity identity)
 {
  mutations.Wait();try{lock(gate){var lobby=RequireLobby(id);RequireHost(lobby,identity);if(lobby.Race is not null)return ProjectLobby(lobby);
   if(lobby.Assignments.Count==0||lobby.Assignments.Values.Any(a=>!a.Approved))throw new SessionFault(SessionFailure.Conflict,"Claim at least one team and approve all assignments before starting.");
   var preset=presets.Get(lobby.Input.Preset,lobby.Input.Laps!.Value);var config=preset.Configuration.WithExplicitTuning();
   var session=new Session(lobby.Id,lobby.Players.Single(p=>p.Uid==lobby.Host).Id,lobby.Devices.First(d=>d.Uid==lobby.Host).Id,
    System.Security.Cryptography.RandomNumberGenerator.GetBytes(32),config,preset.Drivers.ToArray(),lobby.Input.Seed!.Value,new(lobby.Input.Preset,lobby.Input.Laps,lobby.Input.Seed));
   lobby.RequiredTeams=lobby.Assignments.Keys.Order(StringComparer.Ordinal).ToArray();lobby.ScriptedTeams=session.Drivers.Select(d=>d.Team).Distinct().Except(lobby.RequiredTeams).Order(StringComparer.Ordinal).ToArray();
   session.FrozenOpponents=OpponentScript.Freeze(session.Engine,lobby.ScriptedTeams,config.Laps);
   if(session.FrozenOpponents.Count!=lobby.ScriptedTeams.Length*2)throw new InvalidOperationException("The scripted phase-start decision set must cover every scripted driver.");
   session.WindowTimestamp=System.Diagnostics.Stopwatch.GetTimestamp();
   lobby.Race=session;lobby.Lifecycle="started";lobby.Revision++;return ProjectLobby(lobby);
  }}finally{mutations.Release();}
 }
 private Lobby RequireLobby(string id)=>lobbies.GetValueOrDefault(id)??throw new SessionFault(SessionFailure.Unavailable,"Lobby unavailable.");
 private static void RequireHost(Lobby lobby,AuthIdentity identity){if(lobby.Host!=identity.Uid)throw new SessionFault(SessionFailure.Forbidden,"Only this lobby's host can change assignments or start.");}
 private static string Name(string? value)=>!string.IsNullOrWhiteSpace(value)&&value.Trim().Length<=40?value.Trim():throw new SessionFault(SessionFailure.InvalidInput,"Use a player name from 1 to 40 characters.");
 private static LobbySnapshot ProjectLobby(Lobby lobby)=>new(lobby.Id,lobby.Code,"/shared/join?code="+lobby.Code,lobby.Revision,lobby.Lifecycle,lobby.Input.Mode,
  Array.AsReadOnly(lobby.Players.Select(p=>new LobbyPlayer(p.Id,p.Name,p.Uid is null)).ToArray()),
  Array.AsReadOnly(ConstructorCatalogue.Select(t=>{var a=lobby.Assignments.GetValueOrDefault(t.TeamId);return new LobbyTeam(t.TeamId,t.Id,t.Name,a?.PlayerId,a?.Control??(lobby.Input.Mode=="Family"?"Open":"Private"),a?.Approved??false,a?.Revision??0);}).ToArray()),Array.AsReadOnly(lobby.RequiredTeams),Array.AsReadOnly(lobby.ScriptedTeams),lobby.Race is null?null:"/race/display/"+lobby.Id);
}
