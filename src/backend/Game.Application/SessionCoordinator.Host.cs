namespace Game.Application;
public sealed partial class SessionCoordinator
{
 public async Task<LobbySnapshot> SetHouseholdPaused(string id,AuthIdentity identity,bool paused)
 {
  Session session; PublicSnapshot display; TeamSnapshot own; LobbySnapshot lobbyState;
  lock(gate){var lobby=RequireLobby(id);RequireHost(lobby,identity);session=lobby.Race??throw new SessionFault(SessionFailure.Conflict,"Start the race before pausing.");
   if(session.Paused==paused)return ProjectLobby(lobby);
   session.Paused=paused;lobby.Revision++;session.Revision++;
   if(paused&&session.Lifecycle=="awaiting-decisions")session.Lifecycle="paused-decisions";
   if(!paused&&session.Lifecycle=="paused-decisions")session.Lifecycle="awaiting-decisions";
   if(!paused&&session.Lifecycle=="phase-result")OpenHouseholdWindow(session);
   display=Project(session);own=ProjectTeam(session);lobbyState=ProjectLobby(lobby);}
  await PublishRace(session,display,own);return lobbyState;
 }
 private static void OpenHouseholdWindow(Session session)
 {
  session.Phase++;session.Revision++;session.WindowRevision=session.Revision;session.Lifecycle="awaiting-decisions";
  session.ConfirmedPlan=null;session.Locks.Clear();session.HumanDecisions.Clear();
  session.FrozenOpponents=OpponentScript.Freeze(session.Engine,session.Drivers.Select(driver=>driver.Team).Distinct().Where(team=>!session.RequiredTeams.Contains(team)),session.Configuration.Laps);
  session.WindowTimestamp=System.Diagnostics.Stopwatch.GetTimestamp();
 }
 public async Task<LobbySnapshot> RevokeDevice(string id,AuthIdentity identity,string deviceId)
 {
  await mutations.WaitAsync();try{lock(gate){var lobby=RequireLobby(id);RequireHost(lobby,identity);
   if(!lobby.Devices.Any(device=>device.Id==deviceId))throw new SessionFault(SessionFailure.InvalidInput,"Choose a joined device.");
   var affected=lobby.Assignments.Where(pair=>pair.Value.DeviceId==deviceId||(pair.Value.AdditionalDevices?.Contains(deviceId)??false)).ToArray();
   if(affected.Length==0)return ProjectLobby(lobby);lobby.Revision++;
   foreach(var pair in affected)lobby.Assignments[pair.Key]=pair.Value with{DeviceId=pair.Value.DeviceId==deviceId?"":pair.Value.DeviceId,AdditionalDevices=pair.Value.AdditionalDevices?.Where(device=>device!=deviceId).ToArray(),Revision=lobby.Revision};
   return ProjectLobby(lobby);
  }}finally{mutations.Release();}
 }
 public IReadOnlyList<ControllerDevice> ControllerDevices(string id,AuthIdentity identity)
 {lock(gate){var lobby=RequireLobby(id);RequireHost(lobby,identity);return lobby.Devices.Select(device=>new ControllerDevice(device.Id,lobby.Players.FirstOrDefault(player=>player.Uid==device.Uid)?.Name??"Household device",lobby.Assignments.Where(pair=>pair.Value.DeviceId==device.Id||(pair.Value.AdditionalDevices?.Contains(device.Id)??false)).Select(pair=>pair.Key).ToArray())).Where(device=>device.TeamIds.Count>0).ToArray();}}
}



