import test from 'node:test';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
import {mkdtempSync,mkdirSync,writeFileSync,readFileSync,existsSync} from 'node:fs';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
import {fileURLToPath} from 'node:url';
const command=fileURLToPath(new URL('../publish-evidence.mjs',import.meta.url));
test('evidence remains ignored until the explicit public publishing command selects a file',()=>{
 const root=mkdtempSync(join(tmpdir(),'f1-evidence-')),source=join(root,'.tools/reports/gallery'),target=join(root,'.scratch/ticket/evidence');mkdirSync(source,{recursive:true});writeFileSync(join(source,'phone.png'),'reviewed capture');
 assert.equal(existsSync(target),false);
 const result=spawnSync(process.execPath,[command,source,target,'phone.png'],{cwd:root,encoding:'utf8',timeout:5000});assert.equal(result.status,0);assert.equal(readFileSync(join(target,'phone.png'),'utf8'),'reviewed capture');
 const rejected=spawnSync(process.execPath,[command,source,join(root,'outside'),'phone.png'],{cwd:root,encoding:'utf8',timeout:5000});assert.equal(rejected.status,1);assert.equal(existsSync(join(root,'outside')),false);
});
