import {defineConfig} from '@playwright/test';
import {common,frontend,api} from './playwright.runtime';
const server=api(true);
const auth=process.env['F1_PACKAGE_AUTH_PORT']??'20399',firestore=process.env['F1_PACKAGE_FIRESTORE_PORT']??'20400';
export default defineConfig({...common,testDir:'tests/packages',testMatch:'rich.spec.ts',workers:1,timeout:120000,webServer:[
 {command:'node ../../tools/auth-emulator/node_modules/firebase-tools/lib/bin/firebase.js emulators:start --only auth,firestore --project demo-f1game --config '+(process.env['F1_PACKAGE_EMULATOR_CONFIG']??'../../tools/auth-emulator/firebase.packages.json')+' --non-interactive',url:'http://127.0.0.1:'+auth,reuseExistingServer:false,timeout:180000},
 {...server,env:{...server.env,F1_DEVELOPMENT_OUTCOME_FIXTURE:'compromised',F1_CONTROLLED_CLOCK:'1',Wakeups__Mode:'manual',Firebase__ProjectId:'demo-f1game',Firebase__EmulatorUrl:'http://127.0.0.1:'+auth,Persistence__ProjectId:'demo-f1game',FIRESTORE_EMULATOR_HOST:'127.0.0.1:'+firestore,FIREBASE_AUTH_EMULATOR_HOST:'127.0.0.1:'+auth}},frontend]});
