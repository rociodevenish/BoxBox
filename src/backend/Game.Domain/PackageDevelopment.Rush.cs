using System.Security.Cryptography;
using System.Text.Json;
namespace Game.Domain;
public static partial class PackageDevelopment
{
 public const string RushVersion="package-development-v3";
 public static PackageDevelopmentQuote Rush(PackageDevelopmentQuote standard)
 {
  ValidateQuote(standard);
  if(standard.Risk is null||standard.Distribution is null||standard.Rush is not null)throw new ArgumentException("Rush one new rich package creation or refinement quote explicitly.");
  var normal=standard.Distribution;
  var earlier=Math.Min(.08,normal.Delay*.6);
  var additionalCompromise=Math.Min(.1,normal.Normal+normal.Partial);
  var fromNormal=Math.Min(normal.Normal,additionalCompromise);
  var distribution=normal with{Delay=normal.Delay-earlier,Normal=normal.Normal+earlier-fromNormal,Partial=normal.Partial-(additionalCompromise-fromNormal),Compromised=normal.Compromised+additionalCompromise};
  distribution.Validate();
  const int fee=6;
  string Percent(double probability)=>(probability*100).ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)+"%";
  var aim=$"Standard work already aims for the intended preparation close. Quoted intended-event availability: standard {Percent(1-normal.Delay)}, Rush {Percent(1-distribution.Delay)}. "+(earlier>0?"Rush reduces delay probability; earlier availability is an aim, not guaranteed. ":"This frozen scenario already has no delay reduction available; Rush cannot improve its arrival chance. ")+"The fixed extra cost is 6 credits. "+(additionalCompromise>0?"More probability moves to compromised work. ":"This frozen scenario has no normal/partial probability to move into compromised work. ")+"The same project class draw supplies every outcome.";
  var contract=new RushContract("package-rush-v1",standard.Cost,fee,normal,aim);
  var range=distribution.Range+" Rush total includes the fixed 6-credit fee. Standard: "+normal.Range+standard.OutcomeRange[normal.Range.Length..]+" No second staff discount or independent risk draw is added.";
  var timing=aim+" Delayed work still completes at the following preparation close without repayment, or remains pending beyond the calendar.";
  var identity=JsonSerializer.SerializeToUtf8Bytes(new{Base=standard.Id,Version=RushVersion,Rush=contract,Distribution=distribution,Range=range,Timing=timing});
  // Keep OutcomeVersion: Rush changes thresholds on the same project/class/version draw.
  return standard with{Id=Convert.ToHexString(SHA256.HashData(identity)),Version=RushVersion,Cost=standard.Cost+fee,Rush=contract,Distribution=distribution,OutcomeRange=range,Timing=timing};
 }
}
