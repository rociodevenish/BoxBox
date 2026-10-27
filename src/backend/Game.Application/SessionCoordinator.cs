using Game.Domain;
using Game.Simulation;
using System.Security.Cryptography;
using System.Diagnostics;
namespace Game.Application;
public sealed partial class SessionCoordinator(ISessionNotifications notifications,IRacePresets presets, ISessionStore? store = null, TimeProvider? timeProvider = null,IScheduledWakeups? scheduledWakeups=null,IOpeningDisruptionPolicy? openingDisruptions=null)
{
    private sealed partial class Session(string id, string player, string device, byte[] grantHash,
        RaceConfiguration configuration, Driver[] drivers, int seed, CreateSessionInput input, RaceEngine? openingEngine=null)
    {
        public long StartedTimestamp { get; } = Stopwatch.GetTimestamp();
        public long WindowTimestamp { get; set; }
        public double? TotalToFinishMilliseconds { get; set; }
        public double? RecoveryReplayMilliseconds { get; set; }
        public List<BrowserPhaseTiming> PhaseTimings { get; } = [];
        public Dictionary<string, int> PresentationPhases { get; } = [];
        public Dictionary<Guid, (PresentationTimingInput Input, RecordedPresentationTiming Sample)> PresentationSamples { get; } = [];
        public string Id { get; } = id;
        public string Player { get; } = player;
        public string Device { get; } = device;
        public CreateSessionInput Input { get; } = input;
        public byte[] GrantHash { get; } = grantHash;
        public RaceConfiguration Configuration { get; } = configuration;
        public Driver[] Drivers { get; } = drivers;
        public RaceEngine Engine { get; } = openingEngine ?? new(configuration, drivers, seed);
        public AutomaticStartReplay? OpeningReplay {get;set;}
        public PublicOpening? OpeningSummary {get;set;}
        public IReadOnlyDictionary<string, DriverDecision> FrozenOpponents { get; set; } = new Dictionary<string, DriverDecision>();
        public List<IReadOnlyDictionary<string, DriverDecision>> AcceptedPlans { get; } = [];
        public List<RecordedReplayPhase> RecordedPhases { get; } = [];
        public List<RecordedAutomaticPlan> AutomaticPlans { get; } = [];
        public string ScriptVersion { get; set; } = configuration.WetTyres is not null?WeatherDecisionPolicy.Version:OpponentScript.Version;
        public Dictionary<Guid, (string Fingerprint, SubmissionAck Ack)> AcceptedCommands { get; } = [];
        public TeamCommand? ConfirmedPlan { get; set; }
        public Dictionary<string, TeamCommand> Locks { get; } = [];
        public Dictionary<string, DriverDecision> HumanDecisions { get; } = [];
        public string[] RequiredTeams { get; set; } = ["team-01"];
        public bool Paused { get; set; }
        public bool StorageError { get; set; }
        public bool DeferNextWindow { get; set; }
        public bool ScheduledRecovery { get; set; }
        public Dictionary<string,string> ModuleState { get; set; }=[];
        public DateTimeOffset? DecisionDeadlineUtc { get; set; }
        public double? PausedRemainingSeconds { get; set; }
        public DecisionTimingPolicy? TimingPolicy { get; set; }
        public Dictionary<string,DriverDecision> FrozenTimeoutDecisions { get; set; }=[];
        public Dictionary<string,string> TimeoutSources { get; set; }=[];
        public IReadOnlyList<TeamDecisionSource> LastDecisionSources { get; set; }=[];
        public Dictionary<string, DriverDecision>? PendingPlan { get; set; }
        public TeamCommand? PendingCommand { get; set; }
        public string? PendingFingerprint { get; set; }
        public string? WindowKind { get; set; }
        public List<string> OperationKinds { get; } = [];
        public int Phase { get; set; }
        public long Revision { get; set; } = 1;
        public long WindowRevision { get; set; } = 1;
        public string Lifecycle { get; set; } = "awaiting-decisions";
        public IReadOnlyList<PublicEvent> Events { get; set; } = [];
        public int EngineEventCursor { get; set; }
        public PublicPresentation? Presentation { get; set; }
    }
    private readonly object gate = new();
    private readonly SemaphoreSlim mutations = new(1, 1);
    private Session? active;
    public SessionCreated Create(CreateSessionInput input)
    {
        if (input is null || input.Preset is not ("open-equal" or "restricted-equal" or "open-mixed" or "restricted-mixed")
            || input.Laps is not (6 or 10 or 20 or 40 or 50) || input.Seed is null)
            throw new SessionFault(SessionFailure.InvalidInput, "Choose an allowlisted preset, distance (6/10/20/40/50) and designer seed.");
        var capability = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var id = Guid.NewGuid().ToString("N");
        var player = Guid.NewGuid().ToString("N");
        var device = Guid.NewGuid().ToString("N");
        var preset=presets.Get(input.Preset,input.Laps.Value);
        var configuration = preset.Configuration.WithExplicitTuning();
        var session = new Session(id, player, device, SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(capability)),
            configuration, preset.Drivers.ToArray(), input.Seed.Value, input);
        session.FrozenOpponents = OpponentScript.Freeze(session.Engine, session.Drivers.Select(driver => driver.Team).Distinct().Where(team => team != "team-01"), configuration.Laps,wetTyresEnabled:configuration.WetTyres is not null);
        session.WindowTimestamp = Stopwatch.GetTimestamp();
        mutations.Wait();
        try { lock (gate) { active = session; Persist(id); } }
        finally { mutations.Release(); }
        return new(id, $"/race/display/{id}", $"/race/controller/{id}#capability={capability}",
            player, "team-01", device, capability);
    }
    public PublicSnapshot Public(string id)
    { lock (gate) return Project(Require(id)); }
    public SessionCreated Restart(string id, string? capability)
    {
        CreateSessionInput input;
        lock (gate) input = Authorise(id, capability).Input;
        return Create(input);
    }
    public BrowserReplayRecord DesignerRecord(string id)
    {
        lock (gate)
        {
            var session = Require(id);
            var scheduled = session.AcceptedPlans.SelectMany((plan, phase) => plan.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new ScheduledDecision(phase, pair.Key, pair.Value))).ToArray();
            return new(session.OperationKinds.Any(kind=>kind is "SafetyCar" or "Weather")?BrowserReplay.DisruptionFormatVersion:BrowserReplay.FormatVersion, id, session.Input.Preset, "team-01", session.ScriptVersion,
                SimulationReplay.Capture(session.Configuration, session.Drivers, new StrategyPlan(scheduled, []), session.Input.Seed!.Value) with{Opening=session.OpeningReplay},
                Array.AsReadOnly(session.RecordedPhases.ToArray()), session.RecordedPhases.LastOrDefault()?.Finished ?? false,
                new(session.TotalToFinishMilliseconds ?? Stopwatch.GetElapsedTime(session.StartedTimestamp).TotalMilliseconds,
                    session.TotalToFinishMilliseconds, Array.AsReadOnly(session.PhaseTimings.ToArray()),
                    Array.AsReadOnly(session.PresentationSamples.Values.Select(value => value.Sample).ToArray()), session.RecoveryReplayMilliseconds),Array.AsReadOnly(session.AutomaticPlans.ToArray()),AssistanceHistory(session),session.OperationKinds.Any(kind=>kind is "SafetyCar" or "Weather")?session.OperationKinds.ToArray():null);
        }
    }
    private Session Require(string id,bool scheduled=false) { EnsureRecovered(id,scheduled); return active is { } session && session.Id == id ? session
        : lobbies.GetValueOrDefault(id)?.Race ?? throw new SessionFault(SessionFailure.Unavailable, "Session unavailable. Create a new local playtest."); }
    public bool RecordPresentationTiming(string id, PresentationTimingInput? input)
    {
        lock (gate)
        {
            var session = Require(id);
            if (input is null || !Guid.TryParse(input.SampleId, out var sampleId) || !Guid.TryParse(input.DeviceId, out var deviceId)
                || input.Cursor is null || !session.PresentationPhases.TryGetValue(input.Cursor, out var phase)
                || input.DurationMilliseconds is not { } duration || !double.IsFinite(duration) || duration is < 0 or > 3_600_000
                || input.ClientElapsedMilliseconds is not { } elapsed || !double.IsFinite(elapsed) || elapsed is < 0 or > 3_600_000
                || input.PlaybackKind is not ("initial" or "replay" or "recovered")
                || input.Completion is not ("completed" or "skipped" or "reduced-motion" or "new-window"))
                throw new SessionFault(SessionFailure.InvalidInput, "Presentation timing needs a known cursor, identity and finite measured durations.");
            input = input with { SampleId = sampleId.ToString(), DeviceId = deviceId.ToString() };
            if (session.PresentationSamples.TryGetValue(sampleId, out var previous))
                return previous.Input == input ? true
                    : throw new SessionFault(SessionFailure.Conflict, "This timing identity already has a different sample.");
            session.PresentationSamples.Add(sampleId, (input, new(input.SampleId, input.DeviceId, input.Cursor, phase,
                duration, elapsed, input.PlaybackKind, input.Completion)));
            return true;
        }
    }
    private PublicSnapshot Project(Session session)
    {
        var state = session.Engine.GetState();
        var tyres = state.Tyres.ToDictionary(t => t.DriverId);
        var drivers = session.Drivers.ToDictionary(d => d.Id);
        var lapTimes = session.Drivers.SelectMany(driver =>
        {
            double? previous = null;
            return state.Events.Where(item => item.DriverId == driver.Id && item.Kind == RaceEventKind.LapCompleted)
                .Select(item => { var duration = item.Time - previous; previous = item.Time;
                    return (DriverId: driver.Id, Lap: item.Lap, Seconds: duration, CompletedAt: item.Time); })
                .Where(lap => lap.Seconds.HasValue).Select(lap => (lap.DriverId, lap.Lap, Seconds: lap.Seconds!.Value, lap.CompletedAt));
        }).ToArray();
        var fastest = lapTimes.OrderBy(lap => lap.Seconds).ThenBy(lap => lap.CompletedAt)
            .ThenBy(lap => lap.DriverId, StringComparer.Ordinal).FirstOrDefault().DriverId;
        var timing = session.Engine.GetTimingObservation().ToDictionary(row => row.DriverId);
        var ordered = (state.Time == 0 ? state.Classification.OrderBy(entry => drivers[entry.DriverId].Gap)
            .ThenBy(entry => entry.DriverId, StringComparer.Ordinal) : state.Classification.AsEnumerable()).ToArray();
        var rows = ordered.Select((entry, index) =>
        {
            var tyre = tyres[entry.DriverId];
            var driver = drivers[entry.DriverId];
            var gap = index == 0 ? new PublicGap("leader") : state.Time == 0
                ? new PublicGap("time", driver.Gap)
                : ProjectGap(timing[entry.DriverId].ToLeader);
            var ahead = index == 0 ? new PublicGap("unavailable") : state.Time == 0
                ? new PublicGap("time", driver.Gap - drivers[ordered[index - 1].DriverId].Gap)
                : ProjectGap(timing[entry.DriverId].ToAhead);
            var name=session.OpeningReplay?.Input.Weekends.SelectMany(w=>w.Cars).FirstOrDefault(c=>c.Source.EntryId==entry.DriverId)?.Source.Driver.Name ?? "Driver " + entry.DriverId[^2..];
            return new PublicCar(entry.DriverId, name, driver.Team,
                index + 1, entry.Laps, entry.Progress, gap, ahead, tyre.Compound.ToString(), tyre.Age,
                entry.RetirementTime is not null ? "retired" : tyre.InPits ? "in-pit-lane" : "on-track", state.Finished ? state.Eligibility[entry.DriverId] : null,
                Array.AsReadOnly(lapTimes.Where(lap => lap.DriverId == entry.DriverId).TakeLast(5)
                    .Select(lap => new PublicLapTime(lap.Lap, lap.Seconds)).ToArray()),
                state.Events.Count(item => item.DriverId == entry.DriverId && item.Kind == RaceEventKind.PitExit),
                fastest == entry.DriverId,state.CarConditions?.Single(condition=>condition.DriverId==entry.DriverId).DamageSecondsPerLap);
        }).ToArray();
        return new(session.Id, session.Phase, session.Revision, session.StorageError ? "storage-error" : session.Lifecycle, state.LeaderLap,
            session.Configuration.Laps, state.Time, Array.AsReadOnly(rows), session.Events, session.Presentation,
            state.Finished ? 0 : session.Configuration.Incidents is null ? Math.Min(5, session.Configuration.Laps - state.LeaderLap)
                : Math.Round(Math.Min(session.Configuration.NormalPhaseLaps??5,Math.Max(0,session.Configuration.Laps-state.Classification.Where(row=>row.RetirementTime is null&&row.FinishTime is null).Select(row=>row.Laps+row.Progress).DefaultIfEmpty(session.Configuration.Laps).Max())),3),
            Array.AsReadOnly(session.RequiredTeams.Select(team => new TeamReadiness(team, session.Locks.ContainsKey(team))).ToArray()),session.Paused || session.StorageError, session.DecisionDeadlineUtc, session.PausedRemainingSeconds, session.TimingPolicy, clock.GetUtcNow(), session.LastDecisionSources,PublicControl(session),Progress(session).PresentationDeadline,Progress(session).PresentationRemaining,session.WindowRevision,ProjectOpening(session),ProjectSafetyCar(session),state.Weather,session.Configuration.WetTyres is null?null:true,state.WeatherReaction);
    }
    private static PublicGap ProjectGap(TimingGap gap) => gap.Laps > 0 ? new("laps", Laps: gap.Laps)
        : gap.Seconds is { } seconds ? new("time", seconds) : new("unavailable");
    public TeamSnapshot Team(string id, string? capability)
    { lock (gate) return ProjectTeam(Authorise(id, capability)); }
    private Session Authorise(string id, string? capability)
    {
        var session = Require(id);
        if (lobbies.ContainsKey(id))throw new SessionFault(SessionFailure.Forbidden,"Household control requires current authenticated membership and assignment.");
        if (capability is null || !CryptographicOperations.FixedTimeEquals(session.GrantHash,
            SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(capability))))
            throw new SessionFault(SessionFailure.Forbidden, "A separate team controller capability is required.");
        return session;
    }
    private static TeamSnapshot ProjectTeam(Session session, string teamId = "team-01", Assignment? assignment = null)
    {
        var observation = session.Engine.GetTeamObservation(teamId);
        var own = observation.TeamTyres.Select(tyre => new PrivateCar(tyre.DriverId, tyre.Compound.ToString(),
            tyre.Age, tyre.EstimatedCondition, tyre.InPits, tyre.StopQueued,
            Array.AsReadOnly(observation.BattleOptions[tyre.DriverId].Select(option => option.ToString()).ToArray()),session.Configuration.Incidents is not null||session.Configuration.WeatherReactions is not null?session.Engine.GetSafetyCarOptions().Single(option=>option.DriverId==tyre.DriverId):null,
            observation.Conditions?.Single(condition=>condition.DriverId==tyre.DriverId))).ToArray();
        return new(session.Id, session.Phase, session.Revision, session.StorageError ? "storage-error" : session.Lifecycle, assignment?.PlayerId ?? session.Player, teamId,
            assignment?.DeviceId ?? session.Device, Array.AsReadOnly(own), assignment is null ? session.ConfirmedPlan : session.Locks.GetValueOrDefault(teamId), assignment?.Revision ?? 0, session.WindowRevision,ProjectAssistance(session,teamId),assignment is null?null:ControlStatus(session,teamId),session.WindowKind,observation.Forecast,session.Configuration.WetTyres is null?null:true);
    }
    public async Task<SubmissionAck> Submit(string id, string? capability, TeamCommand? command)
    {
        await mutations.WaitAsync();
        try { return await SubmitSerially(id, capability, command); }
        finally { mutations.Release(); }
    }
    private async Task<SubmissionAck> SubmitSerially(string id, string? capability, TeamCommand? command, AuthIdentity? identity = null, string? deviceId = null, string teamId = "team-01")
    {
        Session session; Dictionary<string, DriverDecision> complete; PublicSnapshot resolving; TeamSnapshot teamResolving;
        Guid commandId; string fingerprint;
        double decisionMilliseconds;
        SubmissionAck? waitingAck = null;
        lock (gate)
        {
            session = identity is null ? Authorise(id, capability) : AuthoriseHousehold(id, identity, deviceId!, teamId).Session;
            if (command is null || !Guid.TryParse(command.CommandId, out commandId) || command.Cars is null || command.Cars.Any(car=>car is null))
                throw new SessionFault(SessionFailure.InvalidInput, "Submit exactly both owned cars with a command identity.");
            command = command with { Cars = Array.AsReadOnly(command.Cars.OrderBy(car => car.DriverId).ToArray()) };
            fingerprint = System.Text.Json.JsonSerializer.Serialize(command with { CommandId = commandId.ToString() });
            if (session.AcceptedCommands.TryGetValue(commandId, out var accepted))
                return accepted.Fingerprint == fingerprint ? accepted.Ack
                    : throw new SessionFault(SessionFailure.Conflict, "This command identity was already accepted with different choices.");
            if(session.Lifecycle!="awaiting-decisions")throw new SessionFault(SessionFailure.Conflict,"This decision window is not editable. Refresh the current phase.");
        }
        if(command.Cars.Count != ExpectedCommandDrivers(session,teamId).Length || !command.Cars.Select(car=>car.DriverId).Order().SequenceEqual(ExpectedCommandDrivers(session,teamId).Order()))
            throw new SessionFault(SessionFailure.InvalidInput,"Submit exactly the cars requiring choices in this window.");
        if(identity is not null&&CurrentControl(session,teamId)!=ControlMode.Human)throw new SessionFault(SessionFailure.Conflict,"AI is responsible for this phase. Request reclaim for the next boundary.",Project(session));
        if(IsDecisionDue(session)){await CloseExpiredSerially(session,false);lock(gate)throw new SessionFault(SessionFailure.Conflict,"The decision deadline passed. Refresh the current phase; accepted locks were preserved.",Project(Require(id)));}
        lock(gate)
        {
            if(command.WindowKind!=session.WindowKind)throw new SessionFault(SessionFailure.Conflict,"This command belongs to a different window kind.");
            if (session.Lifecycle != "awaiting-decisions" || command.Phase != session.Phase || command.Revision != (identity is null ? session.Revision : session.WindowRevision))
                throw new SessionFault(SessionFailure.Conflict, "This decision window is not editable. Refresh the current phase.");
            if (session.Locks.ContainsKey(teamId)) throw new SessionFault(SessionFailure.Conflict, "This team already locked its final plan for this phase.");
            var observation = session.Engine.GetTeamObservation(teamId);
            complete = session.FrozenOpponents.ToDictionary();
            foreach (var car in command.Cars)
            {
                if (!TryChoice<PaceInstruction>(car.Pace, out var pace) || !TryChoice<BattleInstruction>(car.Battle, out var battle)
                    || car.Pit is not ("Stay Out" or "Pit") || car.Pit == "Stay Out" && car.Compound is not null
                    || car.Pit == "Pit" && !TryChoice<TyreCompound>(car.Compound, out _)
                    || session.Configuration.WetTyres is null && car.Compound is "Intermediate" or "Wet")
                    throw new SessionFault(SessionFailure.InvalidInput, "Each car needs explicit valid pace, pit and battle choices.");
                var tyre = observation.TeamTyres.Single(tyre => tyre.DriverId == car.DriverId);
                if (session.WindowKind=="SafetyCar" && (pace!=PaceInstruction.Standard||battle!=BattleInstruction.Neutral))throw new SessionFault(SessionFailure.InvalidInput,"Safety Car choices contain only pit instructions.");
                if(session.WindowKind=="Weather"&&WeatherSafetyCarActive(session)&&(pace!=PaceInstruction.Conserve||battle!=BattleInstruction.Neutral))throw new SessionFault(SessionFailure.InvalidInput,"Safety Car control requires Conserve and Neutral until racing resumes.");
                if (!observation.BattleOptions[car.DriverId].Contains(battle)
                    || car.Pit == "Pit" && (tyre.InPits || tyre.StopQueued))
                    throw new SessionFault(SessionFailure.InvalidInput, "A battle or pit instruction is unavailable. Review both cars.");
                complete.Add(car.DriverId, new(pace, car.Pit == "Pit" ? Enum.Parse<TyreCompound>(car.Compound!) : null, battle));
            }
            if(IsDecisionDue(session))throw new SessionFault(SessionFailure.Conflict,"The decision deadline passed before this complete plan could lock.",Project(session));
            if (identity is not null)
            {
                foreach (var car in complete.Where(pair => !session.FrozenOpponents.ContainsKey(pair.Key))) session.HumanDecisions.Add(car.Key, car.Value);
                session.Locks.Add(teamId, command); session.Revision++;
                if (!session.RequiredTeams.All(session.Locks.ContainsKey))
                {
                    var ack = new SubmissionAck(command.CommandId, id, session.Phase, session.Revision, "awaiting-decisions");
                    session.AcceptedCommands.Add(commandId, (fingerprint, ack));
                    // Publish outside the gate below through the common notification helper.
                    waitingAck = ack;
                }
                complete = session.FrozenOpponents.Concat(session.HumanDecisions).ToDictionary();
            }
            if (identity is null) session.Locks[teamId] = command;
            session.ConfirmedPlan = command;
            decisionMilliseconds = Stopwatch.GetElapsedTime(session.WindowTimestamp).TotalMilliseconds;
            if (waitingAck is null) { session.LastDecisionSources=session.RequiredTeams.Select(team=>new TeamDecisionSource(team,"human")).ToArray(); session.Lifecycle = "resolving"; session.Revision++; session.PendingPlan=complete; session.PendingCommand=command; session.PendingFingerprint=fingerprint; }
            Persist(id);
            resolving = Project(session); teamResolving = ProjectTeam(session);
        }
        await PublishRace(session, resolving, teamResolving);
        if (waitingAck is not null) return waitingAck;
        return (await ResolveCommittedPhase(session,complete,command,fingerprint,decisionMilliseconds))!;
    }
    private async Task<SubmissionAck?> ResolveCommittedPhase(Session session,IReadOnlyDictionary<string,DriverDecision> complete,
        TeamCommand? command,string? fingerprint,double decisionMilliseconds,bool recovered=false)
    {
        PublicSnapshot resolved;TeamSnapshot teamResolved;SubmissionAck? acknowledgement=null;
        lock(gate)
        {
            CompleteResolution(session,complete,decisionMilliseconds);
            if(recovered)session.Paused=true;
            acknowledgement=CommitResolvedPhase(session,command,fingerprint);
            resolved=Project(session);teamResolved=ProjectTeam(session);
        }
        await PublishRace(session,resolved,teamResolved);
        if(resolved.Lifecycle!="finished"&&!session.Paused&&!session.DeferNextWindow)
        {
            lock(gate)
            {
                if(session.Lifecycle=="phase-result"&&!session.Paused){OpenHouseholdWindow(session);Persist(session.Id);}
                resolved=Project(session);teamResolved=ProjectTeam(session);
            }
            if(resolved.Lifecycle=="awaiting-decisions")await PublishRace(session,resolved,teamResolved);
        }
        return acknowledgement;
    }    private void CompleteResolution(Session session, IReadOnlyDictionary<string, DriverDecision> complete, double decisionMilliseconds)
    {
            var resolutionStarted = Stopwatch.GetTimestamp();
            var operation=session.WindowKind??"Normal";
            var result = ResolveOperation(session.Engine,operation,complete);
            session.OperationKinds.Add(operation);
            var resolutionMilliseconds = Stopwatch.GetElapsedTime(resolutionStarted).TotalMilliseconds;
            if (result.Finished) session.TotalToFinishMilliseconds = Stopwatch.GetElapsedTime(session.StartedTimestamp).TotalMilliseconds;
            session.PhaseTimings.Add(new(session.Phase, decisionMilliseconds, resolutionMilliseconds, null));
            session.RecordedPhases.Add(new(session.Phase, result.LeaderLap, result.Time, result.Finished, BrowserReplay.OutcomeHash(result)));
            RecordAutomaticResponsibility(session);
            session.AcceptedPlans.Add(new System.Collections.ObjectModel.ReadOnlyDictionary<string, DriverDecision>(complete.ToDictionary()));
            var fresh = result.Events.Skip(session.EngineEventCursor).Select((item, index) => (item, index))
                .Where(pair => pair.item.Kind is RaceEventKind.Overtake or RaceEventKind.PitEntry or RaceEventKind.PitExit or RaceEventKind.TyreChanged or RaceEventKind.Spin or RaceEventKind.Contact or RaceEventKind.MechanicalFailure or RaceEventKind.Retirement or RaceEventKind.LockUp).ToArray();
            session.EngineEventCursor = result.Events.Count;
            var selected = fresh.OrderBy(pair => pair.item.Kind is RaceEventKind.Spin or RaceEventKind.Contact or RaceEventKind.MechanicalFailure or RaceEventKind.Retirement ? 0 : session.Configuration.WetTyres is not null && pair.item.Kind == RaceEventKind.TyreChanged ? 1 : pair.item.Kind == RaceEventKind.Overtake ? 1 : 2)
                .ThenBy(pair => pair.index).Take(12).OrderBy(pair => pair.index).ToArray();
            var precedingIncidents=operation=="SafetyCar"?session.Events.Where(item=>item.Kind is "Spin" or "Contact" or "MechanicalFailure" or "Retirement").ToArray():[];
            session.Events = Array.AsReadOnly(precedingIncidents.Concat(selected.Select(pair => new PublicEvent(Guid.NewGuid().ToString(),
                pair.item.Kind.ToString(), pair.item.Time, pair.item.Lap, pair.item.DriverId, pair.item.OtherDriverId,
                pair.item.Compound?.ToString()))).Take(12).ToArray());
            var overtakes = fresh.Count(pair => pair.item.Kind == RaceEventKind.Overtake);
            var entries = fresh.Count(pair => pair.item.Kind == RaceEventKind.PitEntry);
            var exits = fresh.Count(pair => pair.item.Kind == RaceEventKind.PitExit);
            var changes = fresh.Count(pair => pair.item.Kind == RaceEventKind.TyreChanged);
            session.Presentation = new(Guid.NewGuid().ToString(), session.Phase, fresh.Length, overtakes, entries, exits, changes,
                fresh.Length - selected.Length, fresh.Length == 0 ? "Quiet phase: no overtakes or pit events recorded."
                : $"{overtakes} overtakes · {entries} pit entries · {exits} pit exits · {changes} tyre changes."
                    + (fresh.Length > selected.Length ? $" Showing {selected.Length} highlights of {fresh.Length} recorded events." : ""));
            if(result.WeatherReaction is {} weather)
                session.Presentation=session.Presentation with{Summary=$"Observed {weather.Band} conditions at {weather.Time:F1}s; both-car tyre choices open."
                    +(weather.SafetyCarActive?" Safety Car control continues in the same window.":"")
                    +$" {entries} pit entries · {changes} tyre changes."};
            else if(operation=="SafetyCar")session.Presentation=session.Presentation with{Summary="Safety Car running complete. Racing resumes from current positions with a fresh normal decision window."};
            else if(result.Interruption is {} disruption)session.Presentation=session.Presentation with{Summary=$"Safety Car deployed: {disruption.Kind} observed at {disruption.Time:F1}s. Choose legal pit instructions."};
            session.Lifecycle = result.Finished ? "finished" : "phase-result"; session.Revision++;
            session.PresentationPhases.Add(session.Presentation.Cursor, session.Phase);
    }
    private async Task PublishSafely(PublicSnapshot display, TeamSnapshot team)
    {
        // Delivery is advisory: current snapshots and recorded acknowledgements are authoritative.
        try { await notifications.Publish(display, team); }
        catch (Exception) { }
    }
    private static bool TryChoice<T>(string? text, out T value) where T : struct, Enum => Enum.TryParse(text, out value)
        && Enum.IsDefined(value) && value.ToString() == text;}
