using Game.Domain;
namespace Game.Simulation;
public sealed partial class RaceEngine
{
    public IReadOnlyList<SafetyCarOption> GetSafetyCarOptions()=>Array.AsReadOnly(cars.Select(car=>new SafetyCarOption(car.Driver.Id,
        car.RetirementTime is not null?"retired":car.FinishTime is not null?"finished":car.PitExit is not null?"ongoing":car.PendingPit is not null?"queued":"available",
        car.PendingPit ?? (car.PitExit is not null?car.Compound:null),Active(car)&&car.PitExit is null&&car.PendingPit is null)).ToArray());

    // This operation only commits next-lap-end stops. Neutralised travel is a separate operation.
    public PhaseResult CommitSafetyCarChoices(IReadOnlyDictionary<string,SafetyCarDecision> choices)
    {
        if(interruption is null||Snapshot().Finished)throw new ArgumentException("No Safety Car interruption awaits choices.");
        var options=GetSafetyCarOptions();
        var required=options.Where(option=>option.Status=="available").Select(option=>option.DriverId).Order().ToArray();
        if(choices is null||!choices.Keys.Order().SequenceEqual(required))throw new ArgumentException("Choose exactly the active cars without committed stops.");
        foreach(var choice in choices.Values)
            if(choice is null||choice.PitCompound is {} compound&&!Enum.IsDefined(compound))throw new ArgumentException("Choose Stay Out or a valid pit compound.");
        foreach(var car in cars)
            if(choices.TryGetValue(car.Driver.Id,out var choice))car.PendingPit=choice.PitCompound;
        return Snapshot();
    }
}
