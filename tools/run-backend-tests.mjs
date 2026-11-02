import {runBounded} from './run-bounded.mjs';
const artifacts=process.env.F1_DOTNET_ARTIFACTS_PATH;
const env={...process.env};
const recoveryHost=env.F1_RECOVERY_HOST??env.FIRESTORE_EMULATOR_HOST;
if(recoveryHost!==undefined)env.F1_RECOVERY_HOST=recoveryHost;
const result=await runBounded(process.env.DOTNET_EXE??'dotnet',['test','F1Game.slnx','--nologo',...(artifacts?['--artifacts-path',artifacts]:[])],{env,timeoutMs:600000,report:'.tools/reports/verification/backend-command.json'});process.exit(result.exitCode);
