import {defineConfig} from '@playwright/test';
import {common,frontend,api,baseURL} from './playwright.runtime';
const server=api();
export default defineConfig({...common,testDir:'tests/hosted',workers:1,timeout:60000,webServer:[
 {...server,env:{...server.env,ASPNETCORE_ENVIRONMENT:'Development',Hosting__AllowedOrigins:baseURL}},
 {...frontend,command:frontend.command.replace(' --proxy-config proxy.test.cjs','')}
]});
