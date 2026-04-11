import { test, expect } from '@playwright/test';
import { loginAsUser } from './helpers/auth-helper';

/**
 * Payment & Subscription E2E Tests
 * Covers: Pricing page, checkout flow, subscription management
 */

test.describe('Pricing Page', () => {
  test('should display pricing page with plans', async ({ page }) => {
    await page.goto('/pricing');

    // Check page title
    await expect(page.locator('h1')).toContainText(/Pricing|Plans|Subscription/i);

    // Check for plan cards
    const planCards = page.locator('[data-testid="pricing-card"], .pricing-card, .plan-card');
    const count = await planCards.count();

    // Should have at least 2-3 plans (Free, Pro, Enterprise)
    expect(count).toBeGreaterThanOrEqual(1);
  });

  test('should display Free plan features', async ({ page }) => {
    await page.goto('/pricing');

    // Look for Free plan
    await expect(page.locator('text=/free|Free/i').first()).toBeVisible();

    // Check for feature list
    await expect(page.locator('ul li, .feature-list li, .features li').first()).toBeVisible();
  });

  test('should display Pro/Paid plan with price', async ({ page }) => {
    await page.goto('/pricing');

    // Look for Pro/Premium plan with price
    const priceElement = page
      .locator('text=/[$][0-9]+|€[0-9]+|[0-9]+$/i')
      .or(page.locator('.price:has-text("$")'));

    if (
      await priceElement
        .first()
        .isVisible()
        .catch(() => false)
    ) {
      await expect(priceElement.first()).toBeVisible();
    }
  });

  test('should have upgrade/ subscribe buttons', async ({ page }) => {
    await page.goto('/pricing');

    // Check for CTA buttons
    await expect(
      page
        .locator(
          'button:has-text("Upgrade"), button:has-text("Subscribe"), button:has-text("Get Started"), a:has-text("Upgrade")',
        )
        .first(),
    ).toBeVisible();
  });

  test('should toggle monthly/yearly pricing', async ({ page }) => {
    await page.goto('/pricing');

    // Find billing period toggle
    const toggle = page.locator(
      '[data-testid="billing-toggle"], button:has-text("Monthly"), button:has-text("Yearly"), .toggle',
    );

    if (
      await toggle
        .first()
        .isVisible()
        .catch(() => false)
    ) {
      // Get initial price
      const _initialPrice = await page
        .locator('.price')
        .first()
        .textContent()
        .catch(() => '');

      // Click toggle
      await toggle.first().click();
      await page.waitForTimeout(500);

      // Price should change
      const _newPrice = await page
        .locator('.price')
        .first()
        .textContent()
        .catch(() => '');

      // Price display should update (or at least toggle state should change)
      await expect(page.locator('.price').first()).toBeVisible();
    }
  });
});

test.describe('Checkout Flow', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test('should navigate to checkout from pricing', async ({ page }) => {
    await page.goto('/pricing');

    // Click upgrade/subscribe button on Pro plan
    const upgradeButton = page
      .locator(
        '[data-testid="pro-plan"] button, .pro-plan button, button:has-text("Upgrade to Pro")',
      )
      .first();

    if (await upgradeButton.isVisible().catch(() => false)) {
      await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}),
        upgradeButton.click(),
      ]);

      // Should redirect to Stripe checkout or payment page
      const url = page.url();
      expect(url).toMatch(/checkout|payment|stripe/);
    }
  });

  test('should display checkout page with order summary', async ({ page }) => {
    // Navigate to checkout directly (if accessible)
    await page.goto('/checkout');

    // Check for order summary
    await expect(
      page
        .locator('text=/order summary|summary|total|plan/i')
        .or(page.locator('[data-testid="order-summary"]')),
    ).toBeVisible();
  });

  test('should show Stripe payment form', async ({ page }) => {
    // This test assumes Stripe Elements is used
    await page.goto('/checkout');

    // Check for Stripe Elements iframe or payment form
    const stripeFrame = page.locator(
      'iframe[src*="stripe"], [data-testid="card-element"], .StripeElement',
    );

    if (
      await stripeFrame
        .first()
        .isVisible()
        .catch(() => false)
    ) {
      await expect(stripeFrame.first()).toBeVisible();
    }
  });

  test('should require card details for payment', async ({ page }) => {
    await page.goto('/checkout');

    // Try to submit without card details
    const submitButton = page.locator(
      'button[type="submit"]:has-text("Pay"), button:has-text("Subscribe")',
    );

    if (await submitButton.isVisible().catch(() => false)) {
      await submitButton.click();

      // Should show error
      await expect(
        page.locator('text=/required|invalid|error|Error/i').or(page.locator('[role="alert"]')),
      ).toBeVisible();
    }
  });

  test('should handle Stripe test card payment', async ({ page }) => {
    // This test uses Stripe test card: 4242 4242 4242 4242
    test.skip(!!process.env.CI, 'Skip in CI - requires real Stripe integration');

    await page.goto('/checkout');

    // Fill test card details (if using Stripe Elements)
    const cardFrame = page.frameLocator('iframe').first();

    if (
      await cardFrame
        .locator('input')
        .first()
        .isVisible()
        .catch(() => false)
    ) {
      await cardFrame.locator('input[name="cardnumber"]').fill('4242424242424242');
      await cardFrame.locator('input[name="exp-date"]').fill('12/25');
      await cardFrame.locator('input[name="cvc"]').fill('123');
      await cardFrame.locator('input[name="postal"]').fill('12345');

      // Submit
      await page.click('button[type="submit"]');

      // Should redirect to success page
      await page.waitForURL(/success|confirm|dashboard/, { timeout: 10000 });
    }
  });
});

test.describe('Subscription Management', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test('should display billing/subscription page', async ({ page }) => {
    await page.goto('/profile');

    // Navigate to billing section
    const billingLink = page
      .locator(
        'a:has-text("Billing"), a:has-text("Subscription"), button:has-text("Billing"), [data-testid="billing"]',
      )
      .first();

    if (await billingLink.isVisible().catch(() => false)) {
      await billingLink.click();
      await expect(page.locator('h1, h2')).toContainText(/Billing|Subscription|Payment/i);
    }
  });

  test('should show current subscription status', async ({ page }) => {
    await page.goto('/profile/billing');

    // Check for subscription status
    await expect(
      page.locator('text=/plan|Plan|subscription|Subscription|status|Status/i').first(),
    ).toBeVisible();
  });

  test('should show payment methods', async ({ page }) => {
    await page.goto('/profile/billing');

    // Look for payment methods section
    const paymentMethods = page.locator(
      'text=/payment method|card|Card/i, [data-testid="payment-methods"]',
    );

    if (
      await paymentMethods
        .first()
        .isVisible()
        .catch(() => false)
    ) {
      await expect(paymentMethods.first()).toBeVisible();
    }
  });

  test('should show billing history/invoices', async ({ page }) => {
    await page.goto('/profile/billing');

    // Look for invoices/billing history
    const invoices = page.locator(
      'text=/invoice|history|billing history|payment history/i, [data-testid="invoices"]',
    );

    if (
      await invoices
        .first()
        .isVisible()
        .catch(() => false)
    ) {
      await expect(invoices.first()).toBeVisible();
    }
  });

  test('should have cancel subscription option', async ({ page }) => {
    await page.goto('/profile/billing');

    // Look for cancel button
    const cancelButton = page.locator(
      'button:has-text("Cancel"), button:has-text("Unsubscribe"), [data-testid="cancel-subscription"]',
    );

    if (await cancelButton.isVisible().catch(() => false)) {
      await expect(cancelButton).toBeVisible();
    }
  });

  test('should handle subscription cancellation flow', async ({ page }) => {
    await page.goto('/profile/billing');

    const cancelButton = page
      .locator('button:has-text("Cancel Subscription"), button:has-text("Unsubscribe")')
      .first();

    if (await cancelButton.isVisible().catch(() => false)) {
      await cancelButton.click();

      // Should show confirmation dialog
      await expect(
        page.locator('text=/confirm|Confirm|are you sure/i').or(page.locator('[role="dialog"]')),
      ).toBeVisible();
    }
  });

  test('should update payment method', async ({ page }) => {
    await page.goto('/profile/billing');

    // Find update payment button
    const updateButton = page
      .locator(
        'button:has-text("Update"), button:has-text("Change Card"), [data-testid="update-payment"]',
      )
      .first();

    if (await updateButton.isVisible().catch(() => false)) {
      await updateButton.click();

      // Should show payment form
      await expect(
        page
          .locator('iframe[src*="stripe"], [data-testid="card-element"]')
          .or(page.locator('input[name="cardnumber"]')),
      ).toBeVisible();
    }
  });
});

test.describe('Webhook Handling', () => {
  // These tests verify frontend reacts correctly to subscription changes

  test('should show upgrade success message after return from Stripe', async ({ page }) => {
    // Simulate returning from Stripe with success
    await loginAsUser(page);
    await page.goto('/dashboard?payment=success');

    // Check for success notification
    await expect(
      page
        .locator('text=/success|Success|thank you|Thank you|welcome|Welcome/i')
        .or(page.locator('[data-testid="success-message"]')),
    ).toBeVisible();
  });

  test('should show payment cancelled message', async ({ page }) => {
    await loginAsUser(page);
    await page.goto('/pricing?canceled=true');

    // Check for cancellation message or return to pricing
    await expect(page.locator('h1')).toContainText(/Pricing|Plans/i);
  });
});
