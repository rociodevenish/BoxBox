import {defineConfig} from '@playwright/test';
import {common,frontend,api} from './playwright.runtime';
const server=api();
const authPort=process.env['F1_CONSTRUCTION_AUTH_PORT']??'22509';
const firestorePort=process.env['F1_CONSTRUCTION_FIRESTORE_PORT']??'22500';
const config=process.env['F1_CONSTRUCTION_EMULATOR_CONFIG']??'../../tools/auth-emulator/firebase.construction.json';
export default defineConfig({...common,testDir:'tests/construction',workers:1,timeout:180000,webServer:[
 {command:`node ../../tools/auth-emulator/node_modules/firebase-tools/lib/bin/firebase.js emulators:start --only auth,firestore --project demo-f1game --config ${config} --non-interactive`,url:`http://127.0.0.1:${authPort}`,reuseExistingServer:false,timeout:180000},
 {...server,env:{...server.env,Wakeups__Mode:'manual',Firebase__ProjectId:'demo-f1game',Firebase__EmulatorUrl:`http://127.0.0.1:${authPort}`,Persistence__ProjectId:'demo-f1game',FIRESTORE_EMULATOR_HOST:`127.0.0.1:${firestorePort}`,FIREBASE_AUTH_EMULATOR_HOST:`127.0.0.1:${authPort}`}},frontend]});
