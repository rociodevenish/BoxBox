using System.Collections.Immutable;
using System.Text.Json;
using Game.Domain;
namespace Game.Simulation;

public static class ConstructionCatalog
{
 public static ConstructionCatalogue Open()
 {
  using var json=JsonDocument.Parse(typeof(ConstructionCatalog).Assembly.GetManifestResourceStream("preseason.construction.json")!);
  var source=json.RootElement;
  var version=source.GetProperty("schemaVersion").GetString()+"/"+source.GetProperty("dataVersion").GetString();
  static ImmutableDictionary<string,double> Values(JsonElement element)=>element.EnumerateObject().ToImmutableDictionary(p=>p.Name,p=>p.Value.GetDouble());
  var categories=source.GetProperty("categories").EnumerateArray().Select(c=>new ConstructionCategory(c.GetProperty("id").GetString()!,c.GetProperty("label").GetString()!,c.GetProperty("options").EnumerateArray().Select(o=>new ConstructionOption(o.GetProperty("id").GetString()!,o.GetProperty("name").GetString()!,o.GetProperty("cost").GetInt32(),o.GetProperty("summary").GetString()!,Values(o.GetProperty("modifiers")),Values(o.GetProperty("handlingProfile")))).ToImmutableArray())).ToImmutableArray();
  var catalogue=new ConstructionCatalogue("construction-catalogue-v1/"+version,version,CarConstruction.MappingVersion,Values(source.GetProperty("baseCarReference").GetProperty("detailedRatings")),categories);
  CarConstruction.ValidateCatalogue(catalogue);
  return catalogue;
 }
}
