import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright E2E Test Configuration for ServerEye
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './e2e',
  /* Run tests in files in parallel */
  fullyParallel: true,
  /* Fail the build on CI if you accidentally left test.only in the source code */
  forbidOnly: !!process.env.CI,
  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,
  /* Opt out of parallel tests on CI for stability */
  workers: process.env.CI ? 1 : undefined,
  /* Reporter to use */
  reporter: process.env.CI ? [['html'], ['junit', { outputFile: 'playwright-report/results.xml' }]] : 'html',
  /* Shared settings for all the projects below */
  use: {
    /* Base URL to use in actions like `await page.goto('/')` */
    baseURL: process.env.TEST_BASE_URL || 'http://localhost:3000',
    /* Collect trace when retrying the failed test */
    trace: 'on-first-retry',
    /* Screenshot on failure */
    screenshot: 'only-on-failure',
    /* Video recording for debugging */
    video: process.env.CI ? 'retain-on-failure' : 'off',
  },

  globalSetup: './e2e/global-setup.ts',

  /* Configure projects for major browsers */
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  /* Run local dev server before starting the tests */
  webServer: process.env.CI
    ? {
        command: 'npm run start:next',
        url: 'http://localhost:3000',
        reuseExistingServer: false,
        timeout: 120 * 1000,
      }
    : undefined,
});
