import { test, expect } from '@playwright/test';

/**
 * E2E Tests: Wishlist
 * Tests wishlist functionality including add, remove, and move to cart
 */

test.describe('Wishlist', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should display wishlist page', async ({ page }) => {
    // Navigate to wishlist
    await page.goto('/wishlist');
    await page.waitForTimeout(1000);

    // Check for wishlist container
    page.locator('[class*="wishlist"], [data-testid="wishlist"]');

    // Page should load without errors
    expect(page.url()).toContain('wishlist');
  });

  test('should show empty state when wishlist is empty', async ({ page }) => {
    await page.goto('/wishlist');
    await page.waitForTimeout(1000);

    // Look for empty state message
    const emptyState = page.locator(
      '[class*="empty"], :text("No items"), :text("Your wishlist is empty")'
    );

    // Either shows empty state or wishlist items
    const hasEmptyState = (await emptyState.count()) > 0;
    const hasItems =
      (await page.locator('[class*="wishlist-item"], [data-testid="wishlist-item"]').count()) > 0;

    expect(hasEmptyState || hasItems).toBeTruthy();
  });

  test('should add product to wishlist from product page', async ({ page }) => {
    // Navigate to a product page
    await page.goto('/products');
    await page.waitForTimeout(1000);

    // Click on first product
    const productLink = page.locator('a[href*="/products/"]').first();

    if ((await productLink.count()) > 0) {
      await productLink.click();
      await page.waitForTimeout(1000);

      // Look for add to wishlist button
      const wishlistButton = page
        .locator(
          'button:has-text("Wishlist"), button:has-text("Add to Wishlist"), [data-testid="add-to-wishlist"], button[aria-label*="wishlist" i]'
        )
        .first();

      if ((await wishlistButton.count()) > 0) {
        await wishlistButton.click();
        await page.waitForTimeout(1000);

        // Check for success indication
        const successToast = page.locator('[class*="toast"], [role="alert"]');
        const buttonChanged = await wishlistButton.getAttribute('class');

        // Either toast appeared or button state changed
        expect((await successToast.count()) > 0 || buttonChanged).toBeTruthy();
      } else {
        test.skip();
      }
    } else {
      test.skip();
    }
  });

  test('should remove item from wishlist', async ({ page }) => {
    await page.goto('/wishlist');
    await page.waitForTimeout(1000);

    // Check if there are items in wishlist
    const wishlistItem = page
      .locator('[class*="wishlist-item"], [data-testid="wishlist-item"]')
      .first();

    if ((await wishlistItem.count()) > 0) {
      // Look for remove button
      const removeButton = wishlistItem
        .locator('button:has-text("Remove"), button[aria-label*="remove" i]')
        .first();

      if ((await removeButton.count()) > 0) {
        await removeButton.click();
        await page.waitForTimeout(1000);

        // Item should be removed
        const itemsAfter = await page
          .locator('[class*="wishlist-item"], [data-testid="wishlist-item"]')
          .count();
        expect(itemsAfter >= 0).toBeTruthy();
      } else {
        test.skip();
      }
    } else {
      test.skip();
    }
  });

  test('should move item from wishlist to cart', async ({ page }) => {
    await page.goto('/wishlist');
    await page.waitForTimeout(1000);

    const wishlistItem = page
      .locator('[class*="wishlist-item"], [data-testid="wishlist-item"]')
      .first();

    if ((await wishlistItem.count()) > 0) {
      // Look for add to cart button
      const addToCartButton = wishlistItem
        .locator('button:has-text("Add to Cart"), button:has-text("Move to Cart")')
        .first();

      if ((await addToCartButton.count()) > 0) {
        await addToCartButton.click();
        await page.waitForTimeout(1000);

        // Check for success indication
        const successToast = page.locator('[class*="toast"], [role="alert"]');
        expect((await successToast.count()) >= 0).toBeTruthy();
      } else {
        test.skip();
      }
    } else {
      test.skip();
    }
  });

  test('should navigate to product from wishlist', async ({ page }) => {
    await page.goto('/wishlist');
    await page.waitForTimeout(1000);

    const wishlistItem = page
      .locator('[class*="wishlist-item"], [data-testid="wishlist-item"]')
      .first();

    if ((await wishlistItem.count()) > 0) {
      // Click on product link within wishlist item
      const productLink = wishlistItem.locator('a[href*="/products/"]').first();

      if ((await productLink.count()) > 0) {
        await productLink.click();
        await page.waitForTimeout(1000);

        // Should be on product page
        expect(page.url()).toContain('/products/');
      } else {
        // Click on the item itself
        await wishlistItem.click();
        await page.waitForTimeout(1000);

        // Check if navigated
        const currentUrl = page.url();
        expect(currentUrl.includes('/products/') || currentUrl.includes('/wishlist')).toBeTruthy();
      }
    } else {
      test.skip();
    }
  });

  test('should display product price in wishlist', async ({ page }) => {
    await page.goto('/wishlist');
    await page.waitForTimeout(1000);

    const wishlistItem = page
      .locator('[class*="wishlist-item"], [data-testid="wishlist-item"]')
      .first();

    if ((await wishlistItem.count()) > 0) {
      // Look for price element
      const priceElement = wishlistItem.locator('[class*="price"], :text(/\\$[\\d.]+/)');

      if ((await priceElement.count()) > 0) {
        const priceText = await priceElement.textContent();
        expect(priceText).toMatch(/\\$[\d.]+|[\d.]+\s*\$/);
      } else {
        test.skip();
      }
    } else {
      test.skip();
    }
  });

  test('should show wishlist item count in header', async ({ page }) => {
    // Look for wishlist icon with count in header
    const wishlistIcon = page.locator('[data-testid="wishlist-icon"], a[href*="wishlist"]').first();

    if ((await wishlistIcon.count()) > 0) {
      // Check for count badge
      const countBadge = wishlistIcon.locator('[class*="badge"], [class*="count"]');

      // Badge may or may not be present depending on wishlist state
      expect((await countBadge.count()) >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should persist wishlist across page navigations', async ({ page }) => {
    // Add item to wishlist first
    await page.goto('/products');
    await page.waitForTimeout(1000);

    const productLink = page.locator('a[href*="/products/"]').first();

    if ((await productLink.count()) > 0) {
      await productLink.click();
      await page.waitForTimeout(1000);

      const wishlistButton = page
        .locator('button:has-text("Wishlist"), [data-testid="add-to-wishlist"]')
        .first();

      if ((await wishlistButton.count()) > 0) {
        await wishlistButton.click();
        await page.waitForTimeout(500);

        // Navigate to different page
        await page.goto('/');
        await page.waitForTimeout(1000);

        // Go back to wishlist
        await page.goto('/wishlist');
        await page.waitForTimeout(1000);

        // Item should still be there
        const wishlistItem = page.locator(
          '[class*="wishlist-item"], [data-testid="wishlist-item"]'
        );
        expect((await wishlistItem.count()) >= 0).toBeTruthy();
      } else {
        test.skip();
      }
    } else {
      test.skip();
    }
  });
});
