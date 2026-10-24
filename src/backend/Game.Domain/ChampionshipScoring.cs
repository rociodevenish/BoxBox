namespace Game.Domain;

public record ChampionshipPoints(string Version,int[] ByPosition);
public record ChampionshipEntrant(string PersonId,string TeamId,int StartingOrder);
public record ChampionshipFinish(string PersonId,int Position,bool Eligible,bool Retired);
public record ChampionshipAward(string PersonId,string TeamId,int Position,bool Counted,int Points);
public record ChampionshipStanding(string Id,int Position,int Points,int[] EligibleFinishes,int StartingOrder);
public record ChampionshipTable(IReadOnlyList<ChampionshipStanding> Drivers,IReadOnlyList<ChampionshipStanding> Constructors);

/// <summary>Authored game rules: raw positions, no promotion, no fastest-lap award.</summary>
public static class ChampionshipScoring
{
 public const string Version="championship-scoring-v1";
 public static ChampionshipPoints Default=>new("game-points-25-18-v1",[25,18,15,12,10,8,6,4,2,1]);
 public static ChampionshipAward[] Score(ChampionshipPoints points,IReadOnlyList<ChampionshipEntrant> entrants,IReadOnlyList<ChampionshipFinish> finishes)
 {
  if(string.IsNullOrWhiteSpace(points.Version)||points.ByPosition.Length==0||points.ByPosition.Any(p=>p<0))throw new ArgumentException("Invalid versioned game points.");
  if(entrants.Select(e=>e.PersonId).Distinct(StringComparer.Ordinal).Count()!=entrants.Count||finishes.Count!=entrants.Count||finishes.Select(f=>f.PersonId).Distinct(StringComparer.Ordinal).Count()!=finishes.Count||finishes.Select(f=>f.Position).Order().Where((p,i)=>p!=i+1).Any())throw new ArgumentException("Classification must contain each frozen entrant and raw position once.");
  return finishes.Select(f=>{var entrant=entrants.Single(e=>e.PersonId==f.PersonId);var counted=f.Eligible&&!f.Retired;return new ChampionshipAward(f.PersonId,entrant.TeamId,f.Position,counted,counted&&f.Position<=points.ByPosition.Length?points.ByPosition[f.Position-1]:0);}).ToArray();
 }
 public static ChampionshipTable Rebuild(IReadOnlyList<ChampionshipEntrant> entrants,IEnumerable<IReadOnlyList<ChampionshipAward>> contributions)
 {
  var awards=contributions.SelectMany(c=>c).ToArray();var count=entrants.Count;
  ChampionshipStanding Row(string id,int start,IEnumerable<ChampionshipAward> rows){var values=rows.ToArray();return new(id,0,values.Sum(v=>v.Points),Enumerable.Range(1,count).Select(p=>values.Count(v=>v.Counted&&v.Position==p)).ToArray(),start);}
  IReadOnlyList<ChampionshipStanding> Rank(IEnumerable<ChampionshipStanding> rows)=>rows.OrderBy(r=>r,Comparer<ChampionshipStanding>.Create((a,b)=>{var points=b.Points.CompareTo(a.Points);if(points!=0)return points;for(var p=0;p<count;p++){var finish=b.EligibleFinishes[p].CompareTo(a.EligibleFinishes[p]);if(finish!=0)return finish;}return a.StartingOrder.CompareTo(b.StartingOrder);})).Select((row,i)=>row with{Position=i+1}).ToArray();
  return new(Rank(entrants.Select(e=>Row(e.PersonId,e.StartingOrder,awards.Where(a=>a.PersonId==e.PersonId)))),Rank(entrants.GroupBy(e=>e.TeamId,StringComparer.Ordinal).Select(g=>Row(g.Key,g.Min(e=>e.StartingOrder),awards.Where(a=>a.TeamId==g.Key)))));
 }
}
