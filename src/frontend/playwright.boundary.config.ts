import { defineConfig } from '@playwright/test';
export default defineConfig({testDir:'tests/browser-boundary',workers:1,timeout:60000,use:{baseURL:'http://127.0.0.1:4410'},webServer:[
  {command:'"C:/Program Files/dotnet/dotnet.exe" run --project ../../tests/Game.BrowserFixtureHost',url:'http://127.0.0.1:4510/health',timeout:120000},
  {command:'npm run start:race',url:'http://127.0.0.1:4410',timeout:120000}],reporter:'list'});
