import { FullConfig } from '@playwright/test';

/**
 * Global Setup for E2E Tests
 * Runs once before all tests
 */

async function globalSetup(_config: FullConfig) {
  // Setup test environment

  // In real implementation, you might:
  // 1. Create test user via API
  // 2. Seed test data (servers, subscriptions)
  // 3. Clear previous test state
  // 4. Start mock services

  // Test user configuration (for future use)
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  const _testUser = {
    email: process.env.TEST_USER_EMAIL || 'test@example.com',
    password: process.env.TEST_USER_PASSWORD || 'testpassword123',
  };
}

export default globalSetup;
