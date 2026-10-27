import { defineConfig } from '@playwright/test';
import {common,frontend,api} from './playwright.runtime';
const server=api();
const auth=process.env['F1_SHARED_AUTH_PORT']??'19109',firestore=process.env['F1_SHARED_FIRESTORE_PORT']??'19100';
export default defineConfig({...common,testDir:'tests/shared',workers:1,timeout:60000,webServer:[
 {command:'node ../../tools/auth-emulator/node_modules/firebase-tools/lib/bin/firebase.js emulators:start --only auth,firestore --project demo-f1game --config '+(process.env['F1_SHARED_EMULATOR_CONFIG']??'../../tools/auth-emulator/firebase.recovery.json')+' --non-interactive',url:'http://127.0.0.1:'+auth,reuseExistingServer:false,timeout:180000},
 {...server,env:{...server.env,ASPNETCORE_ENVIRONMENT:'Development',Wakeups__Mode:'manual',Firebase__ProjectId:'demo-f1game',Firebase__EmulatorUrl:'http://127.0.0.1:'+auth,Persistence__ProjectId:'demo-f1game',FIRESTORE_EMULATOR_HOST:'127.0.0.1:'+firestore,FIREBASE_AUTH_EMULATOR_HOST:'127.0.0.1:'+auth}},frontend]});
