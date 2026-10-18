using Game.Domain;
namespace Game.Simulation;

public sealed partial class RaceEngine
{
    private double conditionWetness;
    private bool WetEffects => configuration.WetTyres is not null;
    private double Unsuitability(TyreCompound compound) => compound switch
    {
        TyreCompound.Intermediate => Math.Clamp(Math.Abs(conditionWetness - configuration.WetTyres!.IntermediateOptimumWetness)
            / Math.Max(configuration.WetTyres.IntermediateOptimumWetness, 1 - configuration.WetTyres.IntermediateOptimumWetness), 0, 1),
        TyreCompound.Wet => 1 - conditionWetness,
        _ => conditionWetness
    };
    private double WetPenalty(Car car)
    {
        if (configuration.WetTyres is not { } tuning) return 0;
        var attributes = car.Driver.WetWeather ?? new();
        var wrong = Unsuitability(car.Compound);
        var maximum = car.Compound switch { TyreCompound.Intermediate => tuning.IntermediateUnsuitabilitySeconds,
            TyreCompound.Wet => tuning.WetUnsuitabilitySeconds, _ => tuning.DryUnsuitabilitySeconds };
        // Temperature management softens wrong-condition losses by at most 15%; skill/traction only act in wetness.
        return maximum * wrong * wrong * (1 - .15 * (attributes.TyreTemperatureManagement - 1) / 99)
            + conditionWetness * tuning.WetSkillSeconds
                * ((100 - attributes.WetWeatherSkill) + (100 - attributes.Traction)) / 198;
    }
    private double WetWear(Car car) => configuration.WetTyres is { } tuning
        ? 1 + tuning.WrongConditionWear * Math.Pow(Unsuitability(car.Compound), 2)
            * (1 - .15 * ((car.Driver.WetWeather?.TyreTemperatureManagement ?? 82) - 1) / 99) : 1;
    private double WetError(Car car) => configuration.WetTyres is { } tuning
        ? 1 + tuning.WetErrorMultiplier * conditionWetness
            * (1 + Unsuitability(car.Compound))
            * ((100 - (car.Driver.WetWeather?.WetWeatherSkill ?? 82))
                + (100 - (car.Driver.WetWeather?.Traction ?? 82))) / 198 : 1;

    // One-second absolute-time semantic cells plus rain-process knots, independent of car/segment boundaries.
    // This intentionally bounded numerical approximation samples a continuous curve at the cell's left edge.
    private double NextWetConditionTime()
    {
        if (!WetEffects || configuration.Weather is not { } weather) return double.PositiveInfinity;
        var absolute = weather.TimeOriginSeconds + time;
        return Math.Min(Math.Floor(absolute + 1e-8) + 1,
            (Math.Floor(absolute / weather.IntervalSeconds + 1e-10) + 1) * weather.IntervalSeconds)
            - weather.TimeOriginSeconds;
    }
    private void UpdateWetConditions()
    {
        if (!WetEffects) return;
        conditionWetness = GetCurrentWeather()?.Wetness ?? 0;
        if (neutralised) { UpdateTraffic(); return; }
        foreach (var car in cars.Where(c => Active(c) && c.PitExit is null))
        {
            var progress = LocalProgress(car);
            car.PassTime = null;
            car.Following = null;
            var delay = Math.Max(0, car.FreeDuration - car.WetBaseDuration
                - (car.Damage - car.WetBaseDamage) * (Segments is {} layout
                    ? layout[car.SegmentIndex].BaselineTime / layout.Sum(s => s.BaselineTime) : 1));
            var duration = LapTime(car) + delay;
            car.FreeDuration = duration;
            car.LapStart = time < car.Driver.Gap ? car.Driver.Gap : time - progress * duration;
            car.LapEnd = car.LapStart + duration;
        }
        UpdateTraffic();
    }
}
