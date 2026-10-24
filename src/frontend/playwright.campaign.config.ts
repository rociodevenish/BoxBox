import {defineConfig} from '@playwright/test';
import {common,frontend,api} from './playwright.runtime';
const server=api();
export default defineConfig({...common,testDir:'tests/campaign',workers:1,timeout:90000,webServer:[
 {command:'node ../../tools/auth-emulator/node_modules/firebase-tools/lib/bin/firebase.js emulators:start --only auth,firestore --project demo-f1game --config ../../tools/auth-emulator/firebase.campaign.json --non-interactive',url:'http://127.0.0.1:19579',reuseExistingServer:false,timeout:180000},
 {...server,env:{...server.env,Wakeups__Mode:'manual',Firebase__ProjectId:'demo-f1game',Firebase__EmulatorUrl:'http://127.0.0.1:19579',Persistence__ProjectId:'demo-f1game',FIRESTORE_EMULATOR_HOST:'127.0.0.1:19580',FIREBASE_AUTH_EMULATOR_HOST:'127.0.0.1:19579'}},frontend]});

