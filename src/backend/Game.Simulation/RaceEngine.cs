using Game.Domain;

namespace Game.Simulation;

public sealed class RaceEngine
{
    private sealed class Car(Driver driver)
    {
        public Driver Driver { get; } = driver;
        public int Laps { get; set; }
        public double LapStart { get; set; } = driver.Gap;
        public double LapEnd { get; set; }
        public double? FinishTime { get; set; }
        public TyreCompound Compound { get; set; } = driver.StartingCompound;
        public int TyreAge { get; set; }
        public double Condition { get; set; } = 1;
        public PaceInstruction Pace { get; set; }
        public BattleInstruction Battle { get; set; }
        public double WearProgress { get; set; }
        public TyreCompound? PendingPit { get; set; }
        public double? PitExit { get; set; }
        public HashSet<TyreCompound> UsedCompounds { get; } = [driver.StartingCompound];
        public Car? Following { get; set; }
        public double FreeDuration { get; set; }
        public double? PassTime { get; set; }
        public string? PassTarget { get; set; }
        public List<(double Time, double Distance)> Trace { get; } = [];
    }

    private readonly RaceConfiguration configuration;
    private readonly int seed;
    private readonly List<Car> cars;
    private readonly List<RaceEvent> history = [];
    private double time;
    private bool chequered;
    private TyreTuning Tyres => configuration.Tyres ?? new TyreTuning();
    private TrafficTuning Traffic => configuration.Traffic ?? new TrafficTuning();

    public RaceEngine(RaceConfiguration configuration, IReadOnlyList<Driver> drivers, int seed)
    {
        var tyres = configuration.Tyres ?? new TyreTuning();
        var tyreValues = new[] { tyres.DegradationPerLap, tyres.WarmupPenalty, tyres.DegradationPenalty,
            tyres.ExhaustionPenalty, tyres.PushGain, tyres.PushWear, tyres.ConserveLoss, tyres.ConserveWear,
            tyres.SoftGain, tyres.SoftWear, tyres.HardLoss, tyres.HardWear, tyres.SoftWarmup, tyres.HardWarmup };
        if (configuration.Laps < 1 || !double.IsFinite(configuration.Track.BaseLapTime)
            || configuration.Track.BaseLapTime <= 0 || !double.IsFinite(configuration.Track.SpeedWeight)
            || configuration.Track.SpeedWeight is < 0 or > 1
            || !double.IsFinite(configuration.AttributeSeconds) || configuration.AttributeSeconds < 0
            || !double.IsFinite(configuration.LapVariation) || configuration.LapVariation < 0
            || !double.IsFinite(configuration.Track.PitLaneLoss) || configuration.Track.PitLaneLoss < 0
            || !double.IsFinite(configuration.Track.StationaryStop) || configuration.Track.StationaryStop < 0
            || !double.IsFinite(configuration.StopVariation) || configuration.StopVariation < 0
            || configuration.StopVariation > configuration.Track.StationaryStop * 2
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
        this.configuration = configuration;
        this.seed = seed;
        cars = drivers.Select(d => new Car(d)).ToList();
        foreach (var car in cars)
        {
            _ = Wear(car);
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
            car.Following = null;
            ConsumeTyres(car, fraction);
            car.Pace = decisions?.GetValueOrDefault(car.Driver.Id)?.Pace ?? PaceInstruction.Standard;
            car.Battle = decisions?.GetValueOrDefault(car.Driver.Id)?.Battle ?? BattleInstruction.Neutral;
            if (car.Battle != BattleInstruction.Neutral)
                history.Add(new RaceEvent(time, car.Driver.Id, car.Laps, RaceEventKind.BattleDecision, (int)car.Battle));
            var requestedPit = decisions?.GetValueOrDefault(car.Driver.Id)?.PitCompound;
            car.PendingPit ??= requestedPit;
            var duration = LapTime(car);
            car.FreeDuration = duration;
            car.LapStart = car.PitExit ?? (time < car.Driver.Gap ? car.Driver.Gap : time - fraction * duration);
            car.LapEnd = car.LapStart + duration;
        }
        UpdateTraffic();
        var target = Math.Min(configuration.Laps, cars.Max(c => c.Laps) + 5);
        while (cars.Any(c => c.FinishTime is null))
        {
            var car = cars.Where(c => c.FinishTime is null).OrderBy(NextEvent)
                .ThenBy(c => c.Driver.Id, StringComparer.Ordinal).First();
            var nextTime = NextEvent(car);
            foreach (var current in cars.Where(c => c.Following is not null && c.FinishTime is null && c.PitExit is null))
            {
                var loss = (nextTime - time) * Math.Max(0, 1 - current.FreeDuration / (current.LapEnd - current.LapStart));
                if (loss > 0) history.Add(new RaceEvent(nextTime, current.Driver.Id, current.Laps + 1, RaceEventKind.TrafficLoss, loss));
            }
            time = nextTime;
            foreach (var current in cars) current.Trace.Add((time, Distance(current)));
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
                history.Add(new RaceEvent(time, car.Driver.Id, car.Laps, RaceEventKind.PitExit));
                UpdateTraffic();
                continue;
            }
            if (cars.Any(c => c.FinishTime is null && c.LapEnd == time
                && c.Laps + 1 >= configuration.Laps)) chequered = true;
            ConsumeTyres(car, 1);
            car.Laps++;
            car.Following = null;
            car.WearProgress = 0;
            car.TyreAge++;
            history.Add(new RaceEvent(time, car.Driver.Id, car.Laps, RaceEventKind.TyreCondition, car.Condition));
            history.Add(new RaceEvent(time, car.Driver.Id, car.Laps, RaceEventKind.LapCompleted));
            if (car.Laps >= configuration.Laps) chequered = true;
            if (chequered) car.FinishTime = time;
            else
            {
                if (car.PendingPit is { } compound)
                {
                    var service = configuration.Track.StationaryStop
                        + (Draw(car.Driver.Id, car.Laps, "stop") - .5) * configuration.StopVariation;
                    var loss = configuration.Track.PitLaneLoss + service;
                    history.Add(new RaceEvent(time, car.Driver.Id, car.Laps, RaceEventKind.PitEntry, loss));
                    history.Add(new RaceEvent(time, car.Driver.Id, car.Laps, RaceEventKind.PitService, service));
                    car.Compound = compound;
                    car.UsedCompounds.Add(compound);
                    car.TyreAge = 0;
                    car.Condition = 1;
                    car.PendingPit = null;
                    car.PitExit = time + loss;
                    history.Add(new RaceEvent(time, car.Driver.Id, car.Laps, RaceEventKind.TyreChanged, Compound: compound));
                }
                car.LapStart = car.PitExit ?? time;
                car.LapEnd = car.LapStart + LapTime(car);
                car.FreeDuration = car.LapEnd - car.LapStart;
            }
            UpdateTraffic();
            // Finish the complete field once the leader receives the flag.
            if (!chequered && car.Laps >= target) break;
        }
        foreach (var car in cars.Where(c => c.FinishTime is null)) ConsumeTyres(car, Progress(car));
        return Snapshot();
    }

    private void UpdateTraffic()
    {
        if (!Traffic.Enabled) return;
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

    private static double PhysicalGap(double distance) => distance - Math.Floor(distance);

    private void ResolveEncounter(Car car)
    {
        var other = cars.Single(c => c.Driver.Id == car.PassTarget);
        if (other.PitExit is not null || other.FinishTime is not null) return;
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
        if (Draw(car.Driver.Id, car.Laps + 1, $"pass:{other.Driver.Id}") < chance)
            history.Add(new RaceEvent(time, car.Driver.Id, car.Laps + 1, RaceEventKind.Overtake,
                OtherDriverId: other.Driver.Id));
        else car.Following = other;
    }

    private void SetDuration(Car car, double duration)
    {
        if (Math.Abs(car.LapEnd - car.LapStart - duration) < 1e-9) return;
        var fraction = Distance(car) - car.Laps;
        car.LapStart = time - fraction * duration;
        car.LapEnd = time + (1 - fraction) * duration;
    }

    private static double NextEvent(Car car) => Math.Min(car.PassTime ?? double.PositiveInfinity,
        car.PitExit ?? car.LapEnd);

    private double Distance(Car car) => car.Laps + (car.Laps == 0 && time < car.Driver.Gap
        ? (time - car.Driver.Gap) / (car.LapEnd - car.LapStart) : Progress(car));

    private void Delay(Car car, double loss)
    {
        var fraction = Progress(car);
        var duration = (car.LapEnd - time + loss) / Math.Max(1e-9, 1 - fraction);
        car.LapStart = time - fraction * duration;
        car.LapEnd = time + (1 - fraction) * duration;
    }

    private double LapTime(Car car)
    {
        var d = car.Driver;
        var performance = d.Pace + d.Speed * configuration.Track.SpeedWeight
            + d.Cornering * (1 - configuration.Track.SpeedWeight);
        var duration = configuration.Track.BaseLapTime - performance * configuration.AttributeSeconds
            + (Draw(d.Id, car.Laps + 1, "lap") - .5) * configuration.LapVariation
            + TyrePenalty(car);
        if (!double.IsFinite(duration)) throw new ArgumentException("Lap duration must be finite.");
        return Math.Max(1, duration);
    }

    private double Wear(Car car)
    {
        var compound = car.Compound switch { TyreCompound.Soft => Tyres.SoftWear,
            TyreCompound.Hard => Tyres.HardWear, _ => 1 };
        var instruction = car.Pace switch { PaceInstruction.Push => Tyres.PushWear,
            PaceInstruction.Conserve => Tyres.ConserveWear, _ => 1 };
        var wear = Tyres.DegradationPerLap * compound * instruction
            / Math.Max(.1, 1 + (car.Driver.TyrePreservation + car.Driver.TyreManagement) * .05);
        if (!double.IsFinite(wear)) throw new ArgumentException("Derived tyre wear must be finite.");
        return wear;
    }

    private void ConsumeTyres(Car car, double fraction)
    {
        var wear = Wear(car) * Math.Max(0, fraction - car.WearProgress);
        car.Condition = Math.Max(0, car.Condition - wear);
        car.WearProgress = fraction;
    }

    public PhaseResult GetState() => Snapshot();

    private IReadOnlyList<BattleInstruction> AvailableBattles(Car car)
    {
        var options = new List<BattleInstruction> { BattleInstruction.Neutral };
        if (!Traffic.Enabled || car.PitExit is not null || car.FinishTime is not null) return options;
        foreach (var other in cars.Where(c => c != car && c.PitExit is null && c.FinishTime is null))
        {
            var gap = Distance(other) - Distance(car);
            if (Math.Abs(gap) * configuration.Track.BaseLapTime > Traffic.BattleProximity || Math.Abs(gap) > .5) continue;
            var choice = gap > 0 ? BattleInstruction.Attack : BattleInstruction.Defend;
            if (!options.Contains(choice)) options.Add(choice);
        }
        return options;
    }

    private void ChargeBattle(Car car, BattleInstruction instruction)
    {
        if (car.Battle != instruction) return;
        var cost = Math.Min(car.Condition, Tyres.DegradationPerLap * Traffic.BattleWear);
        car.Condition -= cost;
        history.Add(new RaceEvent(time, car.Driver.Id, car.Laps + 1, RaceEventKind.BattleWear, cost));
    }

    private double TyrePenalty(Car car)
    {
        var compound = car.Compound switch { TyreCompound.Soft => -Tyres.SoftGain,
            TyreCompound.Hard => Tyres.HardLoss, _ => 0 };
        var instruction = car.Pace switch { PaceInstruction.Push => -Tyres.PushGain,
            PaceInstruction.Conserve => Tyres.ConserveLoss, _ => 0 };
        var warmup = car.Compound switch { TyreCompound.Soft => Tyres.SoftWarmup,
            TyreCompound.Hard => Tyres.HardWarmup, _ => 1 };
        return compound + instruction + (car.TyreAge == 0 ? Tyres.WarmupPenalty * warmup : 0)
            + Math.Pow(1 - car.Condition, 2) * Tyres.DegradationPenalty
            + (car.Condition <= 0 ? Tyres.ExhaustionPenalty : 0);
    }

    private double Draw(string driver, int lap, string kind)
    {
        // Explicit stable hash: string.GetHashCode is intentionally process-dependent.
        uint hash = 2166136261;
        foreach (var ch in $"{seed}:{driver}:{lap}:{kind}") hash = unchecked((hash ^ ch) * 16777619);
        hash ^= hash >> 16;
        hash = unchecked(hash * 0x7feb352d);
        hash ^= hash >> 15;
        return hash / (double)uint.MaxValue;
    }

    private double Progress(Car car) => car.FinishTime is not null || car.PitExit is not null ? 0
        : Math.Clamp((time - car.LapStart) / (car.LapEnd - car.LapStart), 0, 1);

    private double Gap(Car leader, Car car)
    {
        if (car == leader) return 0;
        if (car.FinishTime is not null && leader.FinishTime is not null)
            return car.Laps == leader.Laps ? car.FinishTime.Value - leader.FinishTime.Value : 0;
        var distance = Distance(car);
        for (var i = leader.Trace.Count - 1; i > 0; i--)
        {
            var start = leader.Trace[i - 1];
            var end = leader.Trace[i];
            if (start.Distance <= distance && end.Distance >= distance && end.Distance > start.Distance)
                return Math.Max(0, time - (start.Time + (distance - start.Distance)
                    / (end.Distance - start.Distance) * (end.Time - start.Time)));
        }
        return 0;
    }

    private PhaseResult Snapshot()
    {
        var order = cars.OrderByDescending(c => c.Laps).ThenByDescending(Progress)
            .ThenBy(c => c.FinishTime ?? c.LapEnd).ThenBy(c => c.Driver.Id, StringComparer.Ordinal).ToList();
        var leader = order[0];
        var classification = order.Select((c, index) => new ClassificationEntry(c.Driver.Id, index + 1,
            c.Laps, Progress(c), Gap(leader, c), Math.Max(0, (int)Math.Floor(
                leader.Laps + Progress(leader) - c.Laps - Progress(c) + 1e-9)), c.FinishTime)).ToList();
        return new PhaseResult(leader.Laps, time, cars.All(c => c.FinishTime is not null), classification,
            history.ToArray(), cars.Select(c => new TyreState(c.Driver.Id, c.Compound, c.TyreAge,
                c.Condition, c.Pace, c.PitExit is not null)).ToArray(),
            cars.ToDictionary(c => c.Driver.Id, c => c.UsedCompounds.Count >= 2),
            cars.ToDictionary(c => c.Driver.Id, AvailableBattles));
    }
}
