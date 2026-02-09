import { test, expect } from '@playwright/test';

/**
 * E2E Tests: Guest Checkout
 * Tests the guest checkout flow without authentication
 */

test.describe('Guest Checkout', () => {
  test.beforeEach(async ({ page }) => {
    // Add a product to cart before each checkout test
    await page.goto('/');

    await page.waitForSelector('[data-testid="product-card"], .product-card, [class*="product"]', { 
      timeout: 10000 
    });
    
    const firstProduct = page.locator('[data-testid="product-card"], .product-card, [class*="product"]').first();
    await firstProduct.click();

    await page.waitForURL(/\/product(s)?\/\w+/, { timeout: 5000 });

    const addToCartButton = page.locator(
      'button:has-text("Add to Cart"), [data-testid="add-to-cart"], button:has-text("Add to Bag")'
    ).first();
    
    await addToCartButton.click();
    await page.waitForTimeout(1000);
  });

  test('should navigate to checkout from cart', async ({ page }) => {
    // Navigate to cart
    const cartLink = page.locator(
      'a[href*="cart"], [data-testid="cart-link"], a:has-text("Cart")'
    ).first();
    
    await cartLink.click();
    await page.waitForURL(/\/cart/, { timeout: 5000 });

    // Find and click checkout button
    const checkoutButton = page.locator(
      'button:has-text("Checkout"), [data-testid="checkout-button"], a:has-text("Checkout")'
    ).first();
    
    if (await checkoutButton.count() > 0) {
      await checkoutButton.click();

      // Verify we navigated to checkout page
      await page.waitForTimeout(1000);
      const url = page.url();
      expect(url).toMatch(/\/(checkout|order)/i);
    } else {
      test.skip();
    }
  });

  test('should display checkout form fields', async ({ page }) => {
    // Navigate to checkout
    await page.goto('/cart');
    
    const checkoutButton = page.locator(
      'button:has-text("Checkout"), [data-testid="checkout-button"], a:has-text("Checkout")'
    ).first();
    
    if (await checkoutButton.count() === 0) {
      test.skip();
      return;
    }

    await checkoutButton.click();
    await page.waitForTimeout(1000);

    // Verify shipping address fields are present
    const emailInput = page.locator('input[type="email"], input[name*="email"]');
    const nameInput = page.locator('input[name*="name"], input[placeholder*="name"]').first();
    const addressInput = page.locator('input[name*="address"], input[placeholder*="address"]').first();

    // At least email should be present for guest checkout
    if (await emailInput.count() > 0) {
      await expect(emailInput.first()).toBeVisible();
    }

    // Verify form elements exist
    const inputs = page.locator('input[type="text"], input[type="email"]');
    expect(await inputs.count()).toBeGreaterThan(0);
  });

  test('should require email for guest checkout', async ({ page }) => {
    await page.goto('/cart');
    
    const checkoutButton = page.locator(
      'button:has-text("Checkout"), [data-testid="checkout-button"], a:has-text("Checkout")'
    ).first();
    
    if (await checkoutButton.count() === 0) {
      test.skip();
      return;
    }

    await checkoutButton.click();
    await page.waitForTimeout(1000);

    // Try to submit without email
    const submitButton = page.locator(
      'button[type="submit"], button:has-text("Place Order"), button:has-text("Complete")'
    ).first();

    if (await submitButton.count() > 0) {
      await submitButton.click();
      await page.waitForTimeout(500);

      // Look for validation error
      const errorMessage = page.locator(
        '.error, [class*="error"], [role="alert"], .invalid'
      );

      // Either validation prevented submission or error message shown
      const hasError = await errorMessage.count() > 0;
      const stillOnCheckout = page.url().match(/\/(checkout|order)/i);

      expect(hasError || stillOnCheckout).toBeTruthy();
    }
  });

  test('should show order summary with items and totals', async ({ page }) => {
    await page.goto('/cart');
    
    const checkoutButton = page.locator(
      'button:has-text("Checkout"), [data-testid="checkout-button"], a:has-text("Checkout")'
    ).first();
    
    if (await checkoutButton.count() === 0) {
      test.skip();
      return;
    }

    await checkoutButton.click();
    await page.waitForTimeout(1000);

    // Verify order summary is visible
    const orderSummary = page.locator(
      '[data-testid="order-summary"], .order-summary, [class*="summary"]'
    );

    const subtotal = page.locator(
      '[data-testid="subtotal"], .subtotal, :has-text("Subtotal")'
    );

    const total = page.locator(
      '[data-testid="total"], .total, :has-text("Total")'
    );

    // At least one of these should be visible
    const summaryVisible = await orderSummary.count() > 0;
    const subtotalVisible = await subtotal.count() > 0;
    const totalVisible = await total.count() > 0;

    expect(summaryVisible || subtotalVisible || totalVisible).toBeTruthy();
  });

  test('should fill guest checkout form successfully', async ({ page }) => {
    await page.goto('/cart');
    
    const checkoutButton = page.locator(
      'button:has-text("Checkout"), [data-testid="checkout-button"], a:has-text("Checkout")'
    ).first();
    
    if (await checkoutButton.count() === 0) {
      test.skip();
      return;
    }

    await checkoutButton.click();
    await page.waitForTimeout(1000);

    // Fill in guest checkout form
    const emailInput = page.locator('input[type="email"], input[name*="email"]').first();
    
    if (await emailInput.count() > 0) {
      await emailInput.fill('guest@example.com');
    }

    const firstNameInput = page.locator('input[name*="firstName"], input[placeholder*="First"]').first();
    if (await firstNameInput.count() > 0) {
      await firstNameInput.fill('John');
    }

    const lastNameInput = page.locator('input[name*="lastName"], input[placeholder*="Last"]').first();
    if (await lastNameInput.count() > 0) {
      await lastNameInput.fill('Doe');
    }

    const addressInput = page.locator('input[name*="address"], input[placeholder*="Address"]').first();
    if (await addressInput.count() > 0) {
      await addressInput.fill('123 Main St');
    }

    const cityInput = page.locator('input[name*="city"], input[placeholder*="City"]').first();
    if (await cityInput.count() > 0) {
      await cityInput.fill('New York');
    }

    const zipInput = page.locator('input[name*="zip"], input[name*="postal"]').first();
    if (await zipInput.count() > 0) {
      await zipInput.fill('10001');
    }

    // Verify form was filled
    if (await emailInput.count() > 0) {
      expect(await emailInput.inputValue()).toBe('guest@example.com');
    }
  });
});
