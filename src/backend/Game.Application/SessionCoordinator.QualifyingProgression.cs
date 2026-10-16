namespace Game.Application;
public sealed partial class SessionCoordinator
{
    private ScheduledWakeup? QualifyingWakeup(string id)
    {
        var lobby=lobbies.GetValueOrDefault(id);
        return lobby?.ModuleState.ContainsKey(QualifyingModule)==true?ReadQualifying(lobby).Wakeup:null;
    }
    public bool HasQualifying(string id){lock(gate){EnsureRecovered(id,true);var lobby=lobbies.GetValueOrDefault(id);return lobby?.Race is null&&lobby?.ModuleState.ContainsKey(QualifyingModule)==true;}}
    public async Task<object> DeliverScheduledWakeup(WakeupInput input)=>HasQualifying(input.SessionId)
        ? await DeliverQualifyingWakeup(input):await DeliverWakeup(input);
    public async Task<QualifyingPublicSnapshot> DeliverQualifyingWakeup(WakeupInput input)
    {
        if(input is null||!Guid.TryParse(input.Generation,out _))throw new SessionFault(SessionFailure.InvalidInput,"Supply the saved wakeup generation.");
        await mutations.WaitAsync();try
        {
            QualifyingPublicSnapshot snapshot;lock(gate)
            {
                var lobby=RequireLobby(input.SessionId);var state=ReadQualifying(lobby);var wakeup=state.Wakeup;
                if(state.Paused||state.Lifecycle!="awaiting-decisions"||wakeup is null||wakeup.Generation!=input.Generation||wakeup.Phase!=input.Phase
                    ||wakeup.WindowRevision!=input.WindowRevision||clock.GetUtcNow()<wakeup.DueUtc)return ProjectQualifying(lobby,state);
                foreach(var team in lobby.RequiredTeams.Where(team=>!state.Locks.ContainsKey(team)))state.Locks[team]=state.TimeoutLocks[team];
                state=ResolveQualifying(state) with{Revision=state.Revision+1};lobby.Revision++;WriteQualifying(lobby,state);Persist(lobby.Id);snapshot=ProjectQualifying(lobby,state);
            }
            await PublishQualifying(snapshot.SessionId);return snapshot;
        }finally{mutations.Release();}
    }
    private LobbySnapshot SetQualifyingPaused(Lobby lobby,bool paused)
    {
        var state=ReadQualifying(lobby);
        if(state.Paused==paused)return ProjectLobby(lobby);
        // Expired choices settle before the pause; accepted locks remain untouched.
        if(paused&&state.Lifecycle=="awaiting-decisions"&&state.Deadline is {} due&&clock.GetUtcNow()>=due)
        {
            foreach(var team in lobby.RequiredTeams.Where(team=>!state.Locks.ContainsKey(team)))state.Locks[team]=state.TimeoutLocks[team];
            state=ResolveQualifying(state);
        }
        var remaining=state.Deadline is {} deadline?Math.Max(0,(deadline-clock.GetUtcNow()).TotalSeconds):state.Remaining;
        state=state with{Paused=paused,Revision=state.Revision+1,Remaining=paused?remaining:null,Deadline=null,Wakeup=null};
        if(!paused&&state.Lifecycle=="awaiting-decisions")
        {
            var next=clock.GetUtcNow().AddSeconds(remaining??state.TimingPolicy.EffectiveSeconds);
            state=state with{Deadline=next,Wakeup=new(lobby.Id,Guid.NewGuid().ToString("N"),"qualifying-decision",1,state.WindowRevision,next)};
        }
        lobby.Revision++;WriteQualifying(lobby,state);Persist(lobby.Id);return ProjectLobby(lobby);
    }
    public async Task PublishQualifying(string id)
    {
        QualifyingPublicSnapshot display;IReadOnlyList<QualifyingTeamSnapshot> teams;QualifyingTeamSnapshot[] open;
        lock(gate)
        {
            var lobby=RequireLobby(id);display=ProjectQualifying(lobby,ReadQualifying(lobby));teams=QualifyingControllerUpdates(id);
            open=lobby.Input.Mode=="Competitive"?[]:teams.Where(t=>lobby.Assignments[t.TeamId].Approved&&lobby.Assignments[t.TeamId].Control=="Open").ToArray();
        }
        try{await notifications.PublishQualifying(display,teams,open);}catch(Exception){/* Recovery snapshot is authoritative. */}
    }
}
