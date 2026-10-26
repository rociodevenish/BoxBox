import {test} from 'node:test';
import assert from 'node:assert/strict';
import {mkdtempSync,readFileSync,writeFileSync} from 'node:fs';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
import {spawnSync} from 'node:child_process';
import {gzipSync,gunzipSync} from 'node:zlib';
const dll=join(process.cwd(),'tools/AcquisitionEvidence/bin/Debug/net10.0/AcquisitionEvidence.dll');
const run=(...args)=>spawnSync(process.env.DOTNET_EXE??'dotnet',[dll,...args],{encoding:'utf8',timeout:120000});
test('frozen acquisition experiments replay without catalog reads and reject altered evidence',()=>{
 const output=mkdtempSync(join(tmpdir(),'f1-acquisition-'));
 const created=run(output);assert.equal(created.status,0,created.stderr);
 const path=join(output,'acquisition.json.gz'),original=readFileSync(path);
 assert.equal(run('--replay',path).status,0);
 assert.notEqual(run(output).status,0);assert.deepEqual(readFileSync(path),original);
 const saved=JSON.parse(gunzipSync(original));
 assert.equal(saved.schema,'acquisition-evidence-v1');assert.equal(saved.runs.length,48);
 const summary=JSON.parse(readFileSync(join(output,'summary.json'),'utf8'));assert.equal(summary.groups.length,6);assert.equal(summary.paired.length,24);for(const group of summary.groups){assert.equal(group.completedTeams,88);assert.equal(group.balances.n,88);assert.ok(group.minReserveMargin>=0);assert.equal(group.categoryPriority.length,7);assert.ok(group.categoryPriority.every(slot=>slot.positions.length===11&&slot.positions.every(p=>p.paidPrice.n===8)));}
 for(const run of saved.runs){assert.equal(run.teams.length,11);assert.ok(run.teams.every(t=>t.ready&&t.balance>=18));assert.equal(run.actions.filter(a=>a.kind==='hire').length,77);}
 const changed=join(output,'changed.json.gz');
 for(const mutate of [s=>s.runs[0].actions[0].personId='unoffered-person',s=>s.runs[0].schedule[0].teamId='wrong-team',s=>s.runs[0].teams[0].balance++,s=>s.schema='unknown',s=>s.runs.pop()]){
  const damaged=JSON.parse(gunzipSync(original));mutate(damaged);writeFileSync(changed,gzipSync(JSON.stringify(damaged)));assert.notEqual(run('--replay',changed).status,0);
 }
 assert.doesNotMatch(gunzipSync(original).toString(),/"(?:authorization|cookie|idToken|refreshToken)"/i);
});
