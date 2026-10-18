import {resolve} from 'node:path';
import {defineConfig} from '@playwright/test';
import {common,frontend,api} from './playwright.runtime';
export default defineConfig({...common,outputDir:resolve(__dirname,'../../.tools/reports/s10-02/playwright'),testDir:'tests/browser-wet-tyres',workers:1,timeout:60000,webServer:[{...api(true),env:{...api(true).env,F1_WET_TYRE_FIXTURE:'1'}},frontend]});
