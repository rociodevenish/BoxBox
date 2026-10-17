using Game.Domain;
using Game.Simulation;
using Xunit;
using System.Collections.Immutable;
using System.Text.Json;
namespace Game.Simulation.Tests;
public sealed class AutomaticRaceStartTests
{
    [Fact]
    public void Detailed_starts_and_launch_tuning_change_the_opening_without_changing_rolling_pace()
    {
        var baseline=WeekendPresetCatalog.Open("track-monza");
        EffectiveWeekend Prepared(double starts)=>WeekendPreparation.Prepare(baseline with{Cars=baseline.Cars.Select(c=>c with{Driver=c.Driver with{Ratings=c.Driver.Ratings.SetItem("starts",starts)}}).ToImmutableArray()},10,new(0,0,0));
        var strong=Prepared(100);var weak=Prepared(1);
        var grid=strong.Cars.Select((c,i)=>new StartGridSlot(i+1,c.Source.EntryId)).ToArray();
        Assert.Equal(strong.Cars.Select(c=>c.RaceDriver),weak.Cars.Select(c=>c.RaceDriver));
        var paired=Enumerable.Range(1,12).Select(seed=>
        {
            var a=AutomaticRaceStart.Create(new(strong.Configuration,[strong],grid,seed,new(VariationSeconds:0,ErrorSeconds:0)));
            var b=AutomaticRaceStart.Create(new(weak.Configuration,[weak],grid,seed,new(VariationSeconds:0,ErrorSeconds:0)));
            return a.Opening.Time<b.Opening.Time;
        }).ToArray();
        Assert.All(paired,Assert.True);
    }
    [Fact]
    public void Full_field_replays_with_permuted_collections_and_rejects_incomplete_or_forged_inputs()
    {
        var baseline=WeekendPresetCatalog.Open("track-monza");
        var weekends=Enumerable.Range(1,11).Select(team=>
        {
            var source=baseline with{Cars=baseline.Cars.Select((car,index)=>car with{EntryId=$"team-{team:00}-car-{index+1}",TeamId=$"team-{team:00}",Driver=car.Driver with{Id=car.Driver.Id+"-"+team}}).ToImmutableArray()};
            return WeekendPreparation.Prepare(source,10,new(0,0,0));
        }).ToArray();
        var grid=weekends.SelectMany(w=>w.Cars).Select((car,i)=>new StartGridSlot(i+1,car.Source.EntryId)).ToArray();
        var input=new AutomaticStartInput(weekends[0].Configuration,weekends,grid,31,new());
        var first=AutomaticRaceStart.Create(input);
        var second=AutomaticRaceStart.Create(input with{Weekends=weekends.Reverse().ToArray(),Grid=grid.Reverse().ToArray()});
        Assert.Equal(22,first.Opening.Classification.Select(c=>c.DriverId).Distinct().Count());
        Assert.Equal(AutomaticRaceStart.OutcomeHash(first.Opening),AutomaticRaceStart.OutcomeHash(second.Opening));
        _=first.Engine.GetTimingObservation();_=first.Engine.GetTeamObservation("team-01");
        Assert.Equal(AutomaticRaceStart.OutcomeHash(first.Engine.ResolvePhase()),AutomaticRaceStart.OutcomeHash(second.Engine.ResolvePhase()));
        var recorded=new AutomaticStartReplay(input,AutomaticRaceStart.OutcomeHash(first.Opening),"frozen-31");
        var roundTrip=JsonSerializer.Deserialize<AutomaticStartReplay>(JsonSerializer.Serialize(recorded))!;
        Assert.Equal(recorded.OutcomeHash,AutomaticRaceStart.OutcomeHash(AutomaticRaceStart.Replay(roundTrip).Opening));
        Assert.Throws<ArgumentException>(()=>AutomaticRaceStart.Create(input with{Grid=grid.Skip(1).ToArray()}));
        Assert.Throws<ArgumentException>(()=>AutomaticRaceStart.Create(input with{Grid=grid.Select(g=>g.Position==2?grid[0]:g).ToArray()}));
        Assert.Throws<ArgumentException>(()=>AutomaticRaceStart.Create(input with{Grid=grid.Select(g=>g.Position==1?g with{EntryId="unknown"}:g).ToArray()}));
        Assert.Throws<ArgumentException>(()=>AutomaticRaceStart.Create(input with{Tuning=new(GridSpacingMetres:double.NaN)}));
        Assert.Throws<ArgumentException>(()=>AutomaticRaceStart.Create(input with{ModelVersion="future"}));
        Assert.Throws<ArgumentException>(()=>AutomaticRaceStart.Create(input with{RandomnessVersion="future"}));
        Assert.Throws<ArgumentException>(()=>AutomaticRaceStart.Replay(recorded with{OutcomeHash=new string('0',64)}));
        Assert.Throws<ArgumentException>(()=>AutomaticRaceStart.Create(input with{Weekends=weekends.Select((w,i)=>i==0?w with{Configuration=w.Configuration with{Laps=6}}:w).ToArray()}));
    }
    [Fact]
    public void Grid_is_physical_distance_and_one_lap_finishes_without_a_decision()
    {
        var weekend=WeekendPreparation.Prepare(WeekendPresetCatalog.Open("track-monza"),1,new(0,0,0));
        var input=new AutomaticStartInput(weekend.Configuration,[weekend],weekend.Cars.Select((c,i)=>new StartGridSlot(i+1,c.Source.EntryId)).ToArray(),17,new(GridSpacingMetres:8));
        var result=AutomaticRaceStart.Create(input);
        var length=weekend.Configuration.Track.Segments!.Sum(s=>s.Length);
        Assert.Equal(0,result.GridState.Time);
        Assert.Equal(0,result.GridState.Classification[0].Progress);
        Assert.Equal(-8/length,result.GridState.Classification[1].Progress,10);
        Assert.True(result.Opening.Finished);
        Assert.All(result.Opening.Classification,c=>Assert.NotNull(c.FinishTime));
        Assert.DoesNotContain(result.Opening.Events,e=>e.Kind is RaceEventKind.PitEntry or RaceEventKind.BattleDecision);
    }
    [Fact]
    public void Opening_retains_trailing_progress_and_live_engine_reaches_lap_six()
    {
        var weekend=WeekendPreparation.Prepare(WeekendPresetCatalog.Open("track-monza"),10,new(0,0,0));
        var input=new AutomaticStartInput(weekend.Configuration,[weekend],weekend.Cars.Select((c,i)=>new StartGridSlot(i+1,c.Source.EntryId)).ToArray(),17,new());
        var start=AutomaticRaceStart.Create(input);
        Assert.Equal(1,start.Opening.LeaderLap);
        Assert.Contains(start.Opening.Classification,c=>c.Laps==0&&c.Progress is >0 and <1);
        Assert.All(start.Opening.Tyres,t=>Assert.True(t.Condition<1));
        Assert.Contains(start.Engine.GetTimingObservation(),t=>t.ToLeader.Seconds>0);
        Assert.Equal(6,start.Engine.ResolvePhase().LeaderLap);
        var retry=AutomaticRaceStart.Create(input);
        Assert.Equal(start.Opening.Classification,retry.Opening.Classification);
    }
}
