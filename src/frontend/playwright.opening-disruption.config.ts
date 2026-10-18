import {defineConfig} from '@playwright/test';
import {resolve} from 'node:path';
import {common,frontend,api} from './playwright.runtime';
const server=api(true);
const auth=process.env['F1_SHARED_AUTH_PORT']??'19109',firestore=process.env['F1_SHARED_FIRESTORE_PORT']??'19100';
const firebase=process.env['F1_FIREBASE_CLI']??'../../tools/auth-emulator/node_modules/firebase-tools/lib/bin/firebase.js';
const config=process.env['F1_SHARED_EMULATOR_CONFIG']??'../../tools/auth-emulator/firebase.recovery.json';
export default defineConfig({...common,testDir:'tests/browser-opening-disruption',outputDir:resolve(__dirname,'../../.tools/reports/s09-05/opening-playwright'),workers:1,timeout:90000,webServer:[
 {command:'node '+firebase+' emulators:start --only auth,firestore --project demo-f1game --config '+config+' --non-interactive',url:'http://127.0.0.1:'+auth,reuseExistingServer:false,timeout:180000},
 {...server,env:{...server.env,F1_OPENING_DISRUPTION_FIXTURE:'1',Wakeups__Mode:'manual',Firebase__ProjectId:'demo-f1game',Firebase__EmulatorUrl:'http://127.0.0.1:'+auth,Persistence__ProjectId:'demo-f1game',FIRESTORE_EMULATOR_HOST:'127.0.0.1:'+firestore,FIREBASE_AUTH_EMULATOR_HOST:'127.0.0.1:'+auth}},frontend]});
