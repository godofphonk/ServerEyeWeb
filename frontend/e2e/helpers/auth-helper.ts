import { Page, BrowserContext } from '@playwright/test';

/**
 * Authentication Helpers for E2E Tests
 * Provides reusable functions for login/logout operations
 */

export interface UserCredentials {
  email: string;
  password: string;
}

export const DEFAULT_TEST_USER: UserCredentials = {
  email: process.env.TEST_USER_EMAIL || 'test@example.com',
  password: process.env.TEST_USER_PASSWORD || 'testpassword123',
};

/**
 * Login as a user via UI
 */
export async function loginAsUser(page: Page, credentials: UserCredentials = DEFAULT_TEST_USER): Promise<void> {
  await page.goto('/login');
  
  // Fill login form
  await page.fill('input[type="email"], input[name="email"]', credentials.email);
  await page.fill('input[type="password"], input[name="password"]', credentials.password);
  
  // Submit form
  await page.click('button[type="submit"]');
  
  // Wait for navigation or stable state
  await page.waitForTimeout(2000);
  
  // Verify we're logged in (not on login page)
  const url = page.url();
  if (url.includes('/login')) {
    throw new Error('Login failed - still on login page');
  }
}

/**
 * Login via API (faster than UI login)
 */
export async function loginViaAPI(context: BrowserContext, credentials: UserCredentials = DEFAULT_TEST_USER): Promise<void> {
  // Make API call to login endpoint
  const response = await context.request.post('/api/auth/login', {
    data: {
      email: credentials.email,
      password: credentials.password,
    },
  });
  
  if (!response.ok()) {
    throw new Error(`API login failed: ${await response.text()}`);
  }
  
  // Get auth token from response
  const data = await response.json();
  
  // Set auth cookie/token in context
  await context.addCookies([
    {
      name: 'access_token',
      value: data.token || data.accessToken,
      domain: 'localhost',
      path: '/',
      httpOnly: true,
      secure: false,
    },
  ]);
}

/**
 * Logout current user
 */
export async function logout(page: Page): Promise<void> {
  await page.goto('/logout');
  await page.waitForTimeout(1000);
}

/**
 * Clear authentication state
 */
export async function clearAuth(context: BrowserContext): Promise<void> {
  await context.clearCookies();
}

/**
 * Check if user is authenticated
 */
export async function isAuthenticated(page: Page): Promise<boolean> {
  const url = page.url();
  return !url.includes('/login') && !url.includes('/register');
}

/**
 * Create new test user via API
 */
export async function createTestUser(request: any, email: string, password: string): Promise<void> {
  const response = await request.post('/api/auth/register', {
    data: {
      email,
      password,
      userName: `testuser_${Date.now()}`,
    },
  });
  
  if (!response.ok() && response.status() !== 409) { // 409 = user already exists
    throw new Error(`Failed to create test user: ${await response.text()}`);
  }
}

/**
 * Delete test user via API
 */
export async function deleteTestUser(request: any, email: string): Promise<void> {
  // This would require admin API or specific cleanup endpoint
  // Implementation depends on your backend API
  console.log(`Cleanup: delete user ${email}`);
}
