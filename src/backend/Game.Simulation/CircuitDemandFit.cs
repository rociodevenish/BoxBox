using System.Collections.Immutable;
using Game.Domain;
namespace Game.Simulation;

// Shared design-fit arithmetic. Callers retain their saved policy's units and precision.
internal static class CircuitDemandFit
{
 internal static double Score(ImmutableDictionary<string,double> changes,ImmutableDictionary<string,double> ratings,
  ImmutableDictionary<string,double> demands,bool reliabilityActive,double normalization,int? decimalPlaces,
  ImmutableDictionary<string,double>? compromises=null)
 {
  double Weight(string key)=>key switch{
   "highSpeedAero"=>demands["highSpeedCornering"],"lowSpeedAero"=>demands["lowSpeedCornering"],
   "tyreWear"=>demands["tyreDegradation"],"tyreTemperature"=>demands["tyreThermalDemand"],
   "reliability"=>reliabilityActive?50:0,_=>demands[key]};
  var score=WeekendPreparation.CarKeys.Sum(key=>
  {
   var changed=ratings[key]+changes.GetValueOrDefault(key);
   // Selection v1 adds separate strength/compromise terms; project v2 supplies combined changes.
   if(compromises is not null)changed+=compromises.GetValueOrDefault(key);
   return (Math.Clamp(changed,1,100)-ratings[key])*Weight(key)/normalization;
  });
  return decimalPlaces is {} digits?Math.Round(score,digits):score;
 }
}
