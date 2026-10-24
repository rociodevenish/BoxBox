using Game.Domain;
using Xunit;
namespace Game.Simulation.Tests;
public sealed class CampaignScoringTests
{
 [Fact] public void Raw_positions_retirement_and_eligibility_determine_points_without_promotion()
 {
  var entrants=new[]{new ChampionshipEntrant("a","red",0),new ChampionshipEntrant("b","red",1),new ChampionshipEntrant("c","blue",2),new ChampionshipEntrant("d","blue",3)};
  var result=ChampionshipScoring.Score(ChampionshipScoring.Default,entrants,[new("a",1,true,false),new("b",2,false,false),new("c",3,true,false),new("d",4,true,true)]);
  Assert.Equal(new[]{25,0,15,0},result.Select(r=>r.Points));
  var table=new ChampionshipPoints("custom-v1",[9,5,2]);
  Assert.Equal(new[]{9,0,2,0},ChampionshipScoring.Score(table,entrants,[new("a",1,true,false),new("b",2,false,false),new("c",3,true,false),new("d",4,true,true)]).Select(r=>r.Points));
 }
 [Fact] public void Championships_sum_both_drivers_and_break_ties_by_all_eligible_positions_then_frozen_order()
 {
  var entrants=Enumerable.Range(0,12).Select(i=>new ChampionshipEntrant("p"+i,i<2?"red":i<4?"blue":"t"+i,i)).ToArray();
  // Two equal totals and identical finish counts resolve to frozen team order. Drivers p8 and p9
  // both score zero, but p9's eleventh beats p8's twelfth, beyond the points table.
  var first=ChampionshipScoring.Score(ChampionshipScoring.Default,entrants,entrants.Select((e,i)=>new ChampionshipFinish(e.PersonId,i==8?12:i==9?11:i==10?9:i==11?10:i+1,true,false)).ToArray());
  var second=ChampionshipScoring.Score(ChampionshipScoring.Default,entrants,entrants.Select((e,i)=>new ChampionshipFinish(e.PersonId,i==0?4:i==1?3:i==2?2:i==3?1:i==8?12:i==9?11:i==10?9:i==11?10:i+1,true,false)).ToArray());
  var table=ChampionshipScoring.Rebuild(entrants,[first,second]);
  Assert.Equal(new[]{"red","blue"},table.Constructors.Take(2).Select(r=>r.Id));
  Assert.Equal(new[]{70,70},table.Constructors.Take(2).Select(r=>r.Points));
  Assert.True(table.Drivers.Single(r=>r.Id=="p9").Position<table.Drivers.Single(r=>r.Id=="p8").Position);
  var empty=ChampionshipScoring.Rebuild(entrants,[]);
  Assert.Equal(entrants.Select(e=>e.PersonId),empty.Drivers.Select(r=>r.Id));
  Assert.Equal(table.Drivers.Select(r=>r.Id),ChampionshipScoring.Rebuild(entrants,[second,first]).Drivers.Select(r=>r.Id));
 }
 [Fact] public void Eligible_high_finishes_beat_starting_order_for_both_championships()
 {
  var entrants=new[]{new ChampionshipEntrant("a","red",0),new ChampionshipEntrant("b","red",1),new ChampionshipEntrant("c","blue",2),new ChampionshipEntrant("d","blue",3)};
  var result=ChampionshipScoring.Score(new("zero-points-v1",[0]),entrants,[new("a",2,true,false),new("b",3,true,false),new("c",1,true,false),new("d",4,true,false)]);
  var table=ChampionshipScoring.Rebuild(entrants,[result]);
  Assert.Equal(new[]{"blue","red"},table.Constructors.Select(r=>r.Id));
  Assert.Equal(new[]{"c","a","b","d"},table.Drivers.Select(r=>r.Id));
 }
}
