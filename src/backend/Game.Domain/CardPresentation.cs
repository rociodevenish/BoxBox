using System.Collections.Immutable;
namespace Game.Domain;

// Frozen presentation metadata. No conversion into simulation capabilities or outcome controls.
public sealed record CardImage(string AssetId,string EntityId,string Url,string ObjectPosition,int Width,int Height);
public sealed record PersonnelPresentation(string PersonId,string IdentityKind,string ReferenceRole,int Price,
 ImmutableDictionary<string,int> SimpleGroups,ImmutableArray<string> ImplementedAttributes,
 ImmutableArray<string> InactiveAttributes,string RatingScale,string PreferenceMeaning,string EffectScope,CardImage? Image=null);
