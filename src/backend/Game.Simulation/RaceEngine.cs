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
        public List<double> Crossings { get; } = [driver.Gap];
        public TyreCompound Compound { get; set; } = driver.StartingCompound;
        public int TyreAge { get; set; }
        public double Condition { get; set; } = 1;
        public PaceInstruction Pace { get; set; }
        public double WearProgress { get; set; }
    }

    private readonly RaceConfiguration configuration;
    private readonly int seed;
    private readonly List<Car> cars;
    private readonly List<RaceEvent> history = [];
    private double time;
    private bool chequered;
    private TyreTuning Tyres => configuration.Tyres ?? new TyreTuning();

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
            || drivers.Count == 0 || drivers.Select(d => d.Id).Distinct().Count() != drivers.Count
            || drivers.Any(d => string.IsNullOrWhiteSpace(d.Id) || string.IsNullOrWhiteSpace(d.Team)
                || !double.IsFinite(d.Pace) || !double.IsFinite(d.Speed) || !double.IsFinite(d.Cornering)
                || !double.IsFinite(d.Gap) || d.Gap < 0 || !Enum.IsDefined(d.StartingCompound)
                || !double.IsFinite(d.TyrePreservation) || !double.IsFinite(d.TyreManagement))
            || tyreValues.Any(v => !double.IsFinite(v) || v < 0))
            throw new ArgumentException("Invalid race configuration or starting field.");
        this.configuration = configuration;
        this.seed = seed;
        cars = drivers.Select(d => new Car(d)).ToList();
        foreach (var car in cars)
        {
            _ = Wear(car);
            car.LapEnd = car.LapStart + LapTime(car);
        }
    }

    public PhaseResult ResolvePhase(IReadOnlyDictionary<string, DriverDecision>? decisions = null)
    {
        if (decisions is not null && decisions.Any(d => cars.All(c => c.Driver.Id != d.Key)
            || !Enum.IsDefined(d.Value.Pace))) throw new ArgumentException("Invalid driver decision.");
        foreach (var car in cars.Where(c => c.FinishTime is null))
        {
            var fraction = Progress(car);
            ConsumeTyres(car, fraction);
            car.Pace = decisions?.GetValueOrDefault(car.Driver.Id)?.Pace ?? PaceInstruction.Standard;
            var duration = LapTime(car);
            car.LapStart = time < car.Driver.Gap ? car.Driver.Gap : time - fraction * duration;
            car.LapEnd = car.LapStart + duration;
        }
        var target = Math.Min(configuration.Laps, cars.Max(c => c.Laps) + 5);
        while (cars.Any(c => c.FinishTime is null))
        {
            var car = cars.Where(c => c.FinishTime is null).OrderBy(c => c.LapEnd)
                .ThenBy(c => c.Driver.Id, StringComparer.Ordinal).First();
            time = car.LapEnd;
            if (cars.Any(c => c.FinishTime is null && c.LapEnd == time
                && c.Laps + 1 >= configuration.Laps)) chequered = true;
            car.Laps++;
            car.Crossings.Add(time);
            ConsumeTyres(car, 1);
            car.WearProgress = 0;
            car.TyreAge++;
            history.Add(new RaceEvent(time, car.Driver.Id, car.Laps, "TyreCondition", car.Condition));
            history.Add(new RaceEvent(time, car.Driver.Id, car.Laps, "LapCompleted"));
            if (car.Laps >= configuration.Laps) chequered = true;
            if (chequered) car.FinishTime = time;
            else
            {
                car.LapStart = time;
                car.LapEnd = time + LapTime(car);
            }
            // Finish the complete field once the leader receives the flag.
            if (!chequered && car.Laps >= target) break;
        }
        foreach (var car in cars.Where(c => c.FinishTime is null)) ConsumeTyres(car, Progress(car));
        return Snapshot();
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

    private double Progress(Car car) => car.FinishTime is not null ? 0
        : Math.Clamp((time - car.LapStart) / (car.LapEnd - car.LapStart), 0, 1);

    private double Gap(Car leader, Car car)
    {
        if (car == leader) return 0;
        if (car.FinishTime is not null && leader.FinishTime is not null)
            return car.Laps == leader.Laps ? car.FinishTime.Value - leader.FinishTime.Value : 0;
        var lap = Math.Min(car.Laps, leader.Crossings.Count - 1);
        var start = leader.Crossings[lap];
        var end = lap + 1 < leader.Crossings.Count ? leader.Crossings[lap + 1] : leader.LapEnd;
        return Math.Max(0, time - (start + Progress(car) * (end - start)));
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
                c.Condition, c.Pace)).ToArray());
    }
}
