import {runBounded} from '../../../tools/run-bounded.mjs';
for(const group of ['CampaignNextEventTests','CampaignResultsTests','CampaignRecoveryTests']){
 const r=await runBounded('dotnet',['test','tests/Game.Api.Tests','--filter','FullyQualifiedName~'+group,'--nologo','--no-restore','-p:NuGetAudit=false'],{timeoutMs:240000,report:'.tools/reports/s11-remediation/final-'+group+'.json',env:{...process.env,FIREBASE_AUTH_EMULATOR_HOST:'127.0.0.1:19679',FIRESTORE_EMULATOR_HOST:'127.0.0.1:19680',F1_RECOVERY_HOST:'127.0.0.1:19680',Wakeups__Mode:'manual'}});if(r.exitCode)process.exit(r.exitCode);
}
