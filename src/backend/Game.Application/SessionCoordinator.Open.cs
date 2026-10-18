namespace Game.Application;
public sealed partial class SessionCoordinator
{
 public OpenTeamSnapshot OpenTeam(string id,string teamId)
 {
  lock(gate){var lobby=RequireLobby(id);var assignment=lobby.Assignments.GetValueOrDefault(teamId);
   if(lobby.Input.Mode=="Competitive" || assignment is null || !assignment.Approved || assignment.Control!="Open")throw new SessionFault(SessionFailure.Forbidden,"This team keeps its information on authorised private controllers.");
   return ProjectOpen(ProjectTeam(lobby.Race??throw new SessionFault(SessionFailure.Conflict,"The household race has not started."),teamId,assignment));}
 }
 private static OpenTeamSnapshot ProjectOpen(TeamSnapshot team)=>new(team.SessionId,team.Phase,team.Revision,team.Lifecycle,team.TeamId,team.Cars,team.ConfirmedPlan,team.AssignmentRevision,team.Assistance,team.Forecast);
}
