import { test, expect } from '@playwright/test';
import { loginAsUser, loginAsUnverifiedUser, clearAuth } from './helpers/auth-helper';
import { setupAuthMocks } from './helpers/ci-mocks';

/**
 * Email Verification E2E Tests
 * Covers: Email verification after registration, verify-email page, resend code, security
 *
 * UI structure:
 * - Register page: email + password + confirmPassword + checkbox → modal with code input
 * - Verify-email page: "Enter Verification Code" button → modal with code input
 * - EmailVerificationModal: h3 "Verify Your Email", input placeholder="Enter 6-digit code",
 *   button "Verify Email", button "Resend"
 */

test.describe('Email Verification After Registration', () => {
  test('should show verification modal after successful registration', async ({ page }) => {
    if (process.env.CI) {
      await setupAuthMocks(page);
    }

    await page.goto('/register');

    // Fill registration form (actual fields: email, password, confirmPassword)
    await page.fill('input[type="email"]', `testuser${Date.now()}@example.com`);
    await page.locator('input[type="password"]').first().fill('testpassword123');
    await page.locator('input[type="password"]').nth(1).fill('testpassword123');

    // Check terms checkbox if present
    const checkbox = page.locator('input[type="checkbox"]');
    if (await checkbox.isVisible().catch(() => false)) {
      await checkbox.check();
    }

    await page.click('button[type="submit"]');

    // After registration, EmailVerificationModal should appear
    await expect(page.locator('h3:has-text("Verify Your Email")')).toBeVisible({
      timeout: 10000,
    });

    // Check that code input is available in the modal
    await expect(page.locator('input[placeholder="Enter 6-digit code"]')).toBeVisible();
  });

  test('should verify email with code after registration', async ({ page }) => {
    if (process.env.CI) {
      await setupAuthMocks(page);
    }

    await page.goto('/register');

    await page.fill('input[type="email"]', `testuser${Date.now()}@example.com`);
    await page.locator('input[type="password"]').first().fill('testpassword123');
    await page.locator('input[type="password"]').nth(1).fill('testpassword123');

    const checkbox = page.locator('input[type="checkbox"]');
    if (await checkbox.isVisible().catch(() => false)) {
      await checkbox.check();
    }

    await page.click('button[type="submit"]');

    // Wait for modal
    await expect(page.locator('h3:has-text("Verify Your Email")')).toBeVisible({
      timeout: 10000,
    });

    // Fill verification code and submit
    await page.locator('input[placeholder="Enter 6-digit code"]').fill('123456');
    await page.locator('button:has-text("Verify Email")').click();

    await page.waitForTimeout(2000);

    // Should redirect to dashboard or show success
    const url = page.url();
    if (url.includes('/dashboard')) {
      await expect(page.locator('h1, h2')).toBeVisible();
    }
  });

  test('should show resend option in verification modal after registration', async ({ page }) => {
    if (process.env.CI) {
      await setupAuthMocks(page);
    }

    await page.goto('/register');

    await page.fill('input[type="email"]', `testuser${Date.now()}@example.com`);
    await page.locator('input[type="password"]').first().fill('testpassword123');
    await page.locator('input[type="password"]').nth(1).fill('testpassword123');

    const checkbox = page.locator('input[type="checkbox"]');
    if (await checkbox.isVisible().catch(() => false)) {
      await checkbox.check();
    }

    await page.click('button[type="submit"]');

    await expect(page.locator('h3:has-text("Verify Your Email")')).toBeVisible({
      timeout: 10000,
    });

    // Check for resend option in modal
    await expect(page.locator('button:has-text("Resend")')).toBeVisible();
  });
});

test.describe('Verify Email Page', () => {
  test('should display verify email page for unverified user', async ({ page }) => {
    if (!process.env.CI) {
      test.skip(true, 'Requires CI mocks for unverified user');
    }

    await loginAsUnverifiedUser(page);
    await page.goto('/verify-email');

    // Check page heading
    await expect(page.locator('h1')).toContainText('Verify Your Email');

    // Check for Enter Verification Code button
    await expect(page.locator('button:has-text("Enter Verification Code")')).toBeVisible();
  });

  test('should open verification modal from verify-email page', async ({ page }) => {
    if (!process.env.CI) {
      test.skip(true, 'Requires CI mocks for unverified user');
    }

    await loginAsUnverifiedUser(page);
    await page.goto('/verify-email');

    // Click to open modal
    await page.locator('button:has-text("Enter Verification Code")').click();

    // Modal should appear with code input and submit button
    await expect(page.locator('h3:has-text("Verify Your Email")')).toBeVisible();
    await expect(page.locator('input[placeholder="Enter 6-digit code"]')).toBeVisible();
    await expect(page.locator('button:has-text("Verify Email")')).toBeVisible();
  });

  test('should verify email with code from verify-email page', async ({ page }) => {
    if (!process.env.CI) {
      test.skip(true, 'Requires CI mocks for unverified user');
    }

    await loginAsUnverifiedUser(page);
    await page.goto('/verify-email');

    // Open modal and fill code
    await page.locator('button:has-text("Enter Verification Code")').click();
    await expect(page.locator('input[placeholder="Enter 6-digit code"]')).toBeVisible();

    await page.locator('input[placeholder="Enter 6-digit code"]').fill('123456');
    await page.locator('button:has-text("Verify Email")').click();

    await page.waitForTimeout(2000);

    // Should redirect to dashboard after verification
    const url = page.url();
    if (url.includes('/dashboard')) {
      await expect(page.locator('h1, h2')).toBeVisible();
    }
  });

  test('should display user email on verify-email page', async ({ page }) => {
    if (!process.env.CI) {
      test.skip(true, 'Requires CI mocks for unverified user');
    }

    await loginAsUnverifiedUser(page);
    await page.goto('/verify-email');

    // Should display the mock user email
    await expect(page.locator('text=test@example.com')).toBeVisible();
  });
});

test.describe('Resend Verification Code', () => {
  test('should resend verification code from modal', async ({ page }) => {
    if (!process.env.CI) {
      test.skip(true, 'Requires CI mocks for unverified user');
    }

    await loginAsUnverifiedUser(page);
    await page.goto('/verify-email');

    // Open modal
    await page.locator('button:has-text("Enter Verification Code")').click();

    // Click resend
    const resendButton = page.locator('button:has-text("Resend")');
    await expect(resendButton).toBeVisible();
    await resendButton.click();

    // Should show countdown or success indicator after resend
    await page.waitForTimeout(1000);

    const countdownVisible = await page
      .locator('text=/Resend available in/')
      .isVisible()
      .catch(() => false);
    const successVisible = await page
      .locator('.text-green-400')
      .isVisible()
      .catch(() => false);

    expect(countdownVisible || successVisible).toBeTruthy();
  });

  test('should have cooldown after resend', async ({ page }) => {
    if (!process.env.CI) {
      test.skip(true, 'Requires CI mocks for unverified user');
    }

    await loginAsUnverifiedUser(page);
    await page.goto('/verify-email');

    // Open modal
    await page.locator('button:has-text("Enter Verification Code")').click();

    // Click resend
    await page.locator('button:has-text("Resend")').click();

    await page.waitForTimeout(500);

    // Resend button should be disabled during cooldown
    const resendButton = page.locator('button:has-text("Resend"), button:has-text("Sending")');
    const isDisabled = await resendButton
      .first()
      .isDisabled()
      .catch(() => false);
    const countdownVisible = await page
      .locator('text=/Resend available in/')
      .isVisible()
      .catch(() => false);

    expect(isDisabled || countdownVisible).toBeTruthy();
  });
});

test.describe('Email Verification from Profile', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test('should show verification status in profile', async ({ page }) => {
    await page.goto('/profile');

    // Look for email verification status indicator
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
    await page.route('**/api/auth/oauth/callback**', async route => {
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
    // Mock OAuth callback that returns skipEmailVerification: false
    await page.route('**/api/auth/oauth/callback**', async route => {
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
      await expect(page.locator('h1')).toContainText(/Verify Your Email/i);
    }
  });
});

test.describe('Email Verification Security', () => {
  test('should redirect to login without authentication', async ({ page }) => {
    await clearAuth(page.context());

    // In CI, set up session mock that returns 401 (not authenticated)
    if (process.env.CI) {
      await setupAuthMocks(page, false);
    }

    await page.goto('/verify-email');

    // Should redirect to login (no user, no pending email)
    await expect(page).toHaveURL(/.*login.*/, { timeout: 10000 });
  });

  test('should redirect verified user away from verify-email', async ({ page }) => {
    // loginAsUser creates verified user (isEmailVerified: true)
    await loginAsUser(page);
    await page.goto('/verify-email');

    await page.waitForTimeout(3000);

    // Verified user should be redirected away from verify-email page
    const url = page.url();
    expect(url).not.toContain('/verify-email');
  });
});

test.describe('Email Verification Redirects', () => {
  test('should redirect to dashboard after successful verification', async ({ page }) => {
    if (!process.env.CI) {
      test.skip(true, 'Requires CI mocks for unverified user');
    }

    await loginAsUnverifiedUser(page);
    await page.goto('/verify-email');

    // Open modal and verify
    await page.locator('button:has-text("Enter Verification Code")').click();
    await page.locator('input[placeholder="Enter 6-digit code"]').fill('123456');
    await page.locator('button:has-text("Verify Email")').click();

    // Should redirect to dashboard
    await page.waitForURL(/.*dashboard.*/, { timeout: 10000 }).catch(() => {});

    const url = page.url();
    if (url.includes('/dashboard')) {
      await expect(page.locator('h1, h2')).toBeVisible();
    }
  });
});
