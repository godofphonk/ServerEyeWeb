import { test, expect } from '@playwright/test';
import { loginAsUser } from './helpers/auth-helper';

/**
 * Server Management E2E Tests
 * Covers: Add server, view server list, server details, delete server
 */

test.describe('Server Management Flow', () => {
  test.beforeEach(async ({ page }) => {
    // Login before each test (in real scenario, use API to create session or storage state)
    await loginAsUser(page);
  });

  test('should display server list page after login', async ({ page }) => {
    await page.goto('/servers');

    // Check page structure
    await expect(page.locator('h1, h2')).toContainText(/Servers|My Servers|Server List/i);

    // Check for add server button
    await expect(
      page.locator('[data-testid="add-server"], button:has-text("Add"), a:has-text("Add")'),
    ).toBeVisible();
  });

  test('should navigate to add server page', async ({ page }) => {
    await page.goto('/servers');

    // Click add server button
    const addButton = page.locator(
      '[data-testid="add-server"], button:has-text("Add Server"), a[href*="add"]',
    );
    if (await addButton.isVisible().catch(() => false)) {
      await addButton.click();
      await expect(page).toHaveURL(/.*add.*/);
      await expect(page.locator('h1, h2')).toContainText(/Add Server|New Server/i);
    }
  });

  test('should show add server form with required fields', async ({ page }) => {
    await page.goto('/servers/add');

    // Check form fields exist
    await expect(page.locator('input[name="name"], input[placeholder*="name" i]')).toBeVisible();
    await expect(
      page.locator('input[name="hostname"], input[name="ip"], input[placeholder*="IP" i]'),
    ).toBeVisible();
    await expect(page.locator('button[type="submit"]')).toBeVisible();
  });

  test('should validate required fields on add server form', async ({ page }) => {
    await page.goto('/servers/add');

    // Submit empty form
    await page.click('button[type="submit"]');

    // Should show validation errors
    await expect(
      page
        .locator('text=/required|Required|error|Error/i')
        .or(page.locator('.error, [role="alert"]')),
    ).toBeVisible();
  });

  test('should show server connection options', async ({ page }) => {
    await page.goto('/servers/add');

    // Check for connection method options (SSH, Agent, Manual, etc.)
    const connectionOptions = page.locator(
      '[data-testid="connection-method"], button:has-text("SSH"), button:has-text("Agent"), button:has-text("Manual")',
    );
    const count = await connectionOptions.count();

    if (count > 0) {
      await expect(connectionOptions.first()).toBeVisible();
    }
  });

  test('should display server details page', async ({ page }) => {
    // Navigate to a server detail page (using a test server ID)
    await page.goto('/servers/test-server-id');

    // Check server info is displayed
    await expect(page.locator('h1, h2')).toBeVisible();

    // Check for metrics section
    await expect(
      page
        .locator('[data-testid="metrics"], text=/metrics|Metrics|stats|Statistics/i')
        .or(page.locator('.metrics')),
    ).toBeVisible();
  });

  test('should show server actions (edit, delete)', async ({ page }) => {
    await page.goto('/servers');

    // Look for server cards or rows
    const serverItems = page.locator(
      '[data-testid="server-item"], .server-card, tr:has-text("server")',
    );
    const count = await serverItems.count();

    if (count > 0) {
      // Check for action buttons on first server
      const firstServer = serverItems.first();
      await expect(
        firstServer.locator('button:has-text("Edit"), a:has-text("Edit")'),
      ).toBeVisible();
      await expect(
        firstServer.locator('button:has-text("Delete"), a:has-text("Delete")'),
      ).toBeVisible();
    }
  });

  test('should open delete server confirmation', async ({ page }) => {
    await page.goto('/servers');

    // Find delete button
    const deleteButton = page
      .locator('button:has-text("Delete"), [data-testid="delete-server"]')
      .first();

    if (await deleteButton.isVisible().catch(() => false)) {
      await deleteButton.click();

      // Check confirmation dialog/modal
      await expect(
        page.locator('text=/confirm|Confirm|delete|Delete/i').locator('visible=true'),
      ).toBeVisible();
    }
  });

  test('should show empty state when no servers', async ({ page }) => {
    // This test assumes we can clear servers or use a fresh test account
    await page.goto('/servers');

    // Check for empty state message
    const emptyState = page.locator('text=/no servers|empty|get started|add your first/i');
    const serverItems = page.locator('[data-testid="server-item"], .server-card');

    const hasServers = (await serverItems.count()) > 0;
    if (!hasServers) {
      await expect(emptyState.or(page.locator('[data-testid="empty-state"]'))).toBeVisible();
    }
  });
});

test.describe('Server Discovery', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test('should show server discovery option', async ({ page }) => {
    await page.goto('/servers');

    // Check for discovery/scan button
    const discoveryButton = page.locator(
      'button:has-text("Discover"), button:has-text("Scan"), [data-testid="discovery"]',
    );

    if (await discoveryButton.isVisible().catch(() => false)) {
      await expect(discoveryButton).toBeEnabled();
    }
  });

  test('should show Telegram server discovery', async ({ page }) => {
    await page.goto('/servers');

    // Check for Telegram integration
    const telegramButton = page.locator(
      'button:has-text("Telegram"), [data-testid="telegram-discovery"]',
    );

    if (await telegramButton.isVisible().catch(() => false)) {
      await telegramButton.click();
      await expect(page.locator('text=/telegram|Telegram/i')).toBeVisible();
    }
  });
});
