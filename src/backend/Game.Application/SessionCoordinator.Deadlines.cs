namespace Game.Application;
public sealed record DecisionTimingPolicy(int DecisionSeconds,int HandoffSeconds,int AdditionalSharedTeams,int EffectiveSeconds,
    string TimeoutPolicy="Temporary AI fills only missing teams for this phase. Accepted choices are preserved.");
public sealed record TeamDecisionSource(string TeamId,string Source);
public sealed record DueTransitionInput(string CommandId,int Phase,long WindowRevision);
public sealed partial class SessionCoordinator
{
    private readonly TimeProvider clock=timeProvider??TimeProvider.System;
    private void InitializeDecisionWindow(Session session,Lobby lobby)
    {
        ApplyControlBoundary(session);
        session.RequiredTeams=session.RequiredTeams.Where(team=>ExpectedCommandDrivers(session,team).Length>0).ToArray();
        session.FrozenOpponents=OpponentScript.Freeze(session.Engine,session.Drivers.Select(driver=>driver.Team).Distinct().Where(team=>!session.RequiredTeams.Contains(team)),session.Configuration.Laps,session.ScriptVersion,session.Configuration.WetTyres is not null);
        if(session.WindowKind=="SafetyCar")
        {
            session.RequiredTeams=session.RequiredTeams.Where(team=>ExpectedCommandDrivers(session,team).Length>0).ToArray();
            session.FrozenOpponents=FreezeSafetyCar(session,session.Drivers.Select(driver=>driver.Team).Distinct().Where(team=>!session.RequiredTeams.Contains(team)));
        }
        session.TimingPolicy=BuildTimingPolicy(lobby,session.RequiredTeams);
        session.DecisionDeadlineUtc=clock.GetUtcNow().AddSeconds(session.TimingPolicy.EffectiveSeconds);
        session.PausedRemainingSeconds=null;
        session.FrozenTimeoutDecisions=OpponentScript.Freeze(session.Engine,session.RequiredTeams,session.Configuration.Laps,session.ScriptVersion,session.Configuration.WetTyres is not null).ToDictionary();
        if(session.WindowKind=="SafetyCar")session.FrozenTimeoutDecisions=FreezeSafetyCar(session,session.RequiredTeams).ToDictionary();
        if(session.WindowKind=="Weather")
        {
            session.FrozenOpponents=FreezeWeather(session,session.Drivers.Select(driver=>driver.Team).Distinct().Where(team=>!session.RequiredTeams.Contains(team)));
            session.FrozenTimeoutDecisions=FreezeWeather(session,session.RequiredTeams).ToDictionary();
        }
        session.TimeoutSources=session.RequiredTeams.ToDictionary(team=>team,_=>"timeout-ai");
        foreach(var team in session.RequiredTeams)
        {
            var decisions=ExpectedCommandDrivers(session,team).ToDictionary(id=>id,id=>session.FrozenTimeoutDecisions[id]);
            var source=session.TimeoutSources[team];if(session.WindowKind=="SafetyCar")ConfigureSafetyCarAdvice(session,team,ref source);else ConfigureTimeoutPlan(session,team,ref decisions,ref source);
            foreach(var pair in decisions)session.FrozenTimeoutDecisions[pair.Key]=pair.Value;session.TimeoutSources[team]=source;
        }
        if(session.WindowKind=="SafetyCar"&&session.RequiredTeams.Length==0)session.DecisionDeadlineUtc=clock.GetUtcNow();
        WindowOpened(session);
    }
    private static DecisionTimingPolicy BuildTimingPolicy(Lobby lobby,IReadOnlyCollection<string> teams)
    {
        var shared=lobby.Devices.Select(device=>lobby.Assignments.Count(pair=>teams.Contains(pair.Key)&&(pair.Value.DeviceId==device.Id||(pair.Value.AdditionalDevices?.Contains(device.Id)??false)))).DefaultIfEmpty(1).Max();
        var additional=Math.Max(0,shared-1);
        return new(lobby.Input.DecisionSeconds,lobby.Input.HandoffSeconds,additional,lobby.Input.DecisionSeconds+additional*lobby.Input.HandoffSeconds);
    }
    private bool IsDecisionDue(Session session)=>!session.Paused&&session.Lifecycle=="awaiting-decisions"&&
        session.DecisionDeadlineUtc is {} deadline&&clock.GetUtcNow()>=deadline;
    public async Task<PublicSnapshot> AdvanceHouseholdDue(string id,AuthIdentity identity,DueTransitionInput input)
    {
        lock(gate)RequireHost(RequireLobby(id),identity);
        return await AdvanceDue(id,input);
    }
    public async Task<PublicSnapshot> AdvanceDue(string id,DueTransitionInput input)
    {
        if(input is null||!Guid.TryParse(input.CommandId,out _))throw new SessionFault(SessionFailure.InvalidInput,"A due transition needs a command identity.");
        await mutations.WaitAsync();
        try
        {
            Session session;lock(gate){session=Require(id);if(input.Phase!=session.Phase||input.WindowRevision!=session.WindowRevision||!IsDecisionDue(session))return Project(session);}
            await CloseExpiredSerially(session,false);lock(gate)return Project(Require(id));
        }
        finally{mutations.Release();}
    }
    private async Task CloseExpiredSerially(Session session,bool recovered)
    {
        Dictionary<string,Game.Domain.DriverDecision> complete;PublicSnapshot display;TeamSnapshot own;
        lock(gate)
        {
            if(!IsDecisionDue(session))return;
            complete=PrepareExpiredPlan(session);display=Project(session);own=ProjectTeam(session);
        }
        await PublishRace(session,display,own);
        await ResolveCommittedPhase(session,complete,null,null,System.Diagnostics.Stopwatch.GetElapsedTime(session.WindowTimestamp).TotalMilliseconds,recovered);
    }
    private Dictionary<string,Game.Domain.DriverDecision> PrepareExpiredPlan(Session session)
    {
        Dictionary<string,Game.Domain.DriverDecision> complete;
            complete=session.FrozenOpponents.Concat(session.HumanDecisions).ToDictionary();
            var sources=new List<TeamDecisionSource>();
            foreach(var team in session.RequiredTeams)
            {
                if(session.Locks.ContainsKey(team)){sources.Add(new(team,"human"));continue;}
                var plan=ExpectedCommandDrivers(session,team).ToDictionary(id=>id,id=>session.FrozenTimeoutDecisions[id]);
                foreach(var decision in plan)complete.Add(decision.Key,decision.Value);
                var source=session.TimeoutSources[team];sources.Add(new(team,source));
                session.AutomaticPlans.Add(new(session.Phase,session.ModuleState.GetValueOrDefault("automatic-policy:"+team,session.ScriptVersion),source,plan));
            }
            session.LastDecisionSources=sources;session.PendingPlan=complete;session.PendingCommand=null;session.PendingFingerprint=null;
            session.Lifecycle="resolving";session.Revision++;Persist(session.Id);
        return complete;
    }
    private void RecoverExpiredDecision(Session session)
    {
        if(!IsDecisionDue(session))return;
        PrepareExpiredPlan(session);RecoverPending(session);
    }
    // Additive hooks: control04 owns boundary membership, assistance05 owns frozen timeout choices,
    // progression06 owns saved presentation/due intent and scheduling after durable commit.
    partial void ApplyControlBoundary(Session session);
    partial void ConfigureTimeoutPlan(Session session,string teamId,ref Dictionary<string,Game.Domain.DriverDecision> decisions,ref string source);
    partial void PhaseCommitted(Session session);
    partial void WindowOpened(Session session);
}
