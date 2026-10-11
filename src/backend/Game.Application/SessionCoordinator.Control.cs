using System.Text.Json;
using Game.Domain;
namespace Game.Application;
public sealed partial class SessionCoordinator
{
    private const string ControlKey="control04";
    private sealed record ControlIntent(string Mode,int Phase);
    private sealed record ControlledTeam(string Mode,long Revision,int? UntilPhase=null,ControlIntent? Pending=null);
    private sealed record ControlReceipt(string Fingerprint,TeamControlStatus Status);
    private sealed record ControlState(Dictionary<string,ControlledTeam> Teams,Dictionary<string,ControlReceipt> Receipts);
    private static ControlState ReadControl(Session session)=>session.ModuleState.TryGetValue(ControlKey,out var json)
        ?JsonSerializer.Deserialize<ControlState>(json,BrowserReplay.JsonOptions)!:new([],[]);
    private static void SaveControl(Session session,ControlState state)=>session.ModuleState[ControlKey]=JsonSerializer.Serialize(state,BrowserReplay.JsonOptions);
    private static TeamControlStatus ControlStatus(Session session,string teamId)
    {
        var state=ReadControl(session).Teams.GetValueOrDefault(teamId)??new("Human",0);
        return new(teamId,state.Mode,state.Revision,state.Pending?.Mode,state.Pending?.Phase,state.UntilPhase);
    }
    private IReadOnlyList<TeamControlStatus>? PublicControl(Session session)=>lobbies.TryGetValue(session.Id,out var lobby)
        ?lobby.RequiredTeams.Select(team=>ControlStatus(session,team)).ToArray():null;
    public IReadOnlyList<TeamControlStatus> ControlPublic(string id){lock(gate)return PublicControl(Require(id))??[];}
    public async Task<TeamControlStatus> RequestTakeover(string id,AuthIdentity identity,string teamId,ControlRequest input)
        =>await ChangeControl(id,identity,null,teamId,input,false);
    public async Task<TeamControlStatus> RequestReclaim(string id,AuthIdentity identity,string deviceId,string teamId,ControlRequest input)
        =>await ChangeControl(id,identity,deviceId,teamId,input,true);
    private async Task<TeamControlStatus> ChangeControl(string id,AuthIdentity identity,string? deviceId,string teamId,ControlRequest input,bool reclaim)
    {
        await mutations.WaitAsync();try
        {
            Session session;TeamControlStatus status;PublicSnapshot display;TeamSnapshot own;
            lock(gate)
            {
                var lobby=RequireLobby(id);if(reclaim)_=AuthoriseHousehold(id,identity,deviceId!,teamId);else RequireHost(lobby,identity);
                session=lobby.Race??throw new SessionFault(SessionFailure.Conflict,"Start the race before requesting a control transfer.");
                if(!lobby.RequiredTeams.Contains(teamId))throw new SessionFault(SessionFailure.InvalidInput,"Choose an assigned player team.");
                if(input is null||!Guid.TryParse(input.CommandId,out var command)||(reclaim?input.Mode!="Human":input.Mode is not ("TemporaryAi" or "PermanentAi")))
                    throw new SessionFault(SessionFailure.InvalidInput,"Choose temporary or permanent AI control, or authorised human reclaim, with a command identity.");
                var state=ReadControl(session);var fingerprint=JsonSerializer.Serialize(new{teamId,input.Revision,input.Mode});
                if(state.Receipts.TryGetValue(command.ToString(),out var receipt))return receipt.Fingerprint==fingerprint?receipt.Status:throw new SessionFault(SessionFailure.Conflict,"Control command identity already used with different settings.");
                var current=state.Teams.GetValueOrDefault(teamId)??new("Human",0);
                if(input.Revision!=current.Revision)throw new SessionFault(SessionFailure.Conflict,"Control settings changed. Refresh before requesting a transfer.",Project(session));
                if(session.Engine.GetState().Finished)throw new SessionFault(SessionFailure.Conflict,"The race has finished.");
                if(state.Receipts.Count>=256)throw new SessionFault(SessionFailure.Conflict,"This race has reached its control request limit.");
                state.Teams[teamId]=current with{Revision=current.Revision+1,Pending=new(input.Mode,session.Phase+1)};
                SaveControl(session,state);status=ControlStatus(session,teamId);state.Receipts[command.ToString()]=new(fingerprint,status);SaveControl(session,state);
                lobby.Revision++;session.Revision++;Persist(id);display=Project(session);own=ProjectTeam(session);
            }
            await PublishRace(session,display,own);return status;
        }
        finally{mutations.Release();}
    }
    partial void ApplyControlBoundary(Session session)
    {
        if(!lobbies.TryGetValue(session.Id,out var lobby))return;
        var state=ReadControl(session);
        foreach(var team in state.Teams.Keys.ToArray())
        {
            var current=state.Teams[team];
            if(current.Mode=="TemporaryAi"&&current.UntilPhase<=session.Phase)current=current with{Mode="Human",Revision=current.Revision+1,UntilPhase=null};
            if(current.Pending is {} pending&&pending.Phase<=session.Phase)current=current with{Mode=pending.Mode,Revision=current.Revision+1,UntilPhase=pending.Mode=="TemporaryAi"?session.Phase+1:null,Pending=null};
            state.Teams[team]=current;
        }
        SaveControl(session,state);
        session.RequiredTeams=lobby.RequiredTeams.Where(team=>!state.Teams.TryGetValue(team,out var control)||control.Mode=="Human").ToArray();
    }
    private void RecordAutomaticResponsibility(Session session)
    {
        foreach(var group in session.FrozenOpponents.GroupBy(pair=>ControlStatus(session,session.Drivers.Single(driver=>driver.Id==pair.Key).Team).Mode))
        {
            var source=group.Key=="TemporaryAi"?"temporary-takeover-ai":group.Key=="PermanentAi"?"permanent-takeover-ai":"unclaimed-ai";
            session.AutomaticPlans.Add(new(session.Phase,session.ScriptVersion,source,new System.Collections.ObjectModel.ReadOnlyDictionary<string,DriverDecision>(group.ToDictionary())));
        }
        var controlled=PublicControl(session)?.Where(status=>status.Mode!="Human").Select(status=>new TeamDecisionSource(status.TeamId,status.Mode=="TemporaryAi"?"temporary-takeover-ai":"permanent-takeover-ai"))??[];
        session.LastDecisionSources=session.LastDecisionSources.Concat(controlled).ToArray();
    }
}
