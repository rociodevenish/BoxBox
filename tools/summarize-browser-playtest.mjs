import { readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
const input = process.argv[2] ?? '.tools/reports/browser-race/full-distance';
const output = process.argv[3] ?? '.scratch/browser-dry-race/evidence/05-playtest.json';
const cases = ['open-equal','restricted-equal','open-mixed','restricted-mixed','open-equal-lapped'].map(name => {
  const preset=name.replace('-lapped','');
  const run = JSON.parse(readFileSync(join(input,name+'-summary.json'),'utf8'));
  const replay = JSON.parse(readFileSync(join(input,name+'-replay.json'),'utf8'));
  const sum = property => run.timing.phaseTimings.reduce((total, phase)=>total+(phase[property]??0),0);
  return {scenario:name,preset,seed:run.seed,distance:run.distance,configuration:replay.replay.configuration,field:preset.endsWith('equal')?'identical driver and car attributes':'mixed car fit and small driver Pace offsets',
    modelVersion:replay.replay.modelVersion,randomnessVersion:replay.replay.randomnessVersion,opponentScriptVersion:replay.opponentScriptVersion,
    recordedPhases:run.verification.phases.length,acceptedDecisions:replay.replay.plan.scheduled.length,exactReplay:run.verification.matches,finished:run.verification.finished,
    completedLapRange:[Math.min(...run.final.classification.map(car=>car.completedLaps)),Math.max(...run.final.classification.map(car=>car.completedLaps))],
    eligibleCars:run.final.classification.filter(car=>car.eligibleAtFinish).length,
    ownedCars:run.final.classification.filter(car=>car.teamId==='team-01').map(({driverId,completedLaps,eligibleAtFinish})=>({driverId,completedLaps,eligibleAtFinish})),
    observedBattleOptions:[...new Set(run.observations.flatMap(phase=>phase.cars.flatMap(car=>car.battleOptions)))].sort(),
    stopQueuedAtDecisionBoundary:run.observations.some(phase=>phase.cars.some(car=>car.stopQueued)),inPitsAtDecisionBoundary:run.observations.some(phase=>phase.cars.some(car=>car.inPits)),
    milliseconds:{serverWindowToAcceptanceSum:sum('decisionMilliseconds'),engineResolutionSum:sum('resolutionMilliseconds'),readyWaitSum:sum('readyWaitMilliseconds'),serverTotalToFinish:run.timing.totalToFinishMilliseconds,
      clientPresentationSamples:run.timing.presentationSamples.length,clientPresentationSum:run.timing.presentationSamples.reduce((total,sample)=>total+sample.durationMilliseconds,0)},
    decisions:replay.replay.plan.scheduled.filter(choice=>choice.driverId==='driver-01'||choice.driverId==='driver-02')
  };
});
writeFileSync(output,JSON.stringify({measuredOn:'2026-10-09',source:'ticket05 worktree based on 474110d; actual serial Playwright browser plus local API; designer measurements',purpose:'mechanics and replay checks, not human timing or a balance sweep',seedPerPreset:123,
  durationTarget:'Retired by user on 2026-10-09; timing is informative only.',metricSources:{decision:'server monotonic window opening to accepted lock, includes automation and viewing/network wait',resolution:'server monotonic ResolvePhase only',readyWait:'server result readiness to authorized continuation; overlaps client presentation',presentation:'untrusted client-reported page monotonic duration; reduced-motion completion in these cases',total:'server session initialization to authoritative finish, excludes final presentation'},cases},null,2)+'\n');
console.log(cases.map(run=>`${run.preset}: ${run.recordedPhases} phases, replay=${run.exactReplay}, laps=${run.completedLapRange.join('–')}, total=${run.milliseconds.serverTotalToFinish.toFixed(1)} ms`).join('\n'));
