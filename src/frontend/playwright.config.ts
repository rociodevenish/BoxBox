import { defineConfig } from "@playwright/test";
export default defineConfig({
  testDir: "tests",
  testIgnore: "**/browser/**",
  timeout: 20000,
  expect: { timeout: 5000 },
  use: {
    baseURL: "http://127.0.0.1:4400",
    browserName: "chromium",
    launchOptions: {
      executablePath: process.env["PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH"],
    },
  },
  webServer: {
    command: "npm run start",
    url: "http://127.0.0.1:4400",
    reuseExistingServer: !process.env["CI"],
    timeout: 120000,
  },
  reporter: "list",
});

