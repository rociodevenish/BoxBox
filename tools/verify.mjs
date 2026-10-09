import {dirname,resolve} from 'node:path';
import {fileURLToPath} from 'node:url';
import {existsSync,mkdirSync,writeFileSync} from 'node:fs';
import {spawnSync} from 'node:child_process';
import {runBounded} from './run-bounded.mjs';
const [nodeMajor,nodeMinor]=process.versions.node.split('.').map(Number);if(!((nodeMajor===24&&nodeMinor>=15)||nodeMajor>=26))throw new Error('Verification requires Node 24.15+ or Node 26+, matching frontend engines.');
const root=resolve(dirname(fileURLToPath(import.meta.url)),'..');
const dotnet=process.env.DOTNET_EXE??(process.platform==='win32'&&existsSync('C:/Program Files/dotnet/dotnet.exe')?'C:/Program Files/dotnet/dotnet.exe':'dotnet');
const env={...process.env,DOTNET_EXE:dotnet,PATH:dirname(process.execPath)+(process.platform==='win32'?';':':')+process.env.PATH};
const frontend=resolve(root,'src/frontend');const cli=resolve(frontend,'node_modules/@angular/cli/bin/ng.js');const playwright=resolve(frontend,'node_modules/@playwright/test/cli.js');
const firebase=resolve(root,'tools/auth-emulator/node_modules/firebase-tools/lib/bin/firebase.js');
const steps=[
 ['tooling',[ '--test','tools/tests/run-bounded.test.mjs','tools/tests/publish-evidence.test.mjs'],root],
 ['typecheck',[resolve(frontend,'node_modules/@angular/compiler-cli/bundles/src/bin/ngc.js'),'-p','tsconfig.app.json','--noEmit'],frontend],
 ['build',[cli,'build'],frontend],
 ['backend',[firebase,'emulators:exec','--only','auth','--project','demo-f1game','--config',resolve(root,'tools/auth-emulator/firebase.json'),'--non-interactive',`"${process.execPath}" tools/run-backend-tests.mjs`],root],
 ['gallery',[playwright,'test'],frontend],
 ['browser',[playwright,'test','--config','playwright.browser.config.ts'],frontend],
 ['boundary',[playwright,'test','--config','playwright.boundary.config.ts'],frontend],
 ['shared',[playwright,'test','--config','playwright.shared.config.ts'],frontend]
];
const selected=process.argv.slice(2);if(selected.some(name=>!steps.some(([id])=>id===name)))throw new Error('Unknown verification step. Choose '+steps.map(s=>s[0]).join(', '));
const out=resolve(root,'.tools/reports/verification');mkdirSync(out,{recursive:true});const manifest={revision:spawnSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8',timeout:5000}).stdout?.trim(),startedAt:new Date().toISOString(),node:process.version,steps:[]};
for(const [name,args,cwd]of steps){if(selected.length&&!selected.includes(name))continue;console.log(`\nVerification: ${name}`);const result=await runBounded(process.execPath,args,{cwd,env,timeoutMs:900000,report:resolve(out,name+'-command.json')});manifest.steps.push({name,...result});writeFileSync(resolve(out,'result.json'),JSON.stringify(manifest,null,2));if(result.exitCode!==0)process.exit(result.exitCode);}
