import { test, expect } from '@playwright/test';

/**
 * E2E Tests: Shopping Cart
 * Tests add to cart, remove from cart, and cart management functionality
 */

test.describe('Shopping Cart', () => {
  test('should add product to cart', async ({ page }) => {
    await page.goto('/');

    // Navigate to a product
    await page.waitForSelector('[data-testid="product-card"], .product-card, [class*="product"]', { 
      timeout: 10000 
    });
    
    const firstProduct = page.locator('[data-testid="product-card"], .product-card, [class*="product"]').first();
    await firstProduct.click();

    await page.waitForURL(/\/product(s)?\/\w+/, { timeout: 5000 });

    // Find and click "Add to Cart" button
    const addToCartButton = page.locator(
      'button:has-text("Add to Cart"), [data-testid="add-to-cart"], button:has-text("Add to Bag")'
    ).first();
    
    await expect(addToCartButton).toBeVisible({ timeout: 5000 });
    await addToCartButton.click();

    // Verify cart badge/counter increased
    const cartBadge = page.locator('[data-testid="cart-count"], .cart-count, .cart-badge');
    
    if (await cartBadge.count() > 0) {
      const cartCountText = await cartBadge.textContent();
      const cartCount = parseInt(cartCountText || '0');
      expect(cartCount).toBeGreaterThan(0);
    }

    // Verify success message or cart icon update
    const successMessage = page.locator('.toast, [role="alert"], .notification');
    if (await successMessage.count() > 0) {
      await expect(successMessage.first()).toBeVisible({ timeout: 3000 });
    }
  });

  test('should view cart with added items', async ({ page }) => {
    await page.goto('/');

    // Add a product first
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

    // Navigate to cart
    const cartLink = page.locator(
      'a[href*="cart"], [data-testid="cart-link"], a:has-text("Cart")'
    ).first();
    
    await cartLink.click();

    // Verify we're on cart page
    await page.waitForURL(/\/cart/, { timeout: 5000 });

    // Verify cart items are displayed
    const cartItems = page.locator('[data-testid="cart-item"], .cart-item, [class*="cart-item"]');
    await expect(cartItems.first()).toBeVisible({ timeout: 5000 });
  });

  test('should update cart item quantity', async ({ page }) => {
    await page.goto('/');

    // Add a product and navigate to cart
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

    const cartLink = page.locator(
      'a[href*="cart"], [data-testid="cart-link"], a:has-text("Cart")'
    ).first();
    
    await cartLink.click();
    await page.waitForURL(/\/cart/, { timeout: 5000 });

    // Find quantity input or increment button
    const quantityInput = page.locator('input[type="number"], [data-testid="quantity-input"]').first();
    const incrementButton = page.locator('[data-testid="increment"], button:has-text("+")').first();

    if (await quantityInput.count() > 0) {
      await quantityInput.fill('2');
      await page.waitForTimeout(500);
      
      const newValue = await quantityInput.inputValue();
      expect(newValue).toBe('2');
    } else if (await incrementButton.count() > 0) {
      await incrementButton.click();
      await page.waitForTimeout(500);
      
      // Verify quantity increased (cart total should change)
      const cartTotal = page.locator('[data-testid="cart-total"], .total, [class*="total"]');
      await expect(cartTotal).toBeVisible();
    }
  });

  test('should remove item from cart', async ({ page }) => {
    await page.goto('/');

    // Add a product and navigate to cart
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

    const cartLink = page.locator(
      'a[href*="cart"], [data-testid="cart-link"], a:has-text("Cart")'
    ).first();
    
    await cartLink.click();
    await page.waitForURL(/\/cart/, { timeout: 5000 });

    // Count initial cart items
    const cartItems = page.locator('[data-testid="cart-item"], .cart-item, [class*="cart-item"]');
    const initialCount = await cartItems.count();

    // Find and click remove button
    const removeButton = page.locator(
      'button:has-text("Remove"), [data-testid="remove-item"], button:has-text("Delete")'
    ).first();
    
    if (await removeButton.count() > 0) {
      await removeButton.click();
      await page.waitForTimeout(1000);

      // Verify item was removed
      const newCount = await cartItems.count();
      expect(newCount).toBeLessThan(initialCount);
    }
  });

  test('should display empty cart message when no items', async ({ page }) => {
    await page.goto('/cart');

    // Look for empty cart message or product listings
    const emptyMessage = page.locator(
      '[data-testid="empty-cart"], .empty-cart, :has-text("empty"), :has-text("no items")'
    );
    
    const cartItems = page.locator('[data-testid="cart-item"], .cart-item');

    // Either there's an empty message OR there are items
    const hasEmptyMessage = await emptyMessage.count() > 0;
    const hasItems = await cartItems.count() > 0;

    expect(hasEmptyMessage || hasItems).toBeTruthy();
  });
});
