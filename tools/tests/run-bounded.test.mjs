import test from 'node:test';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
import {mkdtempSync,readFileSync} from 'node:fs';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
const runner=new URL('../run-bounded.mjs',import.meta.url);
test('public command times out and records the launched process identity',()=>{
 const report=join(mkdtempSync(join(tmpdir(),'f1-bounded-')),'run.json');
 const result=spawnSync(process.execPath,[runner.pathname.replace(/^\/(?=[A-Z]:)/,''),'--timeout-ms','150','--report',report,'--',process.execPath,'-e','setInterval(()=>{},1000)'],{encoding:'utf8',timeout:7000});
 assert.equal(result.status,124); const record=JSON.parse(readFileSync(report,'utf8'));assert.equal(record.status,'timed-out');assert.ok(record.pid>0);assert.equal(record.command,process.execPath);
});
test('public command preserves failing exit status',()=>{
 const result=spawnSync(process.execPath,[runner.pathname.replace(/^\/(?=[A-Z]:)/,''),'--timeout-ms','1000','--',process.execPath,'-e','process.exit(7)'],{encoding:'utf8',timeout:7000});assert.equal(result.status,7);
});
