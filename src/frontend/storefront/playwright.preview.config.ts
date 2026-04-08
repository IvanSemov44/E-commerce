/// <reference types="node" />
import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright config for preview mode — tests against a production build.
 * Run with: npm run test:e2e:preview
 *
 * Differences from playwright.config.ts:
 *  - Builds the app first (npm run build)
 *  - Serves via vite preview on port 4173 (no HMR WebSocket)
 *  - networkidle resolves fast on a static bundle → much shorter test time
 */
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [['html'], ['list']],

  use: {
    baseURL: process.env.VITE_APP_URL || 'http://localhost:4173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },

    ...(process.env.PLAYWRIGHT_FULL_BROWSERS
      ? [
          { name: 'firefox', use: { ...devices['Desktop Firefox'] } },
          { name: 'webkit', use: { ...devices['Desktop Safari'] } },
          { name: 'Mobile Chrome', use: { ...devices['Pixel 5'] } },
          { name: 'Mobile Safari', use: { ...devices['iPhone 12'] } },
        ]
      : []),
  ],

  webServer: {
    command: 'npm run build:fast && npm run preview',
    url: 'http://localhost:4173',
    reuseExistingServer: false,
    timeout: 180 * 1000,
  },
});
