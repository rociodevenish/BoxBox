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
    }

    private readonly RaceConfiguration configuration;
    private readonly int seed;
    private readonly List<Car> cars;
    private readonly List<RaceEvent> history = [];
    private double time;
    private bool chequered;

    public RaceEngine(RaceConfiguration configuration, IReadOnlyList<Driver> drivers, int seed)
    {
        if (configuration.Laps < 1 || !double.IsFinite(configuration.Track.BaseLapTime)
            || configuration.Track.BaseLapTime <= 0 || !double.IsFinite(configuration.Track.SpeedWeight)
            || configuration.Track.SpeedWeight is < 0 or > 1
            || !double.IsFinite(configuration.AttributeSeconds) || configuration.AttributeSeconds < 0
            || !double.IsFinite(configuration.LapVariation) || configuration.LapVariation < 0
            || drivers.Count == 0 || drivers.Select(d => d.Id).Distinct().Count() != drivers.Count
            || drivers.Any(d => string.IsNullOrWhiteSpace(d.Id) || string.IsNullOrWhiteSpace(d.Team)
                || !double.IsFinite(d.Pace) || !double.IsFinite(d.Speed) || !double.IsFinite(d.Cornering)
                || !double.IsFinite(d.Gap) || d.Gap < 0 || !Enum.IsDefined(d.StartingCompound)))
            throw new ArgumentException("Invalid race configuration or starting field.");
        this.configuration = configuration;
        this.seed = seed;
        cars = drivers.Select(d => new Car(d)).ToList();
        foreach (var car in cars) car.LapEnd = car.LapStart + LapTime(car);
    }

    public PhaseResult ResolvePhase()
    {
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
        return Snapshot();
    }

    private double LapTime(Car car)
    {
        var d = car.Driver;
        var performance = d.Pace + d.Speed * configuration.Track.SpeedWeight
            + d.Cornering * (1 - configuration.Track.SpeedWeight);
        var duration = configuration.Track.BaseLapTime - performance * configuration.AttributeSeconds
            + (Draw(d.Id, car.Laps + 1, "lap") - .5) * configuration.LapVariation;
        if (!double.IsFinite(duration)) throw new ArgumentException("Lap duration must be finite.");
        return Math.Max(1, duration);
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
            history.ToArray());
    }
}
