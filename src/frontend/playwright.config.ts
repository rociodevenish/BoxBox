import { defineConfig } from '@playwright/test';
import {common,frontend} from './playwright.runtime';
export default defineConfig({...common,testDir:'tests',testIgnore:['**/campaign/**','**/browser/**','**/browser-weather/**','**/browser-weather-advice/**','**/browser-weather-reactions/**','**/browser-wet-weekend/**','**/browser-wet-tyres/**','**/browser-boundary/**','**/browser-safety-car/**','**/browser-opening-disruption/**','**/shared/**','**/assistance/**','**/control/**','**/deadlines/**','**/hosted/**'],timeout:20000,expect:{timeout:5000},webServer:frontend});
