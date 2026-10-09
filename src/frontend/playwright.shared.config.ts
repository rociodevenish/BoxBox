import { defineConfig } from '@playwright/test';
import {common,frontend,api} from './playwright.runtime';
const server=api();
export default defineConfig({...common,testDir:'tests/shared',workers:1,timeout:60000,webServer:[
 {command:'node ../../tools/auth-emulator/node_modules/firebase-tools/lib/bin/firebase.js emulators:start --only auth --project demo-f1game --config ../../tools/auth-emulator/firebase.json --non-interactive',url:'http://127.0.0.1:19099',reuseExistingServer:false,timeout:180000},
 {...server,env:{...server.env,ASPNETCORE_ENVIRONMENT:'Development',Firebase__ProjectId:'demo-f1game',FIREBASE_AUTH_EMULATOR_HOST:'127.0.0.1:19099'}},frontend]});
