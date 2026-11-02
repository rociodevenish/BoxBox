import {defineConfig} from '@playwright/test';
import {emulatorLifecycle,common,frontend,api} from './playwright.runtime';
const server=api();
export default defineConfig({...common,testDir:'tests/content',testMatch:'personnel.spec.ts',workers:1,timeout:120000,webServer:[
 {...emulatorLifecycle,command:'node ../../tools/auth-emulator/node_modules/firebase-tools/lib/bin/firebase.js emulators:start --only auth,firestore --project demo-f1game --config ../../tools/auth-emulator/firebase.personnel-content.json --non-interactive',url:'http://127.0.0.1:22109',reuseExistingServer:false,timeout:180000},
 {...server,env:{...server.env,Wakeups__Mode:'manual',Firebase__ProjectId:'demo-f1game',Firebase__EmulatorUrl:'http://127.0.0.1:22109',Persistence__ProjectId:'demo-f1game',FIRESTORE_EMULATOR_HOST:'127.0.0.1:22100',FIREBASE_AUTH_EMULATOR_HOST:'127.0.0.1:22109'}},frontend]});
