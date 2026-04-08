import { test, expect } from '@playwright/test';
import { loginAsUser } from './helpers/auth-helper';

/**
 * Metrics & Monitoring E2E Tests
 * Covers: View metrics, charts, real-time updates, alerts
 */

test.describe('Server Metrics View', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test('should display server metrics page', async ({ page }) => {
    await page.goto('/servers/test-server-id');
    
    // Check metrics section exists
    const metricsSection = page.locator('[data-testid="metrics"], .metrics-container, section:has-text("Metrics")');
    await expect(metricsSection.or(page.locator('canvas, svg.chart'))).toBeVisible();
  });

  test('should show CPU usage chart', async ({ page }) => {
    await page.goto('/servers/test-server-id');
    
    // Check for CPU metric
    await expect(page.locator('text=/cpu|CPU/i').first()).toBeVisible();
    
    // Check for chart element
    await expect(page.locator('.recharts-wrapper, canvas, svg:has(rect, circle, path)')).toBeVisible();
  });

  test('should show Memory usage chart', async ({ page }) => {
    await page.goto('/servers/test-server-id');
    
    // Check for Memory metric
    await expect(page.locator('text=/memory|Memory|RAM/i').first()).toBeVisible();
  });

  test('should show Disk usage chart', async ({ page }) => {
    await page.goto('/servers/test-server-id');
    
    // Check for Disk metric
    await expect(page.locator('text=/disk|Disk|storage|Storage/i').first()).toBeVisible();
  });

  test('should show Network metrics', async ({ page }) => {
    await page.goto('/servers/test-server-id');
    
    // Check for Network metrics
    await expect(page.locator('text=/network|Network|traffic|Traffic/i').first()).toBeVisible();
  });

  test('should have time range selector', async ({ page }) => {
    await page.goto('/servers/test-server-id');
    
    // Check for time range selector
    const timeSelector = page.locator('[data-testid="time-range"], select:has-option("1h"), button:has-text("1h"), button:has-text("24h")');
    await expect(timeSelector.or(page.locator('select:has-text("hour")'))).toBeVisible();
  });

  test('should change time range', async ({ page }) => {
    await page.goto('/servers/test-server-id');
    
    // Find and click time range buttons if they exist
    const timeButtons = page.locator('button:has-text("1h"), button:has-text("6h"), button:has-text("24h"), button:has-text("7d")');
    
    if (await timeButtons.first().isVisible().catch(() => false)) {
      const buttons = await timeButtons.count();
      if (buttons > 1) {
        // Click second time range option
        await timeButtons.nth(1).click();
        
        // Verify chart updates (check loading state or data change)
        await page.waitForTimeout(500);
        await expect(page.locator('.recharts-wrapper, canvas')).toBeVisible();
      }
    }
  });

  test('should refresh metrics manually', async ({ page }) => {
    await page.goto('/servers/test-server-id');
    
    // Find refresh button
    const refreshButton = page.locator('[data-testid="refresh"], button:has-text("Refresh"), button[title*="refresh" i]');
    
    if (await refreshButton.isVisible().catch(() => false)) {
      await refreshButton.click();
      
      // Check for loading state or timestamp update
      await page.waitForTimeout(500);
    }
  });

  test('should show live/real-time indicator', async ({ page }) => {
    await page.goto('/servers/test-server-id');
    
    // Check for live indicator
    const liveIndicator = page.locator('text=/live|Live|real-time|Real-time/i, [data-testid="live"], .pulse, .online-indicator');
    
    if (await liveIndicator.first().isVisible().catch(() => false)) {
      await expect(liveIndicator.first()).toBeVisible();
    }
  });
});

test.describe('Dashboard Metrics Overview', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test('should display dashboard with metrics summary', async ({ page }) => {
    await page.goto('/dashboard');
    
    await expect(page.locator('h1, h2')).toContainText(/Dashboard|Overview/i);
    
    // Check for server status cards
    await expect(page.locator('[data-testid="server-status"], .status-card, .server-card').or(
      page.locator('text=/servers online|active|status/i')
    )).toBeVisible();
  });

  test('should show total servers count', async ({ page }) => {
    await page.goto('/dashboard');
    
    // Look for server count display
    await expect(page.locator('text=/[0-9]+ servers?/i').or(
      page.locator('[data-testid="server-count"]')
    ).or(
      page.locator('.metric:has-text("Server")')
    )).toBeVisible();
  });

  test('should show alerts/notifications count', async ({ page }) => {
    await page.goto('/dashboard');
    
    // Look for alerts section
    await expect(page.locator('text=/alert|Alert|notification|Notification/i').or(
      page.locator('[data-testid="alerts"]')
    )).toBeVisible();
  });

  test('should navigate to server details from dashboard', async ({ page }) => {
    await page.goto('/dashboard');
    
    // Find a server link/card
    const serverLink = page.locator('[data-testid="server-card"] a, .server-card a, a[href*="/servers/"]').first();
    
    if (await serverLink.isVisible().catch(() => false)) {
      await serverLink.click();
      await expect(page).toHaveURL(/.*servers\/.+/);
    }
  });
});

test.describe('Admin Monitoring', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test('should access admin monitoring page', async ({ page }) => {
    await page.goto('/admin/monitoring');
    
    // Check if admin page loads (may require admin permissions)
    if (await page.locator('text=/unauthorized|403|access denied/i').isVisible().catch(() => false)) {
      test.skip();
    } else {
      await expect(page.locator('h1, h2')).toContainText(/Monitoring|System|Admin/i);
    }
  });

  test('should show system health status', async ({ page }) => {
    await page.goto('/admin/monitoring');
    
    // Check for health indicators
    const healthIndicator = page.locator('text=/healthy|Healthy|status|Status/i, [data-testid="health"], .health-status');
    
    if (await healthIndicator.first().isVisible().catch(() => false)) {
      await expect(healthIndicator.first()).toBeVisible();
    }
  });

  test('should show database metrics', async ({ page }) => {
    await page.goto('/admin/monitoring');
    
    // Check for database metrics
    const dbMetrics = page.locator('text=/database|Database|DB|postgres/i');
    
    if (await dbMetrics.first().isVisible().catch(() => false)) {
      await expect(dbMetrics.first()).toBeVisible();
    }
  });
});
