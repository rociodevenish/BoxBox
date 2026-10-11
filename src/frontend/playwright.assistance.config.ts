import {defineConfig} from '@playwright/test';
import {common,frontend,api} from './playwright.runtime';
const auth=process.env['F1_ASSISTANCE_AUTH_PORT']??'19109',firestore=process.env['F1_ASSISTANCE_FIRESTORE_PORT']??'19100';
const server=api(true);
export default defineConfig({...common,use:{...common.use,actionTimeout:15000},testDir:'tests/assistance',workers:1,timeout:90000,webServer:[
{command:'node ../../tools/auth-emulator/node_modules/firebase-tools/lib/bin/firebase.js emulators:start --only auth,firestore --project demo-f1game --config '+(process.env['F1_ASSISTANCE_EMULATOR_CONFIG']??'../../tools/auth-emulator/firebase.recovery.json')+' --non-interactive',url:'http://127.0.0.1:'+auth,reuseExistingServer:false,timeout:120000},
{...server,env:{...server.env,F1_CONTROLLED_CLOCK:'1',ASPNETCORE_ENVIRONMENT:'Development',Firebase__ProjectId:'demo-f1game',Firebase__EmulatorUrl:'http://127.0.0.1:'+auth,FIREBASE_AUTH_EMULATOR_HOST:'127.0.0.1:'+auth,Persistence__ProjectId:'demo-f1game',FIRESTORE_EMULATOR_HOST:'127.0.0.1:'+firestore}},frontend]});


