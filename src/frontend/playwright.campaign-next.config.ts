import {defineConfig} from '@playwright/test';
import {common,frontend,api} from './playwright.runtime';
const server=api(true);
export default defineConfig({...common,testDir:'tests/campaign-next',workers:1,timeout:90000,webServer:[
 {command:'node ../../tools/auth-emulator/node_modules/firebase-tools/lib/bin/firebase.js emulators:start --only auth,firestore --project demo-f1game --config ../../tools/auth-emulator/firebase.campaign-next.json --non-interactive',url:'http://127.0.0.1:19599',reuseExistingServer:false,timeout:180000},
 {...server,env:{...server.env,F1_CONTROLLED_CLOCK:'1',Wakeups__Mode:'manual',Firebase__ProjectId:'demo-f1game',Firebase__EmulatorUrl:'http://127.0.0.1:19599',Persistence__ProjectId:'demo-f1game',FIRESTORE_EMULATOR_HOST:'127.0.0.1:19600',FIREBASE_AUTH_EMULATOR_HOST:'127.0.0.1:19599'}},frontend]});
