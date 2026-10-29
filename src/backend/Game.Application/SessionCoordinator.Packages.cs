using System.Text.Json;
using Game.Simulation;
using Game.Domain;
namespace Game.Application;
public record PackageSelectionCommand(string CommandId,long? Revision,long? AssignmentRevision,RacePackageSelection Selection);
public sealed partial class SessionCoordinator
{
 public SetupAck SelectPackage(string id,AuthIdentity identity,string deviceId,string teamId,PackageSelectionCommand command)
 {
  mutations.Wait();try{lock(gate)
  {
   var access=AuthorisePreparationController(id,identity,deviceId,teamId);
   var lobby=access.Lobby;var state=ReadPreparation(lobby);var team=state.Teams[teamId];
   if(command?.Selection is null)throw new SessionFault(SessionFailure.InvalidInput,"Select one completed package version.");
   var key=ValidatePreparationCommand(command.CommandId,command.Revision,command.AssignmentRevision,access.Assignment.Revision);
   var fingerprint=JsonSerializer.Serialize(new{Operation="package-selection",Session=id,Team=teamId,command.Revision,command.AssignmentRevision,command.Selection,Version=RacePackages.Version});
   if(PreparationReceipt(team,key,fingerprint) is {} accepted)return accepted;
   if(lobby.Lifecycle!="preparing"||team.Lifecycle is not("provisional" or "assessed")||command.Revision!=team.Revision)
    throw new SessionFault(SessionFailure.Conflict,"Reopen final readiness before selecting; qualifying locks package and setup.");
   WeekendPreset preset;
   try{preset=RacePackages.Select(team.Preset,command.Selection);}
   catch(ArgumentException error){throw new SessionFault(SessionFailure.InvalidInput,error.Message);}
   var next=team with{Preset=preset,Effective=ApplyWeekendConditions(lobby,WeekendPreparation.Prepare(preset,lobby.Input.Laps!.Value,team.Setup)),Revision=team.Revision+1};
   var ack=new SetupAck(key,id,teamId,next.Revision,next.Lifecycle);
   next.Commands[key]=new(fingerprint,ack);state.Teams[teamId]=next;
   lobby.Revision++;WritePreparation(lobby,state);Persist(id);return ack;
  }}finally{mutations.Release();}
 }
}
