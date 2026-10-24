import {runBounded} from './run-bounded.mjs';
const result=await runBounded(process.env.DOTNET_EXE??'dotnet',['test','tests/Game.Api.Tests/Game.Api.Tests.csproj','--nologo','-p:NuGetAudit=false','--artifacts-path','.tools/s10-05-dotnet','--filter','FullyQualifiedName~WeatherMixedRecovery|FullyQualifiedName~WeatherAdvice|FullyQualifiedName~Weather_start_advice'],{timeoutMs:240000,report:'.tools/reports/s10-05/backend-tests.json'});process.exit(result.exitCode);

