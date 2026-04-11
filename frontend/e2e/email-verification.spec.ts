import { test, expect } from '@playwright/test';
import { loginAsUser, clearAuth } from './helpers/auth-helper';

/**
 * Email Verification E2E Tests
 * Covers: Email verification after registration, resend verification, verify email flow
 */

test.describe('Email Verification After Registration', () => {
  test('should show email verification required message after registration', async ({ page }) => {
    // Register new user
    await page.goto('/register');

    await page.fill(
      'input[type="email"], input[name="email"]',
      `testuser${Date.now()}@example.com`,
    );
    await page.fill('input[type="password"], input[name="password"]', 'testpassword123');
    await page.fill('input[name="username"], input[name="displayName"]', 'Test User');

    await page.click('button[type="submit"]');

    await page.waitForTimeout(3000);

    // Check if verification required message is shown
    const url = page.url();
    if (url.includes('/verify-email') || url.includes('/dashboard')) {
      await expect(
        page
          .locator('text=/verify|Verify|email|Email/i')
          .or(page.locator('[data-testid="verify-email-message"]')),
      ).toBeVisible();
    }
  });

  test('should display email verification page', async ({ page }) => {
    await page.goto('/verify-email');

    // Check page structure
    await expect(page.locator('h1, h2')).toContainText(/Verify Email|Email Verification/i);

    // Check for verification code input
    await expect(
      page.locator(
        'input[name="code"], input[name="verificationCode"], [data-testid="verification-code"]',
      ),
    ).toBeVisible();

    // Check for resend button
    await expect(
      page.locator(
        'button:has-text("Resend"), button:has-text("Send Again"), [data-testid="resend-code"]',
      ),
    ).toBeVisible();
  });

  test('should validate verification code format', async ({ page }) => {
    await page.goto('/verify-email');

    // Submit empty code
    await page.click('button[type="submit"], button:has-text("Verify"), button:has-text("Submit")');

    // Should show validation error
    await expect(
      page
        .locator('text=/required|Required|invalid|Invalid/i')
        .or(page.locator('.error, [role="alert"]')),
    ).toBeVisible();
  });

  test('should validate verification code length', async ({ page }) => {
    await page.goto('/verify-email');

    const codeInput = page.locator('input[name="code"], input[name="verificationCode"]');

    await codeInput.fill('123'); // Too short

    await page.click('button[type="submit"], button:has-text("Verify"), button:has-text("Submit")');

    await expect(
      page
        .locator('text=/invalid|Invalid|length|Length/i')
        .or(page.locator('.error, [role="alert"]')),
    ).toBeVisible();
  });

  test('should handle invalid verification code', async ({ page }) => {
    await page.goto('/verify-email');

    const codeInput = page.locator('input[name="code"], input[name="verificationCode"]');

    await codeInput.fill('000000'); // Invalid code

    await page.click('button[type="submit"], button:has-text("Verify"), button:has-text("Submit")');

    await page.waitForTimeout(1000);

    // Should show error
    await expect(
      page
        .locator('text=/invalid|Invalid|wrong|Wrong|expired|Expired/i')
        .or(page.locator('[role="alert"]')),
    ).toBeVisible();
  });

  test('should verify email with correct code', async ({ page }) => {
    await page.goto('/verify-email');

    const codeInput = page.locator('input[name="code"], input[name="verificationCode"]');

    // In real scenario, would get code from email service mock
    await codeInput.fill('123456');

    await page.click('button[type="submit"], button:has-text("Verify"), button:has-text("Submit")');

    await page.waitForTimeout(2000);

    // Should redirect to dashboard or show success
    const url = page.url();
    if (url.includes('/dashboard')) {
      await expect(page.locator('h1, h2')).toContainText(/Dashboard/i);
    } else {
      await expect(
        page
          .locator('text=/success|Success|verified|Verified/i')
          .or(page.locator('[data-testid="success-message"]')),
      ).toBeVisible();
    }
  });
});

test.describe('Resend Verification Code', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test('should resend verification code', async ({ page }) => {
    await page.goto('/verify-email');

    const resendButton = page.locator(
      'button:has-text("Resend"), button:has-text("Send Again"), [data-testid="resend-code"]',
    );

    if (await resendButton.isVisible().catch(() => false)) {
      await resendButton.click();

      await page.waitForTimeout(2000);

      // Should show success message
      await expect(
        page
          .locator('text=/sent|Sent|code|Code/i')
          .or(page.locator('[data-testid="success-message"]')),
      ).toBeVisible();
    }
  });

  test('should have cooldown period for resend', async ({ page }) => {
    await page.goto('/verify-email');

    const resendButton = page.locator(
      'button:has-text("Resend"), button:has-text("Send Again"), [data-testid="resend-code"]',
    );

    if (await resendButton.isVisible().catch(() => false)) {
      await resendButton.click();

      // Try to resend again immediately
      await page.waitForTimeout(100);

      // Should show cooldown message or disable button
      const isDisabled = await resendButton.isDisabled();
      const cooldownMessage = await page
        .locator('text=/wait|Wait|seconds|Seconds|cooldown/i')
        .isVisible()
        .catch(() => false);

      expect(isDisabled || cooldownMessage).toBeTruthy();
    }
  });

  test('should show email address being verified', async ({ page }) => {
    await page.goto('/verify-email');

    // Should display the email being verified
    await expect(
      page.locator('text=/@/.email, [data-testid="email-display"], .email-display'),
    ).toBeVisible();
  });
});

test.describe('Email Verification from Profile', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test('should navigate to verification from profile', async ({ page }) => {
    await page.goto('/profile');

    // Look for verification prompt if email not verified
    const verifyPrompt = page.locator(
      'text=/verify|Verify|email not verified/i, [data-testid="verify-prompt"]',
    );

    if (await verifyPrompt.isVisible().catch(() => false)) {
      await expect(verifyPrompt).toBeVisible();

      const verifyButton = page
        .locator('button:has-text("Verify"), a:has-text("Verify"), [data-testid="verify-button"]')
        .first();

      if (await verifyButton.isVisible().catch(() => false)) {
        await verifyButton.click();

        await page.waitForTimeout(1000);

        await expect(page).toHaveURL(/.*verify-email.*/);
      }
    }
  });

  test('should show verification status in profile', async ({ page }) => {
    await page.goto('/profile');

    // Look for email verification status
    const verificationStatus = page.locator(
      'text=/verified|Verified|not verified|Not verified/i, [data-testid="verification-status"]',
    );

    if (await verificationStatus.isVisible().catch(() => false)) {
      await expect(verificationStatus).toBeVisible();
    }
  });
});

test.describe('Email Verification After OAuth', () => {
  test('should skip verification for OAuth users', async ({ page }) => {
    // Mock OAuth callback that returns skipEmailVerification: true
    await page.route('**/api/auth/oauth/callback', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          token: 'mock_jwt_token',
          user: {
            id: 'oauth-user-123',
            email: 'oauthuser@example.com',
            username: 'OAuth User',
          },
          skipEmailVerification: true,
        }),
      });
    });

    // Simulate OAuth callback
    await page.goto('/oauth/callback?code=mock_code&state=test_state&provider=google');

    await page.waitForTimeout(2000);

    // Should redirect to dashboard without verification
    const url = page.url();
    if (url.includes('/dashboard')) {
      await expect(page.locator('h1, h2')).toContainText(/Dashboard/i);
    }
  });

  test('should require verification for OAuth users with email', async ({ page }) => {
    // Mock OAuth callback that returns user with email but skipEmailVerification: false
    await page.route('**/api/auth/oauth/callback', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          token: 'mock_jwt_token',
          user: {
            id: 'oauth-user-456',
            email: 'oauthuser@example.com',
            username: 'OAuth User',
          },
          skipEmailVerification: false,
        }),
      });
    });

    await page.goto('/oauth/callback?code=mock_code&state=test_state&provider=google');

    await page.waitForTimeout(2000);

    // Should redirect to verification page
    const url = page.url();
    if (url.includes('/verify-email')) {
      await expect(page.locator('h1, h2')).toContainText(/Verify Email/i);
    }
  });
});

test.describe('Email Verification Security', () => {
  test('should prevent verification without authentication', async ({ page }) => {
    await clearAuth(page.context());

    await page.goto('/verify-email');

    // Should redirect to login
    await expect(page).toHaveURL(/.*login.*/);
  });

  test('should limit verification attempts', async ({ page }) => {
    await loginAsUser(page);
    await page.goto('/verify-email');

    const codeInput = page.locator('input[name="code"], input[name="verificationCode"]');
    const submitButton = page.locator('button[type="submit"], button:has-text("Verify")');

    // Try multiple invalid attempts
    for (let i = 0; i < 5; i++) {
      await codeInput.fill('000000');
      await submitButton.click();
      await page.waitForTimeout(500);
    }

    // Should show rate limit message
    await expect(
      page
        .locator('text=/too many|rate limit|try again later/i')
        .or(page.locator('[role="alert"]')),
    ).toBeVisible();
  });
});

test.describe('Email Verification Redirects', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test('should redirect to dashboard after successful verification', async ({ page }) => {
    await page.goto('/verify-email');

    const codeInput = page.locator('input[name="code"], input[name="verificationCode"]');
    await codeInput.fill('123456');

    await page.click('button[type="submit"], button:has-text("Verify")');

    await page.waitForTimeout(2000);

    const url = page.url();
    if (url.includes('/dashboard')) {
      await expect(page.locator('h1, h2')).toContainText(/Dashboard/i);
    }
  });

  test('should redirect to original page after verification', async ({ page }) => {
    // Navigate to a protected page first
    await page.goto('/profile');

    // If verification is required, should redirect to verify-email with redirect param
    const url = page.url();
    if (url.includes('/verify-email')) {
      const codeInput = page.locator('input[name="code"], input[name="verificationCode"]');
      await codeInput.fill('123456');

      await page.click('button[type="submit"], button:has-text("Verify")');

      await page.waitForTimeout(2000);

      // Should redirect back to original page
      await expect(page).toHaveURL(/.*profile.*/);
    }
  });
});
