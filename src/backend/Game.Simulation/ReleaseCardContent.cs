using System.Collections.Immutable;
using System.Text.Json;
using System.Text.RegularExpressions;
using Game.Domain;
namespace Game.Simulation;

public static partial class ReleaseContent
{
 public const string CardVersion="release-2026-cards-v1";
 public static bool IsFullCalendar(string? version)=>version is FullCalendarVersion or CardVersion;
 public static ReleaseContentPack Cards()
 {
  var full=FullCalendar();var personnel=PersonnelCandidate();
  using var source=JsonDocument.Parse(typeof(ReleaseContent).Assembly.GetManifestResourceStream("weekend.drivers.json")!);
  var headlines=source.RootElement.GetProperty("drivers").EnumerateArray().ToDictionary(d=>d.GetProperty("id").GetString()!,d=>d.GetProperty("ratings").GetProperty("headline").EnumerateObject().ToImmutableDictionary(p=>char.ToUpperInvariant(p.Name[0])+p.Name[1..],p=>p.Value.GetInt32()));
  var names=source.RootElement.GetProperty("drivers").EnumerateArray().ToDictionary(d=>d.GetProperty("id").GetString()!,d=>d.GetProperty("identity").GetProperty("fullName").GetString()!);
  var images=CardImages();
  var people=personnel.Personnel.People.Select(p=>
  {
   var detail=personnel.PersonnelDetails.Single(d=>d.PersonId==p.Id);
   var display=new PersonnelPresentation(p.Id,detail.IdentityKind,detail.ReferenceRole,p.Price,
    headlines.GetValueOrDefault(p.Id)??ImmutableDictionary<string,int>.Empty,detail.ImplementedAttributes,detail.InactiveAttributes,
    detail.RatingScale,detail.PreferenceMeaning,detail.EffectScope,images.GetValueOrDefault(p.Id));
   return p with{Name=names.GetValueOrDefault(p.Id)??p.Name,Presentation=display,Driver=p.Driver is {} driver?driver with{Name=names[p.Id],Presentation=display}:null};
  }).ToImmutableArray();
  var assets=full.Assets.Concat(personnel.Assets).GroupBy(a=>a.Id).Select(g=>g.First()).Select(a=>images.TryGetValue(a.EntityId,out var image)?a with{RuntimeUrl=image.Url}:a).ToImmutableArray();
  return full with{Version=CardVersion,Personnel=personnel.Personnel with{Version=personnel.Personnel.Version+"/cards-v1",People=people},
   PersonnelDetails=personnel.PersonnelDetails,PaidPackageOptions=personnel.PaidPackageOptions,Assets=assets,
   Circuits=full.Circuits.Select(c=>c with{Image=images.GetValueOrDefault(c.Id),Summary=full.Events.Single(e=>e.TrackId==c.Id).Summary}).ToImmutableArray(),
   Provenance=full.Provenance+" Combined personnel/display release: authored headline groups remain display-only; detailed ratings, actual prices, identities and image references are frozen."};
 }
 private static Dictionary<string,CardImage> CardImages()
 {
  using var stream=typeof(ReleaseContent).Assembly.GetManifestResourceStream("content.assets.json");
  if(stream is null)return [];
  using var document=JsonDocument.Parse(stream);
  return document.RootElement.GetProperty("assets").EnumerateArray().Where(a=>a.GetProperty("status").GetString()=="available").ToDictionary(a=>a.GetProperty("entityId").GetString()!,a=>new CardImage(a.GetProperty("assetId").GetString()!,a.GetProperty("entityId").GetString()!,a.GetProperty("runtimeUrl").GetString()!,a.GetProperty("objectPosition").GetString()!,a.GetProperty("width").GetInt32(),a.GetProperty("height").GetInt32()));
 }
 public static bool BundledCardImage(CardImage image)=>CardImages().TryGetValue(image.EntityId,out var expected)&&expected==image;
 public static bool SafeAssetUrl(string? url)=>url is null||Regex.IsMatch(url,@"^/(?:content/[a-f0-9]{64}\.webp|(?:portraits|tracks)/[a-z0-9][a-z0-9-]*\.(?:webp|png))$",RegexOptions.CultureInvariant);
}
