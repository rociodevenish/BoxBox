import {defineConfig} from '@playwright/test';
import {readFileSync,mkdirSync,writeFileSync} from 'node:fs';
import {resolve} from 'node:path';
import {common,frontend,api} from './playwright.runtime';
const root=resolve(__dirname,'../..');const configuration=JSON.parse(readFileSync(resolve(root,'tools/auth-emulator/firebase.recovery.json'),'utf8'));
configuration.emulators.auth.port=19309;configuration.emulators.firestore.port=19300;configuration.emulators.hub.port=19510;configuration.emulators.logging.port=19560;configuration.firestore.rules=resolve(root,'tools/auth-emulator/firestore.rules');
configuration.emulators.firestore.websocketPort=Number(process.env['F1_CONTROL_WEBSOCKET_PORT']??9150);
mkdirSync(resolve(root,'.tools/config'),{recursive:true});writeFileSync(resolve(root,'.tools/config/control-emulators.json'),JSON.stringify(configuration));
const server=api(true);
export default defineConfig({...common,testDir:'tests/control',workers:1,timeout:90000,webServer:[
{command:'node ../../tools/auth-emulator/node_modules/firebase-tools/lib/bin/firebase.js emulators:start --only auth,firestore --project demo-f1game --config ../../.tools/config/control-emulators.json --non-interactive',url:'http://127.0.0.1:19309',reuseExistingServer:false,timeout:120000},
{...server,env:{...server.env,F1_CONTROLLED_CLOCK:'1',ASPNETCORE_ENVIRONMENT:'Development',Firebase__ProjectId:'demo-f1game',Firebase__EmulatorUrl:'http://127.0.0.1:19309',FIREBASE_AUTH_EMULATOR_HOST:'127.0.0.1:19309',Persistence__ProjectId:'demo-f1game',FIRESTORE_EMULATOR_HOST:'127.0.0.1:19300'}},frontend]});
