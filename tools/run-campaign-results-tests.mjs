import {runBounded} from './run-bounded.mjs';
const r=await runBounded('dotnet',['test','tests/Game.Api.Tests','--filter','FullyQualifiedName~CampaignResultsTests|FullyQualifiedName~CampaignTests','--nologo','-p:NuGetAudit=false'],{timeoutMs:240000,report:'.tools/reports/campaign-results/api-tests.json',env:{...process.env,F1_RECOVERY_HOST:'127.0.0.1:19590',Wakeups__Mode:'manual'}});process.exit(r.exitCode);

