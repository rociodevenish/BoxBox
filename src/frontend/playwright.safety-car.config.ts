import {resolve} from 'node:path';
import {defineConfig} from '@playwright/test';
import {common,frontend,api} from './playwright.runtime';
export default defineConfig({...common,outputDir:resolve(__dirname,'../../.tools/reports/s09-05/playwright'),testDir:'tests/browser-safety-car',workers:1,timeout:60000,webServer:[{...api(true),env:{...api(true).env,F1_SAFETY_CAR_FIXTURE:'1'}},frontend]});
