import {readFileSync,writeFileSync,readdirSync} from 'node:fs';
import {gunzipSync} from 'node:zlib';
import {createHash} from 'node:crypto';
import {join} from 'node:path';
const root=process.argv[2];if(!root)throw Error('Pass archive directory');
const files=readdirSync(root).filter(f=>/^track-.*\.json\.gz$/.test(f)).sort();
if(files.length!==10)throw Error('Expected ten frozen track/profile groups');
const archives=files.map(file=>{const bytes=readFileSync(join(root,file));return {file,sha256:createHash('sha256').update(bytes).digest('hex'),archive:JSON.parse(gunzipSync(bytes))};});
const archive={...archives[0].archive,runs:archives.flatMap(a=>a.archive.runs)};
const grouped=Map.groupBy(archive.runs,r=>`${r.circuit}/${r.profile}`);
const mean=a=>a.reduce((s,x)=>s+x,0)/a.length;
function uncertainty(a){const m=a.length?mean(a):null;return {nSeedPairs:a.length,mean:m,minimum:a.length?Math.min(...a):null,maximum:a.length?Math.max(...a):null,approximate95HalfWidth:a.length>1?2.365*Math.sqrt(a.reduce((s,x)=>s+(x-m)**2,0)/(a.length-1))/Math.sqrt(a.length):null};}
const comparisons=[];const forecastQuality=[];const profiles=[];
for(const [identity,runs] of grouped){
 const arms=Map.groupBy(runs,r=>r.arm);
 for(const seed of new Set(runs.map(r=>r.seed))){
  const paired=runs.filter(r=>r.seed===seed);
  const exogenous=r=>JSON.stringify({...r.opening.configuration.weather,informationQuality:null});
  if(paired.length!==7||new Set(paired.map(exogenous)).size!==1)throw Error('Pair exogenous weather differs: '+identity+'/'+seed);
  if(paired.some(r=>r.final.classification.length!==22||!r.final.finished))throw Error('Incomplete field');
 }
 const targets=r=>r.final.classification.filter(c=>c.driverId.startsWith('team-01-'));
 const firstPit=(r,id)=>r.final.events.find(e=>e.driverId===id&&e.kind==='PitEntry')?.time??null;
 const crossing=(r,id)=>r.final.events.find(e=>e.driverId===id&&e.kind==='LapCompleted'&&e.lap===9)?.time??null;
 for(const [arm,samples] of arms){
  const baseline=arms.get('observed');
  const pairs=samples.map(r=>{
   const b=baseline.find(b=>b.seed===r.seed);
   return {seed:r.seed,positionDifference:mean(targets(r).map(c=>c.position))-mean(targets(b).map(c=>c.position)),targets:targets(r).map(c=>{
    const bc=targets(b).find(t=>t.driverId===c.driverId);const a=crossing(r,c.driverId),z=crossing(b,c.driverId);
    return {entryId:c.driverId,baseline:{laps:bc.laps,eligible:b.final.eligibility[c.driverId],firstPitTime:firstPit(b,c.driverId)},alternative:{laps:c.laps,eligible:r.final.eligibility[c.driverId],firstPitTime:firstPit(r,c.driverId)},matchedLap9DeltaSeconds:a===null||z===null?null:a-z};
   })};
  });
  comparisons.push({identity,arm,baseline:'observed',positionDifference:uncertainty(pairs.map(p=>p.positionDifference)),excludedMissingLap9Seeds:pairs.filter(p=>p.targets.some(t=>t.matchedLap9DeltaSeconds===null)).length,pairedTeamMeanLap9Delta:uncertainty(pairs.filter(p=>p.targets.every(t=>t.matchedLap9DeltaSeconds!==null)).map(p=>mean(p.targets.map(t=>t.matchedLap9DeltaSeconds)))),pairs});
 }
 const low=arms.get('quality-low'),high=arms.get('quality-high');
 for(const horizon of [300,900]){
  const seeds=low.map(l=>{
   const h=high.find(h=>h.seed===l.seed);
   function stats(r){const f=r.forecasts.filter(f=>f.horizonSeconds===horizon);return {observations:f.length,mae:mean(f.map(f=>Math.abs(f.probability-f.actualMeanIntensity))),coverage:mean(f.map(f=>+(f.actualMeanIntensity>=f.lower&&f.actualMeanIntensity<=f.upper))),width:mean(f.map(f=>f.upper-f.lower))};}
   const a=stats(l),b=stats(h);return {seed:l.seed,low:a,high:b,maeDelta:b.mae-a.mae,coverageDelta:b.coverage-a.coverage,widthDelta:b.width-a.width};
  });
  forecastQuality.push({identity,horizonSeconds:horizon,uniqueObservedForecastTrajectories:new Set(low.map(r=>JSON.stringify(r.forecasts))).size,maeDelta:uncertainty(seeds.map(s=>s.maeDelta)),coverageDelta:uncertainty(seeds.map(s=>s.coverageDelta)),widthDelta:uncertainty(seeds.map(s=>s.widthDelta)),seeds});
 }
 const observed=arms.get('observed');
 profiles.push({identity,meanWeatherWindows:mean(observed.map(r=>r.operations.filter(o=>o.kind==='Weather').length)),minWetness:Math.min(...observed.flatMap(r=>r.forecasts.map(f=>f.observed.wetness))),maxWetness:Math.max(...observed.flatMap(r=>r.forecasts.map(f=>f.observed.wetness))),configuration:observed[0].opening.configuration,versions:observed[0].versions});
}
writeFileSync(join(root,'comparisons.json'),JSON.stringify({schema:'weather-paired-summary-v1',archives:archives.map(({file,sha256})=>({file,sha256})),seededWeekendPairs:80,stochasticWeatherPaths:48,deterministicProfileGroups:4,weekends:archive.runs.length,opponentPolicy:archive.opponentPolicy,profileVersion:archive.profileVersion,notes:['Eight seed pairs per track/profile. The two target drivers are a paired team, not independent samples.','Repeated issue times and horizons share the same weather path; uncertainty uses seed-level means.','Approximate t intervals with 7 df are descriptive and unadjusted for multiple comparisons.','Forecast range coverage evaluates mean future rain intensity at fixed 10-second midpoint samples; this is not probability calibration of binary rain.','Fixed compound arms intentionally disable target stops; eligibility and distance groups remain reported.'],profiles,comparisons,forecastQuality}));
console.log(`Summarized ${archive.runs.length} frozen weekends; all exogenous pairs and complete fields verified.`);
for(const p of profiles)console.log(p.identity,'weather windows',p.meanWeatherWindows,'observed wetness',p.minWetness.toFixed(3),p.maxWetness.toFixed(3));
