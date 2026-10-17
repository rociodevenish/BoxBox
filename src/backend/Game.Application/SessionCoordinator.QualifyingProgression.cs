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
                if(state.Paused||wakeup is null||wakeup.Generation!=input.Generation||wakeup.Phase!=input.Phase
                    ||wakeup.WindowRevision!=input.WindowRevision||clock.GetUtcNow()<wakeup.DueUtc)return ProjectQualifying(lobby,state);
                if(state.Lifecycle=="session-result")state=OpenQualifyingStage(lobby,state);
                else if(state.Lifecycle=="awaiting-decisions")
                {
                    foreach(var team in RequiredQualifyingTeams(lobby,state).Where(team=>!state.Locks.ContainsKey(team)))state.Locks[team]=state.TimeoutLocks[team];
                    state=ResolveQualifying(lobby,state) with{Revision=state.Revision+1};
                }
                else return ProjectQualifying(lobby,state);
                lobby.Revision++;WriteQualifying(lobby,state);Persist(lobby.Id);snapshot=ProjectQualifying(lobby,state);
            }
            await PublishQualifying(snapshot.SessionId);return snapshot;
        }finally{mutations.Release();}
    }
    public async Task<QualifyingPublicSnapshot> ContinueQualifying(string id,AuthIdentity identity,DueTransitionInput input)
    {
        if(input is null||!Guid.TryParse(input.CommandId,out var commandId)||commandId==Guid.Empty)
            throw new SessionFault(SessionFailure.InvalidInput,"Continue needs a command identity.");
        input=input with{CommandId=commandId.ToString()};
        await mutations.WaitAsync();try
        {
            QualifyingPublicSnapshot snapshot;
            lock(gate)
            {
                var lobby=RequireLobby(id);RequireHost(lobby,identity);var state=ReadQualifying(lobby);
                var commands=state.ContinueCommands??[];
                if(commands.TryGetValue(input.CommandId,out var saved))return saved==input?ProjectQualifying(lobby,state):throw new SessionFault(SessionFailure.Conflict,"Continue identity was already used differently.");
                if(state.Commands.ContainsKey(input.CommandId)||state.Starts.ContainsKey(input.CommandId))throw new SessionFault(SessionFailure.Conflict,"Command identity already belongs to another operation.");
                if(state.Paused||state.Lifecycle!="session-result"||Stage(state)!=input.Phase||state.WindowRevision!=input.WindowRevision)return ProjectQualifying(lobby,state);
                commands[input.CommandId]=input;state=OpenQualifyingStage(lobby,state with{ContinueCommands=commands});
                lobby.Revision++;WriteQualifying(lobby,state);Persist(id);snapshot=ProjectQualifying(lobby,state);
            }
            await PublishQualifying(id);return snapshot;
        }finally{mutations.Release();}
    }
    // Stage-entry seam: freeze automatic choices here before accepting any human lock (ticket 03).
    private QualifyingState OpenQualifyingStage(Lobby lobby,QualifyingState state)
    {
        if(state.Result is null||state.Session=="Q3")throw new SessionFault(SessionFailure.Conflict,"No next qualifying stage.");
        var session=state.Session=="Q1"?"Q2":"Q3";var retain=session=="Q2"?10:0;
        var order=state.Result.Ranking.Where(r=>r.Advances).Select(r=>r.EntryId).ToArray();
        var input=state.Input with{Session=session,Choices=[],PreSessionOrder=order,Rules=new(retain)};
        var revision=state.Revision+1;
        state=state with{Session=session,Input=input,Revision=revision,WindowRevision=revision,Result=null,Locks=[],TimeoutLocks=[],Lifecycle="awaiting-decisions",Remaining=null};
        var required=RequiredQualifyingTeams(lobby,state);
        var timing=BuildTimingPolicy(lobby,required) with{TimeoutPolicy=state.TimingPolicy.TimeoutPolicy};
        foreach(var team in lobby.ScriptedTeams.Where(team=>QualifyingDrivers(state).Any(d=>d.TeamId==team)))state.Locks[team]=SafeQualifyingLock(input,team,"unclaimed-baseline");
        foreach(var team in required)state.TimeoutLocks[team]=SafeQualifyingLock(input,team,"timeout-baseline");
        var due=clock.GetUtcNow().AddSeconds(timing.EffectiveSeconds);
        state=state with{TimingPolicy=timing,Deadline=due,Wakeup=new(lobby.Id,Guid.NewGuid().ToString("N"),"qualifying-decision",Stage(state),revision,due)};
        return required.Length==0?ResolveQualifying(lobby,state):state;
    }
    private LobbySnapshot SetQualifyingPaused(Lobby lobby,bool paused)
    {
        var state=ReadQualifying(lobby);
        if(state.Paused==paused)return ProjectLobby(lobby);
        // Expired choices settle before the pause; accepted locks remain untouched.
        if(paused&&state.Lifecycle=="awaiting-decisions"&&state.Deadline is {} due&&clock.GetUtcNow()>=due)
        {
            foreach(var team in RequiredQualifyingTeams(lobby,state).Where(team=>!state.Locks.ContainsKey(team)))state.Locks[team]=state.TimeoutLocks[team];
            state=ResolveQualifying(lobby,state);
        }
        var remaining=state.Deadline is {} deadline?Math.Max(0,(deadline-clock.GetUtcNow()).TotalSeconds):state.Remaining;
        state=state with{Paused=paused,Revision=state.Revision+1,Remaining=paused?remaining:null,Deadline=null,Wakeup=null};
        if(!paused&&state.Lifecycle is "awaiting-decisions" or "session-result")
        {
            var next=clock.GetUtcNow().AddSeconds(remaining??(state.Lifecycle=="session-result"?PresentationSeconds:state.TimingPolicy.EffectiveSeconds));
            state=state with{Deadline=next,Wakeup=new(lobby.Id,Guid.NewGuid().ToString("N"),state.Lifecycle=="session-result"?"qualifying-presentation":"qualifying-decision",Stage(state),state.WindowRevision,next)};
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
        Session? race;lock(gate)race=lobbies.GetValueOrDefault(id)?.Race;
        if(race is not null)await PublishRace(race,Public(id),ProjectTeam(race));
    }
}
