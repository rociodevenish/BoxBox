namespace Game.Application;
public sealed partial class SessionCoordinator
{
 private (Session Session, Assignment Assignment) AuthoriseHousehold(string id,AuthIdentity identity,string deviceId,string teamId)
 {
  var lobby=RequireLobby(id);_ = LobbyMembership(id,identity,deviceId);
  var assignment=lobby.Assignments.GetValueOrDefault(teamId);
  if(assignment is null || !assignment.Approved || assignment.DeviceId!=deviceId && !(assignment.AdditionalDevices?.Contains(deviceId) ?? false))
   throw new SessionFault(SessionFailure.Forbidden,"This device is not currently authorised for this team.");
  return (lobby.Race ?? throw new SessionFault(SessionFailure.Conflict,"The household race has not started."),assignment);
 }
 public TeamSnapshot HouseholdTeam(string id,AuthIdentity identity,string deviceId,string teamId)
 {lock(gate){var access=AuthoriseHousehold(id,identity,deviceId,teamId);return ProjectTeam(access.Session,teamId,access.Assignment);}}
 public async Task<SubmissionAck> SubmitHousehold(string id,AuthIdentity identity,string deviceId,string teamId,TeamCommand command)
 {await mutations.WaitAsync();try{return await SubmitSerially(id,null,command,identity,deviceId,teamId);}finally{mutations.Release();}}
 public LobbySnapshot GrantController(string id,AuthIdentity identity,string teamId,string deviceId)
 {lock(gate){var lobby=RequireLobby(id);RequireHost(lobby,identity);RequireLobbyOpen(lobby);
  var assignment=lobby.Assignments.GetValueOrDefault(teamId)??throw new SessionFault(SessionFailure.InvalidInput,"Assign this team first.");
  var player=lobby.Players.Single(p=>p.Id==assignment.PlayerId);var device=lobby.Devices.SingleOrDefault(d=>d.Id==deviceId);
  if(device is null || device.Uid!=player.Uid && !(device.Uid==lobby.Host && assignment.Control is "SharedTablet" or "Open"))throw new SessionFault(SessionFailure.Forbidden,"This joined device does not serve the assigned player.");
  lobby.Revision++;lobby.Assignments[teamId]=assignment with {AdditionalDevices=(assignment.AdditionalDevices??[]).Append(deviceId).Distinct().ToArray(),Revision=lobby.Revision};return ProjectLobby(lobby);}}
 private async Task PublishRace(Session session,PublicSnapshot display,TeamSnapshot legacy)
 {
  TeamSnapshot[]? teams;OpenTeamSnapshot[] open=[];lock(gate){teams=lobbies.TryGetValue(session.Id,out var lobby)
   ? lobby.Assignments.Select(pair=>ProjectTeam(session,pair.Key,pair.Value)).ToArray() : null;
   if(teams is not null && lobby!.Input.Mode!="Competitive")open=teams.Where(team=>lobby.Assignments[team.TeamId].Control=="Open"&&lobby.Assignments[team.TeamId].Approved).Select(ProjectOpen).ToArray();}
  if(teams is null){await PublishSafely(display,legacy);return;}
  try{await notifications.PublishHousehold(display,teams,open);}catch(Exception){}
 }
}
