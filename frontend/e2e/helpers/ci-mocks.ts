import { Page } from '@playwright/test';

/**
 * CI Mock Setup for E2E Tests
 *
 * In CI environment, the backend is not available.
 * These mocks intercept API calls at the browser level via Playwright's page.route()
 * so that authentication and basic API flows work without a real backend.
 */

export const MOCK_USER = {
  id: 'test-user-id',
  email: 'test@example.com',
  userName: 'testuser',
  role: 'admin',
  isEmailVerified: true,
  hasPassword: true,
};

/**
 * Create a mock JWT token that can be decoded by middleware (base64url encoded)
 */
export function createMockJWT(): string {
  const header = { alg: 'HS256', typ: 'JWT' };
  const payload = {
    sub: MOCK_USER.id,
    nameid: MOCK_USER.id,
    email: MOCK_USER.email,
    username: MOCK_USER.userName,
    unique_name: MOCK_USER.userName,
    role: MOCK_USER.role,
    email_verified: 'TRUE',
    exp: Math.floor(Date.now() / 1000) + 3600,
    iat: Math.floor(Date.now() / 1000),
  };

  const base64Header = Buffer.from(JSON.stringify(header)).toString('base64url');
  const base64Payload = Buffer.from(JSON.stringify(payload)).toString('base64url');
  return `${base64Header}.${base64Payload}.mock_signature_for_ci_testing`;
}

/**
 * Set up API mocks for authenticated E2E tests in CI.
 * Intercepts login, session, refresh, and proxy endpoints.
 *
 * Call this BEFORE navigating to any page that requires authentication.
 * Route handlers persist across navigations within the same page object.
 */
export async function setupAuthMocks(page: Page, preAuthenticated = false): Promise<void> {
  let authenticated = preAuthenticated;

  // Mock login endpoint — returns mock user on form submit
  await page.route('**/api/auth/login**', async route => {
    authenticated = true;
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        user: MOCK_USER,
        expiresIn: 1800,
      }),
    });
  });

  // Mock session endpoint — GET returns user if authenticated, POST saves token
  await page.route('**/api/auth/session', async route => {
    const method = route.request().method();
    if (method === 'GET') {
      if (authenticated) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ user: MOCK_USER }),
        });
      } else {
        await route.fulfill({
          status: 401,
          contentType: 'application/json',
          body: JSON.stringify({ user: null }),
        });
      }
    } else if (method === 'POST') {
      authenticated = true;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ success: true }),
      });
    } else {
      await route.continue();
    }
  });

  // Mock refresh endpoint
  await page.route('**/api/auth/refresh', async route => {
    if (authenticated) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          token: createMockJWT(),
          refreshToken: 'mock_refresh_token',
        }),
      });
    } else {
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Not authenticated' }),
      });
    }
  });

  // Mock proxy API endpoints (backend not available in CI)
  // Return empty/default responses to prevent page crashes
  await page.route('**/api/proxy/**', async route => {
    const url = route.request().url();

    // Users/me endpoint — return mock user
    if (url.includes('/users/me') || url.includes('/users/profile')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(MOCK_USER),
      });
      return;
    }

    // Servers endpoints — return empty arrays/objects
    if (url.includes('/servers')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      });
      return;
    }

    // Metrics endpoints — return empty data
    if (url.includes('/metrics') || url.includes('/monitoring')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ data: [], metrics: [] }),
      });
      return;
    }

    // Tickets endpoints
    if (url.includes('/tickets')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      });
      return;
    }

    // External logins / OAuth providers
    if (url.includes('/external-logins') || url.includes('/oauth')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      });
      return;
    }

    // Default: return empty 200 response
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({}),
    });
  });
}

/**
 * Set auth cookies on the browser context for server-side middleware checks.
 * Call this after successful mock login to ensure cookies are present
 * for subsequent page navigations.
 */
export async function setAuthCookies(page: Page): Promise<void> {
  await page.context().addCookies([
    {
      name: 'access_token',
      value: createMockJWT(),
      domain: 'localhost',
      path: '/',
      httpOnly: true,
      secure: false,
      sameSite: 'Lax',
    },
    {
      name: 'refresh_token',
      value: 'mock_refresh_token',
      domain: 'localhost',
      path: '/',
      httpOnly: true,
      secure: false,
      sameSite: 'Lax',
    },
  ]);
}
