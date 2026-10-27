import {resolve} from 'node:path';
import {defineConfig} from '@playwright/test';
import {common,frontend,api} from './playwright.runtime';
const server=api(true),auth='19479',firestore='19480';
export default defineConfig({...common,outputDir:resolve(__dirname,'../../.tools/reports/s10-05/playwright'),testDir:'tests/browser-weather-advice',workers:1,timeout:120000,use:{...common.use,actionTimeout:15000},webServer:[
 {command:'node ../../tools/auth-emulator/node_modules/firebase-tools/lib/bin/firebase.js emulators:start --only auth,firestore --project demo-f1game --config ../../tools/auth-emulator/firebase.weather-advice.json --non-interactive',url:'http://127.0.0.1:'+auth,reuseExistingServer:false,timeout:120000},
 {...server,env:{...server.env,F1_WEATHER_ADVICE_FIXTURE:'1',F1_CONTROLLED_CLOCK:'1',Wakeups__Mode:'manual',Firebase__ProjectId:'demo-f1game',Firebase__EmulatorUrl:'http://127.0.0.1:'+auth,FIREBASE_AUTH_EMULATOR_HOST:'127.0.0.1:'+auth,Persistence__ProjectId:'demo-f1game',FIRESTORE_EMULATOR_HOST:'127.0.0.1:'+firestore}},frontend]});
