import {test} from 'node:test';
import assert from 'node:assert/strict';
import {mkdtempSync,readFileSync,writeFileSync} from 'node:fs';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
import {spawnSync} from 'node:child_process';
import {gzipSync,gunzipSync} from 'node:zlib';
const dll=join(process.cwd(),'tools/SeasonEconomyEvidence/bin/Debug/net10.0/SeasonEconomyEvidence.dll');
const run=(...args)=>spawnSync(process.env.DOTNET_EXE??'dotnet',[dll,...args],{encoding:'utf8',timeout:300000});
test('frozen two-event economy evidence replays complete paired treatments and rejects altered receipts',()=>{
 const output=process.env.SEASON_EVIDENCE_OUTPUT??mkdtempSync(join(tmpdir(),'season-economy-'));
 if(!process.env.SEASON_EVIDENCE_OUTPUT){const created=run('--pilot',output);assert.equal(created.status,0,created.stderr+created.stdout);}
 const path=join(output,'season-economy.json.gz'),original=readFileSync(path),saved=JSON.parse(gunzipSync(original));
 assert.equal(saved.runs.length,saved.seeds.length*18);assert.equal(saved.calendar.length,2);
 assert.equal(run('--replay',path).status,0);
 assert.notEqual(run(output).status,0);assert.deepEqual(readFileSync(path),original);
 for(const mutate of [s=>s.schema='unknown',s=>s.runs.pop(),s=>s.runs[0].ledger[0].credits++,s=>s.runs[0].events[0].hash='bad',s=>s.runs[0].events[1].replay.seed++,s=>s.outcomeVersion='unknown']){
  const changed=JSON.parse(gunzipSync(original));mutate(changed);const damaged=join(tmpdir(),'season-damaged-'+Date.now()+'.gz');writeFileSync(damaged,gzipSync(JSON.stringify(changed)));assert.notEqual(run('--replay',damaged).status,0,mutate.toString());
 }
});
