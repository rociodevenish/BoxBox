using System.Text.Json;
namespace Game.Application;

public sealed record ScheduledWakeup(string SessionId,string Generation,string Kind,int Phase,long WindowRevision,DateTimeOffset DueUtc);
public sealed record WakeupInput(string SessionId,string Generation,int Phase,long WindowRevision);
public interface IScheduledWakeups { void Enqueue(ScheduledWakeup wakeup); }

public sealed partial class SessionCoordinator
{
    private const string ProgressionKey="progression-v1";
    private const int PresentationSeconds=8;
    private sealed record Progression(ScheduledWakeup? Wakeup=null,DateTimeOffset? PresentationDeadline=null,double? PresentationRemaining=null,
        Dictionary<string,DueTransitionInput>? ContinueCommands=null);
    private static Progression Progress(Session session)=>session.ModuleState.TryGetValue(ProgressionKey,out var json)
        ? JsonSerializer.Deserialize<Progression>(json)! : new();
    private static void SaveProgress(Session session,Progression progress)=>session.ModuleState[ProgressionKey]=JsonSerializer.Serialize(progress);
    private static ScheduledWakeup NewWakeup(Session session,string kind,DateTimeOffset due)=>new(session.Id,Guid.NewGuid().ToString("N"),kind,session.Phase,session.WindowRevision,due);
    partial void WindowOpened(Session session)
    {
        if(session.DecisionDeadlineUtc is not {} deadline)return;
        SaveProgress(session,Progress(session) with{Wakeup=NewWakeup(session,"decision",deadline),PresentationDeadline=null,PresentationRemaining=null});
    }
    partial void PhaseCommitted(Session session)
    {
        if(!lobbies.ContainsKey(session.Id))return;
        session.DeferNextWindow=true;
        var deadline=clock.GetUtcNow().AddSeconds(PresentationSeconds);
        SaveProgress(session,Progress(session) with{
            Wakeup=session.Paused||session.Lifecycle=="finished"?null:NewWakeup(session,"presentation",deadline),
            PresentationDeadline=session.Paused||session.Lifecycle=="finished"?null:deadline,
            PresentationRemaining=null});
    }
    private void PauseProgression(Session session)
    {
        var progress=Progress(session);
        SaveProgress(session,progress with{Wakeup=null,PresentationDeadline=null,
            PresentationRemaining=progress.PresentationDeadline is {} deadline?Math.Max(0,(deadline-clock.GetUtcNow()).TotalSeconds):progress.PresentationRemaining});
    }
    private bool ResumePresentation(Session session)
    {
        var progress=Progress(session);
        if(progress.PresentationRemaining is not {} remaining)return false;
        var deadline=clock.GetUtcNow().AddSeconds(remaining);
        SaveProgress(session,progress with{PresentationRemaining=null,PresentationDeadline=deadline,Wakeup=NewWakeup(session,"presentation",deadline)});
        return true;
    }
    private void DispatchSavedWakeup(string id)
    {
        var session=lobbies.GetValueOrDefault(id)?.Race ?? (active?.Id==id?active:null);
        var wakeup=session is null?QualifyingWakeup(id):session.Paused?null:Progress(session).Wakeup;
        if(wakeup is null)return;
        try{scheduledWakeups?.Enqueue(wakeup);}
        catch(Exception error){System.Diagnostics.Trace.TraceWarning("Wakeup enqueue pending for {0}: {1}",id,error.GetType().Name);}
    }
    public async Task<PublicSnapshot> ContinueHousehold(string id,AuthIdentity identity,DueTransitionInput input)
    {
        if(input is null||!Guid.TryParse(input.CommandId,out var commandId))throw new SessionFault(SessionFailure.InvalidInput,"Continue needs a command identity.");
        input=input with{CommandId=commandId.ToString()};
        await mutations.WaitAsync();
        try
        {
            Session session;PublicSnapshot display;TeamSnapshot own;
            lock(gate)
            {
                var lobby=RequireLobby(id);RequireHost(lobby,identity);session=Require(id);var progress=Progress(session);
                var key=commandId.ToString();var commands=progress.ContinueCommands??[];
                if(commands.TryGetValue(key,out var previous))return previous==input?Project(session):throw new SessionFault(SessionFailure.Conflict,"Continue identity was already used differently.");
                if(session.Paused||session.Lifecycle!="phase-result"||session.Phase!=input.Phase||session.WindowRevision!=input.WindowRevision)return Project(session);
                commands[key]=input;SaveProgress(session,progress with{ContinueCommands=commands});
                OpenHouseholdWindow(session);Persist(id);display=Project(session);own=ProjectTeam(session);
            }
            await PublishRace(session,display,own);return display;
        }
        finally{mutations.Release();}
    }
    public async Task<PublicSnapshot> DeliverWakeup(WakeupInput input)
    {
        if(input is null||!Guid.TryParse(input.Generation,out _))throw new SessionFault(SessionFailure.InvalidInput,"Wakeup needs its persisted generation.");
        await mutations.WaitAsync();
        try
        {
            Session session;PublicSnapshot display;TeamSnapshot own;bool decision;bool recovered;
            lock(gate)
            {
                session=Require(input.SessionId,true);var wakeup=Progress(session).Wakeup;
                if(session.Paused||wakeup is null||wakeup.Generation!=input.Generation||wakeup.Phase!=input.Phase||wakeup.WindowRevision!=input.WindowRevision
                    ||wakeup.Phase!=session.Phase||wakeup.WindowRevision!=session.WindowRevision||clock.GetUtcNow()<wakeup.DueUtc)return Project(session);
                decision=wakeup.Kind=="decision";
                recovered=session.ScheduledRecovery;
                if(decision&&session.PendingPlan is not null){session.ScheduledRecovery=false;RecoverPending(session);return Project(session);}
                if(!decision)
                {
                    if(session.Lifecycle!="phase-result")return Project(session);
                    OpenHouseholdWindow(session);Persist(session.Id);
                }
                session.ScheduledRecovery=false;
                display=Project(session);own=ProjectTeam(session);
            }
            if(decision){await CloseExpiredSerially(session,recovered);lock(gate)return Project(Require(session.Id));}
            await PublishRace(session,display,own);return display;
        }
        finally{mutations.Release();}
    }
    public async Task<int> SweepWakeups()
    {
        var count=0;
        // Independent Cloud Scheduler invocation supplies the wakeup even if enqueue never happened.
        foreach(var id in persistence.ListSessions())
        {
            ScheduledWakeup? wakeup;
            await mutations.WaitAsync();
            try{lock(gate){EnsureRecovered(id);var session=lobbies.GetValueOrDefault(id)?.Race ?? (active?.Id==id?active:null);wakeup=session is null?QualifyingWakeup(id):session.Paused?null:Progress(session).Wakeup;if(wakeup is not null)DispatchSavedWakeup(id);}}
            finally{mutations.Release();}
            if(wakeup is not null&&clock.GetUtcNow()>=wakeup.DueUtc){await DeliverScheduledWakeup(new(id,wakeup.Generation,wakeup.Phase,wakeup.WindowRevision));count++;}
        }
        return count;
    }
    private void RecoverPresentation(Session session)
    {
        if(session.Lifecycle!="phase-result"||session.Paused)return;
        // A restarted presentation must not start unattended further decision windows.
        session.Paused=true;session.Revision++;PauseProgression(session);SaveProgress(session,Progress(session) with{PresentationRemaining=null});Persist(session.Id);
    }
}
