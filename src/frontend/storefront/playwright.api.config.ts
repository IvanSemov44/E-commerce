/// <reference types="node" />
import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright configuration for API E2E tests
 * This config tests the API directly without needing the frontend dev server
 */
export default defineConfig({
  testDir: './e2e',
  testMatch: ['api-catalog.spec.ts', 'api-auth.spec.ts'],

  /* Run tests in files in parallel */
  fullyParallel: true,

  /* Fail the build on CI if you accidentally left test.only in the source code */
  forbidOnly: !!process.env.CI,

  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,

  /* Opt out of parallel tests on CI */
  workers: process.env.CI ? 1 : undefined,

  /* Reporter to use */
  reporter: [['html'], ['list']],

  /* Shared settings */
  use: {
    /* API base URL - override the default frontend URL */
    baseURL: process.env.VITE_API_URL || 'http://localhost:5000/api',

    /* Collect trace when retrying the failed test */
    trace: 'on-first-retry',

    /* Screenshot on failure */
    screenshot: 'only-on-failure',

    /* Video on failure */
    video: 'retain-on-failure',
  },

  /* Configure projects - only chromium for API tests */
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  /* Don't start webServer - we test against external API */
  webServer: undefined,
});
