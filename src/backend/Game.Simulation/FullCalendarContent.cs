using System.Collections.Immutable;
using System.Text.Json;
using Game.Domain;
namespace Game.Simulation;

public static partial class ReleaseContent
{
 public const string FullCalendarVersion="release-2026-full-circuits-v1";
 public const string FullCalendarOrderVersion="official-2026-23-events-snapshot-2026.10.30-v1";
 public const string CircuitProfileVersion="authored-four-sector-profiles-v1";
 // These are explicit GAME hypotheses, not real sectors, measured lap times or conversions of card stars.
 // Fractions describe four abstract sectors: start/finish straight, fast complex, second straight, slow complex.
 private sealed record Profile(string Id,double Seconds,double[] Lengths,double[] Times,double Passing,double PitLoss,double Stress,string Weather,double SafetySeconds);
 private static readonly Profile[] Profiles=[
  new("track-albert-park",92,[.24,.30,.19,.27],[.18,.34,.15,.33],.48,21,1.00,"changeable",150),
  new("track-shanghai",96,[.29,.25,.22,.24],[.20,.30,.16,.34],.60,23,1.12,"rain-arrival",150),
  new("track-suzuka",94,[.21,.39,.17,.23],[.16,.40,.13,.31],.35,22,1.18,"changeable",150),
  new("track-miami",93,[.30,.23,.24,.23],[.22,.27,.18,.33],.55,22,1.05,"all-dry",120),
  new("track-circuit-gilles-villeneuve",86,[.29,.22,.26,.23],[.21,.27,.19,.33],.62,20,1.20,"rain-arrival",150),
  new("track-monaco",83,[.16,.26,.13,.45],[.12,.27,.10,.51],.12,20,1.04,"all-dry",180),
  new("track-barcelona-catalunya",91,[.25,.34,.16,.25],[.18,.36,.12,.34],.37,22,1.15,"all-dry",150),
  new("track-red-bull-ring",76,[.31,.22,.25,.22],[.22,.28,.18,.32],.62,19,1.16,"changeable",120),
  new("track-silverstone",94,[.23,.41,.16,.20],[.17,.43,.12,.28],.44,23,1.22,"rain-arrival",150),
  new("track-spa-francorchamps",109,[.28,.33,.22,.17],[.20,.37,.16,.27],.62,24,1.28,"changeable",180),
  new("track-hungaroring",89,[.20,.33,.13,.34],[.15,.35,.10,.40],.24,21,1.08,"all-dry",150),
  new("track-zandvoort",85,[.18,.39,.14,.29],[.13,.42,.10,.35],.26,21,1.17,"changeable",150),
  new("track-monza",84,[.33,.18,.30,.19],[.23,.27,.21,.29],.68,22,1.30,"all-dry",150),
  new("track-madring",94,[.26,.29,.19,.26],[.19,.32,.14,.35],.46,22,1.10,"all-dry",150),
  new("track-baku",104,[.36,.18,.21,.25],[.24,.25,.15,.36],.67,23,1.27,"all-dry",180),
  new("track-sepang",102,[.29,.29,.23,.19],[.20,.34,.16,.30],.57,23,1.25,"steady-wet",180),
  new("track-marina-bay",103,[.21,.24,.17,.38],[.15,.27,.12,.46],.28,25,1.22,"changeable",180),
  new("track-cota",100,[.26,.33,.21,.20],[.18,.36,.15,.31],.52,23,1.20,"all-dry",150),
  new("track-mexico-city",87,[.30,.22,.22,.26],[.21,.28,.16,.35],.57,22,1.24,"all-dry",150),
  new("track-interlagos",82,[.28,.28,.20,.24],[.20,.32,.14,.34],.59,21,1.14,"rain-arrival",150),
  new("track-las-vegas",99,[.38,.17,.25,.20],[.26,.25,.17,.32],.72,23,1.21,"all-dry",150),
  new("track-lusail",88,[.23,.41,.15,.21],[.17,.43,.11,.29],.37,22,1.32,"all-dry",150),
  new("track-yas-marina",94,[.29,.25,.22,.24],[.20,.30,.16,.34],.51,22,1.13,"all-dry",150)
 ];
 public static ReleaseContentPack FullCalendar()
 {
  var pilot=Pilot();var circuits=ImmutableArray.CreateBuilder<CircuitPreset>();var conditions=ImmutableDictionary.CreateBuilder<string,RaceConfiguration>();
  foreach(var profile in Profiles)
  {
   var source=WeekendPresetCatalog.Open(profile.Id).Circuit;var length=source.Track.Segments!.Sum(s=>s.Length);
   string[] names=["start-finish-straight","fast-complex","second-straight","slow-complex"];
   var segments=names.Select((name,i)=>new TrackSegment(profile.Id+"/"+name,length*profile.Lengths[i],profile.Seconds*profile.Times[i],
    SpeedDemand:i is 0 or 2?1:.15,CorneringDemand:i is 0 or 2?.05:1,TyreDemand:i==1?1.15:i==3?1.1:1,
    PassingAllowed:i==0||i==2&&profile.Passing>=.35,PassingDifficulty:1-profile.Passing,PassingZoneId:profile.Id+"/"+name)).ToArray();
   // PitRoute bypass is a DISTANCE fraction. Integrate the authored baseline time of that
   // distance, rather than treating nonuniform sectors as equal-time fractions of a lap.
   var bypassDistance=length*.12;var bypassSeconds=0d;
   foreach(var segment in segments)
   {
    var distance=Math.Min(bypassDistance,segment.Length);bypassSeconds+=segment.BaselineTime*distance/segment.Length;
    bypassDistance-=distance;if(bypassDistance<=1e-9)break;
   }
   var route=new PitRoute(.12,profile.PitLoss+bypassSeconds-3);
   var track=new Track(source.Name,profile.Seconds,profile.Lengths[0]+profile.Lengths[2],profile.PitLoss,3,profile.Passing,Array.AsReadOnly(segments),route);
   var circuit=source with{Track=track};circuits.Add(circuit);
   var neutral=WeekendPreparation.Prepare(WeekendPresetCatalog.Open(profile.Id) with{Circuit=circuit},6,new());
   var weather=WeatherWeekendProfiles.Apply(neutral,profile.Weather) with{SourceVersion=CircuitProfileVersion+"/"+profile.Id+"/"+profile.Weather};
   // Authored short-race rain ramp: a six-lap event can cross the existing dry/intermediate band.
   // These bounded fractions/rates are game tuning, not a calendar or meteorological fact.
   if(profile.Weather=="rain-arrival")weather=weather with{MeanRain=.75,Volatility=.10,WettingPerSecond=.0015,DryingPerSecond=.00015};
   conditions.Add(profile.Id,neutral.Configuration with{Track=track,Incidents=new(ErrorRate:.00002,ContactProbability:.0003,FailureRate:.000001,
    MechanicalStress:profile.Stress,InterruptMajorIncidents:true),SafetyCar=new(DurationSeconds:profile.SafetySeconds),
    Weather=weather,WetTyres=new(),WeatherReactions=new(),DisruptionPolicyVersion=CircuitProfileVersion+"/"+profile.Id});
  }
  using var json=JsonDocument.Parse(typeof(ReleaseContent).Assembly.GetManifestResourceStream("weekend.tracks.json")!);
  var events=json.RootElement.GetProperty("calendar").EnumerateArray().Select(row=>
  {
   var id=row.GetProperty("trackId").GetString()!;var profile=Profiles.Single(p=>p.Id==id);
   return new ContentCalendarEvent(row.GetProperty("id").GetString()!,row.GetProperty("grandPrixName").GetString()!,id,
    row.GetProperty("weekendStart").GetString()!,row.GetProperty("weekendEnd").GetString()!,
    $"{(profile.Passing<.35?"Limited passing; protect track position":"Passing opportunities on the straights")}. Typical green-flag Pit Loss {profile.PitLoss:0} seconds. Balance straight performance, corner support and tyre life.");
  }).ToImmutableArray();
  var assets=pilot.Assets.Where(a=>!a.EntityId.StartsWith("track-",StringComparison.Ordinal)).Concat(circuits.Select(c=>new ContentAsset("asset-"+c.Id,c.Id,"/tracks/"+c.Slug+"-960.webp",
   "Existing explicit track manifest; user-supplied illustration, not simulation geometry.","assets/track images/"+c.Slug+".png"))).ToImmutableArray();
  return pilot with{Version=FullCalendarVersion,CalendarVersion=FullCalendarOrderVersion,Circuits=circuits.ToImmutable(),Calendar=events.Select(e=>e.TrackId).ToImmutableArray(),
   Events=events,Conditions=conditions.ToImmutable(),Assets=assets,Provenance="Verified revised 2026 23-event calendar snapshot accessed 2026-10-30, including Bahrain brand at Sepang, Malaysia. Season-opening contracted roster. Circuit segments, timings, passing, Pit Loss, stress/weather/SC coefficients are authored game hypotheses, not measured physics; see S16 provenance/profile evidence."};
 }
}
