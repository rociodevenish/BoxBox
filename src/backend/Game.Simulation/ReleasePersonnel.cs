using System.Collections.Immutable;
using System.Text.Json;
using Game.Domain;
namespace Game.Simulation;

/// <summary>Frozen identity/card meanings and purchasable reference contracts, separate from owned library.</summary>
public static class ReleasePersonnel
{
 public static void Validate(ImmutableArray<ContentPersonnelDetail> people,ImmutableArray<PackageDevelopmentQuote> paid,ImmutableArray<RacePackageVersion> owned,PersonnelPool? pool=null)
 {
  static void Require(bool valid){if(!valid)throw new ArgumentException("Unsupported personnel candidate metadata or paid package contract.");}
  Require(!people.IsDefaultOrEmpty&&people.Select(p=>p.PersonId).Distinct().Count()==people.Length);
  if(pool is not null)Require(people.Select(p=>p.PersonId).ToHashSet().SetEquals(pool.People.Select(p=>p.Id))&&people.All(p=>pool.People.Single(card=>card.Id==p.PersonId).Role==p.Role));
  foreach(var person in people)
  {
   string[] implemented=person.Role switch{"Driver"=>WeekendPreparation.DriverKeys.ToArray(),"Manager"=>["Efficiency","Development"],"Race Engineer"=>["Setup"],"Technical Engineer"=>["Aerodynamics","Mechanical / Chassis","Power Systems","Reliability","Development Efficiency"],_=>throw new ArgumentException("Unsupported personnel role.")};
   string[] inactive=person.Role switch{"Manager"=>["Leadership","Operations"],"Race Engineer"=>["Tyre Reading","Strategy","Communication"],_=>[]};
   Require(!string.IsNullOrWhiteSpace(person.PersonId)&&!string.IsNullOrWhiteSpace(person.Provenance)&&!string.IsNullOrWhiteSpace(person.RatingScale)&&!string.IsNullOrWhiteSpace(person.PreferenceMeaning)&&!string.IsNullOrWhiteSpace(person.EffectScope));
   Require(!person.ImplementedAttributes.IsDefault&&!person.InactiveAttributes.IsDefault&&person.ImplementedAttributes.Length==implemented.Length&&person.ImplementedAttributes.ToHashSet().SetEquals(implemented)&&person.InactiveAttributes.Length==inactive.Length&&person.InactiveAttributes.ToHashSet().SetEquals(inactive));
   Require(person.Role=="Driver"?person.IdentityKind=="real-driver"&&person.ReferenceRole is "2026 reserve" or "2026 contracted race driver":person.IdentityKind=="fictional-staff"&&person.ReferenceRole==person.Role);
   if(pool is not null&&person.Role!="Driver")Require(pool.People.Single(p=>p.Id==person.PersonId).SourceVersion.StartsWith("fictional-",StringComparison.Ordinal));
  }
  Require(owned.Length==1&&JsonSerializer.Serialize(owned[0])==JsonSerializer.Serialize(RacePackages.Baseline));
  Require(!paid.IsDefault&&paid.Length==10&&paid.Select(q=>q.Id).Distinct().Count()==10&&paid.Select(q=>(q.Template.Id,q.Rush is not null)).Distinct().Count()==10);
  foreach(var quote in paid)
  {
   PackageDevelopment.ValidateQuote(quote);
   Require(quote.SourcePackage is null&&quote.Refinement is null&&quote.Risk==DevelopmentRiskControls.Default&&quote.Personnel==new DevelopmentPersonnel("preset-development-neutral-v1",0,0,0,0));
  }
 }
}
