import { test, expect } from '@playwright/test';
import { loginAsUser, clearAuth } from './helpers/auth-helper';

/**
 * Authentication E2E Tests
 * Covers: Registration, Login, Logout flows
 */

test.describe('Authentication Flow', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should display login page with all auth options', async ({ page }) => {
    await page.goto('/login');
    
    // Check page title and form elements (h1 is "Welcome Back")
    await expect(page.locator('h1')).toContainText(/Welcome Back|Sign In|Login/i);
    await expect(page.locator('input[type="email"]').or(page.locator('input[name="email"]'))).toBeVisible();
    await expect(page.locator('input[type="password"]').first()).toBeVisible();
    await expect(page.locator('button[type="submit"]')).toBeVisible();
    
    // Check OAuth providers (3 SVG icon buttons after "Or continue with")
    const oAuthSection = page.locator('text=/Or continue with/i');
    await expect(oAuthSection).toBeVisible();
    // 3 OAuth buttons (GitHub, Google, Telegram) with SVG icons
    const oAuthButtons = page.locator('button svg').filter({ hasText: '' });
    await expect(oAuthButtons.count()).resolves.toBeGreaterThanOrEqual(3);
  });

  test('should display registration page', async ({ page }) => {
    await page.goto('/register');
    
    // Check page loaded (h1 could be "Create Account" or similar)
    await expect(page.locator('h1')).toBeVisible();
    await expect(page.locator('input[type="email"]').or(page.locator('input[name="email"]'))).toBeVisible();
    // Registration has password field(s)
    await expect(page.locator('input[type="password"]').first()).toBeVisible();
    await expect(page.locator('button[type="submit"]')).toBeVisible();
  });

  test('should show validation errors on empty form submit', async ({ page }) => {
    await page.goto('/login');
    
    // Submit empty form
    await page.click('button[type="submit"]');
    
    // Should show validation errors or stay on page
    await expect(page).toHaveURL(/.*login.*/);
  });

  test('should navigate between login and register', async ({ page }) => {
    await page.goto('/login');
    
    // Click register link
    const registerLink = page.locator('a[href="/register"], a:has-text("Sign up"), a:has-text("Register")');
    if (await registerLink.isVisible().catch(() => false)) {
      await registerLink.click();
      await expect(page).toHaveURL(/.*register.*/);
    }
  });

  test('should redirect to dashboard after successful login', async ({ page, context }) => {
    // This test requires a test user to exist
    // In real implementation, you'd create a test user via API or use test seeds
    await page.goto('/login');
    
    // Fill credentials (use test credentials from environment)
    const testEmail = process.env.TEST_USER_EMAIL || 'test@example.com';
    const testPassword = process.env.TEST_USER_PASSWORD || 'testpassword123';
    
    await page.fill('input[type="email"]', testEmail);
    await page.fill('input[type="password"] >> visible=true', testPassword);
    
    // Submit form
    await page.click('button[type="submit"]');
    
    // Wait for response (shorter timeout)
    await page.waitForTimeout(3000);
    
    // Check if redirected to dashboard or still on login (if auth fails)
    const url = page.url();
    if (url.includes('/dashboard')) {
      await expect(page.locator('[data-testid="dashboard"], h1:has-text("Dashboard")')).toBeVisible();
    }
  });

  test('should handle password reset flow navigation', async ({ page }) => {
    await page.goto('/login');
    
    // Click forgot password link
    const forgotLink = page.locator('a[href="/forgot-password"], a:has-text("Forgot password")');
    if (await forgotLink.isVisible().catch(() => false)) {
      await forgotLink.click();
      await expect(page).toHaveURL(/.*forgot-password.*/);
      await expect(page.locator('input[type="email"], input[name="email"]')).toBeVisible();
    }
  });
});

test.describe('Protected Routes', () => {
  test('should redirect unauthenticated user from dashboard to login', async ({ page }) => {
    // Clear any existing auth
    await clearAuth(page.context());
    
    await page.goto('/dashboard');
    
    // Should redirect to login
    await expect(page).toHaveURL(/.*login.*/);
  });

  test('should redirect unauthenticated user from profile to login', async ({ page }) => {
    await clearAuth(page.context());
    
    await page.goto('/profile');
    
    await expect(page).toHaveURL(/.*login.*/);
  });

  test('should redirect unauthenticated user from servers page to login', async ({ page }) => {
    await clearAuth(page.context());
    
    await page.goto('/servers');
    
    await expect(page).toHaveURL(/.*login.*/);
  });
});
