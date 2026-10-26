using System.Collections.Immutable;
using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public sealed class PersonnelAdviceTests
{
 [Fact]
 public void Advice_uses_only_offered_legal_choices_and_preserves_the_completion_reserve()
 {
  var observation=new PersonnelPolicyObservation("manager",100,96,[new("expensive","Expensive",20,false,90),new("legal","Legal",8,true,88)]);
  var advice=PersonnelDecisionPolicy.Evaluate(observation);
  Assert.Equal("legal",advice.PersonId);Assert.Contains("reserve",advice.Reason);Assert.Equal("offer-only-low-cost-v1",advice.Version);
  Assert.Equal(advice,PersonnelDecisionPolicy.Evaluate(observation with{Offers=observation.Offers.Reverse().ToImmutableArray()}));
 }
}
