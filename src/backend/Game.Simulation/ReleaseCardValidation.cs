using Game.Domain;
namespace Game.Simulation;
public static class ReleaseCardValidation
{
 public static void Image(CardImage? image,string entityId)
 {
  if(image is null)return;
  if(image.EntityId!=entityId||string.IsNullOrWhiteSpace(image.AssetId)||string.IsNullOrWhiteSpace(image.Url)||!ReleaseContent.SafeAssetUrl(image.Url)
   ||image.Width is <1 or >4096||image.Height is <1 or >4096||image.ObjectPosition is not("center 22%" or "center center" or "center"))throw new ArgumentException("Unsupported card image reference.");
 }
 public static void Reference(CardImage? image,IEnumerable<ContentAsset> assets)
 {
  if(image is not null&&!assets.Any(a=>a.Id==image.AssetId&&a.EntityId==image.EntityId&&a.RuntimeUrl==image.Url))throw new ArgumentException("Card image differs from its frozen asset mapping.");
 }
 public static void Person(PersonnelPresentation? display,string id,bool driver,int? price=null)
 {
  if(display is null)return;
  if(display.PersonId!=id||display.Price<0||price is {} actual&&display.Price!=actual||string.IsNullOrWhiteSpace(display.EffectScope)
   ||string.IsNullOrWhiteSpace(display.RatingScale)||string.IsNullOrWhiteSpace(display.PreferenceMeaning)
   ||display.IdentityKind!=(driver?"real-driver":"fictional-staff")||string.IsNullOrWhiteSpace(display.ReferenceRole)
   ||display.ImplementedAttributes.IsDefault||display.InactiveAttributes.IsDefault||display.ImplementedAttributes.Intersect(display.InactiveAttributes).Any())throw new ArgumentException("Unsupported personnel presentation.");
  if(driver&&(!display.SimpleGroups.Keys.ToHashSet().SetEquals(["Pace","Racecraft","Tyres","Consistency","Technical"])||display.SimpleGroups.Values.Any(v=>v is <1 or >5))||!driver&&display.SimpleGroups.Count!=0)throw new ArgumentException("Invalid authored Simple groups.");
  Image(display.Image,id);
 }
}
