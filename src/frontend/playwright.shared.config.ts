import { defineConfig } from '@playwright/test';
export default defineConfig({testDir:'tests/shared',workers:1,timeout:60000,use:{baseURL:'http://127.0.0.1:4410'},webServer:[
 {command:'node ../../tools/auth-emulator/node_modules/firebase-tools/lib/bin/firebase.js emulators:start --only auth --project demo-f1game --config ../../tools/auth-emulator/firebase.json --non-interactive',url:'http://127.0.0.1:19099',reuseExistingServer:true,timeout:180000},
 {command:'"C:/Program Files/dotnet/dotnet.exe" run --project ../backend/Game.Api --urls http://127.0.0.1:4510',env:{ASPNETCORE_ENVIRONMENT:'Development',Firebase__ProjectId:'demo-f1game',FIREBASE_AUTH_EMULATOR_HOST:'127.0.0.1:19099'},url:'http://127.0.0.1:4510/health',timeout:120000},
 {command:'npm run start:race',url:'http://127.0.0.1:4410',timeout:120000}],reporter:'list'});

