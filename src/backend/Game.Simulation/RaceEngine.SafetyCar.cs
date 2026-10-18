using Game.Domain;
namespace Game.Simulation;
public sealed partial class RaceEngine
{
    private bool neutralised;
    private double neutralisedUntil=double.PositiveInfinity;
    private SafetyCarTuning Neutralisation=>configuration.SafetyCar??new();
    public PhaseResult ResolveSafetyCar(IReadOnlyDictionary<string,SafetyCarDecision> choices)
    {
        CommitSafetyCarChoices(choices);
        neutralised=true;
        neutralisedUntil=time+Neutralisation.DurationSeconds;
        foreach(var car in cars.Where(Active))
        {
            ConsumeTyres(car,Progress(car));
            car.Pace=PaceInstruction.Conserve;car.Battle=BattleInstruction.Neutral;
            car.Following=null;car.PassTime=null;
        }
        UpdateTraffic();
        AdvanceToLeaderLap(configuration.Laps);
        neutralised=false;neutralisedUntil=double.PositiveInfinity;interruption=null;normalTargetDistance=null;
        return Snapshot();
    }

    private void UpdateNeutralisedTraffic()
    {
        var running=cars.Where(c=>Active(c)&&c.PitExit is null).ToArray();
        var positions=running.ToDictionary(c=>c,Distance);
        var leader=running.OrderByDescending(c=>positions[c]).ThenBy(c=>c.ContactOrder).FirstOrDefault();
        var baseline=configuration.Track.BaseLapTime;
        var speeds=running.ToDictionary(c=>c,c=>TrackLength/(baseline*(c==leader?Neutralisation.LeaderLapMultiplier:Neutralisation.CatchUpLapMultiplier)));
        var ahead=running.ToDictionary(c=>c,c=>running.Where(o=>o!=c)
            .Select(o=>(Car:o,Gap:PhysicalGap((positions[o]-positions[c])/TrackLength)*TrackLength))
            .Where(o=>o.Gap>1e-7||(Math.Abs(o.Gap)<1e-7&&o.Car.ContactOrder<c.ContactOrder))
            .OrderBy(o=>o.Gap).FirstOrDefault());
        var spacing=TrackLength/(baseline*Neutralisation.LeaderLapMultiplier)*Neutralisation.FollowingSeconds;
        for(var i=0;i<running.Length;i++)foreach(var car in running)
            if(ahead[car].Car is {} front&&ahead[car].Gap<=spacing+1e-6)
                speeds[car]=Math.Min(speeds[car],speeds[front]);
        foreach(var car in running)
        {
            car.Trace.Add((time,Distance(car)));car.PassTime=null;car.Following=null;
            var duration=TrackLength*SegmentShare(car)/speeds[car];
            SetDuration(car,duration);car.FreeDuration=duration;
            if(ahead[car].Car is not {} front)continue;
            if(ahead[car].Gap<=spacing+1e-6){car.Following=front;continue;}
            var relative=speeds[car]-speeds[front];
            if(relative>1e-9)
            {
                var catchTime=time+(ahead[car].Gap-spacing)/relative;
                if(catchTime<car.LapEnd){car.PassTime=catchTime;car.PassTarget=front.Driver.Id;}
            }
        }
    }
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
        if(!WetEffects && choices.Values.Any(choice=>choice.PitCompound is TyreCompound.Intermediate or TyreCompound.Wet))
            throw new ArgumentException("Wet compounds require explicit wet tyre tuning.");
        foreach(var car in cars)
            if(choices.TryGetValue(car.Driver.Id,out var choice))car.PendingPit=choice.PitCompound;
        return Snapshot();
    }
}
