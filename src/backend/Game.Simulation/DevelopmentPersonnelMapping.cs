using System.Collections.Immutable;
using Game.Domain;
namespace Game.Simulation;

/// <summary>Only the team's frozen printed staff attributes enter its development quote.</summary>
public static class DevelopmentPersonnelMapping
{
 public const string Version="matched-specialists-v1";
 public static string Specialty(string optionId)=>optionId switch{"speed" or "low-drag"=>"Aerodynamics and Power Systems","downforce"=>"Aerodynamics","handling" or "tyres" or "tyre-management" or "mechanical-grip"=>"Mechanical / Chassis","reliability"=>"Reliability","cooling-reliability"=>"Reliability and Power Systems",_=>throw new ArgumentException("Choose a supported refinement or package.")};
 public static DevelopmentPersonnel Map(IReadOnlyList<PersonnelCard> roster,string rosterVersion,string optionId)
 {
  _=Specialty(optionId);
  var managers=roster.Where(p=>p.Role=="Manager").ToArray();var technical=roster.Where(p=>p.Role=="Technical Engineer").ToArray();
  if(string.IsNullOrWhiteSpace(rosterVersion)||managers.Length!=1||technical.Length!=2||roster.Select(p=>p.Id).Distinct().Count()!=roster.Count)throw new ArgumentException("Observe one Manager and both distinct Technical Engineers from the frozen roster.");
  int Attribute(PersonnelCard person,string key)=>person.Attributes?.TryGetValue(key,out var value)==true&&value is >=1 and <=100?value:throw new ArgumentException("Missing bounded printed personnel attribute.");
  var quality=(int)technical.Average(p=>optionId switch{"speed" or "low-drag"=>(Attribute(p,"Aerodynamics")+Attribute(p,"Power Systems"))/2d,"downforce"=>Attribute(p,"Aerodynamics"),"reliability"=>Attribute(p,"Reliability"),"cooling-reliability"=>(Attribute(p,"Reliability")+Attribute(p,"Power Systems"))/2d,_=>Attribute(p,"Mechanical / Chassis")});
  return new(Version+"/"+rosterVersion+"/"+optionId+"/"+string.Join("/",roster.Where(p=>p.Role is "Manager" or "Technical Engineer").OrderBy(p=>p.Id,StringComparer.Ordinal).Select(p=>p.Id+"@"+p.SourceVersion)),Attribute(managers[0],"Efficiency"),Attribute(managers[0],"Development"),(int)technical.Average(p=>Attribute(p,"Development Efficiency")),quality);
 }
 public static ImmutableArray<DevelopmentQuote> Quotes(IReadOnlyList<PersonnelCard> roster,string rosterVersion,DevelopmentOutcomeControls? controls=null)=>PermanentDevelopment.Options.Select(o=>PermanentDevelopment.Quote(o.Id,Map(roster,rosterVersion,o.Id),controls)).ToImmutableArray();
 public static ImmutableArray<DevelopmentQuote> PresetQuotes(DevelopmentOutcomeControls? controls=null)=>PermanentDevelopment.Options.Select(o=>PermanentDevelopment.Quote(o.Id,new DevelopmentPersonnel("preset-development-neutral-v1",0,0,0,0),controls)).ToImmutableArray();
}
