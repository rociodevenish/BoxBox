import {runBounded} from './run-bounded.mjs';
const result=await runBounded(process.env.DOTNET_EXE??'dotnet',['test','F1Game.slnx','--nologo'],{timeoutMs:600000,report:'.tools/reports/verification/backend-command.json'});process.exit(result.exitCode);
