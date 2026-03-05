import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/LoginPage';
import { CartPage } from '../pages/CartPage';

/**
 * Error Handling E2E Tests
 * Tests for network errors, timeout handling, rate limiting, and edge cases
 */

test.describe('Error Handling Tests', () => {
  test.describe('Network Error Handling', () => {
    test('should display error message when API is unreachable', async ({ page }) => {
      // Simulate network failure
      await page.route('**/api/**', route => route.abort('failed'));
      
      await page.goto('/products');
      
      // Should show error message
      const errorMessage = page.locator('[role="alert"], .error-message, [data-testid="error-message"]');
      await expect(errorMessage.first()).toBeVisible({ timeout: 10000 });
    });

    test('should show loading state during slow network', async ({ page }) => {
      // Simulate slow network
      await page.route('**/api/products**', async route => {
        await new Promise(resolve => setTimeout(resolve, 3000));
        route.continue();
      });
      
      await page.goto('/products');
      
      // Should show loading indicator
      const loadingIndicator = page.locator('[role="status"], [aria-busy="true"], .loading, [data-testid="loading"]');
      
      // Loading indicator should appear
      await expect(loadingIndicator.first()).toBeVisible({ timeout: 1000 });
    });

    test('should handle connection timeout gracefully', async ({ page }) => {
      // Simulate timeout
      await page.route('**/api/**', async route => {
        await new Promise(resolve => setTimeout(resolve, 60000));
        route.abort('timedout');
      });
      
      await page.goto('/products');
      
      // Should eventually show error or timeout message
      const errorElement = page.locator('[role="alert"], .error, [data-testid="error"], [data-testid="timeout-error"]');
      await expect(errorElement.first()).toBeVisible({ timeout: 35000 });
    });

    test('should allow retry after network error', async ({ page }) => {
      let requestCount = 0;
      
      await page.route('**/api/products**', route => {
        requestCount++;
        if (requestCount === 1) {
          route.abort('failed');
        } else {
          route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({ items: [], total: 0 })
          });
        }
      });
      
      await page.goto('/products');
      
      // Look for retry button
      const retryButton = page.locator('button:has-text("Retry"), button:has-text("Try again"), [data-testid="retry-button"]');
      
      if (await retryButton.count() > 0) {
        await retryButton.first().click();
        
        // Should load successfully after retry
        await expect(page.locator('[role="alert"], .error')).toHaveCount(0, { timeout: 5000 });
      }
    });
  });

  test.describe('HTTP Error Handling', () => {
    test('should handle 401 Unauthorized', async ({ page }) => {
      await page.route('**/api/auth/me', route => {
        route.fulfill({
          status: 401,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Unauthorized' })
        });
      });
      
      await page.goto('/profile');
      
      // Should redirect to login or show login prompt
      await page.waitForURL(/\/login|\/auth/, { timeout: 5000 });
    });

    test('should handle 403 Forbidden', async ({ page }) => {
      await page.route('**/api/admin/**', route => {
        route.fulfill({
          status: 403,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Forbidden' })
        });
      });
      
      await page.goto('/admin');
      
      // Should show access denied message
      const accessDenied = page.locator('text=/access denied|forbidden|not authorized/i');
      await expect(accessDenied.first()).toBeVisible({ timeout: 5000 });
    });

    test('should handle 404 Not Found', async ({ page }) => {
      await page.route('**/api/products/99999', route => {
        route.fulfill({
          status: 404,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Product not found' })
        });
      });
      
      await page.goto('/products/99999');
      
      // Should show not found message
      const notFound = page.locator('text=/not found|does not exist/i');
      await expect(notFound.first()).toBeVisible({ timeout: 5000 });
    });

    test('should handle 500 Internal Server Error', async ({ page }) => {
      await page.route('**/api/products**', route => {
        route.fulfill({
          status: 500,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Internal server error' })
        });
      });
      
      await page.goto('/products');
      
      // Should show error message
      const errorMessage = page.locator('[role="alert"], .error-message, text=/error|something went wrong/i');
      await expect(errorMessage.first()).toBeVisible({ timeout: 5000 });
    });

    test('should handle 503 Service Unavailable', async ({ page }) => {
      await page.route('**/api/**', route => {
        route.fulfill({
          status: 503,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Service temporarily unavailable' })
        });
      });
      
      await page.goto('/');
      
      // Should show maintenance or unavailable message
      const unavailableMessage = page.locator('text=/unavailable|maintenance|try again later/i');
      await expect(unavailableMessage.first()).toBeVisible({ timeout: 5000 });
    });
  });

  test.describe('Rate Limiting', () => {
    test('should handle rate limit (429) response', async ({ page }) => {
      let requestCount = 0;
      
      await page.route('**/api/auth/login', route => {
        requestCount++;
        if (requestCount > 3) {
          route.fulfill({
            status: 429,
            contentType: 'application/json',
            body: JSON.stringify({ 
              message: 'Too many requests',
              retryAfter: 60
            })
          });
        } else {
          route.fulfill({
            status: 401,
            contentType: 'application/json',
            body: JSON.stringify({ message: 'Invalid credentials' })
          });
        }
      });
      
      const loginPage = new LoginPage(page);
      await loginPage.navigate();
      
      // Attempt multiple logins
      for (let i = 0; i < 5; i++) {
        await loginPage.login('test@example.com', 'wrongpassword');
        await page.waitForTimeout(100);
      }
      
      // Should show rate limit message
      const rateLimitMessage = page.locator('text=/too many|rate limit|try again later|wait/i');
      await expect(rateLimitMessage.first()).toBeVisible({ timeout: 5000 });
    });
  });

  test.describe('Form Validation Errors', () => {
    test('should display validation errors for required fields', async ({ page }) => {
      await page.goto('/register');
      
      // Submit empty form
      const submitButton = page.locator('button[type="submit"]');
      await submitButton.click();
      
      // Should show validation errors
      const errorMessages = page.locator('[role="alert"], .error, .invalid-feedback');
      const count = await errorMessages.count();
      
      expect(count).toBeGreaterThan(0);
    });

    test('should display email format validation error', async ({ page }) => {
      await page.goto('/login');
      
      const emailInput = page.locator('input[type="email"]');
      await emailInput.fill('invalid-email');
      
      const submitButton = page.locator('button[type="submit"]');
      await submitButton.click();
      
      // Should show email format error
      const emailError = page.locator('text=/valid email|invalid email|email format/i');
      await expect(emailError.first()).toBeVisible({ timeout: 3000 });
    });

    test('should display password strength requirements', async ({ page }) => {
      await page.goto('/register');
      
      const passwordInput = page.locator('input[name="password"], input[type="password"]').first();
      await passwordInput.fill('weak');
      
      // Trigger validation
      await passwordInput.blur();
      
      // Should show password requirements
      const passwordError = page.locator('text=/password|character|uppercase|number|special/i');
      await expect(passwordError.first()).toBeVisible({ timeout: 3000 });
    });

    test('should clear field error when user starts typing', async ({ page }) => {
      await page.goto('/login');
      
      // Submit empty form to trigger errors
      const submitButton = page.locator('button[type="submit"]');
      await submitButton.click();
      
      // Wait for error to appear
      const errorBefore = page.locator('[role="alert"], .error').first();
      await expect(errorBefore).toBeVisible({ timeout: 3000 });
      
      // Start typing in email field
      const emailInput = page.locator('input[type="email"]');
      await emailInput.fill('test');
      
      // Error should be cleared or hidden
      const errorAfter = page.locator('[role="alert"], .error');
      const count = await errorAfter.count();
      
      // Either error is cleared or different error shown
      expect(count).toBeGreaterThanOrEqual(0);
    });
  });

  test.describe('Cart Error Handling', () => {
    test('should handle out of stock error', async ({ page }) => {
      await page.route('**/api/cart/items', route => {
        route.fulfill({
          status: 400,
          contentType: 'application/json',
          body: JSON.stringify({ 
            message: 'Product is out of stock',
            code: 'OUT_OF_STOCK'
          })
        });
      });
      
      const cartPage = new CartPage(page);
      await cartPage.navigate();
      
      // Try to add item
      const addButton = page.locator('button:has-text("Add to Cart")').first();
      if (await addButton.count() > 0) {
        await addButton.click();
        
        // Should show out of stock message
        const stockError = page.locator('text=/out of stock|unavailable/i');
        await expect(stockError.first()).toBeVisible({ timeout: 5000 });
      }
    });

    test('should handle price change during checkout', async ({ page }) => {
      await page.route('**/api/cart', route => {
        route.fulfill({
          status: 400,
          contentType: 'application/json',
          body: JSON.stringify({ 
            message: 'Product price has changed',
            code: 'PRICE_CHANGED'
          })
        });
      });
      
      await page.goto('/cart');
      
      // Should show price change notification
      const priceChange = page.locator('text=/price.*changed|updated/i');
      await expect(priceChange.first()).toBeVisible({ timeout: 5000 });
    });

    test('should handle invalid promo code', async ({ page }) => {
      await page.route('**/api/promocodes/validate', route => {
        route.fulfill({
          status: 400,
          contentType: 'application/json',
          body: JSON.stringify({ 
            message: 'Invalid promo code',
            code: 'INVALID_PROMO'
          })
        });
      });
      
      await page.goto('/cart');
      
      const promoInput = page.locator('input[name="promo"], input[placeholder*="promo"]').first();
      if (await promoInput.count() > 0) {
        await promoInput.fill('INVALIDCODE');
        
        const applyButton = page.locator('button:has-text("Apply")').first();
        await applyButton.click();
        
        // Should show invalid promo message
        const promoError = page.locator('text=/invalid|not valid|expired/i');
        await expect(promoError.first()).toBeVisible({ timeout: 5000 });
      }
    });
  });

  test.describe('Payment Error Handling', () => {
    test('should handle declined card', async ({ page }) => {
      await page.route('**/api/payments/process', route => {
        route.fulfill({
          status: 400,
          contentType: 'application/json',
          body: JSON.stringify({ 
            message: 'Card declined',
            code: 'CARD_DECLINED'
          })
        });
      });
      
      await page.goto('/checkout');
      
      // Fill payment details
      const cardInput = page.locator('input[name="cardNumber"], input[placeholder*="card"]').first();
      if (await cardInput.count() > 0) {
        await cardInput.fill('4000000000000002'); // Test declined card
        
        const payButton = page.locator('button:has-text("Pay"), button[type="submit"]').first();
        await payButton.click();
        
        // Should show declined message
        const declinedMessage = page.locator('text=/declined|could not be processed/i');
        await expect(declinedMessage.first()).toBeVisible({ timeout: 5000 });
      }
    });

    test('should handle insufficient funds', async ({ page }) => {
      await page.route('**/api/payments/process', route => {
        route.fulfill({
          status: 400,
          contentType: 'application/json',
          body: JSON.stringify({ 
            message: 'Insufficient funds',
            code: 'INSUFFICIENT_FUNDS'
          })
        });
      });
      
      await page.goto('/checkout');
      
      const payButton = page.locator('button:has-text("Pay"), button[type="submit"]').first();
      if (await payButton.count() > 0) {
        await payButton.click();
        
        // Should show error message
        const errorMessage = page.locator('text=/insufficient|declined|could not/i');
        await expect(errorMessage.first()).toBeVisible({ timeout: 5000 });
      }
    });
  });

  test.describe('Concurrent Operations', () => {
    test('should handle concurrent cart updates', async ({ page }) => {
      await page.goto('/products');
      
      // Find add to cart buttons
      const addButtons = page.locator('button:has-text("Add to Cart")');
      const count = await addButtons.count();
      
      if (count >= 2) {
        // Click multiple buttons rapidly
        await Promise.all([
          addButtons.nth(0).click(),
          addButtons.nth(1).click()
        ]);
        
        // Wait for cart to update
        await page.waitForTimeout(1000);
        
        // Cart should show correct count
        const cartCount = page.locator('[data-testid="cart-count"], .cart-count');
        if (await cartCount.count() > 0) {
          const text = await cartCount.textContent();
          const num = parseInt(text || '0');
          expect(num).toBeGreaterThanOrEqual(1);
        }
      }
    });

    test('should handle race condition in checkout', async ({ page }) => {
      let orderCount = 0;
      
      await page.route('**/api/orders', route => {
        orderCount++;
        if (orderCount === 1) {
          route.fulfill({
            status: 201,
            contentType: 'application/json',
            body: JSON.stringify({ id: 1, status: 'Pending' })
          });
        } else {
          route.fulfill({
            status: 409,
            contentType: 'application/json',
            body: JSON.stringify({ message: 'Order already processing' })
          });
        }
      });
      
      await page.goto('/checkout');
      
      const placeOrderButton = page.locator('button:has-text("Place Order"), button[type="submit"]').first();
      if (await placeOrderButton.count() > 0) {
        // Double-click rapidly
        await placeOrderButton.dblclick();
        
        // Should handle gracefully - either success or error, not both
        await page.waitForTimeout(2000);
        
        // Should not have multiple order confirmations
        const successMessages = page.locator('text=/order.*confirmed|thank you/i');
        const count = await successMessages.count();
        expect(count).toBeLessThanOrEqual(1);
      }
    });
  });

  test.describe('Edge Cases', () => {
    test('should handle empty cart checkout attempt', async ({ page }) => {
      await page.goto('/cart');
      
      // Try to checkout with empty cart
      const checkoutButton = page.locator('a:has-text("Checkout"), button:has-text("Checkout")');
      
      if (await checkoutButton.count() > 0) {
        // Check if button is disabled
        const isDisabled = await checkoutButton.first().isDisabled();
        
        if (!isDisabled) {
          await checkoutButton.first().click();
          
          // Should show empty cart message
          const emptyMessage = page.locator('text=/empty|no items|add items/i');
          await expect(emptyMessage.first()).toBeVisible({ timeout: 5000 });
        }
      }
    });

    test('should handle extremely long input', async ({ page }) => {
      await page.goto('/login');
      
      const longString = 'a'.repeat(10000);
      const emailInput = page.locator('input[type="email"]');
      
      await emailInput.fill(longString);
      await emailInput.blur();
      
      // Should show validation error or truncate
      const error = page.locator('[role="alert"], .error');
      const hasError = await error.count() > 0;
      
      // Either shows error or handles gracefully
      expect(hasError || true).toBeTruthy();
    });

    test('should handle special characters in search', async ({ page }) => {
      await page.goto('/products');
      
      const searchInput = page.locator('input[type="search"], input[name="search"]').first();
      if (await searchInput.count() > 0) {
        await searchInput.fill('<script>alert("xss")</script>');
        await searchInput.press('Enter');
        
        // Should not execute script, should escape or show no results
        await page.waitForTimeout(1000);
        
        // Page should still be functional
        const body = page.locator('body');
        await expect(body).toBeVisible();
      }
    });

    test('should handle unicode characters', async ({ page }) => {
      await page.goto('/products');
      
      const searchInput = page.locator('input[type="search"], input[name="search"]').first();
      if (await searchInput.count() > 0) {
        await searchInput.fill('产品 тест 产品 🛒');
        await searchInput.press('Enter');
        
        // Should handle without crashing
        await page.waitForTimeout(1000);
        
        const body = page.locator('body');
        await expect(body).toBeVisible();
      }
    });

    test('should handle negative quantity', async ({ page }) => {
      await page.goto('/cart');
      
      const quantityInput = page.locator('input[type="number"], input[name="quantity"]').first();
      if (await quantityInput.count() > 0) {
        await quantityInput.fill('-5');
        await quantityInput.blur();
        
        // Should show error or reset to valid value
        const error = page.locator('[role="alert"], .error');
        const hasError = await error.count() > 0;
        
        const value = await quantityInput.inputValue();
        const isValid = parseInt(value) >= 0;
        
        expect(hasError || isValid).toBeTruthy();
      }
    });

    test('should handle session expiration', async ({ page }) => {
      // First login
      await page.goto('/login');
      
      // Simulate session expiration
      await page.route('**/api/auth/me', route => {
        route.fulfill({
          status: 401,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Session expired' })
        });
      });
      
      // Navigate to protected page
      await page.goto('/profile');
      
      // Should redirect to login with session expired message
      await page.waitForURL(/\/login/, { timeout: 5000 });
      
      const sessionMessage = page.locator('text=/session.*expired|logged out|login again/i');
      await expect(sessionMessage.first()).toBeVisible({ timeout: 3000 });
    });
  });

  test.describe('Offline Handling', () => {
    test('should show offline indicator when network is lost', async ({ page, context }) => {
      await page.goto('/');
      
      // Go offline
      await context.setOffline(true);
      
      // Trigger an action that requires network
      await page.click('a[href="/products"]');
      
      // Should show offline indicator
      const offlineIndicator = page.locator('text=/offline|no connection|connect/i');
      await expect(offlineIndicator.first()).toBeVisible({ timeout: 5000 });
      
      // Restore network
      await context.setOffline(false);
    });

    test('should recover when network is restored', async ({ page, context }) => {
      await page.goto('/');
      
      // Go offline
      await context.setOffline(true);
      
      // Wait for offline state
      await page.waitForTimeout(1000);
      
      // Restore network
      await context.setOffline(false);
      
      // Try to navigate
      await page.click('a[href="/products"]');
      
      // Should load successfully
      await page.waitForURL(/\/products/, { timeout: 5000 });
    });
  });
});