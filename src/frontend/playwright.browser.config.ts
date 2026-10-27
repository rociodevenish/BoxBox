import { defineConfig } from '@playwright/test';
import {common,frontend,api} from './playwright.runtime';
export default defineConfig({...common,testDir:'tests/browser',workers:1,timeout:60000,webServer:[api(),frontend]});
