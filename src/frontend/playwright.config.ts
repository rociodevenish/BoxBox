import { defineConfig } from '@playwright/test';
import {common,frontend} from './playwright.runtime';
export default defineConfig({...common,testDir:'tests',testIgnore:['**/browser/**','**/browser-boundary/**','**/shared/**','**/assistance/**','**/control/**','**/deadlines/**','**/hosted/**'],timeout:20000,expect:{timeout:5000},webServer:frontend});
