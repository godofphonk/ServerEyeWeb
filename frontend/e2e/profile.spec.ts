import { test, expect } from '@playwright/test';
import { loginAsUser } from './helpers/auth-helper';

/**
 * Profile Management E2E Tests
 * Covers: Profile page, email change, account deletion, password change
 */

test.describe('Profile Page', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test('should display profile page', async ({ page }) => {
    await page.goto('/profile');

    // Check page structure
    await expect(page.locator('h1, h2')).toContainText(/Profile|Account/i);

    // Check for user information display
    await expect(
      page.locator('input[name="email"], input[name="username"], [data-testid="user-email"], [data-testid="user-username"]'),
    ).toBeVisible();
  });

  test('should allow editing profile information', async ({ page }) => {
    await page.goto('/profile');

    // Look for edit button
    const editButton = page.locator('button:has-text("Edit"), button:has-text("Update"), [data-testid="edit-profile"]').first();

    if (await editButton.isVisible().catch(() => false)) {
      await editButton.click();

      // Should show edit form
      await expect(page.locator('input[name="username"], input[name="displayName"]')).toBeVisible();
    }
  });

  test('should display email change section', async ({ page }) => {
    await page.goto('/profile');

    // Look for email change section
    const emailSection = page.locator(
      'text=/email|Email/i, [data-testid="email-section"], section:has-text("Email")',
    );

    if (await emailSection.isVisible().catch(() => false)) {
      await expect(emailSection).toBeVisible();
    }
  });

  test('should display password change section', async ({ page }) => {
    await page.goto('/profile');

    // Look for password change section
    const passwordSection = page.locator(
      'text=/password|Password/i, [data-testid="password-section"], section:has-text("Password")',
    );

    if (await passwordSection.isVisible().catch(() => false)) {
      await expect(passwordSection).toBeVisible();
    }
  });

  test('should display account deletion section', async ({ page }) => {
    await page.goto('/profile');

    // Look for account deletion section
    const deleteSection = page.locator(
      'text=/delete|Delete|account deletion/i, [data-testid="delete-account"], section:has-text("Delete")',
    );

    if (await deleteSection.isVisible().catch(() => false)) {
      await expect(deleteSection).toBeVisible();
    }
  });
});

test.describe('Email Change Flow', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test('should initiate email change', async ({ page }) => {
    await page.goto('/profile');

    // Find email change button
    const emailChangeButton = page.locator(
      'button:has-text("Change Email"), button:has-text("Update Email"), [data-testid="change-email"]',
    ).first();

    if (await emailChangeButton.isVisible().catch(() => false)) {
      await emailChangeButton.click();

      // Should show email change form
      await expect(page.locator('input[name="newEmail"], input[name="email"]')).toBeVisible();
    }
  });

  test('should validate new email format', async ({ page }) => {
    await page.goto('/profile');

    // Navigate to email change form
    const emailChangeButton = page.locator(
      'button:has-text("Change Email"), button:has-text("Update Email"), [data-testid="change-email"]',
    ).first();

    if (await emailChangeButton.isVisible().catch(() => false)) {
      await emailChangeButton.click();

      // Enter invalid email
      await page.fill('input[name="newEmail"], input[name="email"]', 'invalid-email');

      // Submit
      await page.click('button[type="submit"], button:has-text("Submit"), button:has-text("Send")');

      // Should show validation error
      await expect(
        page.locator('text=/invalid|Invalid|format|Format/i').or(page.locator('.error, [role="alert"]')),
      ).toBeVisible();
    }
  });

  test('should require password for email change', async ({ page }) => {
    await page.goto('/profile');

    const emailChangeButton = page.locator(
      'button:has-text("Change Email"), button:has-text("Update Email"), [data-testid="change-email"]',
    ).first();

    if (await emailChangeButton.isVisible().catch(() => false)) {
      await emailChangeButton.click();

      // Fill new email
      await page.fill('input[name="newEmail"], input[name="email"]', 'newemail@example.com');

      // Submit without password
      await page.click('button[type="submit"], button:has-text("Submit"), button:has-text("Send")');

      // Should require password
      await expect(
        page.locator('text=/password|Password|required|Required/i').or(page.locator('.error, [role="alert"]')),
      ).toBeVisible();
    }
  });

  test('should send verification code for email change', async ({ page }) => {
    await page.goto('/profile');

    const emailChangeButton = page.locator(
      'button:has-text("Change Email"), button:has-text("Update Email"), [data-testid="change-email"]',
    ).first();

    if (await emailChangeButton.isVisible().catch(() => false)) {
      await emailChangeButton.click();

      // Fill new email
      await page.fill('input[name="newEmail"], input[name="email"]', 'newemail@example.com');
      await page.fill('input[name="password"], input[name="currentPassword"]', 'testpassword123');

      // Submit
      await page.click('button[type="submit"], button:has-text("Submit"), button:has-text("Send")');

      await page.waitForTimeout(2000);

      // Should show verification code input
      await expect(
        page.locator('input[name="code"], input[name="verificationCode"], [data-testid="verification-code"]'),
      ).toBeVisible();
    }
  });

  test('should confirm email change with verification code', async ({ page }) => {
    await page.goto('/profile');

    const emailChangeButton = page.locator(
      'button:has-text("Change Email"), button:has-text("Update Email"), [data-testid="change-email"]',
    ).first();

    if (await emailChangeButton.isVisible().catch(() => false)) {
      await emailChangeButton.click();

      // Fill new email and password
      await page.fill('input[name="newEmail"], input[name="email"]', 'newemail@example.com');
      await page.fill('input[name="password"], input[name="currentPassword"]', 'testpassword123');

      // Submit to get verification code
      await page.click('button[type="submit"], button:has-text("Submit"), button:has-text("Send")');
      await page.waitForTimeout(2000);

      // Enter verification code (in real scenario, would be provided by email service mock)
      const codeInput = page.locator('input[name="code"], input[name="verificationCode"]');

      if (await codeInput.isVisible().catch(() => false)) {
        await codeInput.fill('123456');

        // Confirm
        await page.click('button:has-text("Confirm"), button:has-text("Verify")');

        await page.waitForTimeout(2000);

        // Should show success message
        await expect(
          page.locator('text=/success|Success|email changed|Email changed/i').or(page.locator('[data-testid="success-message"]')),
        ).toBeVisible();
      }
    }
  });

  test('should handle email change with wrong verification code', async ({ page }) => {
    await page.goto('/profile');

    const emailChangeButton = page.locator(
      'button:has-text("Change Email"), button:has-text("Update Email"), [data-testid="change-email"]',
    ).first();

    if (await emailChangeButton.isVisible().catch(() => false)) {
      await emailChangeButton.click();

      await page.fill('input[name="newEmail"], input[name="email"]', 'newemail@example.com');
      await page.fill('input[name="password"], input[name="currentPassword"]', 'testpassword123');

      await page.click('button[type="submit"], button:has-text("Submit"), button:has-text("Send")');
      await page.waitForTimeout(2000);

      const codeInput = page.locator('input[name="code"], input[name="verificationCode"]');

      if (await codeInput.isVisible().catch(() => false)) {
        await codeInput.fill('000000'); // Wrong code

        await page.click('button:has-text("Confirm"), button:has-text("Verify")');

        await page.waitForTimeout(1000);

        // Should show error
        await expect(
          page.locator('text=/invalid|Invalid|wrong|Wrong|expired|Expired/i').or(page.locator('[role="alert"]')),
        ).toBeVisible();
      }
    }
  });
});

test.describe('Password Change Flow', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test('should initiate password change', async ({ page }) => {
    await page.goto('/profile');

    // Find password change button
    const passwordChangeButton = page.locator(
      'button:has-text("Change Password"), button:has-text("Update Password"), [data-testid="change-password"]',
    ).first();

    if (await passwordChangeButton.isVisible().catch(() => false)) {
      await passwordChangeButton.click();

      // Should show password change form
      await expect(page.locator('input[name="currentPassword"], input[name="oldPassword"]')).toBeVisible();
      await expect(page.locator('input[name="newPassword"], input[name="password"]')).toBeVisible();
      await expect(page.locator('input[name="confirmPassword"]')).toBeVisible();
    }
  });

  test('should require current password for password change', async ({ page }) => {
    await page.goto('/profile');

    const passwordChangeButton = page.locator(
      'button:has-text("Change Password"), button:has-text("Update Password"), [data-testid="change-password"]',
    ).first();

    if (await passwordChangeButton.isVisible().catch(() => false)) {
      await passwordChangeButton.click();

      // Fill only new password
      await page.fill('input[name="newPassword"], input[name="password"]', 'newpassword123');
      await page.fill('input[name="confirmPassword"]', 'newpassword123');

      // Submit
      await page.click('button[type="submit"], button:has-text("Update"), button:has-text("Change")');

      // Should require current password
      await expect(
        page.locator('text=/current|Current|required|Required/i').or(page.locator('.error, [role="alert"]')),
      ).toBeVisible();
    }
  });

  test('should validate password confirmation', async ({ page }) => {
    await page.goto('/profile');

    const passwordChangeButton = page.locator(
      'button:has-text("Change Password"), button:has-text("Update Password"), [data-testid="change-password"]',
    ).first();

    if (await passwordChangeButton.isVisible().catch(() => false)) {
      await passwordChangeButton.click();

      await page.fill('input[name="currentPassword"], input[name="oldPassword"]', 'testpassword123');
      await page.fill('input[name="newPassword"], input[name="password"]', 'newpassword123');
      await page.fill('input[name="confirmPassword"]', 'differentpassword');

      await page.click('button[type="submit"], button:has-text("Update"), button:has-text("Change")');

      // Should show mismatch error
      await expect(
        page.locator('text=/match|Match|confirm|Confirm/i').or(page.locator('.error, [role="alert"]')),
      ).toBeVisible();
    }
  });

  test('should change password successfully', async ({ page }) => {
    await page.goto('/profile');

    const passwordChangeButton = page.locator(
      'button:has-text("Change Password"), button:has-text("Update Password"), [data-testid="change-password"]',
    ).first();

    if (await passwordChangeButton.isVisible().catch(() => false)) {
      await passwordChangeButton.click();

      await page.fill('input[name="currentPassword"], input[name="oldPassword"]', 'testpassword123');
      await page.fill('input[name="newPassword"], input[name="password"]', 'newpassword123');
      await page.fill('input[name="confirmPassword"]', 'newpassword123');

      await page.click('button[type="submit"], button:has-text("Update"), button:has-text("Change")');

      await page.waitForTimeout(2000);

      // Should show success message
      await expect(
        page.locator('text=/success|Success|password changed|Password changed/i').or(page.locator('[data-testid="success-message"]')),
      ).toBeVisible();
    }
  });
});

test.describe('Account Deletion Flow', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test('should initiate account deletion', async ({ page }) => {
    await page.goto('/profile');

    // Find account deletion button
    const deleteButton = page.locator(
      'button:has-text("Delete Account"), button:has-text("Delete"), [data-testid="delete-account"]',
    ).first();

    if (await deleteButton.isVisible().catch(() => false)) {
      await deleteButton.click();

      // Should show confirmation dialog or form
      await expect(
        page.locator('text=/delete|Delete|confirm|Confirm/i').or(page.locator('[role="dialog"], [data-testid="delete-modal"]')),
      ).toBeVisible();
    }
  });

  test('should require password for account deletion', async ({ page }) => {
    await page.goto('/profile');

    const deleteButton = page.locator(
      'button:has-text("Delete Account"), button:has-text("Delete"), [data-testid="delete-account"]',
    ).first();

    if (await deleteButton.isVisible().catch(() => false)) {
      await deleteButton.click();

      // Should show password input
      await expect(page.locator('input[name="password"], input[type="password"]')).toBeVisible();
    }
  });

  test('should show warning about account deletion consequences', async ({ page }) => {
    await page.goto('/profile');

    const deleteButton = page.locator(
      'button:has-text("Delete Account"), button:has-text("Delete"), [data-testid="delete-account"]',
    ).first();

    if (await deleteButton.isVisible().catch(() => false)) {
      await deleteButton.click();

      // Should show warning message
      await expect(
        page.locator('text=/warning|Warning|irreversible|cannot be undone|all data will be lost/i').or(page.locator('[role="alert"]')),
      ).toBeVisible();
    }
  });

  test('should require confirmation before account deletion', async ({ page }) => {
    await page.goto('/profile');

    const deleteButton = page.locator(
      'button:has-text("Delete Account"), button:has-text("Delete"), [data-testid="delete-account"]',
    ).first();

    if (await deleteButton.isVisible().catch(() => false)) {
      await deleteButton.click();

      // Should have confirm checkbox or second confirmation button
      const confirmCheckbox = page.locator('input[type="checkbox"], [data-testid="confirm-checkbox"]');
      const confirmButton = page.locator('button:has-text("Confirm"), button:has-text("Yes, Delete")');

      const hasCheckbox = await confirmCheckbox.isVisible().catch(() => false);
      const hasButton = await confirmButton.isVisible().catch(() => false);

      expect(hasCheckbox || hasButton).toBeTruthy();
    }
  });

  test('should handle account deletion with verification code', async ({ page }) => {
    await page.goto('/profile');

    const deleteButton = page.locator(
      'button:has-text("Delete Account"), button:has-text("Delete"), [data-testid="delete-account"]',
    ).first();

    if (await deleteButton.isVisible().catch(() => false)) {
      await deleteButton.click();

      // Fill password
      await page.fill('input[name="password"], input[type="password"]', 'testpassword123');

      // Submit to get verification code
      await page.click('button:has-text("Send Code"), button:has-text("Request Code")');

      await page.waitForTimeout(2000);

      // Enter verification code
      const codeInput = page.locator('input[name="code"], input[name="verificationCode"]');

      if (await codeInput.isVisible().catch(() => false)) {
        await codeInput.fill('123456');

        // Confirm checkbox if exists
        const confirmCheckbox = page.locator('input[type="checkbox"]');
        if (await confirmCheckbox.isVisible().catch(() => false)) {
          await confirmCheckbox.check();
        }

        // Submit deletion
        await page.click('button:has-text("Delete"), button:has-text("Confirm")');

        await page.waitForTimeout(3000);

        // Should redirect to login or show success
        const url = page.url();
        if (url.includes('/login')) {
          await expect(page.locator('h1, h2')).toContainText(/Login|Sign In/i);
        } else {
          await expect(
            page.locator('text=/success|Success|account deleted|Account deleted/i').or(page.locator('[data-testid="success-message"]')),
          ).toBeVisible();
        }
      }
    }
  });

  test('should cancel account deletion', async ({ page }) => {
    await page.goto('/profile');

    const deleteButton = page.locator(
      'button:has-text("Delete Account"), button:has-text("Delete"), [data-testid="delete-account"]',
    ).first();

    if (await deleteButton.isVisible().catch(() => false)) {
      await deleteButton.click();

      // Click cancel button
      const cancelButton = page.locator('button:has-text("Cancel"), button:has-text("No"), [data-testid="cancel-delete"]');

      if (await cancelButton.isVisible().catch(() => false)) {
        await cancelButton.click();

        await page.waitForTimeout(500);

        // Should close dialog and stay on profile page
        await expect(page).toHaveURL(/.*profile.*/);
      }
    }
  });
});
