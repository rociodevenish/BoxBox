using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public sealed class QualifyingTests
{
    [Fact]
    public void Policy_rejects_missing_driver_observations_before_issuing_choices()
    {
        Assert.Throws<ArgumentException>(()=>QualifyingDecisionPolicy.Evaluate(new("Q1","team",null!)));
        Assert.Throws<ArgumentException>(()=>QualifyingDecisionPolicy.Evaluate(new("Q2","team",[new("same","Comfortable","High"),new("same","Comfortable","High")])));
    }
    [Fact]
    public void Advice_uses_each_eligible_drivers_disclosed_fit_and_keeps_all_approaches()
    {
        var observation=new QualifyingPolicyObservation("Q2","team-01",[
            new("entry-a","Comfortable","High"),new("entry-b","Preference mismatch","Moderate")]);
        var plan=QualifyingDecisionPolicy.Evaluate(observation);
        Assert.Equal(new[]{"Push","Safe"},plan.Cars.Select(c=>c.Choice.Approach));
        Assert.Equal(new[]{"entry-a","entry-b"},plan.Cars.Select(c=>c.Choice.EntryId));
        Assert.All(plan.Cars,car=>{Assert.Equal(new[]{"Safe","Push","All-Out"},car.LegalApproaches);Assert.Contains("uncertain",car.Uncertainty);});
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(plan),System.Text.Json.JsonSerializer.Serialize(QualifyingDecisionPolicy.Evaluate(observation)));
    }
    [Fact]
    public void Grid_preserves_each_elimination_group_even_with_ties_and_no_times()
    {
        var order=Enumerable.Range(1,22).Select(i=>$"entry-{i:00}").ToArray();
        QualifyingResult Session(string name,string[] entries,int retain)=>new(name,1,Qualifying.ModelVersion,Qualifying.RandomnessVersion,Qualifying.RulesVersion,
            Qualifying.Rank(entries.Reverse().Select(id=>new QualifyingTime(id,"driver-"+id,"team",id=="entry-01"?"valid":"no-time",id=="entry-01"?90000:null)).ToArray(),entries,new(retain)));
        var q1=Session("Q1",order,16);var q2=Session("Q2",order.Take(16).Reverse().ToArray(),10);
        var q3=Session("Q3",q2.Ranking.Take(10).Select(r=>r.EntryId).Reverse().ToArray(),0);
        var grid=Qualifying.Grid([q1,q2,q3]);
        Assert.Equal(new[]{"entry-01","entry-08","entry-09","entry-10","entry-11","entry-12","entry-13","entry-14","entry-15","entry-16","entry-07","entry-06","entry-05","entry-04","entry-03","entry-02","entry-17","entry-18","entry-19","entry-20","entry-21","entry-22"},grid.Select(r=>r.EntryId));
        Assert.Equal(Enumerable.Range(1,22),grid.Select(r=>r.Position));
        Assert.Throws<ArgumentException>(()=>Qualifying.Grid([q1,q2,q3 with{Ranking=q3.Ranking.Take(9).ToArray()}]));
    }
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
