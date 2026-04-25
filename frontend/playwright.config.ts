import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { defineConfig, devices } from '@playwright/test';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '..');
const apiProject = path.join(repoRoot, 'src', 'Greenhouse.Api', 'Greenhouse.Api.csproj');
const e2eDb = path.join(repoRoot, 'tmp', 'e2e-playwright.db');

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? 'github' : 'list',
  globalSetup: path.join(__dirname, 'e2e', 'global-setup.ts'),
  use: {
    ...devices['Desktop Chrome'],
    baseURL: 'http://127.0.0.1:5057',
    trace: 'on-first-retry',
  },
  webServer: {
    command: `dotnet run --project "${apiProject}" --urls http://127.0.0.1:5057 --no-launch-profile`,
    cwd: repoRoot,
    url: 'http://127.0.0.1:5057/health/live',
    reuseExistingServer: !process.env.CI,
    timeout: 180_000,
    env: {
      ASPNETCORE_ENVIRONMENT: 'Production',
      Mqtt__Enabled: 'false',
      Infrastructure__DatabasePath: e2eDb,
      ApiSecurity__RequireForMutations: 'false',
    },
  },
});
