import {readFileSync,writeFileSync} from 'node:fs';
import {join} from 'node:path';
const directory=process.argv[2];
if(!directory)throw new Error('Pass generated evidence directory');
const read=name=>JSON.parse(readFileSync(join(directory,name),'utf8'));
const summary=read('summary.json'),probes=read('isolation-probes.json');
const rows=summary.summaries;
const comparison=(circuit,field,variant,mode)=>rows.find(r=>r.circuit===circuit&&r.field===field&&r.variant===variant&&r.mode===mode);
const contradictions=rows.filter(r=>r.mode==='traffic-noisy').map(row=>{
 const reference=comparison(row.circuit,row.field,row.variant,'clear-zero').statistics.meanSeconds;
 return {...row,clearZeroReferenceSeconds:reference,oppositeToClearControlCount:reference<0?row.statistics.slower:reference>0?row.statistics.faster:0};
}).filter(r=>r.oppositeToClearControlCount||r.statistics.signChanges);
const zeroVariation=rows.filter(r=>r.mode==='traffic-noisy'&&r.statistics.standardDeviationSeconds<1e-9).map(r=>({circuit:r.circuit,field:r.field,variant:r.variant}));
const negligibleVariation=rows.filter(r=>r.mode==='traffic-noisy'&&r.statistics.standardDeviationSeconds<.001).map(r=>({circuit:r.circuit,field:r.field,variant:r.variant,standardDeviationSeconds:r.statistics.standardDeviationSeconds}));
const target=outcome=>outcome.classification.find(c=>c.driverId==='team-01-car-1');
const tyre=outcome=>outcome.tyres.find(c=>c.driverId==='team-01-car-1');
const stints=probes.tyreProbes.map(p=>({circuit:p.circuit,laps:p.laps,setup:p.name,finishSeconds:target(p.outcome).finishTime,finalCondition:tyre(p.outcome).condition,eligibility:p.outcome.eligibility['team-01-car-1']}));
const person=probes.personProbes.map(p=>({adaptability:p.adaptability,information:p.information,firstFinishSeconds:target(p.outcome).finishTime,
 secondFinishSeconds:p.outcome.classification.find(c=>c.driverId==='team-01-car-2').finishTime,
 exactFits:p.assessment.diagnostics.exactFits,observed:p.assessment.feedback.drivers,outcomeSha256:p.outcomeSha256}));
const qualityPairs=[10,95].map(adaptability=>{const low=person.find(p=>p.adaptability===adaptability&&p.information===10),high=person.find(p=>p.adaptability===adaptability&&p.information===95);return {adaptability,lowInformation:low.information,highInformation:high.information,fullFieldRaceOutcomeIdentical:low.outcomeSha256===high.outcomeSha256};});
const postPit=[];
for(const circuit of ['track-monza','track-monaco']){
 const caseData=read(circuit+'-mixed-clear-zero.json');
 for(const [name,outcome] of [['neutral',caseData.baselineRuns[0]],...caseData.comparisons.filter(c=>['warm-up','preservation'].includes(c.name)).map(c=>[c.name,c.runs[0]])]){
  const events=outcome.targetEvents.filter(e=>e.driverId==='team-01-car-1');
  const exit=events.find(e=>e.kind==='PitExit');
  const firstLap=exit&&events.find(e=>e.kind==='LapCompleted'&&e.time>exit.time);
  postPit.push({circuit,setup:name,pitExitTime:exit?.time,firstPostPitLapCompleteTime:firstLap?.time,
   firstPostPitLapDurationSeconds:firstLap&&exit?firstLap.time-exit.time:null,finishSeconds:target(outcome).finishTime,finalCondition:tyre(outcome).condition});
 }
}
const analysis={schema:'weekend-setup-analysis-v1',sign:'alternative minus neutral seconds; negative faster',
 comparisons:rows,contradictions,zeroVariationPairedCases:zeroVariation,negligibleVariationPairedCases:negligibleVariation,stints,postPit,person,qualityPairs,
 replayChecks:summary.replayChecks.length,allReplaysIdentical:summary.replayChecks.every(r=>r.identical)};
writeFileSync(join(directory,'analysis.json'),JSON.stringify(analysis,null,2));
let md='# Full-field setup results\n\nNegative delta means faster than the neutral target team; all20 other cars and the plan/grid stay fixed. Population SD across12 paired seeds, not a confidence interval. Clear and traffic controls each use one deterministic seed.\n\n| Circuit / field | Variant | Clear zero delta(s) | Traffic zero delta(s) | Noisy mean(s) | SD(s) | Range(s) | Faster/tied/slower |\n| --- | --- | ---: | ---: | ---: | ---: | --- | --- |\n';
for(const row of rows.filter(r=>r.mode==='traffic-noisy')){
 const s=row.statistics;
 md+=`| ${row.circuit} / ${row.field} | ${row.variant} | ${comparison(row.circuit,row.field,row.variant,'clear-zero').statistics.meanSeconds.toFixed(3)} | ${comparison(row.circuit,row.field,row.variant,'traffic-zero').statistics.meanSeconds.toFixed(3)} | ${s.meanSeconds.toFixed(3)} | ${s.standardDeviationSeconds.toFixed(3)} | ${s.minSeconds.toFixed(3)} to ${s.maxSeconds.toFixed(3)} | ${s.faster}/${s.tied}/${s.slower} |\n`;
}
md+='\n## Fixed-compound stint probes\n\nNo pit stops, no traffic/noise; all22 cars retained. These laboratory stints are not eligible two-compound race plans.\n\n| Circuit | Laps | Setup | First-car finish(s) | Final condition |\n| --- | ---: | --- | ---: | ---: |\n';
for(const p of stints)md+=`| ${p.circuit} | ${p.laps} | ${p.setup} | ${p.finishSeconds.toFixed(3)} | ${p.finalCondition.toFixed(4)} |\n`;
md+='\n## First lap after the planned Hard change\n\nMeasured public LapCompleted time minus preceding PitExit time (excludes pit loss); includes first Hard warm-up and ordinary wear.\n\n| Circuit | Setup | First post-pit lap(s) | Finish(s) | Final condition |\n| --- | --- | ---: | ---: | ---: |\n';
for(const p of postPit)md+=`| ${p.circuit} | ${p.setup} | ${p.firstPostPitLapDurationSeconds?.toFixed(3)} | ${p.finishSeconds.toFixed(3)} | ${p.finalCondition.toFixed(4)} |\n`;
md+=`\n${contradictions.length} noisy comparison cases have a seed sign opposite to their clear control or mixed signs. ${zeroVariation.length} noisy paired cases have effectively zero delta variation; ${negligibleVariation.length} have SD below0.001s (the table rounds to3 decimals). Raw values and all such cases are retained in analysis.json. ${summary.replayChecks.length} full-field saved replays reproduce identical outcomes.\n`;
writeFileSync(join(directory,'RESULTS.md'),md);
console.log(JSON.stringify({comparisons:rows.length,contradictions:contradictions.length,zeroVariationCases:zeroVariation.length,qualityPairs,stints,postPit,person:person.map(p=>({...p,exactFits:p.exactFits.map(d=>({driver:d.name,compatibility:d.compatibility})),observed:p.observed.map(d=>({driver:d.name,fit:d.fit,confidence:d.confidence,uncertainty:d.uncertainty}))}))},null,2));
