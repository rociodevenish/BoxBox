using Game.Domain;
using Game.Simulation;
using System.Security.Cryptography;
namespace Game.Application;
public sealed class SessionCoordinator(ISessionNotifications notifications)
{
    private sealed class Session(string id, string player, string device, byte[] grantHash,
        RaceConfiguration configuration, Driver[] drivers, int seed)
    {
        public string Id { get; } = id;
        public string Player { get; } = player;
        public string Device { get; } = device;
        public byte[] GrantHash { get; } = grantHash;
        public RaceConfiguration Configuration { get; } = configuration;
        public Driver[] Drivers { get; } = drivers;
        public RaceEngine Engine { get; } = new(configuration, drivers, seed);
        public IReadOnlyDictionary<string, DriverDecision> FrozenOpponents { get; set; } = new Dictionary<string, DriverDecision>();
        public List<IReadOnlyDictionary<string, DriverDecision>> AcceptedPlans { get; } = [];
        public string ScriptVersion { get; } = OpponentScript.Version;
        public Dictionary<Guid, (string Fingerprint, SubmissionAck Ack)> AcceptedCommands { get; } = [];
        public TeamCommand? ConfirmedPlan { get; set; }
        public int Phase { get; set; }
        public long Revision { get; set; } = 1;
        public string Lifecycle { get; set; } = "awaiting-decisions";
        public IReadOnlyList<PublicEvent> Events { get; set; } = [];
    }
    private readonly object gate = new();
    private readonly SemaphoreSlim mutations = new(1, 1);
    private Session? active;
    public SessionCreated Create(CreateSessionInput input)
    {
        var variants = new Dictionary<string, (bool Restricted, bool Mixed)>
        { ["open-equal"] = (false, false), ["restricted-equal"] = (true, false),
          ["open-mixed"] = (false, true), ["restricted-mixed"] = (true, true) };
        if (input is null || input.Preset is null || !variants.TryGetValue(input.Preset, out var variant)
            || input.Laps is not (6 or 10 or 20 or 40 or 50) || input.Seed is null)
            throw new SessionFault(SessionFailure.InvalidInput, "Choose an allowlisted preset, distance (6/10/20/40/50) and designer seed.");
        var capability = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var id = Guid.NewGuid().ToString("N");
        var player = Guid.NewGuid().ToString("N");
        var device = Guid.NewGuid().ToString("N");
        var configuration = new RaceConfiguration(input.Laps.Value, LaboratoryFixtures.SegmentedTrack(variant.Restricted),
            LapVariation: .2).WithExplicitTuning();
        var session = new Session(id, player, device, SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(capability)),
            configuration, LaboratoryFixtures.SegmentedField(variant.Mixed), input.Seed.Value);
        session.FrozenOpponents = OpponentScript.Freeze(session.Engine, session.Drivers.Select(driver => driver.Team).Distinct(), configuration.Laps);
        mutations.Wait();
        try { lock (gate) active = session; }
        finally { mutations.Release(); }
        return new(id, $"/race/display/{id}", $"/race/controller/{id}#capability={capability}",
            player, "team-01", device, capability);
    }
    public PublicSnapshot Public(string id)
    { lock (gate) return Project(Require(id)); }
    private Session Require(string id) => active is { } session && session.Id == id ? session
        : throw new SessionFault(SessionFailure.Unavailable, "Session unavailable. Create a new local playtest.");
    private static PublicSnapshot Project(Session session)
    {
        var state = session.Engine.GetState();
        var tyres = state.Tyres.ToDictionary(t => t.DriverId);
        var drivers = session.Drivers.ToDictionary(d => d.Id);
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
            return new PublicCar(entry.DriverId, "Driver " + entry.DriverId[^2..], driver.Team,
                index + 1, entry.Laps, entry.Progress, gap, ahead, tyre.Compound.ToString(), tyre.Age,
                tyre.InPits ? "in-pit-lane" : "on-track", state.Finished ? state.Eligibility[entry.DriverId] : null);
        }).ToArray();
        return new(session.Id, session.Phase, session.Revision, session.Lifecycle, state.LeaderLap,
            session.Configuration.Laps, state.Time, Array.AsReadOnly(rows), session.Events);
    }
    private static PublicGap ProjectGap(TimingGap gap) => gap.Laps > 0 ? new("laps", Laps: gap.Laps)
        : gap.Seconds is { } seconds ? new("time", seconds) : new("unavailable");
    public TeamSnapshot Team(string id, string? capability)
    { lock (gate) return ProjectTeam(Authorise(id, capability)); }
    private Session Authorise(string id, string? capability)
    {
        var session = Require(id);
        if (capability is null || !CryptographicOperations.FixedTimeEquals(session.GrantHash,
            SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(capability))))
            throw new SessionFault(SessionFailure.Forbidden, "A separate team controller capability is required.");
        return session;
    }
    private static TeamSnapshot ProjectTeam(Session session)
    {
        var observation = session.Engine.GetTeamObservation("team-01");
        var own = observation.TeamTyres.Select(tyre => new PrivateCar(tyre.DriverId, tyre.Compound.ToString(),
            tyre.Age, tyre.EstimatedCondition, tyre.InPits, tyre.StopQueued,
            Array.AsReadOnly(observation.BattleOptions[tyre.DriverId].Select(option => option.ToString()).ToArray()))).ToArray();
        return new(session.Id, session.Phase, session.Revision, session.Lifecycle, session.Player, "team-01",
            session.Device, Array.AsReadOnly(own), session.ConfirmedPlan);
    }
    public async Task<SubmissionAck> Submit(string id, string? capability, TeamCommand? command)
    {
        await mutations.WaitAsync();
        try { return await SubmitSerially(id, capability, command); }
        finally { mutations.Release(); }
    }
    private async Task<SubmissionAck> SubmitSerially(string id, string? capability, TeamCommand? command)
    {
        Session session; Dictionary<string, DriverDecision> complete; PublicSnapshot resolving; TeamSnapshot teamResolving;
        Guid commandId; string fingerprint;
        lock (gate)
        {
            session = Authorise(id, capability);
            if (command is null || !Guid.TryParse(command.CommandId, out commandId) || command.Cars is null || command.Cars.Count != 2
                || !command.Cars.Select(car => car?.DriverId).Order().SequenceEqual(new[] { "driver-01", "driver-02" }))
                throw new SessionFault(SessionFailure.InvalidInput, "Submit exactly both owned cars with a command identity.");
            command = command with { Cars = Array.AsReadOnly(command.Cars.OrderBy(car => car.DriverId).ToArray()) };
            fingerprint = System.Text.Json.JsonSerializer.Serialize(command with { CommandId = commandId.ToString() });
            if (session.AcceptedCommands.TryGetValue(commandId, out var accepted))
                return accepted.Fingerprint == fingerprint ? accepted.Ack
                    : throw new SessionFault(SessionFailure.Conflict, "This command identity was already accepted with different choices.");
            if (session.Lifecycle != "awaiting-decisions" || command.Phase != session.Phase || command.Revision != session.Revision)
                throw new SessionFault(SessionFailure.Conflict, "This decision window is not editable. Refresh the current phase.");
            var observation = session.Engine.GetTeamObservation("team-01");
            complete = session.FrozenOpponents.ToDictionary();
            foreach (var car in command.Cars)
            {
                if (!TryChoice<PaceInstruction>(car.Pace, out var pace) || !TryChoice<BattleInstruction>(car.Battle, out var battle)
                    || car.Pit is not ("Stay Out" or "Pit") || car.Pit == "Stay Out" && car.Compound is not null
                    || car.Pit == "Pit" && !TryChoice<TyreCompound>(car.Compound, out _))
                    throw new SessionFault(SessionFailure.InvalidInput, "Each car needs explicit valid pace, pit and battle choices.");
                var tyre = observation.TeamTyres.Single(tyre => tyre.DriverId == car.DriverId);
                if (!observation.BattleOptions[car.DriverId].Contains(battle)
                    || car.Pit == "Pit" && (tyre.InPits || tyre.StopQueued))
                    throw new SessionFault(SessionFailure.InvalidInput, "A battle or pit instruction is unavailable. Review both cars.");
                complete.Add(car.DriverId, new(pace, car.Pit == "Pit" ? Enum.Parse<TyreCompound>(car.Compound!) : null, battle));
            }
            session.ConfirmedPlan = command;
            session.Lifecycle = "resolving"; session.Revision++;
            resolving = Project(session); teamResolving = ProjectTeam(session);
        }
        await PublishSafely(resolving, teamResolving);
        PublicSnapshot resolved; TeamSnapshot teamResolved;
        lock (gate)
        {
            _ = Require(id);
            var result = session.Engine.ResolvePhase(complete);
            session.AcceptedPlans.Add(new System.Collections.ObjectModel.ReadOnlyDictionary<string, DriverDecision>(complete));
            session.Events = Array.AsReadOnly(result.Events.Select((item, index) => (item, index))
                .Where(pair => pair.item.Kind is RaceEventKind.Overtake or RaceEventKind.PitEntry or RaceEventKind.PitExit or RaceEventKind.TyreChanged)
                .Select(pair => new PublicEvent($"{session.Id}:{pair.index}", pair.item.Kind.ToString(), pair.item.Time,
                    pair.item.Lap, pair.item.DriverId, pair.item.OtherDriverId, pair.item.Compound?.ToString())).ToArray());
            session.Lifecycle = result.Finished ? "finished" : "phase-result"; session.Revision++;
            resolved = Project(session); teamResolved = ProjectTeam(session);
        }
        var acknowledgement = new SubmissionAck(command.CommandId, session.Id, session.Phase, resolved.Revision, resolved.Lifecycle);
        lock (gate) session.AcceptedCommands.Add(commandId, (fingerprint, acknowledgement));
        await PublishSafely(resolved, teamResolved);
        return acknowledgement;
    }
    private async Task PublishSafely(PublicSnapshot display, TeamSnapshot team)
    {
        // Delivery is advisory: current snapshots and recorded acknowledgements are authoritative.
        try { await notifications.Publish(display, team); }
        catch (Exception) { }
    }
    private static bool TryChoice<T>(string? text, out T value) where T : struct, Enum => Enum.TryParse(text, out value)
        && Enum.IsDefined(value) && value.ToString() == text;}
