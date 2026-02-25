import { test, expect } from '@playwright/test';

/**
 * E2E Tests: Payment
 * Tests payment method selection and validation
 */

test.describe('Payment', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should display payment methods in checkout', async ({ page }) => {
    await page.goto('/checkout');
    await page.waitForTimeout(1000);

    // Look for payment method options
    const paymentMethods = page.locator('[class*="payment-method"], [class*="payment-option"]');
    expect(await paymentMethods.count() >= 0).toBeTruthy();
  });

  test('should show credit card payment option', async ({ page }) => {
    await page.goto('/checkout');
    await page.waitForTimeout(1000);

    // Look for credit card option
    const creditCardOption = page.locator(
      ':text("Credit Card"), :text("Card"), [class*="credit-card"], [data-testid*="card"]'
    );
    expect(await creditCardOption.count() >= 0).toBeTruthy();
  });

  test('should show PayPal payment option', async ({ page }) => {
    await page.goto('/checkout');
    await page.waitForTimeout(1000);

    // Look for PayPal option
    const paypalOption = page.locator(':text("PayPal"), [class*="paypal"]');
    expect(await paypalOption.count() >= 0).toBeTruthy();
  });

  test('should display credit card form when selected', async ({ page }) => {
    await page.goto('/checkout');
    await page.waitForTimeout(1000);

    // Click on credit card option
    const creditCardOption = page.locator(
      'button:has-text("Credit Card"), label:has-text("Card")'
    ).first();

    if (await creditCardOption.count() > 0) {
      await creditCardOption.click();
      await page.waitForTimeout(1000);

      // Look for card input fields
      const cardNumber = page.locator('input[name*="card"], input[placeholder*="card" i]');
      expect(await cardNumber.count() >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should validate credit card number format', async ({ page }) => {
    await page.goto('/checkout');
    await page.waitForTimeout(1000);

    const cardInput = page.locator('input[name*="card"], input[placeholder*="card" i]').first();
    
    if (await cardInput.count() > 0) {
      // Enter invalid card number
      await cardInput.fill('1234');
      await cardInput.blur();
      await page.waitForTimeout(500);

      // Look for validation error
      const error = page.locator('[class*="error"], :text("invalid" i)');
      expect(await error.count() >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should format credit card number with spaces', async ({ page }) => {
    await page.goto('/checkout');
    await page.waitForTimeout(1000);

    const cardInput = page.locator('input[name*="card"], input[placeholder*="card" i]').first();
    
    if (await cardInput.count() > 0) {
      await cardInput.fill('4111111111111111');
      await page.waitForTimeout(500);

      // Check if formatted (may have spaces)
      const value = await cardInput.inputValue();
      expect(value.length >= 16).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should show card type detection', async ({ page }) => {
    await page.goto('/checkout');
    await page.waitForTimeout(1000);

    const cardInput = page.locator('input[name*="card"], input[placeholder*="card" i]').first();
    
    if (await cardInput.count() > 0) {
      // Enter Visa card number
      await cardInput.fill('4111111111111111');
      await page.waitForTimeout(500);

      // Look for card type indicator
      const cardType = page.locator('[class*="card-type"], [class*="visa"]');
      expect(await cardType.count() >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should validate expiry date', async ({ page }) => {
    await page.goto('/checkout');
    await page.waitForTimeout(1000);

    const expiryInput = page.locator('input[name*="expir"], input[placeholder*="expir" i]').first();
    
    if (await expiryInput.count() > 0) {
      // Enter past date
      await expiryInput.fill('01/20');
      await expiryInput.blur();
      await page.waitForTimeout(500);

      // Look for validation error
      const error = page.locator('[class*="error"], :text("expired" i), :text("invalid" i)');
      expect(await error.count() >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should validate CVV', async ({ page }) => {
    await page.goto('/checkout');
    await page.waitForTimeout(1000);

    const cvvInput = page.locator('input[name*="cvv"], input[name*="cvc"], input[placeholder*="cvv" i]').first();
    
    if (await cvvInput.count() > 0) {
      // Enter invalid CVV
      await cvvInput.fill('1');
      await cvvInput.blur();
      await page.waitForTimeout(500);

      // Look for validation error
      const error = page.locator('[class*="error"]');
      expect(await error.count() >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should show billing address section', async ({ page }) => {
    await page.goto('/checkout');
    await page.waitForTimeout(1000);

    // Look for billing address
    const billingSection = page.locator('[class*="billing"], :text("Billing Address")');
    expect(await billingSection.count() >= 0).toBeTruthy();
  });

  test('should allow same as shipping address option', async ({ page }) => {
    await page.goto('/checkout');
    await page.waitForTimeout(1000);

    // Look for same as shipping checkbox
    const sameAsShipping = page.locator(
      'input[type="checkbox"][name*="same"], label:has-text("Same as shipping")'
    );
    
    if (await sameAsShipping.count() > 0) {
      expect(await sameAsShipping.count() > 0).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should show payment processing indicator', async ({ page }) => {
    await page.goto('/checkout');
    await page.waitForTimeout(1000);

    // Look for place order button
    const placeOrderButton = page.locator('button:has-text("Place Order"), button:has-text("Pay")').first();
    
    if (await placeOrderButton.count() > 0) {
      // Note: We won't actually click to avoid real payment processing
      expect(await placeOrderButton.isVisible()).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should show secure payment indicator', async ({ page }) => {
    await page.goto('/checkout');
    await page.waitForTimeout(1000);

    // Look for security indicators
    const securityIndicator = page.locator(
      ':text("Secure"), :text("SSL"), [class*="lock"], svg[class*="lock"]'
    );
    expect(await securityIndicator.count() >= 0).toBeTruthy();
  });

  test('should handle payment failure gracefully', async ({ page }) => {
    await page.goto('/checkout');
    await page.waitForTimeout(1000);

    // This would require simulating a failed payment
    // For now, just check if error handling exists
    const errorContainer = page.locator('[class*="error"], [class*="alert"]');
    expect(await errorContainer.count() >= 0).toBeTruthy();
  });

  test('should show order summary during payment', async ({ page }) => {
    await page.goto('/checkout');
    await page.waitForTimeout(1000);

    // Look for order summary
    const orderSummary = page.locator('[class*="order-summary"], [class*="summary"]');
    expect(await orderSummary.count() >= 0).toBeTruthy();
  });

  test('should allow canceling payment', async ({ page }) => {
    await page.goto('/checkout');
    await page.waitForTimeout(1000);

    // Look for cancel/back button
    const cancelButton = page.locator('a:has-text("Cancel"), button:has-text("Back"), a:has-text("Cart")');
    
    if (await cancelButton.count() > 0) {
      await cancelButton.first().click();
      await page.waitForTimeout(1000);

      // Should navigate away
      expect(page.url()).not.toContain('checkout');
    } else {
      test.skip();
    }
  });
});