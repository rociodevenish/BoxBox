import {resolve} from 'node:path';
import {defineConfig} from '@playwright/test';
import {common,frontend,api} from './playwright.runtime';
const server=api(true),auth='19869',firestore='19860';
export default defineConfig({...common,outputDir:resolve(__dirname,'../../.tools/reports/s10-04/playwright'),testDir:'tests/browser-wet-weekend',workers:1,timeout:120000,webServer:[
 {command:'node ../../tools/auth-emulator/node_modules/firebase-tools/lib/bin/firebase.js emulators:start --only auth,firestore --project demo-f1game --config ../../tools/auth-emulator/firebase.wet-weekend.json --non-interactive',url:'http://127.0.0.1:'+auth,reuseExistingServer:false,timeout:180000},
 {...server,env:{...server.env,F1_WET_TYRE_FIXTURE:'1',ASPNETCORE_ENVIRONMENT:'Development',Wakeups__Mode:'manual',Firebase__ProjectId:'demo-f1game',Firebase__EmulatorUrl:'http://127.0.0.1:'+auth,Persistence__ProjectId:'demo-f1game',FIRESTORE_EMULATOR_HOST:'127.0.0.1:'+firestore,FIREBASE_AUTH_EMULATOR_HOST:'127.0.0.1:'+auth}},frontend]});

