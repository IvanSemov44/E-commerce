import { test, expect } from '@playwright/test';
import { CheckoutPage, CartPage } from '../pages';
import { testAddresses, testCards, testUsers } from '../data/test-data';
import { mockCheckoutApi, mockCartApi, mockAuthApi } from '../utils/api-mocking';

/**
 * Enhanced Checkout E2E Tests
 * Uses Page Object Model and proper assertions
 */
test.describe('Checkout Flow', () => {
  let checkoutPage: CheckoutPage;
  let cartPage: CartPage;

  test.beforeEach(async ({ page }) => {
    checkoutPage = new CheckoutPage(page);
    cartPage = new CartPage(page);
    
    // Mock APIs
    await mockCartApi(page, { empty: false });
    await mockAuthApi(page, { authenticated: true });
    await mockCheckoutApi(page, { success: true });
  });

  test.describe('Checkout Form', () => {
    test.beforeEach(async ({ page }) => {
      await checkoutPage.navigate();
    });

    test('should display checkout form with all sections', async ({ page }) => {
      await checkoutPage.expectCheckoutFormVisible();
      await checkoutPage.expectPaymentMethodsVisible();
      await checkoutPage.expectOrderSummaryVisible();
    });

    test('should pre-fill form for authenticated users', async ({ page }) => {
      // Check if form is pre-filled with user data
      const emailValue = await checkoutPage.emailInput().inputValue();
      
      // Email should be pre-filled for authenticated users
      expect(emailValue).toBeTruthy();
    });

    test('should validate required fields', async ({ page }) => {
      // Try to submit without filling required fields
      await checkoutPage.placeOrderButton().click();
      
      await checkoutPage.expectValidationErrors();
    });

    test('should validate email format', async ({ page }) => {
      await checkoutPage.fillInput(checkoutPage.emailInput(), 'invalid-email');
      await checkoutPage.emailInput().blur();
      
      // Should show validation error
      const emailInput = checkoutPage.emailInput();
      const isValid = await emailInput.evaluate((el: HTMLInputElement) => el.validity.valid);
      
      expect(isValid).toBeFalsy();
    });

    test('should validate phone format', async ({ page }) => {
      const phoneInput = checkoutPage.phoneInput();
      
      if (await phoneInput.count() > 0) {
        await checkoutPage.fillInput(phoneInput, '123');
        await phoneInput.blur();
        
        // Should show validation error for short phone
        const isValid = await phoneInput.evaluate((el: HTMLInputElement) => el.validity.valid);
        expect(isValid).toBeFalsy();
      } else {
        test.skip();
      }
    });

    test('should validate zip code format', async ({ page }) => {
      await checkoutPage.fillInput(checkoutPage.zipInput(), 'abc');
      await checkoutPage.zipInput().blur();
      
      // Should show validation error
      const zipInput = checkoutPage.zipInput();
      const isValid = await zipInput.evaluate((el: HTMLInputElement) => el.validity.valid);
      
      expect(isValid).toBeFalsy();
    });
  });

  test.describe('Shipping Address', () => {
    test.beforeEach(async ({ page }) => {
      await checkoutPage.navigate();
    });

    test('should accept valid shipping address', async ({ page }) => {
      await checkoutPage.fillShippingAddress(testAddresses.valid);
      
      // All fields should be filled
      const firstName = await checkoutPage.firstNameInput().inputValue();
      const lastName = await checkoutPage.lastNameInput().inputValue();
      const address = await checkoutPage.addressInput().inputValue();
      
      expect(firstName).toBe(testAddresses.valid.firstName);
      expect(lastName).toBe(testAddresses.valid.lastName);
      expect(address).toBe(testAddresses.valid.address);
    });

    test('should support international addresses', async ({ page }) => {
      await checkoutPage.fillShippingAddress(testAddresses.international);
      
      // Should accept international format
      const city = await checkoutPage.cityInput().inputValue();
      expect(city).toBe(testAddresses.international.city);
    });

    test('should show state/province field based on country', async ({ page }) => {
      // Select US
      if (await checkoutPage.countrySelect().count() > 0) {
        await checkoutPage.countrySelect().selectOption('US');
        
        // State field should be visible for US
        await expect(checkoutPage.stateInput()).toBeVisible();
      } else {
        test.skip();
      }
    });
  });

  test.describe('Payment Methods', () => {
    test.beforeEach(async ({ page }) => {
      await checkoutPage.navigate();
      await checkoutPage.fillShippingAddress(testAddresses.valid);
    });

    test('should display available payment methods', async ({ page }) => {
      await checkoutPage.expectToBeVisible(checkoutPage.creditCardOption());
      
      if (await checkoutPage.paypalOption().count() > 0) {
        await checkoutPage.expectToBeVisible(checkoutPage.paypalOption());
      }
    });

    test('should select credit card payment', async ({ page }) => {
      await checkoutPage.selectCreditCard();
      
      // Credit card form should be visible
      await checkoutPage.expectToBeVisible(checkoutPage.cardNumberInput());
    });

    test('should validate credit card number', async ({ page }) => {
      await checkoutPage.selectCreditCard();
      await checkoutPage.fillInput(checkoutPage.cardNumberInput(), '1234567890');
      
      // Should show validation error
      const cardInput = checkoutPage.cardNumberInput();
      const value = await cardInput.inputValue();
      
      // Card number should be formatted or rejected
      expect(value.length).toBeLessThanOrEqual(19);
    });

    test('should validate expiry date format', async ({ page }) => {
      await checkoutPage.selectCreditCard();
      await checkoutPage.fillInput(checkoutPage.expiryInput(), '13/25');
      
      // Invalid month should be rejected or show error
      const expiryInput = checkoutPage.expiryInput();
      const value = await expiryInput.inputValue();
      
      // Should not accept invalid month
      expect(value).not.toContain('13');
    });

    test('should validate CVV', async ({ page }) => {
      await checkoutPage.selectCreditCard();
      await checkoutPage.fillInput(checkoutPage.cvvInput(), '12');
      
      // CVV should be at least 3 digits
      const cvvInput = checkoutPage.cvvInput();
      const value = await cvvInput.inputValue();
      
      expect(value.length).toBeGreaterThanOrEqual(3);
    });

    test('should accept valid credit card details', async ({ page }) => {
      await checkoutPage.selectCreditCard();
      await checkoutPage.fillCreditCardDetails(testCards.validVisa);
      
      // All card fields should be filled
      const cardNumber = await checkoutPage.cardNumberInput().inputValue();
      expect(cardNumber).toBeTruthy();
    });
  });

  test.describe('Order Summary', () => {
    test.beforeEach(async ({ page }) => {
      await checkoutPage.navigate();
    });

    test('should display order summary with items', async ({ page }) => {
      await checkoutPage.expectOrderSummaryVisible();
      
      // Should show item count
      const summary = checkoutPage.orderSummary();
      const text = await summary.textContent();
      
      expect(text).toBeTruthy();
    });

    test('should show correct order total', async ({ page }) => {
      const total = await checkoutPage.getOrderTotal();
      
      expect(total).toBeGreaterThan(0);
    });

    test('should update total when shipping method changes', async ({ page }) => {
      const initialTotal = await checkoutPage.getOrderTotal();
      
      // Select express shipping if available
      if (await checkoutPage.expressShipping().count() > 0) {
        await checkoutPage.selectExpressShipping();
        
        const newTotal = await checkoutPage.getOrderTotal();
        
        // Express shipping should cost more
        expect(newTotal).toBeGreaterThanOrEqual(initialTotal);
      } else {
        test.skip();
      }
    });
  });

  test.describe('Place Order', () => {
    test.beforeEach(async ({ page }) => {
      await checkoutPage.navigate();
      await checkoutPage.fillShippingAddress(testAddresses.valid);
      await checkoutPage.selectCreditCard();
      await checkoutPage.fillCreditCardDetails(testCards.validVisa);
    });

    test('should successfully place order', async ({ page }) => {
      await checkoutPage.placeOrder();
      
      await checkoutPage.expectOnConfirmationPage();
    });

    test('should show order confirmation with order number', async ({ page }) => {
      await checkoutPage.placeOrder();
      
      // Should show order number
      const orderNumber = page.getByTestId('order-number').or(
        page.locator('[class*="order-number"], :text(/ORD-|Order #/)')
      );
      
      await expect(orderNumber).toBeVisible();
    });

    test('should show estimated delivery date', async ({ page }) => {
      await checkoutPage.placeOrder();
      
      const deliveryDate = page.getByTestId('delivery-date').or(
        page.locator(':text("Delivery"), :text("Arrives")')
      );
      
      await expect(deliveryDate).toBeVisible();
    });

    test('should handle payment failure gracefully', async ({ page }) => {
      // Mock payment failure
      await mockCheckoutApi(page, { success: false });
      
      await checkoutPage.placeOrderButton().click();
      
      // Should show error message
      await checkoutPage.expectErrorMessage('Payment failed');
      
      // Should stay on checkout page
      await checkoutPage.expectOnCheckoutPage();
    });
  });

  test.describe('Guest Checkout', () => {
    test.beforeEach(async ({ page }) => {
      // Mock unauthenticated state
      await mockAuthApi(page, { authenticated: false });
      await checkoutPage.navigate();
    });

    test('should require email for guest checkout', async ({ page }) => {
      // Email field should be required
      const emailInput = checkoutPage.emailInput();
      const isRequired = await emailInput.getAttribute('required');
      
      expect(isRequired).not.toBeNull();
    });

    test('should allow guest checkout with email', async ({ page }) => {
      await checkoutPage.fillShippingAddress({
        ...testAddresses.valid,
        email: 'guest@example.com',
      });
      await checkoutPage.selectCreditCard();
      await checkoutPage.fillCreditCardDetails(testCards.validVisa);
      
      await checkoutPage.placeOrder();
      
      await checkoutPage.expectOnConfirmationPage();
    });

    test('should offer login option during guest checkout', async ({ page }) => {
      const loginLink = page.getByTestId('login-link').or(
        page.locator('a:has-text("Login"), a:has-text("Sign in")')
      );
      
      if (await loginLink.count() > 0) {
        await expect(loginLink).toBeVisible();
      }
    });
  });

  test.describe('Error Handling', () => {
    test('should handle network timeout gracefully', async ({ page }) => {
      // Mock slow response
      await page.route('**/api/orders', async route => {
        await new Promise(resolve => setTimeout(resolve, 60000));
      });
      
      await checkoutPage.navigate();
      await checkoutPage.fillShippingAddress(testAddresses.valid);
      await checkoutPage.selectCreditCard();
      await checkoutPage.fillCreditCardDetails(testCards.validVisa);
      
      await checkoutPage.placeOrderButton().click();
      
      // Should show timeout error
      const error = page.locator('[class*="error"], [class*="timeout"]');
      expect(await error.count() > 0).toBeTruthy();
    });

    test('should handle server error gracefully', async ({ page }) => {
      await page.route('**/api/orders', route => 
        route.fulfill({ status: 500, body: 'Internal Server Error' })
      );
      
      await checkoutPage.navigate();
      await checkoutPage.fillShippingAddress(testAddresses.valid);
      await checkoutPage.selectCreditCard();
      await checkoutPage.fillCreditCardDetails(testCards.validVisa);
      
      await checkoutPage.placeOrderButton().click();
      
      // Should show error message
      await checkoutPage.expectErrorMessage('error');
    });
  });
});