using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public sealed class QualifyingTests
{
    [Fact]
    public void Frozen_attempts_replay_independently_of_input_iteration_and_retries()
    {
        var weekend=WeekendPreparation.Prepare(WeekendPresetCatalog.Open("track-monza"),6,new(0,0,0));
        var input=new QualifyingAttemptInput("Q1",1,17,[weekend],weekend.Cars.Select((c,i)=>new QualifyingChoice(c.Source.EntryId,i==0?"Safe":"All-Out")).ToArray(),weekend.Cars.Select(c=>c.Source.EntryId).ToArray(),new(1));
        var first=Qualifying.Resolve(input);
        Assert.Equal(2,first.Ranking.Count);
        Assert.Single(first.Ranking,r=>r.Advances);
        Assert.Equal(first.Ranking,Qualifying.Resolve(input with{Choices=input.Choices.Reverse().ToArray()}).Ranking);
        Assert.Equal(first.Ranking,Qualifying.Resolve(input).Ranking);
    }
    [Fact]
    public void Equal_valid_times_and_no_times_use_fixed_pre_session_order()
    {
        var order=new[]{"a","b","c","d"};
        var times=new[]{new QualifyingTime("d","dd","t2","no-time",null),new("c","dc","t2","valid",90000),new("b","db","t1","no-time",null),new("a","da","t1","valid",90000)};
        var ranked=Qualifying.Rank(times,order,new(2));
        Assert.Equal(new[]{"a","c","b","d"},ranked.Select(r=>r.EntryId));
        Assert.Equal(new[]{true,true,false,false},ranked.Select(r=>r.Advances));
        Assert.Throws<ArgumentException>(()=>Qualifying.Rank([times[0] with{TimeMilliseconds=0}], ["d"],new(0)));
    }    [Fact]
    public void Attempt_rejects_incomplete_or_unknown_choices_and_incompatible_frozen_versions()
    {
        var weekend=WeekendPreparation.Prepare(WeekendPresetCatalog.Open("track-monza"),6,new(0,0,0));
        var input=new QualifyingAttemptInput("Q1",1,17,[weekend],weekend.Cars.Select(c=>new QualifyingChoice(c.Source.EntryId,"Safe")).ToArray(),weekend.Cars.Select(c=>c.Source.EntryId).ToArray(),new(1));
        Assert.Throws<ArgumentException>(()=>Qualifying.Resolve(input with{Choices=[input.Choices[0]]}));
        Assert.Throws<ArgumentException>(()=>Qualifying.Resolve(input with{Choices=[input.Choices[0],input.Choices[0]]}));
        Assert.Throws<ArgumentException>(()=>Qualifying.Resolve(input with{Choices=[input.Choices[0] with{Approach="Aggressive"},input.Choices[1]]}));
        Assert.Throws<ArgumentException>(()=>Qualifying.Resolve(input with{ModelVersion="future"}));
        Assert.Throws<ArgumentException>(()=>Qualifying.Resolve(input with{RandomnessVersion="future"}));
        Assert.Throws<ArgumentException>(()=>Qualifying.Resolve(input with{Weekends=[weekend with{ModelVersion="future"}]}));
    }    [Fact]
    public void Frozen_inputs_with_invalid_detailed_ratings_are_rejected()
    {
        var weekend=WeekendPreparation.Prepare(WeekendPresetCatalog.Open("track-monza"),6,new(0,0,0));
        var car=weekend.Cars[0];var invalid=car.Source with{Driver=car.Source.Driver with{Ratings=car.Source.Driver.Ratings.SetItem("errorControl",double.NaN)}};
        var forged=weekend with{Cars=weekend.Cars.SetItem(0,car with{Source=invalid})};
        var input=new QualifyingAttemptInput("Q1",1,17,[forged],weekend.Cars.Select(c=>new QualifyingChoice(c.Source.EntryId,"Safe")).ToArray(),weekend.Cars.Select(c=>c.Source.EntryId).ToArray(),new(1));
        Assert.Throws<ArgumentException>(()=>Qualifying.Resolve(input));
    }}
