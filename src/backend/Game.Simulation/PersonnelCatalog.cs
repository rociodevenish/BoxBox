using System.Collections.Immutable;
using System.Text.Json;
using Game.Domain;
namespace Game.Simulation;

/// <summary>Provisional authored prices and fictional staff; source driver inputs retain their detailed-rating mapping.</summary>
public static class PersonnelCatalog
{
 public static PersonnelPool Open()
 {
  using var drivers=JsonDocument.Parse(typeof(PersonnelCatalog).Assembly.GetManifestResourceStream("weekend.drivers.json")!);
  var version=drivers.RootElement.GetProperty("schemaVersion").GetString()+"/"+drivers.RootElement.GetProperty("dataVersion").GetString();
  var people=drivers.RootElement.GetProperty("drivers").EnumerateArray().Select(row=>
  {
   var id=row.GetProperty("id").GetString()!;
   var driver=WeekendPresetCatalog.Open("track-monza",id).Cars[0].Driver;
   // Provisional price tiers use detailed race pace only; no conversion of headline stars into simulation attributes.
   var price=driver.Ratings["racePace"]>=90?24:driver.Ratings["racePace"]>=82?18:12;
   return new PersonnelCard(id,driver.Name,"Driver",price,version,"Detailed driver inputs apply after roster completion; preferences describe compatibility.",driver);
  }).ToList();
  string[] first=["Alex","Morgan","Taylor","Sam","Robin","Jamie","Casey","Avery","Jordan","Riley","Quinn","Cameron","Drew","Reese","Hayden","Skyler","Rowan","Parker","Emery","Finley","Harper","Sage","Ellis","Blair","Charlie","Frankie"];
  string[] last=["Bennett","Patel","Chen","Silva","Martin","Khan","Reed","Walker","Costa","Kim","Shaw","Brooks","Ali","Ward","Lane","Fox","Ross","Gray","Bell","Wood","King","Hill","Green","Scott","Adams","Baker"];
  foreach(var (role,prefix,count,basePrice) in new[]{("Manager","manager",15,8),("Race Engineer","race-engineer",26,6),("Technical Engineer","technical-engineer",26,5)})
   for(var i=0;i<count;i++)
   {
    var attributes=role=="Manager"?new[]{"Leadership","Operations","Development","Efficiency"}:role=="Race Engineer"?new[]{"Setup","Tyre Reading","Strategy","Communication"}:new[]{"Aerodynamics","Mechanical / Chassis","Power Systems","Reliability","Development Efficiency"};
    people.Add(new($"staff-{prefix}-{i+1:00}",$"{first[i]} {last[(i+(prefix=="manager"?0:prefix=="race-engineer"?7:13))%last.Length]}",role,basePrice+i%3,"fictional-staff-v1",
     role=="Race Engineer"?"Provisional fictional staff. Information support activates with personnel integration; no pace or error bonus.":"Provisional fictional staff. Development and resource effects await the economy system; no current performance bonus.",
     Attributes:attributes.ToImmutableDictionary(k=>k,_=>70+i%5*4)));
   }
  return new("personnel-pool-v1/"+version,"provisional-prices-v1",100,18,people.ToImmutableArray());
 }
}
