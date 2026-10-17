using Game.Domain;
namespace Game.Simulation;

public sealed partial class RaceEngine
{
    private sealed partial class Car
    {
        public double ComponentCondition { get; set; } = driver.IncidentProfile?.InitialComponentCondition ?? 1;
        public double Damage { get; set; }
        public double? RetirementTime { get; set; }
        public double RetiredProgress { get; set; }
        public double ErrorExposure { get; set; }
        public double FailureExposure { get; set; }
        public int ErrorOrdinal { get; set; }
        public int FailureOrdinal { get; set; }
        public double LapTyreCondition { get; set; } = 1;
        public double LapComponentCondition { get; set; } = driver.IncidentProfile?.InitialComponentCondition ?? 1;
        public HashSet<string> ContactIncidents { get; } = [];
    }
    private IncidentScenario? incidentScenario;
    private readonly HashSet<int> consumedForcedIncidents = [];
    public RaceEngine(RaceConfiguration configuration, IReadOnlyList<Driver> drivers, int seed, IncidentScenario scenario)
        : this(configuration, drivers, seed)
    {
        if (configuration.Incidents is null || scenario.Events.Any(e => !drivers.Any(d => d.Id == e.DriverId)
            || !double.IsFinite(e.Time) || e.Time < 0 || !Enum.IsDefined(e.Kind)))
            throw new ArgumentException("Diagnostic scenarios require opt-in incidents and valid events.");
        incidentScenario = new(scenario.Events.ToArray());
    }
    private static bool Active(Car car) => car.FinishTime is null && car.RetirementTime is null;
    private void ValidateIncidents(IReadOnlyList<Driver> drivers)
    {
        if (configuration.Incidents is not { } tuning) return;
        if (tuning.Version != "local-incidents-v1" || new[] { tuning.ErrorRate, tuning.ContactProbability,
            tuning.FailureRate, tuning.ComponentWearPerSecond, tuning.MechanicalStress }.Any(v => !double.IsFinite(v) || v < 0)
            || tuning.ContactProbability > 1 || tuning.MechanicalStress > 2 || tuning.ErrorRate > .01
            || tuning.FailureRate > 1 || tuning.ComponentWearPerSecond > 1
            || drivers.Any(d => d.IncidentProfile is { } p && (!double.IsFinite(p.ErrorControl)
                || p.ErrorControl is < 1 or > 100 || !double.IsFinite(p.Reliability) || p.Reliability is < 1 or > 100
                || !double.IsFinite(p.InitialComponentCondition) || p.InitialComponentCondition is < 0 or > 1)))
            throw new ArgumentException("Invalid versioned incident tuning/profile.");
    }
    private double Demand(Car car) => car.Pace switch { PaceInstruction.Push => 1.8, PaceInstruction.Conserve => .55, _ => 1 };
    // Inputs are sampled at lap entry: splitting equivalent geometry cannot create opportunities or change rates.
    private double ErrorRate(Car car) => configuration.Incidents is { } t ? t.ErrorRate * Demand(car)
        * (2 - (car.Driver.IncidentProfile?.ErrorControl ?? 82) / 100) * (2 - car.LapTyreCondition) : 0;
    private double FailureRate(Car car) => configuration.Incidents is { } t ? t.FailureRate * Demand(car)
        * t.MechanicalStress * (2 - (car.Driver.IncidentProfile?.Reliability ?? 82) / 100)
        * (2 - car.LapComponentCondition) : 0;
    private double IncidentDraw(Car car, string channel, int ordinal, string? encounter = null)
    {
        var scope = $"local-incidents-v1:{channel}:{ordinal}:{encounter}";
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{seed}:{Uri.EscapeDataString(car.Driver.Id)}:{scope}"));
        var unit = (System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(bytes) + .5) / 4294967296d;
        RecordRandom($"local-incidents-v1/{channel}", car.Driver.Id, scope, unit);
        return unit;
    }
    private double Threshold(Car car, bool failure) => -Math.Log(IncidentDraw(car,
        failure ? "mechanical" : "error", failure ? car.FailureOrdinal : car.ErrorOrdinal));
    private double NextIncidentTime(Car car)
    {
        if (configuration.Incidents is null || !Active(car)) return double.PositiveInfinity;
        var next = double.PositiveInfinity;
        if (car.PitExit is null && time >= car.Driver.Gap)
        {
            if (ErrorRate(car) > 0) next = time + Math.Max(0, Threshold(car, false) - car.ErrorExposure) / ErrorRate(car);
            if (FailureRate(car) > 0) next = Math.Min(next, time + Math.Max(0, Threshold(car, true) - car.FailureExposure) / FailureRate(car));
        }
        if (incidentScenario is { } scenario)
            for (var i = 0; i < scenario.Events.Count; i++)
                if (!consumedForcedIncidents.Contains(i) && scenario.Events[i] is { Kind: not IncidentKind.Contact } e && e.DriverId == car.Driver.Id)
                    next = Math.Min(next, Math.Max(time, e.Time));
        return next;
    }
    private void AdvanceIncidentExposure(double until)
    {
        if (configuration.Incidents is not { } tuning) return;
        foreach (var car in cars.Where(c => Active(c) && c.PitExit is null))
        {
            var elapsed = Math.Max(0, until - Math.Max(time, car.Driver.Gap));
            car.ErrorExposure += elapsed * ErrorRate(car);
            car.FailureExposure += elapsed * FailureRate(car);
            car.ComponentCondition = Math.Max(0, car.ComponentCondition - elapsed * tuning.ComponentWearPerSecond * Demand(car) * tuning.MechanicalStress);
        }
    }
    private void ResolveIncident(Car car)
    {
        if (incidentScenario is { } scenario)
            for (var i = 0; i < scenario.Events.Count; i++)
                if (!consumedForcedIncidents.Contains(i) && scenario.Events[i] is { Kind: not IncidentKind.Contact } e && e.DriverId == car.Driver.Id && e.Time <= time + 1e-8)
                { consumedForcedIncidents.Add(i); ApplyIncident(car, e.Kind, $"forced:{i}"); return; }
        if (FailureRate(car) > 0 && car.FailureExposure >= Threshold(car, true) - 1e-9)
        { car.FailureExposure = 0; car.FailureOrdinal++; ApplyIncident(car, IncidentKind.MechanicalFailure, $"mechanical:{car.FailureOrdinal}"); }
        else if (ErrorRate(car) > 0 && car.ErrorExposure >= Threshold(car, false) - 1e-9)
        {
            car.ErrorExposure = 0;
            var spin = IncidentDraw(car, "severity", car.ErrorOrdinal++) < .25;
            ApplyIncident(car, spin ? IncidentKind.Spin : IncidentKind.LockUp, $"error:{car.ErrorOrdinal}");
        }
    }
    private void ApplyIncident(Car car, IncidentKind kind, string identity, Car? other = null)
    {
        if (!Active(car)) return;
        if (configuration.Incidents is { InterruptMajorIncidents: true } && kind != IncidentKind.LockUp && !chequered)
            interruption ??= new($"local-incidents-v1:{Uri.EscapeDataString(car.Driver.Id)}:{identity}", time, car.Driver.Id, kind);
        ConsumeTyres(car, Progress(car));
        car.Trace.Add((time, Distance(car)));
        var loss = kind switch { IncidentKind.LockUp => 2d, IncidentKind.Spin => 8, IncidentKind.Contact => 5, _ => 0 };
        history.Add(new(time, car.Driver.Id, car.Laps + 1, kind switch {
            IncidentKind.LockUp => RaceEventKind.LockUp, IncidentKind.Spin => RaceEventKind.Spin,
            IncidentKind.Contact => RaceEventKind.Contact, _ => RaceEventKind.MechanicalFailure },
            loss, other?.Driver.Id, DistanceMetres: Distance(car), EncounterId: $"local-incidents-v1:{identity}"));
        if (kind == IncidentKind.MechanicalFailure)
        {
            car.RetiredProgress = Math.Max(0, Progress(car)); car.RetirementTime = time;
            car.PendingPit = null; car.PitExit = null; car.PassTime = null; car.Following = null;
            history.Add(new(time, car.Driver.Id, car.Laps, RaceEventKind.Retirement, DistanceMetres: Distance(car)));
        }
        else
        {
            car.Condition = Math.Max(0, car.Condition - (kind == IncidentKind.LockUp ? .03 : .08));
            var addedDamage = Math.Min(4 - car.Damage, kind == IncidentKind.LockUp ? 0 : 1);
            car.Damage += addedDamage;
            Delay(car, loss + addedDamage * SegmentShare(car) * (1 - LocalProgress(car)));
            car.FreeDuration = car.LapEnd - car.LapStart;
        }
    }
    private void ContactIncident(Car car, Car other, string identity)
    {
        if (configuration.Incidents is not { } tuning || !car.ContactIncidents.Add(identity)) return;
        var forced = false;
        if (incidentScenario is { } scenario)
            for (var i = 0; i < scenario.Events.Count; i++)
                if (!consumedForcedIncidents.Contains(i) && scenario.Events[i] is { Kind: IncidentKind.Contact } e && e.DriverId == car.Driver.Id && e.Time <= time)
                { consumedForcedIncidents.Add(i); forced = true; break; }
        var chance = Math.Clamp(tuning.ContactProbability * (car.Battle == BattleInstruction.Attack ? 1.8 : 1)
            * (other.Battle == BattleInstruction.Defend ? 1.5 : 1), 0, 1);
        if (forced || chance > 0 && IncidentDraw(car, "contact", 0, identity) < chance)
        { ApplyIncident(car, IncidentKind.Contact, identity, other); ApplyIncident(other, IncidentKind.Contact, identity, car); }
    }
    private IReadOnlyList<CarCondition>? Conditions() => configuration.Incidents is null ? null : cars.Select(c =>
        new CarCondition(c.Driver.Id, c.Damage, c.RetirementTime is not null, c.RetirementTime)).ToArray();
    public IReadOnlyList<IncidentDiagnostic> GetDesignerIncidentDiagnostics() => cars.Select(c =>
        new IncidentDiagnostic(c.Driver.Id, c.ComponentCondition, c.ErrorExposure, c.FailureExposure)).ToArray();
}
