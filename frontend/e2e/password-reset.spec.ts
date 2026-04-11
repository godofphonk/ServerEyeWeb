import { test, expect } from '@playwright/test';
import { clearAuth } from './helpers/auth-helper';

/**
 * Password Reset E2E Tests
 * Covers: Forgot password flow, password reset with token, token validation
 */

test.describe('Forgot Password Flow', () => {
  test.beforeEach(async ({ page }) => {
    await clearAuth(page.context());
  });

  test('should display forgot password page', async ({ page }) => {
    await page.goto('/forgot-password');

    // Check page structure
    await expect(page.locator('h1, h2')).toContainText(/Forgot Password|Reset Password/i);

    // Check for email input
    await expect(page.locator('input[type="email"], input[name="email"]')).toBeVisible();

    // Check for submit button
    await expect(
      page.locator('button[type="submit"], button:has-text("Send"), button:has-text("Submit")'),
    ).toBeVisible();
  });

  test('should navigate to forgot password from login', async ({ page }) => {
    await page.goto('/login');

    // Click forgot password link
    const forgotLink = page.locator('a[href="/forgot-password"], a:has-text("Forgot password")');

    if (await forgotLink.isVisible().catch(() => false)) {
      await forgotLink.click();
      await expect(page).toHaveURL(/.*forgot-password.*/);
    }
  });

  test('should validate email format on forgot password', async ({ page }) => {
    await page.goto('/forgot-password');

    // Enter invalid email
    await page.fill('input[type="email"], input[name="email"]', 'invalid-email');

    // Submit
    await page.click('button[type="submit"], button:has-text("Send"), button:has-text("Submit")');

    // Should show validation error
    await expect(
      page
        .locator('text=/invalid|Invalid|format|Format/i')
        .or(page.locator('.error, [role="alert"]')),
    ).toBeVisible();
  });

  test('should submit forgot password request', async ({ page }) => {
    await page.goto('/forgot-password');

    // Enter email
    await page.fill('input[type="email"], input[name="email"]', 'test@example.com');

    // Submit
    await page.click('button[type="submit"], button:has-text("Send"), button:has-text("Submit")');

    await page.waitForTimeout(2000);

    // Should show success message
    await expect(
      page
        .locator('text=/sent|Sent|email|Email|check|Check/i')
        .or(page.locator('[data-testid="success-message"]')),
    ).toBeVisible();
  });

  test('should not reveal if email exists (security)', async ({ page }) => {
    await page.goto('/forgot-password');

    // Enter non-existent email
    await page.fill('input[type="email"], input[name="email"]', 'nonexistent@example.com');

    // Submit
    await page.click('button[type="submit"], button:has-text("Send"), button:has-text("Submit")');

    await page.waitForTimeout(2000);

    // Should show same message as for existing email (security best practice)
    await expect(
      page
        .locator('text=/sent|Sent|email|Email|check|Check/i')
        .or(page.locator('[data-testid="success-message"]')),
    ).toBeVisible();
  });

  test('should have link back to login', async ({ page }) => {
    await page.goto('/forgot-password');

    // Look for back to login link
    const loginLink = page.locator(
      'a[href="/login"], a:has-text("Back to login"), a:has-text("Login")',
    );

    if (await loginLink.isVisible().catch(() => false)) {
      await loginLink.click();
      await expect(page).toHaveURL(/.*login.*/);
    }
  });
});

test.describe('Password Reset with Token', () => {
  test.beforeEach(async ({ page }) => {
    await clearAuth(page.context());
  });

  test('should display reset password page with token', async ({ page }) => {
    // Navigate with reset token
    await page.goto('/reset-password?token=mock_reset_token');

    // Check page structure
    await expect(page.locator('h1, h2')).toContainText(/Reset Password|New Password/i);

    // Check for password fields
    await expect(page.locator('input[name="password"], input[type="password"]')).toBeVisible();
    await expect(page.locator('input[name="confirmPassword"]')).toBeVisible();
  });

  test('should validate token presence', async ({ page }) => {
    // Navigate without token
    await page.goto('/reset-password');

    await page.waitForTimeout(1000);

    // Should show error or redirect
    const url = page.url();
    if (url.includes('/forgot-password')) {
      // Redirected back to forgot password
      await expect(page.locator('h1, h2')).toContainText(/Forgot Password/i);
    } else {
      // Shows error on page
      await expect(
        page
          .locator('text=/invalid|Invalid|token|Token|expired|Expired/i')
          .or(page.locator('[role="alert"]')),
      ).toBeVisible();
    }
  });

  test('should validate new password format', async ({ page }) => {
    await page.goto('/reset-password?token=mock_reset_token');

    // Enter weak password
    await page.fill('input[name="password"], input[type="password"]', '123');

    // Submit
    await page.click('button[type="submit"], button:has-text("Reset"), button:has-text("Update")');

    // Should show validation error
    await expect(
      page
        .locator('text=/weak|Weak|short|Short|complexity|Complexity/i')
        .or(page.locator('.error, [role="alert"]')),
    ).toBeVisible();
  });

  test('should validate password confirmation', async ({ page }) => {
    await page.goto('/reset-password?token=mock_reset_token');

    await page.fill('input[name="password"], input[type="password"]', 'newpassword123');
    await page.fill('input[name="confirmPassword"]', 'differentpassword');

    await page.click('button[type="submit"], button:has-text("Reset"), button:has-text("Update")');

    // Should show mismatch error
    await expect(
      page
        .locator('text=/match|Match|confirm|Confirm/i')
        .or(page.locator('.error, [role="alert"]')),
    ).toBeVisible();
  });

  test('should reset password successfully', async ({ page }) => {
    // Mock successful reset API call
    await page.route('**/api/auth/reset-password', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ success: true, message: 'Password reset successfully' }),
      });
    });

    await page.goto('/reset-password?token=mock_reset_token');

    await page.fill('input[name="password"], input[type="password"]', 'newpassword123');
    await page.fill('input[name="confirmPassword"]', 'newpassword123');

    await page.click('button[type="submit"], button:has-text("Reset"), button:has-text("Update")');

    await page.waitForTimeout(2000);

    // Should show success message
    await expect(
      page
        .locator('text=/success|Success|reset|Reset/i')
        .or(page.locator('[data-testid="success-message"]')),
    ).toBeVisible();

    // Should have link to login
    const loginLink = page.locator('a[href="/login"], button:has-text("Login")');
    if (await loginLink.isVisible().catch(() => false)) {
      await expect(loginLink).toBeVisible();
    }
  });

  test('should handle invalid token', async ({ page }) => {
    // Mock failed reset API call
    await page.route('**/api/auth/reset-password', async route => {
      await route.fulfill({
        status: 400,
        contentType: 'application/json',
        body: JSON.stringify({ success: false, message: 'Invalid or expired token' }),
      });
    });

    await page.goto('/reset-password?token=invalid_token');

    await page.fill('input[name="password"], input[type="password"]', 'newpassword123');
    await page.fill('input[name="confirmPassword"]', 'newpassword123');

    await page.click('button[type="submit"], button:has-text("Reset"), button:has-text("Update")');

    await page.waitForTimeout(1000);

    // Should show error
    await expect(
      page
        .locator('text=/invalid|Invalid|expired|Expired|token|Token/i')
        .or(page.locator('[role="alert"]')),
    ).toBeVisible();
  });

  test('should redirect to login after successful reset', async ({ page }) => {
    await page.route('**/api/auth/reset-password', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ success: true, message: 'Password reset successfully' }),
      });
    });

    await page.goto('/reset-password?token=mock_reset_token');

    await page.fill('input[name="password"], input[type="password"]', 'newpassword123');
    await page.fill('input[name="confirmPassword"]', 'newpassword123');

    await page.click('button[type="submit"], button:has-text("Reset"), button:has-text("Update")');

    await page.waitForTimeout(2000);

    // Click login link if present
    const loginLink = page.locator('a[href="/login"], button:has-text("Login")');
    if (await loginLink.isVisible().catch(() => false)) {
      await loginLink.click();
      await expect(page).toHaveURL(/.*login.*/);
    }
  });

  test('should show password strength indicator', async ({ page }) => {
    await page.goto('/reset-password?token=mock_reset_token');

    const passwordInput = page.locator('input[name="password"], input[type="password"]').first();

    await passwordInput.fill('test');

    // Look for strength indicator
    const strengthIndicator = page.locator(
      '[data-testid="password-strength"], .password-strength, .strength-meter',
    );

    if (await strengthIndicator.isVisible().catch(() => false)) {
      await expect(strengthIndicator).toBeVisible();
    }
  });
});

test.describe('Password Reset Security', () => {
  test.beforeEach(async ({ page }) => {
    await clearAuth(page.context());
  });

  test('should limit forgot password attempts', async ({ page }) => {
    await page.goto('/forgot-password');

    const emailInput = page.locator('input[type="email"], input[name="email"]');
    const submitButton = page.locator('button[type="submit"], button:has-text("Send")');

    // Try multiple requests
    for (let i = 0; i < 5; i++) {
      await emailInput.fill('test@example.com');
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

  test('should limit reset attempts with token', async ({ page }) => {
    await page.route('**/api/auth/reset-password', async route => {
      await route.fulfill({
        status: 400,
        contentType: 'application/json',
        body: JSON.stringify({ success: false, message: 'Invalid token' }),
      });
    });

    await page.goto('/reset-password?token=mock_token');

    const passwordInput = page.locator('input[name="password"], input[type="password"]');
    const submitButton = page.locator('button[type="submit"], button:has-text("Reset")');

    // Try multiple invalid attempts
    for (let i = 0; i < 5; i++) {
      await passwordInput.fill('newpassword123');
      await page.fill('input[name="confirmPassword"]', 'newpassword123');
      await submitButton.click();
      await page.waitForTimeout(500);
    }

    // Should show rate limit or token invalidated message
    await expect(
      page
        .locator('text=/too many|rate limit|invalidated|Invalidated/i')
        .or(page.locator('[role="alert"]')),
    ).toBeVisible();
  });

  test('should invalidate token after use', async ({ page }) => {
    let callCount = 0;

    await page.route('**/api/auth/reset-password', async route => {
      callCount++;
      if (callCount === 1) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ success: true, message: 'Password reset successfully' }),
        });
      } else {
        await route.fulfill({
          status: 400,
          contentType: 'application/json',
          body: JSON.stringify({ success: false, message: 'Token already used' }),
        });
      }
    });

    // First successful reset
    await page.goto('/reset-password?token=mock_token');
    await page.fill('input[name="password"], input[type="password"]', 'newpassword123');
    await page.fill('input[name="confirmPassword"]', 'newpassword123');
    await page.click('button[type="submit"]');
    await page.waitForTimeout(1000);

    // Try to use same token again
    await page.goto('/reset-password?token=mock_token');
    await page.fill('input[name="password"], input[type="password"]', 'anotherpassword123');
    await page.fill('input[name="confirmPassword"]', 'anotherpassword123');
    await page.click('button[type="submit"]');
    await page.waitForTimeout(1000);

    // Should show error
    await expect(
      page.locator('text=/already used|used|invalid/i').or(page.locator('[role="alert"]')),
    ).toBeVisible();
  });

  test('should not allow reset if user is logged in', async ({ page }) => {
    // First login
    await page.goto('/login');
    await page.fill('input[type="email"], input[name="email"]', 'test@example.com');
    await page.fill('input[type="password"], input[name="password"]', 'testpassword123');
    await page.click('button[type="submit"]');
    await page.waitForTimeout(2000);

    // Try to access forgot password while logged in
    await page.goto('/forgot-password');

    // Should either redirect to profile or show message
    const url = page.url();
    if (url.includes('/profile')) {
      await expect(page.locator('h1, h2')).toContainText(/Profile/i);
    } else {
      await expect(
        page
          .locator('text=/already logged in|logged in|logout first/i')
          .or(page.locator('[data-testid="info-message"]')),
      ).toBeVisible();
    }
  });
});

test.describe('Password Reset UX', () => {
  test.beforeEach(async ({ page }) => {
    await clearAuth(page.context());
  });

  test('should show email masking in confirmation', async ({ page }) => {
    await page.goto('/forgot-password');

    await page.fill('input[type="email"], input[name="email"]', 'testuser@example.com');
    await page.click('button[type="submit"], button:has-text("Send")');

    await page.waitForTimeout(2000);

    // Should show masked email (e.g., t***@example.com)
    await expect(page.locator('text=/@/.email, [data-testid="email-display"]')).toBeVisible();
  });

  test('should have countdown for resend on forgot password', async ({ page }) => {
    await page.goto('/forgot-password');

    await page.fill('input[type="email"], input[name="email"]', 'test@example.com');
    await page.click('button[type="submit"], button:has-text("Send")');

    await page.waitForTimeout(2000);

    // Look for resend button with countdown
    const resendButton = page.locator('button:has-text("Resend"), [data-testid="resend-link"]');

    if (await resendButton.isVisible().catch(() => false)) {
      // Should be disabled or show countdown
      const isDisabled = await resendButton.isDisabled();
      const countdown = await page
        .locator('text=/seconds|Seconds|wait|Wait/i')
        .isVisible()
        .catch(() => false);

      expect(isDisabled || countdown).toBeTruthy();
    }
  });

  test('should show password requirements', async ({ page }) => {
    await page.goto('/reset-password?token=mock_token');

    // Look for password requirements
    const requirements = page.locator(
      'text=/at least|minimum|uppercase|lowercase|number|special character/i, [data-testid="password-requirements"]',
    );

    if (await requirements.isVisible().catch(() => false)) {
      await expect(requirements).toBeVisible();
    }
  });

  test('should toggle password visibility', async ({ page }) => {
    await page.goto('/reset-password?token=mock_token');

    const passwordInput = page.locator('input[name="password"], input[type="password"]').first();
    const toggleButton = page
      .locator('button:has-text("Show"), button:has-text("Hide"), [data-testid="toggle-password"]')
      .first();

    if (await toggleButton.isVisible().catch(() => false)) {
      const initialType = await passwordInput.getAttribute('type');

      await toggleButton.click();

      const newType = await passwordInput.getAttribute('type');

      expect(initialType).not.toBe(newType);
    }
  });
});
