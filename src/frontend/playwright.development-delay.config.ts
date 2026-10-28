import {defineConfig} from '@playwright/test';
import normal from './playwright.development.config';
process.env['F1_DEVELOPMENT_DELAY_FIXTURE']='1';
export default defineConfig({...normal,webServer:(Array.isArray(normal.webServer)?normal.webServer:[normal.webServer!]).map(server=>server.env?.['F1_CONTROLLED_CLOCK']==='1'?{...server,env:{...server.env,F1_DEVELOPMENT_DELAY_FIXTURE:'1'}}:server)});
