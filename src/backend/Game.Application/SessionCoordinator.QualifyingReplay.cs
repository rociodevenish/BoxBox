using Game.Domain;
namespace Game.Application;

/// <summary>Frozen deterministic qualifying evidence; excludes membership, device grants and private session state.</summary>
public sealed record QualifyingReplayRecord(string SessionId,QualifyingAttemptInput[] Attempts,QualifyingResult[] Results,QualifyingRank[] Grid);
public sealed partial class SessionCoordinator
{
    public QualifyingReplayRecord DesignerQualifyingRecord(string id)
    {
        lock(gate)
        {
            var state=ReadQualifying(RequireLobby(id));
            if(state.Attempts is not {Length:3} attempts||state.Results is not {Length:3} results||state.Grid is null)
                throw new SessionFault(SessionFailure.Conflict,"Complete qualifying before exporting its replay.");
            return new(id,attempts,results,state.Grid);
        }
    }
}
