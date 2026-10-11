using Game.Domain;

namespace Game.Simulation;

public sealed class RaceEngine
{
    private sealed class Car(Driver driver)
    {
        public Driver Driver { get; } = driver;
        public int Laps { get; set; }
        public int SegmentIndex { get; set; }
        public double LapPenalty { get; set; }
        public double RaceFormSeconds { get; set; }
        public double LapVariationSeconds { get; set; }
        public int ContactOrder { get; set; }
        public double SegmentEntryTime { get; set; } = driver.Gap;
        public Dictionary<string, bool> Opportunities { get; } = [];
        public HashSet<string> PassedOpportunities { get; } = [];
        public double LapStart { get; set; } = driver.Gap;
        public double LapEnd { get; set; }
        public double? FinishTime { get; set; }
        public TyreCompound Compound { get; set; } = driver.StartingCompound;
        public int TyreAge { get; set; }
        public int Stops { get; set; }
        public double Condition { get; set; } = 1;
        public PaceInstruction Pace { get; set; }
        public BattleInstruction Battle { get; set; }
        public double WearProgress { get; set; }
        public TyreCompound? PendingPit { get; set; }
        public double? PitExit { get; set; }
        public HashSet<TyreCompound> UsedCompounds { get; } = [driver.StartingCompound];
        public Car? Following { get; set; }
        public double FreeDuration { get; set; }
        public SegmentProjection? FreeProjection { get; set; }
        public double? PassTime { get; set; }
        public string? PassTarget { get; set; }
        public List<(double Time, double Distance)> Trace { get; } = [];
    }

    private readonly RaceConfiguration configuration;
    private readonly int seed;
    private readonly List<Car> cars;
    private readonly List<RaceEvent> history = [];
    private readonly List<DecisionRecord> decisionHistory = [];
    private readonly List<RandomDiagnostic> randomDiagnostics = [];
    private readonly HashSet<string> diagnosticScopes = [];
    private StrategyEvaluator? strategies;
    private int phaseIndex;
    private double time;
    private bool chequered;
    private IReadOnlyList<TrackSegment>? Segments => configuration.Track.Segments;
    private double TrackLength => Segments?.Sum(s => s.Length) ?? 1;
    private double SegmentStart(Car car) => Segments?.Take(car.SegmentIndex).Sum(s => s.Length) / TrackLength ?? 0;
    private double SegmentShare(Car car) => Segments is { } layout ? layout[car.SegmentIndex].Length / TrackLength : 1;
    private double LocalProgress(Car car) => car.FinishTime is not null || car.PitExit is not null ? 0
        : Math.Clamp((time - car.LapStart) / (car.LapEnd - car.LapStart), 0, 1);
    private TyreTuning Tyres => configuration.Tyres ?? new TyreTuning();
    private TrafficTuning Traffic => configuration.Traffic ?? new TrafficTuning();

    public RaceEngine(RaceConfiguration configuration, IReadOnlyList<Driver> drivers, int seed)
    {
        if (configuration is null || configuration.Track is null || drivers is null
            || string.IsNullOrWhiteSpace(configuration.Track.Name) || drivers.Any(d => d is null))
            throw new ArgumentException("Race configuration, track and drivers are required.");
        var tyres = configuration.Tyres ?? new TyreTuning();
        var tyreValues = new[] { tyres.DegradationPerLap, tyres.WarmupPenalty, tyres.DegradationPenalty,
            tyres.ExhaustionPenalty, tyres.PushGain, tyres.PushWear, tyres.ConserveLoss, tyres.ConserveWear,
            tyres.SoftGain, tyres.SoftWear, tyres.HardLoss, tyres.HardWear, tyres.SoftWarmup, tyres.HardWarmup };
        if (configuration.Laps < 1 || !double.IsFinite(configuration.Track.BaseLapTime)
            || configuration.Track.BaseLapTime <= 0 || !double.IsFinite(configuration.Track.SpeedWeight)
            || configuration.Track.SpeedWeight is < 0 or > 1
            || !double.IsFinite(configuration.AttributeSeconds) || configuration.AttributeSeconds < 0
            || !double.IsFinite(configuration.LapVariation) || configuration.LapVariation < 0
            || !double.IsFinite(configuration.RaceFormVariation) || configuration.RaceFormVariation < 0
            || !double.IsFinite(configuration.PassingVariation) || configuration.PassingVariation is < 0 or > 1
            || !double.IsFinite(configuration.Track.PitLaneLoss) || configuration.Track.PitLaneLoss < 0
            || !double.IsFinite(configuration.Track.StationaryStop) || configuration.Track.StationaryStop < 0
            || !double.IsFinite(configuration.StopVariation) || configuration.StopVariation < 0
            || configuration.StopVariation > configuration.Track.StationaryStop * 2
            || !double.IsFinite(configuration.Track.PitLaneLoss + configuration.Track.StationaryStop + configuration.StopVariation)
            || !double.IsFinite(configuration.EstimateError) || configuration.EstimateError is < 0 or > 1
            || !double.IsFinite(configuration.Track.PassingChance) || configuration.Track.PassingChance is < 0 or > 1
            || (configuration.Traffic ?? new()).AttackMultiplier < 1 || (configuration.Traffic ?? new()).DefendMultiplier > 1
            || new[] { (configuration.Traffic ?? new()).FollowingLoss, (configuration.Traffic ?? new()).BlueFlagLoss,
                (configuration.Traffic ?? new()).BattleProximity, (configuration.Traffic ?? new()).AttackMultiplier,
                (configuration.Traffic ?? new()).DefendMultiplier, (configuration.Traffic ?? new()).BattleWear }.Any(v => !double.IsFinite(v) || v < 0)
            || drivers.Count == 0 || drivers.Select(d => d.Id).Distinct().Count() != drivers.Count
            || drivers.Any(d => string.IsNullOrWhiteSpace(d.Id) || string.IsNullOrWhiteSpace(d.Team)
                || !double.IsFinite(d.Pace) || !double.IsFinite(d.Speed) || !double.IsFinite(d.Cornering)
                || !double.IsFinite(d.Gap) || d.Gap < 0 || !Enum.IsDefined(d.StartingCompound)
                || !double.IsFinite(d.TyrePreservation) || !double.IsFinite(d.TyreManagement) || !double.IsFinite(d.Racecraft))
            || tyreValues.Any(v => !double.IsFinite(v) || v < 0))
            throw new ArgumentException("Invalid race configuration or starting field.");
        if (configuration.Track.Segments is { } segments)
        {
            if (segments.Count == 0 || segments.Any(s => s is null)
                || segments.Select(s => s.Id).Distinct().Count() != segments.Count)
                throw new ArgumentException("A track needs unique ordered segments.");
            foreach (var segment in segments) segment.Validate();
            var zoneGroups = segments.GroupBy(s => s.PassingZoneId ?? s.Id);
            foreach (var group in zoneGroups)
            {
                var first = group.First();
                if (group.Any(s => s.PassingAllowed != first.PassingAllowed || s.PassingDifficulty != first.PassingDifficulty
                    || s.SpeedDemand != first.SpeedDemand || s.CorneringDemand != first.CorneringDemand
                    || s.TyreDemand != first.TyreDemand
                    || Math.Abs(s.Length / s.BaselineTime - first.Length / first.BaselineTime) > 1e-8))
                    throw new ArgumentException("Shared zones must describe equivalent subdivisions.");
                var entries = Enumerable.Range(0, segments.Count).Count(i =>
                    (segments[i].PassingZoneId ?? segments[i].Id) == group.Key
                    && (i == 0 || (segments[i - 1].PassingZoneId ?? segments[i - 1].Id) != group.Key));
                var wraps = (segments[0].PassingZoneId ?? segments[0].Id) == group.Key
                    && (segments[^1].PassingZoneId ?? segments[^1].Id) == group.Key;
                if (entries > (wraps ? 2 : 1))
                    throw new ArgumentException("A passing zone must be contiguous around the lap.");
            }
            if (!double.IsFinite(segments.Sum(s => s.Length)) || !double.IsFinite(segments.Sum(s => s.BaselineTime)))
                throw new ArgumentException("Track totals must be finite.");
            configuration = configuration with { Track = configuration.Track with
                { Segments = Array.AsReadOnly(segments.ToArray()) } };
        }
        this.configuration = configuration;
        this.seed = seed;
        ValidateDerivedConfiguration(drivers);
        cars = drivers.Select(d => new Car(d)).ToList();
        var ordered = cars.OrderBy(c => c.Driver.Gap).ThenBy(c => c.Driver.Id, StringComparer.Ordinal).ToArray();
        for (var i = 0; i < ordered.Length; i++) ordered[i].ContactOrder = i;
        foreach (var car in cars)
        {
            _ = Wear(car);
            car.RaceFormSeconds = RandomOffset(RandomChannel.RaceForm, "race-form", car.Driver.Id, 0, "race", configuration.RaceFormVariation);
            car.LapVariationSeconds = RandomOffset(RandomChannel.LapVariation, "lap-variation", car.Driver.Id, 1, "lap:1", configuration.LapVariation);
            car.LapPenalty = TyrePenalty(car);
            car.LapEnd = car.LapStart + LapTime(car);
            car.FreeDuration = car.LapEnd - car.LapStart;
            car.Trace.Add((0, Distance(car)));
        }
    }

    public PhaseResult ResolvePhase(IReadOnlyDictionary<string, DriverDecision>? decisions = null)
    {
        if (decisions is not null)
            foreach (var (id, decision) in decisions)
            {
                if (cars.All(c => c.Driver.Id != id) || decision is null) throw new ArgumentException("Invalid driver decision.");
                decision.Validate();
            }
        foreach (var car in cars)
        {
            var decision = decisions?.GetValueOrDefault(car.Driver.Id);
            if (decision is null) continue;
            if (!AvailableBattles(car).Contains(decision.Battle))
                throw new ArgumentException($"Battle instruction is unavailable for {car.Driver.Id}.");
            if (decision.PitCompound is not null && (car.PitExit is not null
                || car.PendingPit is not null && car.PendingPit != decision.PitCompound))
                throw new ArgumentException("A queued or ongoing stop cannot be replaced.");
        }
        foreach (var car in cars.Where(c => c.FinishTime is null))
        {
            var fraction = Progress(car);
            car.Trace.Add((time, Distance(car)));
            car.PassTime = null;
            if (Segments is null) car.Following = null;
            ConsumeTyres(car, fraction);
            car.Pace = decisions?.GetValueOrDefault(car.Driver.Id)?.Pace ?? PaceInstruction.Standard;
            car.Battle = decisions?.GetValueOrDefault(car.Driver.Id)?.Battle ?? BattleInstruction.Neutral;
            if (car.Battle != BattleInstruction.Neutral)
                history.Add(new RaceEvent(time, car.Driver.Id, car.Laps, RaceEventKind.BattleDecision, (int)car.Battle));
            var requestedPit = decisions?.GetValueOrDefault(car.Driver.Id)?.PitCompound;
            car.PendingPit ??= requestedPit;
            car.LapPenalty = TyrePenalty(car);
            var duration = LapTime(car);
            car.FreeDuration = duration;
            car.LapStart = car.PitExit ?? (time < car.Driver.Gap ? car.Driver.Gap : time - LocalProgress(car) * duration);
            car.LapEnd = car.LapStart + duration;
        }
        UpdateTraffic();
        var target = Math.Min(configuration.Laps, cars.Max(c => c.Laps) + 5);
        while (cars.Any(c => c.FinishTime is null))
        {
            var car = cars.Where(c => c.FinishTime is null).OrderBy(NextEvent)
                .ThenBy(c => Segments is null ? 0 : c.ContactOrder)
                .ThenBy(c => c.Driver.Id, StringComparer.Ordinal).First();
            // Complete every event at the boundary timestamp before accepting new decisions.
            if (!chequered && cars.Max(c => c.Laps) >= target && NextEvent(car) > time + 1e-8) break;
            var nextTime = NextEvent(car);
            foreach (var current in cars.Where(c => c.Following is not null && c.FinishTime is null && c.PitExit is null))
            {
                var loss = (nextTime - time) * Math.Max(0, 1 - current.FreeDuration / (current.LapEnd - current.LapStart));
                if (loss > 0) history.Add(new RaceEvent(nextTime, current.Driver.Id, current.Laps + 1, RaceEventKind.TrafficLoss, loss, SegmentId: Segments?[current.SegmentIndex].Id));
            }
            var starting = Segments is not null && car.Laps == 0 && time < car.Driver.Gap && nextTime == car.Driver.Gap;
            time = nextTime;
            foreach (var current in cars) current.Trace.Add((time, Distance(current)));
            if (starting) { UpdateTraffic(); continue; }
            if (car.PassTime == time)
            {
                ResolveEncounter(car);
                car.PassTime = null;
                UpdateTraffic();
                continue;
            }
            if (car.PitExit is not null)
            {
                car.PitExit = null;
                if (Segments is not null)
                {
                    var rejoinDistance = Distance(car);
                    var contact = cars.Where(c => c != car && c.PitExit is null && c.FinishTime is null
                        && SameCoordinate(Distance(c), rejoinDistance))
                        .OrderByDescending(c => c.ContactOrder).FirstOrDefault();
                    if (contact is not null) { MoveBehind(car, contact); car.Following = contact; }
                }
                history.Add(new RaceEvent(time, car.Driver.Id, car.Laps, RaceEventKind.PitExit,
                    SegmentId: Segments?[0].Id, DistanceMetres: Segments is null ? null : car.Laps * TrackLength));
                UpdateTraffic();
                continue;
            }
            if (Segments is { } layout)
            {
                ConsumeTyres(car, SegmentStart(car) + SegmentShare(car));
                history.Add(new RaceEvent(time, car.Driver.Id, car.Laps + 1, RaceEventKind.SegmentExited,
                    SegmentId: layout[car.SegmentIndex].Id,
                    DistanceMetres: (car.Laps + SegmentStart(car) + SegmentShare(car)) * TrackLength));
                if (car.SegmentIndex < layout.Count - 1)
                {
                    car.SegmentIndex++;
                    car.SegmentEntryTime = time;
                    car.LapStart = time;
                    car.LapEnd = time + LapTime(car);
                    car.FreeDuration = car.LapEnd - car.LapStart;
                    UpdateTraffic();
                    continue;
                }
            }
            if (cars.Any(c => c.FinishTime is null && c.LapEnd == time
                && c.Laps + 1 >= configuration.Laps && (Segments is null || c.SegmentIndex == Segments.Count - 1))) chequered = true;
            // Contact ordering alone cannot give a queue simultaneous chequered crossings.
            // Move trailing cars back along their live trajectory before recording lap completion.
            if (Segments is not null && chequered && car.SegmentIndex == Segments.Count - 1)
            {
                var queue = cars.Where(other => other != car && other.FinishTime is null
                    && other.PitExit is null && other.Following is not null && other.Laps == car.Laps
                    && other.SegmentIndex == car.SegmentIndex && other.ContactOrder > car.ContactOrder
                    && SameCoordinate(Distance(other), Distance(car)))
                    .OrderBy(other => other.ContactOrder).ToArray();
                for (var index = 0; index < queue.Length; index++)
                {
                    var separation = (index + 1) * Traffic.FollowingLoss;
                    queue[index].LapStart += separation;
                    queue[index].LapEnd += separation;
                    queue[index].Following = null;
                }
            }
            ConsumeTyres(car, 1);
            car.Laps++;
            car.SegmentIndex = 0;
            if (Segments is null) car.Following = null;
            car.WearProgress = 0;
            car.TyreAge++;
            history.Add(new RaceEvent(time, car.Driver.Id, car.Laps, RaceEventKind.TyreCondition, car.Condition));
            history.Add(new RaceEvent(time, car.Driver.Id, car.Laps, RaceEventKind.LapCompleted,
                SegmentId: Segments?[^1].Id, DistanceMetres: Segments is null ? null : car.Laps * TrackLength));
            if (car.Laps >= configuration.Laps) chequered = true;
            if (chequered) car.FinishTime = time;
            else
            {
                car.LapVariationSeconds = RandomOffset(RandomChannel.LapVariation, "lap-variation",
                    car.Driver.Id, car.Laps + 1, $"lap:{car.Laps + 1}", configuration.LapVariation);
                if (car.PendingPit is { } compound)
                {
                    car.Stops++;
                    var service = configuration.Track.StationaryStop
                        + RandomOffset(RandomChannel.PitService, "pit-service", car.Driver.Id,
                            Segments is null ? car.Laps : car.Stops,
                            Segments is null ? $"lap:{car.Laps}" : $"stop:{car.Stops}", configuration.StopVariation);
                    var loss = configuration.Track.PitLaneLoss + service;
                    history.Add(new RaceEvent(time, car.Driver.Id, car.Laps, RaceEventKind.PitEntry, loss,
                        SegmentId: Segments?[0].Id, DistanceMetres: Segments is null ? null : car.Laps * TrackLength));
                    history.Add(new RaceEvent(time, car.Driver.Id, car.Laps, RaceEventKind.PitService, service,
                        SegmentId: Segments?[0].Id, DistanceMetres: Segments is null ? null : car.Laps * TrackLength));
                    car.Compound = compound;
                    car.UsedCompounds.Add(compound);
                    car.TyreAge = 0;
                    car.Condition = 1;
                    car.PendingPit = null;
                    car.PitExit = time + loss;
                    history.Add(new RaceEvent(time, car.Driver.Id, car.Laps, RaceEventKind.TyreChanged, Compound: compound));
                }
                car.LapPenalty = TyrePenalty(car);
                car.SegmentEntryTime = car.PitExit ?? time;
                car.LapStart = car.PitExit ?? time;
                car.LapEnd = car.LapStart + LapTime(car);
                car.FreeDuration = car.LapEnd - car.LapStart;
            }
            UpdateTraffic();
        }
        foreach (var car in cars.Where(c => c.FinishTime is null)) ConsumeTyres(car, Progress(car));
        phaseIndex++;
        return Snapshot();
    }

    public PhaseResult ResolvePhase(StrategyPlan plan)
    {
        var evaluator = strategies ?? new StrategyEvaluator(plan, cars.Select(c => c.Driver.Id).ToArray(), configuration.Laps);
        evaluator.RequireSamePlan(plan);
        var observations = cars.Select(c => c.Driver.Team).Distinct().Select(GetTeamObservation).ToArray();
        var evaluation = evaluator.Evaluate(phaseIndex, observations);
        _ = ResolvePhase(evaluation.Choices);
        evaluator.Accept(evaluation);
        strategies = evaluator;
        decisionHistory.AddRange(evaluation.Records);
        return Snapshot();
    }
    public TeamObservation GetTeamObservation(string team)
    {
        if (cars.All(c => c.Driver.Team != team)) throw new ArgumentException("Unknown team.");
        var state = Snapshot();
        var own = cars.Where(c => c.Driver.Team == team).Select(c => new EstimatedTyreState(c.Driver.Id,
            c.Compound, c.TyreAge, Math.Clamp(c.Condition + RandomOffset(RandomChannel.EstimateError, "estimate-error", c.Driver.Id, phaseIndex,
                $"phase:{phaseIndex}", 2 * configuration.EstimateError), 0, 1), c.PitExit is not null, c.PendingPit is not null, Array.AsReadOnly(c.UsedCompounds.Order().ToArray()), c.PendingPit)).ToArray();
        return new TeamObservation(time, state.LeaderLap, state.Classification, own,
            state.BattleOptions.Where(kv => own.Any(t => t.DriverId == kv.Key)).ToDictionary());
    }

    private void UpdateTraffic()
    {
        if (!Traffic.Enabled) return;
        if (Segments is not null)
        {
            // Apply all trajectories at a timestamp before deriving entry encounters from them.
            if (cars.Any(c => c.FinishTime is null && NextEvent(c) <= time + 1e-8)) return;
            UpdateSegmentTraffic();
            return;
        }
        var positions = cars.ToDictionary(c => c, Distance);
        foreach (var car in cars.OrderByDescending(c => positions[c]))
        {
            car.PassTime = null;
            if (car.PitExit is not null || car.FinishTime is not null) continue;
            SetDuration(car, car.FreeDuration);
            if (car.Following is { } followed)
            {
                var gap = positions[followed] - positions[car];
                if (followed.PitExit is not null || followed.FinishTime is not null || gap < -1e-7
                    || gap * configuration.Track.BaseLapTime > 2)
                    car.Following = null;
                else SetDuration(car, Math.Max(car.FreeDuration, followed.LapEnd - followed.LapStart + Traffic.FollowingLoss));
            }
            if (car.Following is not null) continue;
            var ahead = cars.Where(c => c != car && c.PitExit is null && c.FinishTime is null)
                .Select(c => (Car: c, Gap: PhysicalGap(positions[c] - positions[car])))
                .Where(c => c.Gap > 1e-7 && c.Gap < 1 - 1e-7)
                .OrderBy(c => c.Gap).FirstOrDefault();
            if (ahead.Car is null) continue;
            var relativeSpeed = 1 / (car.LapEnd - car.LapStart) - 1 / (ahead.Car.LapEnd - ahead.Car.LapStart);
            if (relativeSpeed <= 0) continue;
            var catchTime = time + ahead.Gap / relativeSpeed;
            if (catchTime > time + 1e-7 && catchTime < car.LapEnd)
            { car.PassTime = catchTime; car.PassTarget = ahead.Car.Driver.Id; }
        }
    }

    private SegmentProjection Projection(Car car)
    {
        var segment = Segments![car.SegmentIndex];
        var projection = car.FreeProjection ?? throw new InvalidOperationException("Segment trajectory is unresolved.");
        // Following is a sequencing constraint applied to the same free segment trajectory.
        var constrainedDuration = car.LapEnd - car.LapStart;
        return projection with { PositionMetres = LocalProgress(car) * segment.Length,
            SpeedMetresPerSecond = segment.Length / constrainedDuration,
            ExitAfterSeconds = Math.Max(0, car.LapEnd - time) };
    }

    private void UpdateSegmentTraffic()
    {
        var positions = cars.ToDictionary(c => c, Distance);
        foreach (var car in cars.OrderByDescending(c => Math.Round(positions[c], 6)).ThenBy(c => c.ContactOrder))
        {
            if (car.PassTime is { } pending && pending <= time + 1e-8) continue;
            car.PassTime = null;
            if (car.PitExit is not null || car.FinishTime is not null || time < car.Driver.Gap) continue;
            SetDuration(car, car.FreeDuration);
            if (car.Following is not null)
            {
                var nearestContact = cars.Where(c => c != car && c.PitExit is null && c.FinishTime is null
                    && SameCoordinate(positions[c], positions[car]) && c.ContactOrder < car.ContactOrder)
                    .OrderByDescending(c => c.ContactOrder).FirstOrDefault();
                if (nearestContact is not null) car.Following = nearestContact;
            }
            if (car.Following is { } followed)
            {
                if (followed.PitExit is not null || followed.FinishTime is not null || !SameCoordinate(positions[followed], positions[car]))
                    car.Following = null;
                else
                {
                    if (AttemptSegmentPass(car, followed)) car.Following = null;
                    var speed = Projection(followed).SpeedMetresPerSecond;
                    var free = Projection(car).SpeedMetresPerSecond;
                    if (free > speed && car.Following is not null)
                        SetDuration(car, Segments![car.SegmentIndex].Length / speed);
                    else car.Following = null;
                }
            }
            if (car.Following is not null) continue;
            while (true)
            {
                var contact = cars.Where(c => c != car && c.PitExit is null && c.FinishTime is null
                    && time >= c.Driver.Gap && SameCoordinate(positions[c], positions[car])
                    && c.ContactOrder < car.ContactOrder)
                    .OrderByDescending(c => c.ContactOrder).FirstOrDefault();
                if (contact is null || Projection(car).SpeedMetresPerSecond <= Projection(contact).SpeedMetresPerSecond) break;
                if (AttemptSegmentPass(car, contact)) continue;
                car.Following = contact;
                SetDuration(car, Segments![car.SegmentIndex].Length / Projection(contact).SpeedMetresPerSecond);
                break;
            }
            if (car.Following is not null) continue;
            var ahead = cars.Where(c => c != car && c.PitExit is null && c.FinishTime is null && time >= c.Driver.Gap)
                .Select(c => (Car: c, Gap: (positions[c] - positions[car]) % TrackLength))
                .Select(c => (c.Car, Gap: c.Gap < 0 ? c.Gap + TrackLength : c.Gap))
                .Where(c => c.Gap > 1e-6 && c.Gap < TrackLength - 1e-6)
                .OrderBy(c => c.Gap).FirstOrDefault();
            if (ahead.Car is null) continue;
            if (Math.Abs(time - car.SegmentEntryTime) <= 1e-8
                && ahead.Gap / Projection(car).SpeedMetresPerSecond <= Traffic.BattleProximity)
                _ = AttemptSegmentPass(car, ahead.Car, false);
            var catchAfter = SegmentSimulation.PredictCatch(Projection(car), Projection(ahead.Car), ahead.Gap);
            if (catchAfter is > 1e-8)
            { car.PassTime = time + catchAfter.Value; car.PassTarget = ahead.Car.Driver.Id; }
        }
    }

    private bool SameCoordinate(double first, double second)
    {
        var separation = Math.Abs(first - second) % TrackLength;
        return separation <= 1e-6 || TrackLength - separation <= 1e-6;
    }

    private void MoveBehind(Car car, Car other)
    {
        if (car.ContactOrder >= other.ContactOrder) return;
        var destination = other.ContactOrder;
        foreach (var current in cars.Where(c => c != car && c.ContactOrder > car.ContactOrder && c.ContactOrder <= destination))
            current.ContactOrder--;
        car.ContactOrder = destination;
    }

    private void MoveAhead(Car car, Car other)
    {
        if (car.ContactOrder <= other.ContactOrder) return;
        var destination = other.ContactOrder;
        foreach (var current in cars.Where(c => c != car && c.ContactOrder >= destination && c.ContactOrder < car.ContactOrder))
            current.ContactOrder++;
        car.ContactOrder = destination;
    }

    private string Opportunity(Car car, Car other)
    {
        var segment = Segments![car.SegmentIndex];
        return string.Join("|", Uri.EscapeDataString(car.Driver.Id), Uri.EscapeDataString(other.Driver.Id),
            Visit(car), Uri.EscapeDataString(segment.PassingZoneId ?? segment.Id));
    }

    private int Visit(Car car)
    {
        var zone = Segments![car.SegmentIndex].PassingZoneId ?? Segments[car.SegmentIndex].Id;
        var firstZone = Segments[0].PassingZoneId ?? Segments[0].Id;
        var lastZone = Segments[^1].PassingZoneId ?? Segments[^1].Id;
        var wraps = firstZone == lastZone && Segments.Any(s => (s.PassingZoneId ?? s.Id) != firstZone);
        var prefix = Segments.Take(car.SegmentIndex + 1).All(s => (s.PassingZoneId ?? s.Id) == zone);
        return car.Laps + 1 - (wraps && prefix ? 1 : 0);
    }

    private bool AttemptSegmentPass(Car car, Car other, bool caught = true)
    {
        if (other.PitExit is not null || other.FinishTime is not null || car.FinishTime is not null) return false;
        var segment = Segments![car.SegmentIndex];
        var identity = Opportunity(car, other);
        if (caught && Distance(car) - Distance(other) > TrackLength * .5)
        {
            if (car.PassedOpportunities.Add($"blue:{identity}"))
            {
                Delay(car, Traffic.BlueFlagLoss);
                car.FreeDuration = car.LapEnd - car.LapStart;
                history.Add(new RaceEvent(time, car.Driver.Id, car.Laps + 1, RaceEventKind.BlueFlag,
                    Traffic.BlueFlagLoss, other.Driver.Id, SegmentId: segment.Id,
                    DistanceMetres: Distance(car), EncounterId: identity));
            }
            MoveAhead(car, other);
            return true;
        }
        if (!segment.PassingAllowed) return false;
        if (!car.Opportunities.TryGetValue(identity, out var success))
        {
            var carSpeed = segment.Length / car.FreeDuration;
            var otherSpeed = Segments[other.SegmentIndex].Length / other.FreeDuration;
            var advantage = Math.Max(0, 1 - otherSpeed / carSpeed) * Segments.Sum(s => s.BaselineTime);
            if (advantage <= 0) return false;
            ChargeBattle(car, BattleInstruction.Attack, identity);
            ChargeBattle(other, BattleInstruction.Defend, identity);
            var chance = Math.Clamp(configuration.Track.PassingChance * (1 - segment.PassingDifficulty)
                * Math.Min(1, advantage / 2) * (1 + (car.Driver.Racecraft - other.Driver.Racecraft) * .03)
                * (car.Battle == BattleInstruction.Attack ? Traffic.AttackMultiplier : 1)
                * (other.Battle == BattleInstruction.Defend ? Traffic.DefendMultiplier : 1), 0, 1);
            success = PassingThreshold(car, Visit(car), $"encounter:{identity}") < chance;
            car.Opportunities.Add(identity, success);
            history.Add(new RaceEvent(time, car.Driver.Id, car.Laps + 1, RaceEventKind.PassingAttempt,
                0, other.Driver.Id, SegmentId: segment.Id,
                DistanceMetres: Distance(car), EncounterId: identity));
        }
        if (success && caught)
        {
            MoveAhead(car, other);
            if (!car.PassedOpportunities.Add(identity)) return true;
            history.Add(new RaceEvent(time, car.Driver.Id, car.Laps + 1, RaceEventKind.Overtake,
                OtherDriverId: other.Driver.Id, SegmentId: segment.Id,
                DistanceMetres: Distance(car), EncounterId: identity));
        }
        return success;
    }

    private static double PhysicalGap(double distance) => distance - Math.Floor(distance);

    private void ResolveEncounter(Car car)
    {
        var other = cars.Single(c => c.Driver.Id == car.PassTarget);
        if (other.PitExit is not null || other.FinishTime is not null) return;
        if (Segments is not null)
        {
            if (!AttemptSegmentPass(car, other))
            {
                MoveBehind(car, other);
                car.Following = other;
            }
            return;
        }
        var gap = PhysicalGap(Distance(other) - Distance(car));
        if (gap > 1e-6 && gap < 1 - 1e-6) return;
        if (Distance(car) - Distance(other) > .5)
        {
            Delay(car, Traffic.BlueFlagLoss);
            car.FreeDuration = car.LapEnd - car.LapStart;
            history.Add(new RaceEvent(time, car.Driver.Id, car.Laps + 1, RaceEventKind.BlueFlag, Traffic.BlueFlagLoss));
            return;
        }
        var advantage = Math.Max(0, other.FreeDuration - car.FreeDuration);
        ChargeBattle(car, BattleInstruction.Attack);
        ChargeBattle(other, BattleInstruction.Defend);
        var chance = Math.Clamp(configuration.Track.PassingChance * Math.Min(1, advantage / 2)
            * (1 + (car.Driver.Racecraft - other.Driver.Racecraft) * .03)
            * (car.Battle == BattleInstruction.Attack ? Traffic.AttackMultiplier : 1)
            * (other.Battle == BattleInstruction.Defend ? Traffic.DefendMultiplier : 1), 0, 1);
        if (PassingThreshold(car, car.Laps + 1, $"pass:{other.Driver.Id}") < chance)
            history.Add(new RaceEvent(time, car.Driver.Id, car.Laps + 1, RaceEventKind.Overtake,
                OtherDriverId: other.Driver.Id));
        else car.Following = other;
    }

    private void SetDuration(Car car, double duration)
    {
        if (Math.Abs(car.LapEnd - car.LapStart - duration) < 1e-9) return;
        var fraction = Segments is null ? Distance(car) - car.Laps : LocalProgress(car);
        car.LapStart = time - fraction * duration;
        car.LapEnd = time + (1 - fraction) * duration;
    }

    private double NextEvent(Car car) => Segments is not null && car.Laps == 0 && time < car.Driver.Gap
        ? car.Driver.Gap : Math.Min(car.PassTime ?? double.PositiveInfinity, car.PitExit ?? car.LapEnd);

    private double Distance(Car car) => (car.Laps + (car.Laps == 0 && time < car.Driver.Gap
        ? (time - car.Driver.Gap) / (car.LapEnd - car.LapStart) * SegmentShare(car) : Progress(car))) * TrackLength;

    private void Delay(Car car, double loss)
    {
        var fraction = Segments is null ? Progress(car) : LocalProgress(car);
        var duration = (car.LapEnd - time + loss) / Math.Max(1e-9, 1 - fraction);
        car.LapStart = time - fraction * duration;
        car.LapEnd = time + (1 - fraction) * duration;
    }

    private double LapTime(Car car)
    {
        var d = car.Driver;
        if (Segments is { } layout)
        {
            car.FreeProjection = SegmentSimulation.Project(layout[car.SegmentIndex], new SegmentInputs(
                layout.Sum(s => s.BaselineTime), d.Speed, d.Cornering, d.Pace,
                configuration.AttributeSeconds, car.LapPenalty,
                car.LapVariationSeconds, car.RaceFormSeconds));
            return car.FreeProjection.TravelSeconds;
        }
        return AggregateLapTime(d, TyrePenalty(car), car.LapVariationSeconds, car.RaceFormSeconds);
    }

    private double AggregateLapTime(Driver driver, double tyrePenalty, double lapVariation, double raceForm)
    {
        var performance = driver.Pace + driver.Speed * configuration.Track.SpeedWeight
            + driver.Cornering * (1 - configuration.Track.SpeedWeight);
        var duration = configuration.Track.BaseLapTime - performance * configuration.AttributeSeconds
            + lapVariation + raceForm + tyrePenalty;
        if (!double.IsFinite(duration)) throw new ArgumentException("Lap duration must be finite.");
        return Math.Max(1, duration);
    }

    private void ValidateDerivedConfiguration(IReadOnlyList<Driver> drivers)
    {
        var minimumPenalty = double.PositiveInfinity;
        var maximumPenalty = double.NegativeInfinity;
        var compounds = Enum.GetValues<TyreCompound>();
        var paces = Enum.GetValues<PaceInstruction>();
        // Penalties are monotone in warm-up/wear; two endpoints bound all supported tyre states.
        foreach (var compound in compounds)
        foreach (var pace in paces)
        {
            var minimum = TyrePenalty(compound, pace, 1, 1);
            var maximum = TyrePenalty(compound, pace, 0, 0);
            if (!double.IsFinite(minimum) || !double.IsFinite(maximum))
                throw new ArgumentException("Derived tyre penalty must be finite for supported controls.");
            minimumPenalty = Math.Min(minimumPenalty, minimum);
            maximumPenalty = Math.Max(maximumPenalty, maximum);
        }
        if (!double.IsFinite(Tyres.DegradationPerLap * Traffic.BattleWear))
            throw new ArgumentException("Derived battle wear must be finite.");
        var maximumDemand = Segments?.Max(s => s.TyreDemand) ?? 1;
        var baselineLap = Segments?.Sum(s => s.BaselineTime) ?? configuration.Track.BaseLapTime;
        foreach (var driver in drivers)
        {
            foreach (var compound in compounds)
            foreach (var pace in paces)
                if (!double.IsFinite(Wear(driver, compound, pace) * maximumDemand))
                    throw new ArgumentException("Derived segment tyre wear must be finite for supported controls.");
            if (Segments is { } layout)
                foreach (var segment in layout)
                {
                    _ = SegmentSimulation.Project(segment, new(baselineLap, driver.Speed, driver.Cornering, driver.Pace,
                        configuration.AttributeSeconds, minimumPenalty, -configuration.LapVariation / 2, -configuration.RaceFormVariation / 2));
                    _ = SegmentSimulation.Project(segment, new(baselineLap, driver.Speed, driver.Cornering, driver.Pace,
                        configuration.AttributeSeconds, maximumPenalty, configuration.LapVariation / 2, configuration.RaceFormVariation / 2));
                }
            else
            {
                _ = AggregateLapTime(driver, minimumPenalty, -configuration.LapVariation / 2, -configuration.RaceFormVariation / 2);
                _ = AggregateLapTime(driver, maximumPenalty, configuration.LapVariation / 2, configuration.RaceFormVariation / 2);
            }
        }
    }

    private double Wear(Car car) => Wear(car.Driver, car.Compound, car.Pace);
    private double Wear(Driver driver, TyreCompound tyreCompound, PaceInstruction pace)
    {
        var compound = tyreCompound switch { TyreCompound.Soft => Tyres.SoftWear,
            TyreCompound.Hard => Tyres.HardWear, _ => 1 };
        var instruction = pace switch { PaceInstruction.Push => Tyres.PushWear,
            PaceInstruction.Conserve => Tyres.ConserveWear, _ => 1 };
        var preservation = 1 + (driver.TyrePreservation + driver.TyreManagement) * .05;
        if (!double.IsFinite(preservation)) throw new ArgumentException("Derived tyre preservation must be finite.");
        var wear = Tyres.DegradationPerLap * compound * instruction
            / Math.Max(.1, preservation);
        if (!double.IsFinite(wear)) throw new ArgumentException("Derived tyre wear must be finite.");
        return wear;
    }

    private void ConsumeTyres(Car car, double fraction)
    {
        var wear = Wear(car) * Math.Max(0, fraction - car.WearProgress)
            * (Segments is { } layout ? layout[car.SegmentIndex].TyreDemand : 1);
        car.Condition = Math.Max(0, car.Condition - wear);
        car.WearProgress = fraction;
    }

    private double RandomOffset(RandomChannel channel, string name, string driver, int scope, string identity, double magnitude)
    {
        var value = ScopedRandom.Offset(seed, channel, driver, scope, magnitude);
        RecordRandom(name, driver, identity, value);
        return value;
    }
    private double PassingThreshold(Car car, int scope, string encounter)
    {
        var value = ScopedRandom.PassingThreshold(seed, car.Driver.Id, scope, encounter, configuration.PassingVariation);
        RecordRandom("passing", car.Driver.Id, $"{scope}:{encounter}", value);
        return value;
    }
    private void RecordRandom(string channel, string driver, string scope, double value)
    {
        if (diagnosticScopes.Add(string.Join("|", channel, Uri.EscapeDataString(driver), scope)))
            randomDiagnostics.Add(new(channel, driver, scope, value));
    }
    public IReadOnlyList<RandomDiagnostic> GetDesignerDiagnostics() => randomDiagnostics.ToArray();

    public PhaseResult GetState() => Snapshot();

    private IReadOnlyList<BattleInstruction> AvailableBattles(Car car)
    {
        var options = new List<BattleInstruction> { BattleInstruction.Neutral };
        if (!Traffic.Enabled || car.PitExit is not null || car.FinishTime is not null) return options;
        foreach (var other in cars.Where(c => c != car && c.PitExit is null && c.FinishTime is null))
        {
            var gap = (Distance(other) - Distance(car)) / TrackLength;
            if (Math.Abs(gap) * configuration.Track.BaseLapTime > Traffic.BattleProximity || Math.Abs(gap) > .5) continue;
            var choice = gap > 0 ? BattleInstruction.Attack : BattleInstruction.Defend;
            if (!options.Contains(choice)) options.Add(choice);
        }
        return options;
    }

    private void ChargeBattle(Car car, BattleInstruction instruction, string? encounter = null)
    {
        if (car.Battle != instruction) return;
        var cost = Math.Min(car.Condition, Tyres.DegradationPerLap * Traffic.BattleWear);
        car.Condition -= cost;
        history.Add(new RaceEvent(time, car.Driver.Id, car.Laps + 1, RaceEventKind.BattleWear, cost,
            SegmentId: Segments?[car.SegmentIndex].Id, DistanceMetres: Segments is null ? null : Distance(car), EncounterId: encounter));
    }

    private double TyrePenalty(Car car) => TyrePenalty(car.Compound, car.Pace, car.TyreAge, car.Condition);
    private double TyrePenalty(TyreCompound tyreCompound, PaceInstruction pace, int tyreAge, double condition)
    {
        var compound = tyreCompound switch { TyreCompound.Soft => -Tyres.SoftGain,
            TyreCompound.Hard => Tyres.HardLoss, _ => 0 };
        var instruction = pace switch { PaceInstruction.Push => -Tyres.PushGain,
            PaceInstruction.Conserve => Tyres.ConserveLoss, _ => 0 };
        var warmup = tyreCompound switch { TyreCompound.Soft => Tyres.SoftWarmup,
            TyreCompound.Hard => Tyres.HardWarmup, _ => 1 };
        return compound + instruction + (tyreAge == 0 ? Tyres.WarmupPenalty * warmup : 0)
            + Math.Pow(1 - condition, 2) * Tyres.DegradationPenalty
            + (condition <= 0 ? Tyres.ExhaustionPenalty : 0);
    }

    private double Progress(Car car) => car.FinishTime is not null || car.PitExit is not null ? 0
        : SegmentStart(car) + SegmentShare(car) * LocalProgress(car);

    public IReadOnlyList<RaceTimingEntry> GetTimingObservation()
    {
        var byId = cars.ToDictionary(car => car.Driver.Id);
        var order = Snapshot().Classification.Select(row => byId[row.DriverId]).ToArray();
        return Array.AsReadOnly(order.Select((car, index) => new RaceTimingEntry(car.Driver.Id,
            ObservedGap(order[0], car), index == 0 ? new TimingGap(null) : ObservedGap(order[index - 1], car))).ToArray());
    }

    private TimingGap ObservedGap(Car reference, Car car)
    {
        var laps = Math.Max(0, (int)Math.Floor(reference.Laps + Progress(reference) - car.Laps - Progress(car) + 1e-9));
        return new(laps > 0 || time == 0 && reference != car ? null : RecordedGap(reference, car), laps);
    }

    // Keep the historical diagnostic/replay contract's numeric fallback unchanged.
    private double Gap(Car leader, Car car) => RecordedGap(leader, car) ?? 0;

    private double? RecordedGap(Car leader, Car car)
    {
        if (car == leader) return 0;
        if (car.FinishTime is not null && leader.FinishTime is not null)
            return car.Laps == leader.Laps ? car.FinishTime.Value - leader.FinishTime.Value : null;
        var distance = Distance(car);
        for (var i = leader.Trace.Count - 1; i > 0; i--)
        {
            var start = leader.Trace[i - 1];
            var end = leader.Trace[i];
            if (start.Distance <= distance && end.Distance >= distance && end.Distance > start.Distance)
                return Math.Max(0, time - (start.Time + (distance - start.Distance)
                    / (end.Distance - start.Distance) * (end.Time - start.Time)));
        }
        return null;
    }

    private PhaseResult Snapshot()
    {
        var order = cars.OrderByDescending(c => c.Laps).ThenByDescending(c => Segments is null ? Progress(c) : Math.Round(Progress(c), 10))
            .ThenBy(c => Segments is null ? c.FinishTime ?? c.LapEnd : Math.Round(c.FinishTime ?? c.LapEnd, 8))
            .ThenBy(c => Segments is null ? 0 : c.ContactOrder)
            .ThenBy(c => c.Driver.Id, StringComparer.Ordinal).ToList();
        var leader = order[0];
        var classification = order.Select((c, index) => new ClassificationEntry(c.Driver.Id, index + 1,
            c.Laps, Progress(c), Gap(leader, c), Math.Max(0, (int)Math.Floor(
                leader.Laps + Progress(leader) - c.Laps - Progress(c) + 1e-9)), c.FinishTime)).ToList();
        return new PhaseResult(leader.Laps, time, cars.All(c => c.FinishTime is not null), classification,
            history.ToArray(), cars.Select(c => new TyreState(c.Driver.Id, c.Compound, c.TyreAge,
                c.Condition, c.Pace, c.PitExit is not null)).ToArray(),
            cars.ToDictionary(c => c.Driver.Id, c => c.UsedCompounds.Count >= 2),
            cars.ToDictionary(c => c.Driver.Id, AvailableBattles), decisionHistory.ToArray());
    }
}
