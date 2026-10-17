import {defineConfig} from '@playwright/test';
import {common,frontend,api} from './playwright.runtime';
export default defineConfig({...common,testDir:'tests/browser-safety-car',workers:1,timeout:60000,webServer:[{...api(true),env:{...api(true).env,F1_SAFETY_CAR_FIXTURE:'1'}},frontend]});
