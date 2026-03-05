import { test, expect } from '@playwright/test';
import { ProductsPage, CartPage } from '../pages';
import { testPromoCodes } from '../data/test-data';
import { mockProductsApi, mockCartApi } from '../utils/api-mocking';

/* eslint-disable @typescript-eslint/no-unused-vars */

/**
 * Enhanced Cart E2E Tests
 * Uses Page Object Model and proper assertions
 */
test.describe('Shopping Cart', () => {
  let productsPage: ProductsPage;
  let cartPage: CartPage;

  test.beforeEach(async ({ page }) => {
    productsPage = new ProductsPage(page);
    cartPage = new CartPage(page);
    
    // Mock APIs for consistent testing
    await mockProductsApi(page);
    await mockCartApi(page, { empty: true });
  });

  test.describe('Empty Cart', () => {
    test('should display empty cart message when no items', async ({ page }) => {
      await cartPage.navigate();
      
      await cartPage.expectCartEmpty();
      await cartPage.expectToBeVisible(cartPage.emptyCartMessage());
    });

    test('should show continue shopping button', async ({ page }) => {
      await cartPage.navigate();
      
      await cartPage.expectToBeVisible(cartPage.continueShoppingButton());
    });

    test('should have disabled checkout button', async ({ page }) => {
      await cartPage.navigate();
      
      await cartPage.expectCheckoutButtonDisabled();
    });
  });

  test.describe('Add to Cart', () => {
    test.beforeEach(async ({ page }) => {
      await mockCartApi(page, { empty: false });
    });

    test('should add product to cart from product listing', async ({ page }) => {
      await productsPage.navigate();
      
      const initialCount = await productsPage.getProductCount();
      expect(initialCount).toBeGreaterThan(0);
      
      // Click first product
      await productsPage.clickProduct(0);
      
      // Add to cart
      const addToCartButton = page.getByTestId('add-to-cart').or(
        page.locator('button:has-text("Add to Cart")')
      );
      await addToCartButton.click();
      
      // Verify success
      await cartPage.expectToastMessage('added');
    });

    test('should update cart badge count', async ({ page }) => {
      await productsPage.navigate();
      await productsPage.clickProduct(0);
      
      const addToCartButton = page.getByTestId('add-to-cart').or(
        page.locator('button:has-text("Add to Cart")')
      );
      await addToCartButton.click();
      
      // Check cart badge
      const cartBadge = page.getByTestId('cart-count').or(
        page.locator('[class*="cart-badge"], [class*="cart-count"]')
      );
      
      if (await cartBadge.count() > 0) {
        const count = await cartBadge.textContent();
        expect(parseInt(count || '0')).toBeGreaterThan(0);
      }
    });

    test('should add multiple products to cart', async ({ page }) => {
      await productsPage.navigate();
      
      // Add first product
      await productsPage.clickProduct(0);
      await page.getByTestId('add-to-cart').or(page.locator('button:has-text("Add to Cart")')).click();
      
      // Go back and add second product
      await page.goBack();
      await productsPage.clickProduct(1);
      await page.getByTestId('add-to-cart').or(page.locator('button:has-text("Add to Cart")')).click();
      
      // Navigate to cart
      await cartPage.navigate();
      
      // Should have at least 2 items
      const itemCount = await cartPage.getItemCount();
      expect(itemCount).toBeGreaterThanOrEqual(2);
    });
  });

  test.describe('Cart Management', () => {
    test.beforeEach(async ({ page }) => {
      await mockCartApi(page, { empty: false });
      await cartPage.navigate();
    });

    test('should display cart items with correct information', async ({ page }) => {
      await cartPage.expectCartNotEmpty();
      
      // Check first item has name, price, quantity
      const itemName = await cartPage.getItemName(0);
      const itemPrice = await cartPage.getItemPrice(0);
      const itemQuantity = await cartPage.getItemQuantity(0);
      
      expect(itemName).toBeTruthy();
      expect(itemPrice).toBeGreaterThan(0);
      expect(itemQuantity).toBeGreaterThanOrEqual(1);
    });

    test('should update item quantity', async ({ page }) => {
      const initialQuantity = await cartPage.getItemQuantity(0);
      
      // Increment quantity
      await cartPage.incrementQuantity(0);
      
      const newQuantity = await cartPage.getItemQuantity(0);
      expect(newQuantity).toBe(initialQuantity + 1);
    });

    test('should remove item from cart', async ({ page }) => {
      const initialCount = await cartPage.getItemCount();
      
      // Remove first item
      await cartPage.removeItem(0);
      
      const newCount = await cartPage.getItemCount();
      expect(newCount).toBe(initialCount - 1);
    });

    test('should clear entire cart', async ({ page }) => {
      await cartPage.clearCart();
      
      await cartPage.expectCartEmpty();
    });

    test('should calculate correct totals', async ({ page }) => {
      const subtotal = await cartPage.getSubtotal();
      const shipping = await cartPage.getShipping();
      const tax = await cartPage.getTax();
      const total = await cartPage.getTotal();
      
      // Total should be subtotal + shipping + tax
      const expectedTotal = subtotal + shipping + tax;
      expect(total).toBeCloseTo(expectedTotal, 2);
    });
  });

  test.describe('Promo Codes', () => {
    test.beforeEach(async ({ page }) => {
      await mockCartApi(page, { empty: false });
      await cartPage.navigate();
    });

    test('should apply valid promo code', async ({ page }) => {
      const initialTotal = await cartPage.getTotal();
      
      await cartPage.applyPromoCode(testPromoCodes.valid.code);
      
      await cartPage.expectPromoCodeApplied();
      
      // Total should be reduced
      const newTotal = await cartPage.getTotal();
      expect(newTotal).toBeLessThan(initialTotal);
    });

    test('should show error for invalid promo code', async ({ page }) => {
      await cartPage.applyPromoCode(testPromoCodes.invalid.code);
      
      await cartPage.expectPromoCodeError('invalid');
    });

    test('should show error for expired promo code', async ({ page }) => {
      await cartPage.applyPromoCode(testPromoCodes.expired.code);
      
      await cartPage.expectPromoCodeError('expired');
    });
  });

  test.describe('Checkout Navigation', () => {
    test.beforeEach(async ({ page }) => {
      await mockCartApi(page, { empty: false });
      await cartPage.navigate();
    });

    test('should navigate to checkout when clicking checkout button', async ({ page }) => {
      await cartPage.proceedToCheckout();
      
      await expect(page).toHaveURL(/\/checkout/);
    });

    test('should continue shopping when clicking continue button', async ({ page }) => {
      await cartPage.continueShopping();
      
      await expect(page).toHaveURL(/\/products|\/$/);
    });
  });

  test.describe('Cart Persistence', () => {
    test('should persist cart across page navigation', async ({ page }) => {
      await mockCartApi(page, { empty: false });
      
      // Add item to cart
      await productsPage.navigate();
      await productsPage.clickProduct(0);
      await page.getByTestId('add-to-cart').or(page.locator('button:has-text("Add to Cart")')).click();
      
      // Navigate away
      await page.goto('/');
      
      // Navigate back to cart
      await cartPage.navigate();
      
      // Cart should still have items
      await cartPage.expectCartNotEmpty();
    });

    test('should persist cart after page reload', async ({ page }) => {
      await mockCartApi(page, { empty: false });
      await cartPage.navigate();
      
      const itemCount = await cartPage.getItemCount();
      
      // Reload page
      await page.reload();
      
      // Cart should still have same items
      const newItemCount = await cartPage.getItemCount();
      expect(newItemCount).toBe(itemCount);
    });
  });

  test.describe('Error Handling', () => {
    test('should handle out of stock gracefully', async ({ page }) => {
      // Mock out of stock scenario
      await page.route('**/api/cart/add', route => 
        route.fulfill({ 
          status: 400, 
          body: JSON.stringify({ message: 'Product is out of stock' }) 
        })
      );
      
      await productsPage.navigate();
      await productsPage.clickProduct(0);
      await page.getByTestId('add-to-cart').or(page.locator('button:has-text("Add to Cart")')).click();
      
      // Should show error message
      await cartPage.expectErrorMessage('out of stock');
    });

    test('should handle network errors gracefully', async ({ page }) => {
      // Mock network error
      await page.route('**/api/cart', route => route.abort('failed'));
      
      await cartPage.navigate();
      
      // Should show error state
      const errorState = page.locator('[class*="error"], [class*="retry"]');
      expect(await errorState.count() > 0).toBeTruthy();
    });
  });
});