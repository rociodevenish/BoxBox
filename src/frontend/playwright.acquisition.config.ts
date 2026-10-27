import {defineConfig} from '@playwright/test';
import {common,frontend,api} from './playwright.runtime';
const server=api();
const authPort=process.env['F1_ACQUISITION_AUTH_PORT']??'21979';
const firestorePort=process.env['F1_ACQUISITION_FIRESTORE_PORT']??'21980';
const emulatorConfig=process.env['F1_ACQUISITION_EMULATOR_CONFIG']??'../../tools/auth-emulator/firebase.acquisition.json';
export default defineConfig({...common,testDir:'tests/acquisition',workers:1,timeout:90000,webServer:[
 {command:`node ../../tools/auth-emulator/node_modules/firebase-tools/lib/bin/firebase.js emulators:start --only auth,firestore --project demo-f1game --config ${emulatorConfig} --non-interactive`,url:`http://127.0.0.1:${authPort}`,reuseExistingServer:false,timeout:180000},
 {...server,env:{...server.env,Wakeups__Mode:'manual',Firebase__ProjectId:'demo-f1game',Firebase__EmulatorUrl:`http://127.0.0.1:${authPort}`,Persistence__ProjectId:'demo-f1game',FIRESTORE_EMULATOR_HOST:`127.0.0.1:${firestorePort}`,FIREBASE_AUTH_EMULATOR_HOST:`127.0.0.1:${authPort}`}},frontend]});
