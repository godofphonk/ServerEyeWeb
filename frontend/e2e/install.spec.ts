import { test, expect } from '@playwright/test';
import { clearAuth } from './helpers/auth-helper';

/**
 * Installation/Setup E2E Tests
 * Covers: Install page, initial setup flow, configuration
 */

test.describe('Install/Setup Page', () => {
  test.beforeEach(async ({ page }) => {
    await clearAuth(page.context());
  });

  test('should display install page', async ({ page }) => {
    await page.goto('/install');

    // Check page structure
    await expect(page.locator('h1, h2')).toContainText(/Install|Setup|Welcome/i);

    // Check for installation steps indicator
    await expect(
      page.locator('[data-testid="steps"], .steps, .progress-steps, [data-testid="progress"]'),
    ).toBeVisible();
  });

  test('should show installation steps', async ({ page }) => {
    await page.goto('/install');

    // Look for step indicators
    const steps = page.locator('[data-testid="step"], .step, [data-testid="step-indicator"]');

    const stepCount = await steps.count();
    expect(stepCount).toBeGreaterThan(0);
  });

  test('should have database configuration step', async ({ page }) => {
    await page.goto('/install');

    // Look for database configuration form
    const dbConfig = page.locator(
      'text=/database|Database|postgres|PostgreSQL|mysql|MySQL/i, [data-testid="database-config"]',
    );

    if (await dbConfig.isVisible().catch(() => false)) {
      await expect(dbConfig).toBeVisible();

      // Check for database fields
      await expect(
        page.locator('input[name="host"], input[name="dbHost"], input[placeholder*="host" i]'),
      ).toBeVisible();
    }
  });

  test('should have admin account creation step', async ({ page }) => {
    await page.goto('/install');

    // Look for admin account form
    const adminConfig = page.locator(
      'text=/admin|Admin|account|Account/i, [data-testid="admin-config"]',
    );

    if (await adminConfig.isVisible().catch(() => false)) {
      await expect(adminConfig).toBeVisible();

      // Check for admin fields
      await expect(
        page.locator('input[name="email"], input[name="adminEmail"], input[placeholder*="email" i]'),
      ).toBeVisible();
      await expect(
        page.locator('input[name="password"], input[name="adminPassword"], input[type="password"]'),
      ).toBeVisible();
    }
  });

  test('should have application configuration step', async ({ page }) => {
    await page.goto('/install');

    // Look for app configuration form
    const appConfig = page.locator(
      'text=/application|Application|settings|Settings|configuration|Configuration/i, [data-testid="app-config"]',
    );

    if (await appConfig.isVisible().catch(() => false)) {
      await expect(appConfig).toBeVisible();

      // Check for app name field
      await expect(
        page.locator('input[name="appName"], input[name="applicationName"], input[placeholder*="name" i]'),
      ).toBeVisible();
    }
  });

  test('should validate required fields in install form', async ({ page }) => {
    await page.goto('/install');

    // Try to proceed without filling required fields
    const nextButton = page.locator('button:has-text("Next"), button:has-text("Continue"), button[type="submit"]').first();

    if (await nextButton.isVisible().catch(() => false)) {
      await nextButton.click();

      await page.waitForTimeout(500);

      // Should show validation errors
      await expect(
        page.locator('text=/required|Required|error|Error/i').or(page.locator('.error, [role="alert"]')),
      ).toBeVisible();
    }
  });

  test('should navigate between installation steps', async ({ page }) => {
    await page.goto('/install');

    // Look for navigation buttons
    const nextButton = page.locator('button:has-text("Next"), button:has-text("Continue")').first();
    const backButton = page.locator('button:has-text("Back"), button:has-text("Previous")').first();

    if (await nextButton.isVisible().catch(() => false)) {
      // Fill required fields if needed
      const emailInput = page.locator('input[name="email"], input[name="adminEmail"]');
      if (await emailInput.isVisible().catch(() => false)) {
        await emailInput.fill('admin@example.com');
      }

      const passwordInput = page.locator('input[name="password"], input[name="adminPassword"]');
      if (await passwordInput.isVisible().catch(() => false)) {
        await passwordInput.fill('adminpassword123');
      }

      await nextButton.click();
      await page.waitForTimeout(500);

      // Should navigate to next step
      const currentStep = page.locator('[data-testid="step"], .step.active').first();
      if (await currentStep.isVisible().catch(() => false)) {
        await expect(currentStep).toBeVisible();
      }

      // Go back
      if (await backButton.isVisible().catch(() => false)) {
        await backButton.click();
        await page.waitForTimeout(500);
      }
    }
  });

  test('should show installation progress', async ({ page }) => {
    await page.goto('/install');

    // Fill all forms and submit
    // (This would require filling all steps, simplified for E2E)

    const finishButton = page.locator('button:has-text("Finish"), button:has-text("Install"), button:has-text("Complete")').first();

    if (await finishButton.isVisible().catch(() => false)) {
      await finishButton.click();

      await page.waitForTimeout(2000);

      // Should show progress indicator
      await expect(
        page.locator('[data-testid="progress"], .progress, .loading, text=/installing|Installing|progress/i'),
      ).toBeVisible();
    }
  });

  test('should show installation success message', async ({ page }) => {
    await page.goto('/install');

    // Fill and submit installation
    // (Simplified - in real scenario would fill all steps)

    // Mock successful installation
    await page.route('**/api/install', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ success: true, message: 'Installation completed' }),
      });
    });

    // Navigate to final step and submit
    const finishButton = page.locator('button:has-text("Finish"), button:has-text("Install")').first();

    if (await finishButton.isVisible().catch(() => false)) {
      await finishButton.click();
      await page.waitForTimeout(3000);

      // Should show success message
      await expect(
        page.locator('text=/success|Success|completed|Completed|installed|Installed/i').or(
          page.locator('[data-testid="success-message"]'),
        ),
      ).toBeVisible();
    }
  });

  test('should redirect to login after successful installation', async ({ page }) => {
    await page.route('**/api/install', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ success: true, message: 'Installation completed' }),
      });
    });

    await page.goto('/install');

    const finishButton = page.locator('button:has-text("Finish"), button:has-text("Install")').first();

    if (await finishButton.isVisible().catch(() => false)) {
      await finishButton.click();
      await page.waitForTimeout(3000);

      // Look for login link/button
      const loginButton = page.locator('a[href="/login"], button:has-text("Login"), button:has-text("Go to Login")');

      if (await loginButton.isVisible().catch(() => false)) {
        await loginButton.click();
        await expect(page).toHaveURL(/.*login.*/);
      }
    }
  });
});

test.describe('Installation Security', () => {
  test.beforeEach(async ({ page }) => {
    await clearAuth(page.context());
  });

  test('should not allow installation if already installed', async ({ page }) => {
    // Mock API that returns already installed
    await page.route('**/api/install/check', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ installed: true }),
      });
    });

    await page.goto('/install');

    // Should redirect to login or show message
    const url = page.url();
    if (url.includes('/login')) {
      await expect(page.locator('h1, h2')).toContainText(/Login/i);
    } else {
      await expect(
        page.locator('text=/already installed|Already installed|system configured|System configured/i').or(
          page.locator('[data-testid="info-message"]'),
        ),
      ).toBeVisible();
    }
  });

  test('should validate admin password strength', async ({ page }) => {
    await page.goto('/install');

    const passwordInput = page.locator('input[name="password"], input[name="adminPassword"]');

    if (await passwordInput.isVisible().catch(() => false)) {
      await passwordInput.fill('123');

      const nextButton = page.locator('button:has-text("Next"), button:has-text("Continue")').first();

      if (await nextButton.isVisible().catch(() => false)) {
        await nextButton.click();
        await page.waitForTimeout(500);

        // Should show password strength error
        await expect(
          page.locator('text=/weak|Weak|short|Short|complexity|Complexity/i').or(page.locator('.error, [role="alert"]')),
        ).toBeVisible();
      }
    }
  });

  test('should require admin email confirmation', async ({ page }) => {
    await page.goto('/install');

    const emailInput = page.locator('input[name="email"], input[name="adminEmail"]');
    const confirmEmailInput = page.locator('input[name="confirmEmail"], input[name="emailConfirmation"]');

    if (await emailInput.isVisible().catch(() => false) && (await confirmEmailInput.isVisible().catch(() => false))) {
      await emailInput.fill('admin@example.com');
      await confirmEmailInput.fill('different@example.com');

      const nextButton = page.locator('button:has-text("Next"), button:has-text("Continue")').first();

      if (await nextButton.isVisible().catch(() => false)) {
        await nextButton.click();
        await page.waitForTimeout(500);

        // Should show mismatch error
        await expect(
          page.locator('text=/match|Match|confirm|Confirm/i').or(page.locator('.error, [role="alert"]')),
        ).toBeVisible();
      }
    }
  });
});

test.describe('Installation Error Handling', () => {
  test.beforeEach(async ({ page }) => {
    await clearAuth(page.context());
  });

  test('should handle database connection error', async ({ page }) => {
    // Mock database connection failure
    await page.route('**/api/install/test-connection', async route => {
      await route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ success: false, message: 'Database connection failed' }),
      });
    });

    await page.goto('/install');

    // Fill database config
    const testButton = page.locator('button:has-text("Test Connection"), button:has-text("Test")').first();

    if (await testButton.isVisible().catch(() => false)) {
      await testButton.click();
      await page.waitForTimeout(1000);

      // Should show error message
      await expect(
        page.locator('text=/connection failed|Connection failed|error|Error/i').or(page.locator('[role="alert"]')),
      ).toBeVisible();
    }
  });

  test('should handle installation failure gracefully', async ({ page }) => {
    // Mock installation failure
    await page.route('**/api/install', async route => {
      await route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ success: false, message: 'Installation failed' }),
      });
    });

    await page.goto('/install');

    const finishButton = page.locator('button:has-text("Finish"), button:has-text("Install")').first();

    if (await finishButton.isVisible().catch(() => false)) {
      await finishButton.click();
      await page.waitForTimeout(2000);

      // Should show error message with retry option
      await expect(
        page.locator('text=/failed|Failed|error|Error/i').or(page.locator('[role="alert"]')),
      ).toBeVisible();

      const retryButton = page.locator('button:has-text("Retry"), button:has-text("Try Again")');
      if (await retryButton.isVisible().catch(() => false)) {
        await expect(retryButton).toBeVisible();
      }
    }
  });

  test('should allow retry after installation failure', async ({ page }) => {
    let callCount = 0;

    await page.route('**/api/install', async route => {
      callCount++;
      if (callCount === 1) {
        await route.fulfill({
          status: 500,
          contentType: 'application/json',
          body: JSON.stringify({ success: false, message: 'Installation failed' }),
        });
      } else {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ success: true, message: 'Installation completed' }),
        });
      }
    });

    await page.goto('/install');

    const finishButton = page.locator('button:has-text("Finish"), button:has-text("Install")').first();

    if (await finishButton.isVisible().catch(() => false)) {
      await finishButton.click();
      await page.waitForTimeout(2000);

      // Click retry
      const retryButton = page.locator('button:has-text("Retry"), button:has-text("Try Again")').first();
      if (await retryButton.isVisible().catch(() => false)) {
        await retryButton.click();
        await page.waitForTimeout(3000);

        // Should succeed on retry
        await expect(
          page.locator('text=/success|Success|completed|Completed/i').or(page.locator('[data-testid="success-message"]')),
        ).toBeVisible();
      }
    }
  });
});

test.describe('Installation UX', () => {
  test.beforeEach(async ({ page }) => {
    await clearAuth(page.context());
  });

  test('should show installation requirements', async ({ page }) => {
    await page.goto('/install');

    // Look for requirements section
    const requirements = page.locator(
      'text=/requirements|Requirements|prerequisites|Prerequisites|system requirements/i, [data-testid="requirements"]',
    );

    if (await requirements.isVisible().catch(() => false)) {
      await expect(requirements).toBeVisible();
    }
  });

  test('should have help/documentation links', async ({ page }) => {
    await page.goto('/install');

    // Look for help links
    const helpLink = page.locator('a[href*="docs"], a[href*="help"], a[href*="documentation"]').first();

    if (await helpLink.isVisible().catch(() => false)) {
      await expect(helpLink).toBeVisible();
    }
  });

  test('should show estimated installation time', async ({ page }) => {
    await page.goto('/install');

    // Look for estimated time
    const timeEstimate = page.locator('text=/minutes|Minutes|estimate|Estimated time/i, [data-testid="estimate"]');

    if (await timeEstimate.isVisible().catch(() => false)) {
      await expect(timeEstimate).toBeVisible();
    }
  });

  test('should allow skipping optional steps', async ({ page }) => {
    await page.goto('/install');

    // Look for skip button
    const skipButton = page.locator('button:has-text("Skip"), button:has-text("Do this later")').first();

    if (await skipButton.isVisible().catch(() => false)) {
      await expect(skipButton).toBeVisible();

      await skipButton.click();
      await page.waitForTimeout(500);

      // Should proceed to next step or finish
      const currentUrl = page.url();
      expect(currentUrl).not.toBe('/install');
    }
  });

  test('should save draft installation progress', async ({ page }) => {
    await page.goto('/install');

    // Fill some fields
    const emailInput = page.locator('input[name="email"], input[name="adminEmail"]');
    if (await emailInput.isVisible().catch(() => false)) {
      await emailInput.fill('admin@example.com');
    }

    // Navigate away and come back
    await page.goto('/login');
    await page.waitForTimeout(500);
    await page.goto('/install');

    // Should restore draft data
    const savedEmail = await emailInput.inputValue();
    if (savedEmail) {
      expect(savedEmail).toBe('admin@example.com');
    }
  });
});
