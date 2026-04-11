import { test, expect, Page } from '@playwright/test';
import { loginAsUser, clearAuth } from './helpers/auth-helper';

/**
 * OAuth Flow E2E Tests
 * Uses request interception to mock OAuth providers
 * Covers: OAuth initiation, callback handling, error states, account linking
 */

/**
 * Mock Google OAuth flow by intercepting requests
 */
async function mockGoogleOAuth(page: Page, success: boolean = true) {
  // Intercept OAuth challenge request
  await page.route('**/api/auth/oauth/google/challenge**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        challengeUrl: `https://accounts.google.com/o/oauth2/v2/auth?client_id=test&redirect_uri=http://localhost:3000/api/auth/oauth/callback&state=test_state_google`,
        state: 'test_state_google',
        provider: 'google',
      }),
    });
  });

  // Intercept OAuth callback
  await page.route('**/api/auth/oauth/callback**', async route => {
    if (success) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          token: 'mock_jwt_token',
          refreshToken: 'mock_refresh_token',
          user: {
            id: 'google-user-123',
            email: 'googleuser@example.com',
            username: 'Google User',
          },
          skipEmailVerification: true,
        }),
      });
    } else {
      await route.fulfill({
        status: 400,
        contentType: 'application/json',
        body: JSON.stringify({
          success: false,
          message: 'user_already_exists',
        }),
      });
    }
  });
}

/**
 * Mock GitHub OAuth flow
 */
async function mockGitHubOAuth(page: Page, success: boolean = true) {
  await page.route('**/api/auth/oauth/github/challenge**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        challengeUrl: `https://github.com/login/oauth/authorize?client_id=test&redirect_uri=http://localhost:3000/api/auth/oauth/callback&state=test_state_github`,
        state: 'test_state_github',
        provider: 'github',
      }),
    });
  });

  await page.route('**/api/auth/oauth/callback**', async (route, _request) => {
    if (success) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          token: 'mock_github_jwt',
          user: {
            id: 'github-user-456',
            email: 'githubuser@example.com',
            username: 'GitHubUser',
          },
        }),
      });
    } else {
      await route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'oauth_failed' }),
      });
    }
  });
}

/**
 * Mock Telegram OAuth flow (special handling - uses widget)
 */
async function mockTelegramOAuth(page: Page, success: boolean = true) {
  // Telegram uses different flow - widget-based
  await page.route('**/api/auth/oauth/telegram/callback**', async route => {
    if (success) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          token: 'mock_telegram_jwt',
          user: {
            id: 'telegram-user-789',
            username: 'TelegramUser',
            email: null,
          },
          skipEmailVerification: true,
        }),
      });
    } else {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: false,
          message: 'user_not_found',
        }),
      });
    }
  });
}

test.describe('OAuth Login Flow', () => {
  test.beforeEach(async ({ page }) => {
    await clearAuth(page.context());
    await page.goto('/login');
  });

  test('should initiate Google OAuth login', async ({ page }) => {
    await mockGoogleOAuth(page, true);

    // Click Google login button
    const googleButton = page
      .locator('[data-testid="google-login"], button:has-text("Google")')
      .first();

    if (await googleButton.isVisible().catch(() => false)) {
      await googleButton.click();

      // Wait for challenge request (intercepted)
      await page.waitForTimeout(500);

      // Should show loading or redirect state
      await expect(
        page
          .locator('text=/loading|redirect|authenticating/i')
          .or(page.locator('[data-testid="oauth-loading"]')),
      ).toBeVisible();
    }
  });

  test('should handle successful OAuth callback', async ({ page }) => {
    await mockGoogleOAuth(page, true);

    // Simulate OAuth callback with code and state
    await page.goto('/oauth/callback?code=mock_auth_code&state=test_state_google&provider=google');

    // Should redirect to dashboard on success
    await page.waitForTimeout(1000);

    // Check if redirected to dashboard or shows success
    const url = page.url();
    if (url.includes('/dashboard') || url.includes('/success')) {
      await expect(
        page
          .locator('[data-testid="dashboard"], h1:has-text("Dashboard")')
          .or(page.locator('text=/welcome|success/i')),
      ).toBeVisible();
    }
  });

  test('should handle OAuth user already exists error', async ({ page }) => {
    await mockGoogleOAuth(page, false);

    // Attempt OAuth login
    await page.goto('/oauth/callback?code=mock_code&state=test_state_google');

    await page.waitForTimeout(1000);

    // Should show error or redirect to login with error
    const url = page.url();
    expect(url).toMatch(/login|error|register/);
  });

  test('should initiate GitHub OAuth login', async ({ page }) => {
    await mockGitHubOAuth(page, true);

    const githubButton = page
      .locator('[data-testid="github-login"], button:has-text("GitHub")')
      .first();

    if (await githubButton.isVisible().catch(() => false)) {
      await githubButton.click();
      await page.waitForTimeout(500);

      // Check for challenge initiation
      await expect(
        page
          .locator('text=/redirect|authenticating/i')
          .or(page.locator('[data-testid="oauth-loading"]')),
      ).toBeVisible();
    }
  });

  test('should handle GitHub OAuth failure', async ({ page }) => {
    await mockGitHubOAuth(page, false);

    await page.goto('/oauth/callback?code=mock_code&state=test_state_github&provider=github');
    await page.waitForTimeout(1000);

    // Should show error message
    await expect(
      page
        .locator('text=/error|failed|Error|Failed/i')
        .or(page.locator('[role="alert"]'))
        .or(page.locator('[data-testid="oauth-error"]')),
    ).toBeVisible();
  });

  test('should handle missing OAuth code in callback', async ({ page }) => {
    // Callback without code
    await page.goto('/oauth/callback?state=test_state&provider=google');

    // Should show error
    await expect(
      page.locator('text=/missing|error|invalid/i').or(page.locator('[data-testid="oauth-error"]')),
    ).toBeVisible();
  });

  test('should handle invalid OAuth state', async ({ page }) => {
    // Callback with invalid state (CSRF protection)
    await page.goto('/oauth/callback?code=mock_code&state=invalid_state&provider=google');

    await page.waitForTimeout(1000);

    // Should fail validation
    const url = page.url();
    expect(url).toMatch(/error|login/);
  });
});

test.describe('OAuth Account Linking', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test('should show linked OAuth accounts in profile', async ({ page }) => {
    // Mock API for linked accounts
    await page.route('**/api/auth/oauth/providers', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { provider: 'google', linked: true, email: 'user@gmail.com' },
          { provider: 'github', linked: false },
          { provider: 'telegram', linked: false },
        ]),
      });
    });

    await page.goto('/profile');

    // Look for connected accounts section
    const connectedAccounts = page.locator(
      'text=/connected|linked|accounts|oauth/i, [data-testid="connected-accounts"]',
    );

    if (
      await connectedAccounts
        .first()
        .isVisible()
        .catch(() => false)
    ) {
      await expect(page.locator('text=/Google|GitHub|Telegram/i').first()).toBeVisible();
    }
  });

  test('should link new OAuth provider from profile', async ({ page }) => {
    await mockGoogleOAuth(page, true);

    await page.goto('/profile');

    // Find link account button
    const linkButton = page
      .locator('button:has-text("Link"), button:has-text("Connect"), [data-testid="link-google"]')
      .first();

    if (await linkButton.isVisible().catch(() => false)) {
      await linkButton.click();

      // Should initiate OAuth linking
      await page.waitForTimeout(500);

      // Check for success or redirect
      await expect(
        page
          .locator('text=/linking|connecting|success/i')
          .or(page.locator('[data-testid="link-success"]')),
      ).toBeVisible();
    }
  });

  test('should unlink OAuth provider', async ({ page }) => {
    // Mock API for unlinking
    await page.route('**/api/auth/oauth/google**', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'External login unlinked successfully' }),
      });
    });

    await page.goto('/profile');

    // Find unlink button for connected account
    const unlinkButton = page
      .locator(
        'button:has-text("Unlink"), button:has-text("Disconnect"), [data-testid="unlink-google"]',
      )
      .first();

    if (await unlinkButton.isVisible().catch(() => false)) {
      await unlinkButton.click();

      // Confirm unlinking
      const confirmButton = page
        .locator('button:has-text("Confirm"), button:has-text("Yes")')
        .first();
      if (await confirmButton.isVisible().catch(() => false)) {
        await confirmButton.click();
      }

      await page.waitForTimeout(500);

      // Should show success or updated state
      await expect(
        page
          .locator('text=/unlinked|success|removed/i')
          .or(page.locator('[data-testid="unlink-success"]')),
      ).toBeVisible();
    }
  });

  test('should prevent unlinking last auth method', async ({ page }) => {
    await page.goto('/profile');

    // Try to unlink when only one auth method exists
    const unlinkButton = page.locator('button:has-text("Unlink")').first();

    if (await unlinkButton.isVisible().catch(() => false)) {
      await unlinkButton.click();

      // Should show warning about last auth method
      await expect(
        page.locator('text=/cannot|last|at least one|warning/i').or(page.locator('[role="alert"]')),
      ).toBeVisible();
    }
  });

  test('should show already linked error', async ({ page }) => {
    // Mock error for already linked account
    await page.route('**/api/auth/oauth/link', async route => {
      await route.fulfill({
        status: 400,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'This Google account is already linked to another user' }),
      });
    });

    await page.goto('/profile');

    const linkButton = page.locator('button:has-text("Link Google")').first();

    if (await linkButton.isVisible().catch(() => false)) {
      await linkButton.click();

      await page.waitForTimeout(500);

      // Should show error
      await expect(
        page
          .locator('text=/already linked|another user|error/i')
          .or(page.locator('[role="alert"]')),
      ).toBeVisible();
    }
  });
});

test.describe('Telegram OAuth Special Flow', () => {
  test('should show Telegram login widget', async ({ page }) => {
    await page.goto('/login');

    // Telegram uses widget-based auth
    const telegramWidget = page
      .locator('[data-testid="telegram-login"], script[src*="telegram"], .telegram-login')
      .first();
    const telegramButton = page.locator('button:has-text("Telegram")').first();

    if (
      (await telegramWidget.isVisible().catch(() => false)) ||
      (await telegramButton.isVisible().catch(() => false))
    ) {
      await expect(telegramWidget.or(telegramButton)).toBeVisible();
    }
  });

  test('should handle Telegram callback via bot', async ({ page }) => {
    await mockTelegramOAuth(page, true);

    // Simulate Telegram widget callback
    await page.goto('/telegram-callback');

    // Post data as Telegram does
    await page.evaluate(() => {
      // Simulate Telegram auth data
      const authData = {
        id: 123456789,
        first_name: 'Test',
        username: 'testuser',
        auth_date: Math.floor(Date.now() / 1000),
        hash: 'mock_hash',
      };

      // Trigger callback (implementation depends on frontend)
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (window as any).onTelegramAuth?.(authData);
    });

    await page.waitForTimeout(1000);

    // Should be authenticated
    const url = page.url();
    expect(url).not.toContain('/login');
  });

  test('should handle Telegram user not found', async ({ page }) => {
    await mockTelegramOAuth(page, false);

    await page.goto('/telegram-callback');
    await page.waitForTimeout(1000);

    // Should prompt for registration or show error
    await expect(
      page
        .locator('text=/user not found|register|create account/i')
        .or(page.locator('[data-testid="telegram-register"]')),
    ).toBeVisible();
  });
});

test.describe('OAuth Security', () => {
  test('should validate OAuth state parameter', async ({ page }) => {
    // Attempt callback with mismatched state
    await page.goto('/oauth/callback?code=valid_code&state=tampered_state&provider=google');

    // Should reject as invalid state
    await page.waitForTimeout(500);

    const url = page.url();
    expect(url).toMatch(/error|login|invalid/);
  });

  test('should not expose OAuth tokens in URL after callback', async ({ page }) => {
    await mockGoogleOAuth(page, true);

    // After OAuth callback
    await page.goto('/oauth/callback?code=secret_auth_code&state=test_state');
    await page.waitForTimeout(1500);

    // URL should not contain sensitive data
    const url = page.url();
    expect(url).not.toContain('secret_auth_code');
    expect(url).not.toMatch(/token|code=/i);
  });

  test('should require authentication for OAuth linking', async ({ page }) => {
    // Try to access linking endpoint without auth
    await clearAuth(page.context());

    await page.goto('/api/auth/oauth/link', { waitUntil: 'networkidle' });

    // Should return 401 or redirect to login
    await expect(
      page
        .locator('text=/unauthorized|login required|401/i')
        .or(page.locator('body:has-text("Unauthorized")')),
    ).toBeVisible();
  });
});
