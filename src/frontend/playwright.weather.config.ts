import {resolve} from 'node:path';
import {defineConfig} from '@playwright/test';
import {common,frontend,api} from './playwright.runtime';
export default defineConfig({...common,outputDir:resolve(__dirname,'../../.tools/reports/s10-01/playwright'),testDir:'tests/browser-weather',workers:1,timeout:60000,webServer:[{...api(true),env:{...api(true).env,F1_WEATHER_FIXTURE:'1'}},frontend]});
