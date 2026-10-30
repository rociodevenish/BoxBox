import {test} from 'node:test';
import assert from 'node:assert/strict';
import {mkdtempSync,readFileSync,writeFileSync} from 'node:fs';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
import {spawnSync} from 'node:child_process';
import {gzipSync,gunzipSync} from 'node:zlib';
const dll=join(process.cwd(),'tools/RacePackageEvidence/bin/Debug/net10.0/RacePackageEvidence.dll');
const run=(...args)=>spawnSync(process.env.DOTNET_EXE??'dotnet',[dll,...args],{encoding:'utf8',timeout:240000});
test('public package evidence reconstructs both declared scopes, preserves aliases and rejects damaged frozen inputs',()=>{
 const output=process.env.PACKAGE_EVIDENCE_OUTPUT??mkdtempSync(join(tmpdir(),'race-package-'));
 if(!process.env.PACKAGE_EVIDENCE_OUTPUT){const made=run('--pilot',output);assert.equal(made.status,0,made.stderr+made.stdout);}
 const path=join(output,'race-packages.json.gz'),original=readFileSync(path),saved=JSON.parse(gunzipSync(original));
 assert.equal(saved.runs.length,saved.seeds.length*28);
 assert.deepEqual([...new Set(saved.runs.map(r=>r.scope))],['production-short','synthetic-alternating-six']);
 assert.equal(saved.runs.filter(r=>r.scope==='production-short').length,saved.seeds.length*14);
 for(const r of saved.runs.filter(r=>r.scope==='production-short'&&['large-library','refinement'].includes(r.arm))){
  const small=saved.runs.find(s=>s.scope===r.scope&&s.seed===r.seed&&s.personnelPolicy===r.personnelPolicy&&s.arm==='small-library');
  assert.deepEqual(r.development,small.development);assert.deepEqual(r.ledger,small.ledger);assert.deepEqual(r.events,small.events);
 }
 const baseRows=saved.runs.filter(r=>r.arm==='base-car');
 assert.equal(baseRows.length,saved.seeds.length*4);
 for(const r of baseRows){
  assert.ok(r.development.opportunities.every(o=>o.decisions.every(d=>d.actionKind==='Save')));assert.equal(r.steps.every(s=>s.planned.action==='Save'),true);
  const baseline=saved.runs.find(s=>s.scope===r.scope&&s.seed===r.seed&&s.personnelPolicy===r.personnelPolicy&&s.arm==='Save');
  const car=r.construction.confirmed['team-01'],minimum=baseline.construction.confirmed['team-01'];
  assert.equal(r.events[0].presets[0].baseCarId,car.id);
  if(r.baseCarFailure){assert.equal(car.cost,minimum.cost);assert.deepEqual(car.ratings,minimum.ratings);assert.equal(r.events[0].hash,baseline.events[0].hash);}
  else{assert.ok(car.cost>minimum.cost);assert.notDeepEqual(r.events[0].presets[0].cars[0].car.ratings,baseline.events[0].presets[0].cars[0].car.ratings);}
 }
 const replay=run('--replay',path);assert.equal(replay.status,0,replay.stderr+replay.stdout);
 assert.notEqual(run(output).status,0);assert.deepEqual(readFileSync(path),original);
 const damageDir=mkdtempSync(join(tmpdir(),'package-damage-'));
 const mutations=[s=>s.schema='unsupported',s=>s.runs.pop(),s=>s.rules.packageTemplates[0].baseCost++,s=>s.runs[0].ledger[0].credits++,s=>s.runs[0].events[1].hash='wrong',s=>s.runs[0].events[1].presets[0].selectedPackage.version++,s=>s.runs[2].development.packageOutcomes[0].completionEventId='wrong-event',s=>s.runs[2].development.opportunities[0].decisions[0].packageQuote.cost++];
 mutations.forEach((mutate,i)=>{const changed=JSON.parse(gunzipSync(original));mutate(changed);const damaged=join(damageDir,i+'.gz');writeFileSync(damaged,gzipSync(JSON.stringify(changed)));const result=run('--replay',damaged);assert.notEqual(result.status,0,'accepted corruption '+i);assert.equal(result.error,undefined);});
});
