import {defineConfig} from '@playwright/test';
import {common,frontend,api} from './playwright.runtime';
const server=api(true);
export default defineConfig({...common,testDir:'tests/content',testMatch:'cards.spec.ts',workers:1,timeout:240000,use:{...common.use,reducedMotion:'reduce',actionTimeout:15000},webServer:[
 {command:'node ../../tools/auth-emulator/node_modules/firebase-tools/lib/bin/firebase.js emulators:start --only auth,firestore --project demo-f1game --config ../../tools/auth-emulator/firebase.cards.json --non-interactive',url:'http://127.0.0.1:24109',reuseExistingServer:false,timeout:180000},
 {...server,env:{...server.env,Wakeups__Mode:'manual',Firebase__ProjectId:'demo-f1game',Firebase__EmulatorUrl:'http://127.0.0.1:24109',Persistence__ProjectId:'demo-f1game',FIRESTORE_EMULATOR_HOST:'127.0.0.1:24100',FIREBASE_AUTH_EMULATOR_HOST:'127.0.0.1:24109'}},frontend]});
