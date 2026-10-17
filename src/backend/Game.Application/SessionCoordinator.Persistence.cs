using System.Diagnostics;
using System.Text.Json;
using Game.Domain;
namespace Game.Application;
public sealed partial class SessionCoordinator
{
    private readonly ISessionStore persistence = store ?? new VolatileSessionStore();
    private readonly Dictionary<string,string> storageVersions=[];
    private readonly Dictionary<string,StoredSession> durableRecords=[];
    private readonly Dictionary<string,string> recoveryLimitations=[];
    private const string RecoverySchema="session-recovery-v1";
    private const string ContentVersion="synthetic-dry-presets-v1";
    private sealed record SavedCommand(Guid Id,string Fingerprint,SubmissionAck Ack);
    private sealed record SavedWindow(DateTimeOffset? Deadline,double? PausedRemaining,DecisionTimingPolicy? TimingPolicy,
        Dictionary<string,DriverDecision> FrozenTimeoutDecisions,Dictionary<string,string> TimeoutSources,TeamDecisionSource[] LastDecisionSources,bool DeferNextWindow,Dictionary<string,string> ModuleState);
    private sealed record SavedRace(string Id,string Player,string Device,byte[] GrantHash,
        CreateSessionInput Input,RaceConfiguration Configuration,Driver[] Drivers,
        int Phase,long Revision,long WindowRevision,string Lifecycle,bool Paused,
        string[] RequiredTeams,Dictionary<string,TeamCommand> Locks,
        Dictionary<string,DriverDecision> HumanDecisions,Dictionary<string,DriverDecision> FrozenOpponents,
        Dictionary<string,DriverDecision>[] Plans,RecordedReplayPhase[] Outcomes,
        SavedCommand[] Commands,TeamCommand? ConfirmedPlan,PublicEvent[] Events,PublicPresentation? Presentation,
        int EngineEventCursor,Dictionary<string,int> PresentationPhases,
        Dictionary<string,DriverDecision>? PendingPlan,TeamCommand? PendingCommand,string? PendingFingerprint,RecordedAutomaticPlan[]? AutomaticPlans=null,string AutomaticSource="unclaimed-ai",SavedWindow? Window=null,AutomaticStartReplay? Opening=null,string? WindowKind=null,string[]? OperationKinds=null);
    private sealed record SavedLobby(string Id,string Code,string Host,CreateLobbyInput Input,long Revision,
        Player[] Players,Device[] Devices,Dictionary<string,Assignment> Assignments,string Lifecycle,
        string[] RequiredTeams,string[] ScriptedTeams,SavedRace? Race,Dictionary<string,string>? ModuleState=null);
    private sealed record RecoveryRecord(string Schema,string Model,string Randomness,string Content,string Policy,
        SavedLobby? Lobby,SavedRace? Race);
    private static SavedRace CaptureRace(Session session)=>new(session.Id,session.Player,session.Device,session.GrantHash,
        session.Input,session.Configuration,session.Drivers,session.Phase,session.Revision,session.WindowRevision,session.Lifecycle,
        session.Paused,session.RequiredTeams,session.Locks,session.HumanDecisions,session.FrozenOpponents.ToDictionary(),
        session.AcceptedPlans.Select(p=>p.ToDictionary()).ToArray(),session.RecordedPhases.ToArray(),
        session.AcceptedCommands.Select(p=>new SavedCommand(p.Key,p.Value.Fingerprint,p.Value.Ack)).ToArray(),
        session.ConfirmedPlan,session.Events.ToArray(),session.Presentation,session.EngineEventCursor,session.PresentationPhases,
        session.PendingPlan,session.PendingCommand,session.PendingFingerprint,session.AutomaticPlans.ToArray(),Window:new(session.DecisionDeadlineUtc,session.PausedRemainingSeconds,session.TimingPolicy,session.FrozenTimeoutDecisions,session.TimeoutSources,session.LastDecisionSources.ToArray(),session.DeferNextWindow,session.ModuleState),Opening:session.OpeningReplay,WindowKind:session.WindowKind,OperationKinds:session.OperationKinds.ToArray());
    private string Capture(string id)
    {
        var lobby=lobbies.GetValueOrDefault(id);var race=lobby?.Race ?? (active?.Id==id?active:null);
        var savedLobby=lobby is null?null:new SavedLobby(lobby.Id,lobby.Code,lobby.Host,lobby.Input,lobby.Revision,
            lobby.Players.ToArray(),lobby.Devices.ToArray(),lobby.Assignments,lobby.Lifecycle,lobby.RequiredTeams,lobby.ScriptedTeams,
            race is null?null:CaptureRace(race),lobby.ModuleState);
        return JsonSerializer.Serialize(new RecoveryRecord(RecoverySchema,SimulationVersions.SegmentedModel,
            SimulationVersions.SegmentedRandomness,ContentVersion,race?.ScriptVersion??OpponentScript.Version,savedLobby,lobby is null&&race is not null?CaptureRace(race):null),BrowserReplay.JsonOptions);
    }
    // Called while holding gate; no acknowledgement or notification precedes this conditional write.
    private void Persist(string id)
    {
        try
        {
            var payload=Capture(id);
            var session=lobbies.GetValueOrDefault(id)?.Race ?? (active?.Id==id?active:null);
            storageVersions[id]=persistence.Write(id,storageVersions.GetValueOrDefault(id),payload,lobbies.GetValueOrDefault(id)?.Code,session is null?QualifyingWakeup(id):session.Paused?null:Progress(session).Wakeup);
            durableRecords[id]=new(storageVersions[id],payload);
            if(lobbies.GetValueOrDefault(id)?.Race is {} race)race.StorageError=false;
            if(active?.Id==id)active.StorageError=false;
        }
        catch(Exception error)
        {
            Trace.TraceError("Durable save failed for session {0}: {1}",id,error.GetType().Name);
            // Discard speculative memory, including command acknowledgements and advanced engine state.
            StoredSession? saved;try{saved=persistence.Read(id);}catch(Exception){saved=durableRecords.GetValueOrDefault(id);}
            lobbies.Remove(id);if(active?.Id==id)active=null;
            if(saved is not null){Restore(id,saved);var recovered=lobbies.GetValueOrDefault(id)?.Race ?? active;if(recovered is not null)recovered.StorageError=true;}
            if(error is SessionFault)throw;
            throw new SessionFault(SessionFailure.StorageUnavailable,"The save failed. Progression is paused; retry the same command after storage recovers.");
        }
        DispatchSavedWakeup(id);
    }
    private void EnsureRecovered(string id,bool scheduled=false)
    {
        if(recoveryLimitations.TryGetValue(id,out var limitation))throw new SessionFault(SessionFailure.Conflict,limitation);
        if(lobbies.ContainsKey(id)||active?.Id==id)
        {
            var loaded=lobbies.GetValueOrDefault(id)?.Race ?? active;
            if(loaded?.ScheduledRecovery==true&&!scheduled){loaded.ScheduledRecovery=false;if(loaded.PendingPlan is not null)RecoverPending(loaded);else{RecoverExpiredDecision(loaded);RecoverPresentation(loaded);}}
            else if(loaded?.StorageError==true&&loaded.PendingPlan is not null&&!scheduled)RecoverPending(loaded);
            return;
        }
        StoredSession? record;try{record=persistence.Read(id);}catch(Exception error) when(error is not SessionFault){throw new SessionFault(SessionFailure.StorageUnavailable,"Saved session is temporarily unavailable. Retry recovery when storage returns.");}if(record is null)return;
        Restore(id,record);
        var session=lobbies.GetValueOrDefault(id)?.Race ?? (active?.Id==id?active:null);
        if(scheduled){if(session is not null)session.ScheduledRecovery=true;return;}
        if(session?.PendingPlan is not null)RecoverPending(session);
        else if(session is not null){RecoverExpiredDecision(session);if(!scheduled)RecoverPresentation(session);}
    }
    private void RecoverPending(Session session)
    {
        CompleteResolution(session,session.PendingPlan!,0);
        session.Paused=true;
        CommitResolvedPhase(session,session.PendingCommand,session.PendingFingerprint);
    }
    private SubmissionAck? CommitResolvedPhase(Session session,TeamCommand? command,string? fingerprint)
    {
        SubmissionAck? acknowledgement=null;
        if(command is not null)
        {
            acknowledgement=new(command.CommandId,session.Id,command.Phase!.Value,session.Revision,session.Lifecycle);
            session.AcceptedCommands[Guid.Parse(command.CommandId)]=(fingerprint!,acknowledgement);
        }
        session.PendingPlan=null;session.PendingCommand=null;session.PendingFingerprint=null;
        session.DecisionDeadlineUtc=null;session.PausedRemainingSeconds=null;
        PhaseCommitted(session);
        if(session.WindowKind=="SafetyCar"&&!lobbies.ContainsKey(session.Id))session.DeferNextWindow=false;
        if(session.WindowKind!="SafetyCar"&&session.Engine.GetState().Interruption is not null&&session.Lifecycle!="finished")
        {
            OpenHouseholdWindow(session);session.DeferNextWindow=true;
            if(session.RequiredTeams.Length==0)
            {
                CompleteResolution(session,session.FrozenOpponents,0);
                CommitResolvedPhase(session,null,null);
            }
        }
        Persist(session.Id);
        return acknowledgement;
    }
    private void Restore(string id,StoredSession record)
    {
        var saved=JsonSerializer.Deserialize<RecoveryRecord>(record.Payload,BrowserReplay.JsonOptions)??throw new InvalidDataException("Missing recovery record.");
        if(saved.Schema!=RecoverySchema||saved.Model!=SimulationVersions.SegmentedModel||saved.Randomness!=SimulationVersions.SegmentedRandomness||saved.Content!=ContentVersion||!OpponentScript.Supports(saved.Policy))
        {
            var limitation="Saved race uses an incompatible schema, simulation, randomness, content or decision policy version. Deploy its compatible version to resume; saved inputs are preserved.";
            recoveryLimitations[id]=limitation;throw new SessionFault(SessionFailure.Conflict,limitation);
        }
        var watch=Stopwatch.StartNew();
        Session? RestoreRace(SavedRace? race)
        {
            if(race is null)return null;
            if(race.Plans.Length!=race.Outcomes.Length||race.OperationKinds is not null&&race.OperationKinds.Length!=race.Plans.Length)throw new SessionFault(SessionFailure.Conflict,"Saved race is missing a committed replay outcome. Preserve its recovery record.");
            var replayWatch=Stopwatch.StartNew();
            Game.Simulation.RaceEngine? restoredEngine;
            try{restoredEngine=race.Opening is null?null:Game.Simulation.SimulationReplay.CreateEngine(new(Game.Simulation.SimulationReplay.Model(race.Configuration),Game.Simulation.SimulationReplay.Randomness(race.Configuration),race.Configuration,race.Drivers,new([],[]),race.Input.Seed!.Value,race.Opening));}
            catch(ArgumentException){throw new SessionFault(SessionFailure.Conflict,"Saved opening inputs or outcome are incompatible. Preserve the frozen preparation and deploy its compatible version.");}
            var session=new Session(race.Id,race.Player,race.Device,race.GrantHash,race.Configuration,race.Drivers,race.Input.Seed!.Value,race.Input,restoredEngine){OpeningReplay=race.Opening};
            foreach(var (plan,index) in race.Plans.Select((plan,index)=>(plan,index)))
            {
                var kind=race.OperationKinds is null?"Normal":race.OperationKinds[index];
                var outcome=ResolveOperation(session.Engine,kind,plan);
                session.OperationKinds.Add(kind);
                if(index>=race.Outcomes.Length||BrowserReplay.OutcomeHash(outcome)!=race.Outcomes[index].OutcomeHash)
                    throw new SessionFault(SessionFailure.Conflict,"Saved replay does not match its recorded outcome. Preserve this race for recovery diagnostics.");
                session.AcceptedPlans.Add(plan);session.RecordedPhases.Add(race.Outcomes[index]);
            }
            session.RecoveryReplayMilliseconds=replayWatch.Elapsed.TotalMilliseconds;
            session.WindowKind=race.WindowKind;
            session.Phase=race.Phase;session.Revision=race.Revision;session.WindowRevision=race.WindowRevision;session.Lifecycle=race.Lifecycle;
            session.Paused=race.Paused;session.RequiredTeams=race.RequiredTeams;session.ConfirmedPlan=race.ConfirmedPlan;
            foreach(var pair in race.Locks)session.Locks.Add(pair.Key,pair.Value);
            foreach(var pair in race.HumanDecisions)session.HumanDecisions.Add(pair.Key,pair.Value);
            session.FrozenOpponents=race.FrozenOpponents;session.ScriptVersion=saved.Policy;
            if(race.AutomaticPlans is not null)session.AutomaticPlans.AddRange(race.AutomaticPlans);
            foreach(var command in race.Commands)session.AcceptedCommands.Add(command.Id,(command.Fingerprint,command.Ack));
            session.Events=race.Events;session.Presentation=race.Presentation;session.EngineEventCursor=race.EngineEventCursor;
            foreach(var pair in race.PresentationPhases)session.PresentationPhases.Add(pair.Key,pair.Value);
            session.PendingPlan=race.PendingPlan;session.PendingCommand=race.PendingCommand;session.PendingFingerprint=race.PendingFingerprint;
            if(race.Window is {} window){session.DecisionDeadlineUtc=window.Deadline;session.PausedRemainingSeconds=window.PausedRemaining;session.TimingPolicy=window.TimingPolicy;session.FrozenTimeoutDecisions=window.FrozenTimeoutDecisions;session.TimeoutSources=window.TimeoutSources;session.LastDecisionSources=window.LastDecisionSources;session.DeferNextWindow=window.DeferNextWindow;session.ModuleState=window.ModuleState;}
            session.WindowTimestamp=Stopwatch.GetTimestamp();return session;
        }
        if(saved.Lobby is {} lobby)
        {
            var restored=new Lobby(lobby.Id,lobby.Code,lobby.Host,lobby.Input){Revision=lobby.Revision,Lifecycle=lobby.Lifecycle,RequiredTeams=lobby.RequiredTeams,ScriptedTeams=lobby.ScriptedTeams,Race=RestoreRace(lobby.Race)};
            restored.Players.AddRange(lobby.Players);restored.Devices.AddRange(lobby.Devices);
            restored.ModuleState=lobby.ModuleState??[];
            foreach(var pair in lobby.Assignments)restored.Assignments.Add(pair.Key,pair.Value);
            lobbies[id]=restored;
        }
        else active=RestoreRace(saved.Race);
        storageVersions[id]=record.Version;durableRecords[id]=record;
        Trace.WriteLine($"Recovery {id}: replay {watch.Elapsed.TotalMilliseconds:F2}ms");
    }
}
