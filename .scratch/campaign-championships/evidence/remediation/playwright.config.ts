import {resolve} from 'node:path';
import {defineConfig} from '../../../src/frontend/node_modules/@playwright/test';
import {common,frontend,api} from '../../../src/frontend/playwright.runtime';
const server=api(true);
export default defineConfig({...common,testDir:'../../../src/frontend/tests/campaign-recovery',workers:1,timeout:150000,webServer:[
 {command:'node ../../tools/auth-emulator/node_modules/firebase-tools/lib/bin/firebase.js emulators:start --only auth,firestore --project demo-f1game --config ../../.tools/reports/s11-remediation/firebase.json --non-interactive',cwd:resolve(__dirname,'../../../src/frontend'),url:'http://127.0.0.1:19679',reuseExistingServer:false,timeout:180000},
 {...server,cwd:resolve(__dirname,'../../../src/frontend'),env:{...server.env,F1_CONTROLLED_CLOCK:'1',Wakeups__Mode:'manual',Firebase__ProjectId:'demo-f1game',Firebase__EmulatorUrl:'http://127.0.0.1:19679',Persistence__ProjectId:'demo-f1game',FIRESTORE_EMULATOR_HOST:'127.0.0.1:19680',FIREBASE_AUTH_EMULATOR_HOST:'127.0.0.1:19679'}},
 {...frontend,cwd:resolve(__dirname,'../../../src/frontend')}]});
