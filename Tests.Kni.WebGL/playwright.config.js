const { defineConfig, devices } = require("@playwright/test");

module.exports = defineConfig({
  testDir: "./tests",
  timeout: 90000,
  workers: 1,
  expect: { timeout: 30000 },
  use: {
    baseURL: "http://127.0.0.1:5099",
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
  },
  projects: [
    { name: "chromium", use: { ...devices["Desktop Chrome"] } },
    { name: "chromium-dpr-1.25", use: { ...devices["Desktop Chrome"], deviceScaleFactor: 1.25 } },
    { name: "chromium-dpr-1.5", use: { ...devices["Desktop Chrome"], deviceScaleFactor: 1.5 } },
    { name: "chromium-dpr-2", use: { ...devices["Desktop Chrome"], deviceScaleFactor: 2 } },
    { name: "firefox", use: { ...devices["Desktop Firefox"] } },
    { name: "webkit", use: { ...devices["Desktop Safari"] } },
  ],
  webServer: {
    command: "dotnet run --project ../Sample.Kni.WebGL/Sample.Kni.WebGL.csproj -c Release --no-build --urls http://127.0.0.1:5099",
    url: "http://127.0.0.1:5099",
    reuseExistingServer: !process.env.CI,
    timeout: 120000,
  },
});
