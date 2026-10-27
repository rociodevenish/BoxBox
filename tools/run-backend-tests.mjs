import {runBounded} from './run-bounded.mjs';
const artifacts=process.env.F1_DOTNET_ARTIFACTS_PATH;
const result=await runBounded(process.env.DOTNET_EXE??'dotnet',['test','F1Game.slnx','--nologo',...(artifacts?['--artifacts-path',artifacts]:[])],{timeoutMs:600000,report:'.tools/reports/verification/backend-command.json'});process.exit(result.exitCode);
