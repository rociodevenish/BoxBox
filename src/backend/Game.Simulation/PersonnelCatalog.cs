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
     role=="Race Engineer"?"Provisional fictional staff. Setup improves the assigned driver's Practice Assessment confidence. Tyre Reading, Strategy and Communication are inactive; no pace or error bonus.":role=="Manager"?"Efficiency contributes once to the bounded development discount. Development improves normal-outcome likelihood within the shared 10 percentage-point quality cap. Leadership and Operations are inactive; no raw pace bonus.":"Development Efficiency contributes once to the bounded cost discount. Matching Aerodynamics/Power Systems, Mechanical / Chassis or Reliability contributes to bounded development quality. Both Technical Engineers contribute; no raw pace or development-speed bonus.",
     Attributes:attributes.ToImmutableDictionary(k=>k,_=>70+i%5*4)));
   }
  // Additional unique fictional staff back every mandatory staff place at base price.
  // Existing people and printed prices are unchanged; no team receives extra credits.
  foreach(var (role,prefix,count,price) in new[]{("Manager","manager",6,8),("Race Engineer","race-engineer",13,6),("Technical Engineer","technical-engineer",13,5)})
   for(var i=0;i<count;i++)
   {
    var reference=people.First(p=>p.Role==role);
    people.Add(new($"staff-{prefix}-completion-{i+1:00}",$"{first[(i+8)%first.Length]} {last[(i+19+(role=="Manager"?0:role=="Race Engineer"?5:10))%last.Length]}",role,price,"fictional-completion-staff-v1",reference.EffectLabel,
     Attributes:reference.Attributes!.ToImmutableDictionary(k=>k.Key,_=>70+i%5*4)));
   }
  using var construction=JsonDocument.Parse(typeof(PersonnelCatalog).Assembly.GetManifestResourceStream("preseason.construction.json")!);
  var source=construction.RootElement;
  var minimum=source.GetProperty("categories").EnumerateArray().Sum(category=>category.GetProperty("options").EnumerateArray().Min(option=>option.GetProperty("cost").GetInt32()));
  if(minimum!=18||source.GetProperty("budgetModel").GetProperty("currency").GetString()!="budgetUnits"||source.GetProperty("budgetModel").GetProperty("possibleBuildCostRange").GetProperty("min").GetInt32()!=minimum)throw new InvalidOperationException("Review the provisional minimum construction mapping before opening acquisition.");
  var mapping=new ConstructionCreditMapping("budgetUnits-to-credits-v1",source.GetProperty("schemaVersion").GetString()+"/"+source.GetProperty("dataVersion").GetString(),minimum,1);
  return new("personnel-pool-v1/"+version+"/completion-supply-v1","provisional-prices-v1",100,minimum,people.ToImmutableArray(),mapping);
 }
}
