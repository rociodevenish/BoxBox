import {spawn} from 'node:child_process';
import {mkdirSync,writeFileSync} from 'node:fs';
import {dirname} from 'node:path';
import {pathToFileURL} from 'node:url';
export async function runBounded(command,args,{timeoutMs=600000,cwd=process.cwd(),env=process.env,report}={}) {
 if(!Number.isFinite(timeoutMs)||timeoutMs<1)throw new Error('Timeout must be positive');
 const started=Date.now();const child=spawn(command,args,{cwd,env,stdio:'inherit',detached:process.platform!=='win32',windowsHide:true});
 const record={command,args,cwd,pid:child.pid,startedAt:new Date(started).toISOString(),status:'running'};
 const save=()=>{if(report){mkdirSync(dirname(report),{recursive:true});writeFileSync(report,JSON.stringify(record,null,2));}};save();
 return await new Promise(resolve=>{
 let finished=false;
 const finish=(code,status,error)=>{if(finished)return;finished=true;clearTimeout(timer);Object.assign(record,{exitCode:code,status,elapsedMs:Date.now()-started,...(error?{error:String(error)}:{})});save();resolve(record);};
 const timer=setTimeout(()=>{
  // Only the PID launched above is targeted; never enumerate other processes.
  if(process.platform==='win32'){
   const cleanup=spawn('taskkill',['/PID',String(child.pid),'/T','/F'],{stdio:'ignore',windowsHide:true});
   const bound=setTimeout(()=>{cleanup.kill();finish(124,'timed-out');},5000);cleanup.once('exit',()=>{clearTimeout(bound);finish(124,'timed-out');});cleanup.once('error',()=>{clearTimeout(bound);child.kill();finish(124,'timed-out');});
  }else{try{process.kill(-child.pid,'SIGKILL');}catch{} finish(124,'timed-out');}
 },timeoutMs);
 child.once('error',error=>finish(1,'failed',error));
 child.once('exit',code=>{if(Date.now()-started<timeoutMs)finish(code??1,code===0?'passed':'failed');});
 });
}
if(import.meta.url===pathToFileURL(process.argv[1]).href){
 const argv=process.argv.slice(2);const separator=argv.indexOf('--');let timeoutMs=600000,report;
 for(let i=0;i<separator;i+=2){if(argv[i]==='--timeout-ms')timeoutMs=Number(argv[i+1]);else if(argv[i]==='--report')report=argv[i+1];else throw new Error('Unknown option '+argv[i]);}
 if(separator<0||!argv[separator+1])throw new Error('Usage: node tools/run-bounded.mjs [--timeout-ms N] [--report file] -- command args');
 const result=await runBounded(argv[separator+1],argv.slice(separator+2),{timeoutMs,report});console.log(`${result.status}: ${result.elapsedMs}ms`);process.exit(result.exitCode);
}
