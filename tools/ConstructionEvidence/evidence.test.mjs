import {test} from 'node:test';
import assert from 'node:assert/strict';
import {mkdtempSync,readFileSync,writeFileSync} from 'node:fs';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
import {spawnSync} from 'node:child_process';
import {gzipSync,gunzipSync} from 'node:zlib';
const dll=process.env.CONSTRUCTION_EVIDENCE_DLL??join(process.cwd(),'tools/ConstructionEvidence/bin/Debug/net10.0/ConstructionEvidence.dll');
const run=(...args)=>spawnSync(process.env.DOTNET_EXE??'dotnet',[dll,...args],{encoding:'utf8',timeout:300000});
test('frozen construction race evidence replays and rejects corruption and overwrite',()=>{
 const output=process.env.CONSTRUCTION_EVIDENCE_OUTPUT??mkdtempSync(join(tmpdir(),'construction-'));
 if(!process.env.CONSTRUCTION_EVIDENCE_OUTPUT){const created=run(output);assert.equal(created.status,0,created.stderr);}
 const path=join(output,'construction.json.gz'),original=readFileSync(path);
 assert.equal(run('--replay',path).status,0);
 assert.notEqual(run(output).status,0);assert.deepEqual(readFileSync(path),original);
 const saved=JSON.parse(gunzipSync(original));assert.equal(saved.cases.length,20);
 for(const mutate of [s=>s.schema='unknown',s=>s.cases.pop(),s=>s.cases[0].outcomes[0].hash='bad',s=>s.cases[0].cost++,s=>s.cases[0].presets[0].cars[0].car.ratings.topSpeed++,s=>s.cases[0].replay.drivers[0].speed++,s=>s.cases[0].replay.modelVersion='unknown']){
  const changed=JSON.parse(gunzipSync(original));mutate(changed);const damaged=join(output,'damaged.gz');writeFileSync(damaged,gzipSync(JSON.stringify(changed)));assert.notEqual(run('--replay',damaged).status,0);
 }
});
