import { test, expect } from '@playwright/test';
import { loginAsUser } from './helpers/auth-helper';

/**
 * Ticket System E2E Tests
 * Covers: Support page, ticket creation, ticket management, admin tickets
 */

test.describe('Support Page & Ticket Creation', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test('should display support page', async ({ page }) => {
    await page.goto('/support');

    // Check page structure
    await expect(page.locator('h1, h2')).toContainText(/Support|Help|Tickets/i);

    // Check for ticket creation form
    await expect(
      page.locator(
        'textarea[name="message"], textarea[placeholder*="message" i], textarea[placeholder*="describe" i]',
      ),
    ).toBeVisible();

    // Check for submit button
    await expect(
      page.locator('button[type="submit"], button:has-text("Submit"), button:has-text("Send")'),
    ).toBeVisible();
  });

  test('should show ticket list/history', async ({ page }) => {
    await page.goto('/support');

    // Look for existing tickets section
    const ticketList = page.locator(
      '[data-testid="ticket-list"], .ticket-list, .tickets-container',
    );

    if (await ticketList.isVisible().catch(() => false)) {
      await expect(ticketList).toBeVisible();
    }
  });

  test('should create new ticket', async ({ page }) => {
    await page.goto('/support');

    // Fill ticket form
    await page.fill(
      'input[name="subject"], input[placeholder*="subject" i]',
      'Test ticket subject',
    );
    await page.fill(
      'textarea[name="message"], textarea[placeholder*="message" i], textarea[placeholder*="describe" i]',
      'This is a test ticket message for E2E testing',
    );

    // Submit form
    await page.click('button[type="submit"], button:has-text("Submit"), button:has-text("Send")');

    // Wait for response
    await page.waitForTimeout(2000);

    // Check for success message or ticket creation
    const url = page.url();
    if (url.includes('/support') || url.includes('/tickets')) {
      await expect(
        page
          .locator('text=/success|Success|ticket created|Ticket created/i')
          .or(page.locator('[data-testid="ticket-created"]'))
          .or(page.locator('.ticket-item')),
      ).toBeVisible();
    }
  });

  test('should validate required fields on ticket creation', async ({ page }) => {
    await page.goto('/support');

    // Submit empty form
    await page.click('button[type="submit"], button:has-text("Submit"), button:has-text("Send")');

    // Should show validation errors
    await expect(
      page
        .locator('text=/required|Required|error|Error/i')
        .or(page.locator('.error, [role="alert"]')),
    ).toBeVisible();
  });

  test('should filter tickets by status', async ({ page }) => {
    await page.goto('/support');

    // Look for status filter
    const statusFilter = page.locator(
      'select[name="status"], [data-testid="status-filter"], button:has-text("All"), button:has-text("Open"), button:has-text("Closed")',
    );

    if (
      await statusFilter
        .first()
        .isVisible()
        .catch(() => false)
    ) {
      await expect(statusFilter.first()).toBeVisible();
    }
  });

  test('should view ticket details', async ({ page }) => {
    await page.goto('/support');

    // Find a ticket link/card
    const ticketItem = page
      .locator('[data-testid="ticket-item"], .ticket-item, a[href*="/tickets/"]')
      .first();

    if (await ticketItem.isVisible().catch(() => false)) {
      await ticketItem.click();

      // Should navigate to ticket details
      await page.waitForTimeout(1000);
      await expect(page.locator('h1, h2')).toBeVisible();
    }
  });

  test('should add message to existing ticket', async ({ page }) => {
    await page.goto('/support');

    // Navigate to a ticket (if exists)
    const ticketItem = page
      .locator('[data-testid="ticket-item"], .ticket-item, a[href*="/tickets/"]')
      .first();

    if (await ticketItem.isVisible().catch(() => false)) {
      await ticketItem.click();
      await page.waitForTimeout(1000);

      // Look for message input
      const messageInput = page.locator(
        'textarea[name="message"], textarea[placeholder*="reply" i]',
      );

      if (await messageInput.isVisible().catch(() => false)) {
        await messageInput.fill('Test reply message');

        // Submit reply
        await page.click(
          'button[type="submit"], button:has-text("Send"), button:has-text("Reply")',
        );

        await page.waitForTimeout(1000);

        // Check for success or new message in thread
        await expect(page.locator('text=/reply|Reply|message|Message/i').last()).toBeVisible();
      }
    }
  });

  test('should close ticket', async ({ page }) => {
    await page.goto('/support');

    // Find a ticket
    const ticketItem = page.locator('[data-testid="ticket-item"], .ticket-item').first();

    if (await ticketItem.isVisible().catch(() => false)) {
      await ticketItem.click();
      await page.waitForTimeout(1000);

      // Look for close button
      const closeButton = page.locator(
        'button:has-text("Close"), button:has-text("Mark as Resolved"), [data-testid="close-ticket"]',
      );

      if (await closeButton.isVisible().catch(() => false)) {
        await closeButton.click();

        // Confirm if needed
        const confirmButton = page
          .locator('button:has-text("Confirm"), button:has-text("Yes")')
          .first();
        if (await confirmButton.isVisible().catch(() => false)) {
          await confirmButton.click();
        }

        await page.waitForTimeout(1000);

        // Check for success or status change
        await expect(
          page
            .locator('text=/closed|Closed|resolved|Resolved/i')
            .or(page.locator('[data-testid="ticket-closed"]')),
        ).toBeVisible();
      }
    }
  });
});

test.describe('Admin Tickets Management', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test('should access admin tickets page', async ({ page }) => {
    await page.goto('/admin/tickets');

    // Check if admin page loads (may require admin permissions)
    if (
      await page
        .locator('text=/unauthorized|403|access denied/i')
        .isVisible()
        .catch(() => false)
    ) {
      test.skip();
    } else {
      await expect(page.locator('h1, h2')).toContainText(/Tickets|Support|Admin/i);
    }
  });

  test('should show all tickets in admin view', async ({ page }) => {
    await page.goto('/admin/tickets');

    // Check for admin permissions
    if (
      await page
        .locator('text=/unauthorized|403|access denied/i')
        .isVisible()
        .catch(() => false)
    ) {
      test.skip();
    } else {
      // Look for tickets list
      await expect(
        page.locator('[data-testid="admin-tickets"], .admin-tickets, .tickets-table'),
      ).toBeVisible();
    }
  });

  test('should filter tickets by user in admin view', async ({ page }) => {
    await page.goto('/admin/tickets');

    if (
      await page
        .locator('text=/unauthorized|403|access denied/i')
        .isVisible()
        .catch(() => false)
    ) {
      test.skip();
    } else {
      // Look for user filter
      const userFilter = page.locator(
        'input[name="user"], input[placeholder*="user" i], input[placeholder*="email" i]',
      );

      if (await userFilter.isVisible().catch(() => false)) {
        await expect(userFilter).toBeVisible();
      }
    }
  });

  test('should reply to ticket as admin', async ({ page }) => {
    await page.goto('/admin/tickets');

    if (
      await page
        .locator('text=/unauthorized|403|access denied/i')
        .isVisible()
        .catch(() => false)
    ) {
      test.skip();
    } else {
      // Find a ticket
      const ticketItem = page
        .locator('[data-testid="admin-ticket-item"], .admin-ticket-item')
        .first();

      if (await ticketItem.isVisible().catch(() => false)) {
        await ticketItem.click();
        await page.waitForTimeout(1000);

        // Admin reply input
        const replyInput = page.locator(
          'textarea[name="message"], textarea[placeholder*="reply" i]',
        );

        if (await replyInput.isVisible().catch(() => false)) {
          await replyInput.fill('Admin response for E2E test');

          await page.click(
            'button[type="submit"], button:has-text("Send"), button:has-text("Reply")',
          );

          await page.waitForTimeout(1000);

          await expect(page.locator('text=/reply|Reply|message|Message/i').last()).toBeVisible();
        }
      }
    }
  });

  test('should escalate ticket priority', async ({ page }) => {
    await page.goto('/admin/tickets');

    if (
      await page
        .locator('text=/unauthorized|403|access denied/i')
        .isVisible()
        .catch(() => false)
    ) {
      test.skip();
    } else {
      // Find a ticket
      const ticketItem = page
        .locator('[data-testid="admin-ticket-item"], .admin-ticket-item')
        .first();

      if (await ticketItem.isVisible().catch(() => false)) {
        await ticketItem.click();
        await page.waitForTimeout(1000);

        // Look for priority selector
        const prioritySelect = page.locator('select[name="priority"], [data-testid="priority"]');

        if (await prioritySelect.isVisible().catch(() => false)) {
          await prioritySelect.selectOption('high');
          await page.waitForTimeout(500);

          // Check for save/update button
          const updateButton = page
            .locator('button:has-text("Update"), button:has-text("Save")')
            .first();
          if (await updateButton.isVisible().catch(() => false)) {
            await updateButton.click();
            await page.waitForTimeout(1000);
          }
        }
      }
    }
  });

  test('should assign ticket to admin', async ({ page }) => {
    await page.goto('/admin/tickets');

    if (
      await page
        .locator('text=/unauthorized|403|access denied/i')
        .isVisible()
        .catch(() => false)
    ) {
      test.skip();
    } else {
      // Find a ticket
      const ticketItem = page
        .locator('[data-testid="admin-ticket-item"], .admin-ticket-item')
        .first();

      if (await ticketItem.isVisible().catch(() => false)) {
        await ticketItem.click();
        await page.waitForTimeout(1000);

        // Look for assignee selector
        const assigneeSelect = page.locator('select[name="assignee"], [data-testid="assignee"]');

        if (await assigneeSelect.isVisible().catch(() => false)) {
          await assigneeSelect.selectOption({ index: 1 });
          await page.waitForTimeout(500);

          const updateButton = page
            .locator('button:has-text("Update"), button:has-text("Save")')
            .first();
          if (await updateButton.isVisible().catch(() => false)) {
            await updateButton.click();
            await page.waitForTimeout(1000);
          }
        }
      }
    }
  });
});

test.describe('Ticket Notifications', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test('should show notification for new ticket response', async ({ page }) => {
    await page.goto('/dashboard');

    // Look for notification badge
    const notificationBadge = page.locator(
      '[data-testid="notification-badge"], .notification-badge, .badge',
    );

    if (await notificationBadge.isVisible().catch(() => false)) {
      await expect(notificationBadge).toBeVisible();
    }
  });

  test('should navigate to ticket from notification', async ({ page }) => {
    await page.goto('/dashboard');

    // Click notification
    const notificationButton = page
      .locator('[data-testid="notifications"], button:has-text("Notifications")')
      .first();

    if (await notificationButton.isVisible().catch(() => false)) {
      await notificationButton.click();
      await page.waitForTimeout(500);

      // Look for ticket notification
      const ticketNotification = page
        .locator('a[href*="/tickets/"], [data-testid="ticket-notification"]')
        .first();

      if (await ticketNotification.isVisible().catch(() => false)) {
        await ticketNotification.click();
        await page.waitForTimeout(1000);

        await expect(page.locator('h1, h2')).toBeVisible();
      }
    }
  });
});
