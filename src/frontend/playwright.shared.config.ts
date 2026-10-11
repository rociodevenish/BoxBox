import { defineConfig } from '@playwright/test';
import {common,frontend,api} from './playwright.runtime';
const server=api();
export default defineConfig({...common,testDir:'tests/shared',workers:1,timeout:60000,webServer:[
 {command:'node ../../tools/auth-emulator/node_modules/firebase-tools/lib/bin/firebase.js emulators:start --only auth,firestore --project demo-f1game --config ../../tools/auth-emulator/firebase.recovery.json --non-interactive',url:'http://127.0.0.1:19109',reuseExistingServer:false,timeout:180000},
 {...server,env:{...server.env,ASPNETCORE_ENVIRONMENT:'Development',Firebase__ProjectId:'demo-f1game',Firebase__EmulatorUrl:'http://127.0.0.1:19109',Persistence__ProjectId:'demo-f1game',FIRESTORE_EMULATOR_HOST:'127.0.0.1:19100',FIREBASE_AUTH_EMULATOR_HOST:'127.0.0.1:19109'}},frontend]});
