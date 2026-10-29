using System.Security.Cryptography;
using System.Text.Json;
namespace Game.Domain;
public sealed record DevelopmentRiskControls(string Version,double Normal,double Partial,double Breakthrough,double Compromised,double Delay)
{
 public const string Model="development-risk-v1";
 public static readonly DevelopmentRiskControls Default=new(Model,.45,.2,.1,.15,.1);
 public static DevelopmentRiskControls Only(string kind)=>new(Model,kind=="normal"?1:0,kind=="partial"?1:0,kind=="breakthrough"?1:0,kind=="compromised"?1:0,kind=="delay"?1:0);
 public void Validate(){var values=new[]{Normal,Partial,Breakthrough,Compromised,Delay};if(Version!=Model||values.Any(v=>!double.IsFinite(v)||v<0)||Math.Abs(values.Sum()-1)>1e-9)throw new ArgumentException("Supply five finite nonnegative outcome probabilities summing to one.");}
 public DevelopmentRiskControls WithPersonnel(DevelopmentPersonnel personnel)
 {
  Validate();var quality=Math.Min(.1,(personnel.ManagerDevelopment+personnel.TechnicalQuality)/2000d);
  var partial=Math.Min(Partial,quality/2);var compromised=Math.Min(Compromised,quality/2);
  return this with{Normal=Normal+partial+compromised,Partial=Partial-partial,Compromised=Compromised-compromised};
 }
 private static string Percent(double value)=>(value*100).ToString("0",System.Globalization.CultureInfo.InvariantCulture)+"%";
 public string Range=>$"Normal {Percent(Normal)}; partial {Percent(Partial)}; breakthrough {Percent(Breakthrough)}; compromised {Percent(Compromised)}; delay {Percent(Delay)}.";
 public string Resolve(double draw){Validate();return draw<Delay?"delay":draw<Delay+Compromised?"compromised":draw<Delay+Compromised+Breakthrough?"breakthrough":draw<Delay+Compromised+Breakthrough+Partial?"partial":"normal";}
 public static double Draw(string entropy,string opportunity,string project,string version,string channel)=>System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(HMACSHA256.HashData(Convert.FromHexString(entropy),JsonSerializer.SerializeToUtf8Bytes(new[]{version,opportunity,project,channel})))/(uint.MaxValue+1d);
}
