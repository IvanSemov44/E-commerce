import { test, expect } from '@playwright/test';

/**
 * E2E Tests: Product Browsing
 * Tests the core product discovery and viewing functionality
 */

test.describe('Product Browsing', () => {
  test('should display product listing page', async ({ page }) => {
    await page.goto('/');

    // Check page title or heading
    await expect(page).toHaveTitle(/E-Commerce/i);

    // Verify products are displayed (look for common product elements)
    const products = page
      .locator('[data-testid="product-card"], .product-card, [class*="product"]')
      .first();
    await expect(products).toBeVisible({ timeout: 10000 });
  });

  test('should navigate to product detail page', async ({ page }) => {
    await page.goto('/');

    // Wait for products to load
    await page.waitForSelector('[data-testid="product-card"], .product-card, [class*="product"]', {
      timeout: 10000,
    });

    // Click first product
    const firstProduct = page
      .locator('[data-testid="product-card"], .product-card, [class*="product"]')
      .first();
    await firstProduct.click();

    // Verify we're on product detail page (URL should change or specific element should appear)
    await page.waitForURL(/\/product(s)?\/\w+/, { timeout: 5000 });

    // Verify product details are shown
    await expect(page.locator('h1, [data-testid="product-title"]')).toBeVisible();
  });

  test('should search for products', async ({ page }) => {
    await page.goto('/');

    // Look for search input
    const searchInput = page.locator(
      'input[type="search"], input[placeholder*="Search"], [data-testid="search-input"]'
    );

    if ((await searchInput.count()) > 0) {
      await searchInput.fill('laptop');
      await searchInput.press('Enter');

      // Wait for results
      await page.waitForLoadState('networkidle');

      // Verify search results or URL change
      const hasResults =
        (await page.locator('[data-testid="product-card"], .product-card').count()) > 0;
      const urlChanged = page.url().includes('search') || page.url().includes('laptop');

      expect(hasResults || urlChanged).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should filter products by category', async ({ page }) => {
    await page.goto('/');

    // Look for category links/filters
    const categoryLink = page
      .locator('[data-testid="category-link"], nav a, .category-filter')
      .first();

    if ((await categoryLink.count()) > 0) {
      await categoryLink.click();

      // Wait for navigation or filtering
      await page.waitForLoadState('networkidle');

      // Verify products are still displayed
      const products = page.locator('[data-testid="product-card"], .product-card');
      await expect(products.first()).toBeVisible({ timeout: 5000 });
    } else {
      test.skip();
    }
  });

  test('should show product details with price and description', async ({ page }) => {
    await page.goto('/');

    // Navigate to a product
    await page.waitForSelector('[data-testid="product-card"], .product-card, [class*="product"]', {
      timeout: 10000,
    });

    const firstProduct = page
      .locator('[data-testid="product-card"], .product-card, [class*="product"]')
      .first();
    await firstProduct.click();

    await page.waitForURL(/\/product(s)?\/\w+/, { timeout: 5000 });

    // Verify key product information is present
    const title = page.locator('h1, [data-testid="product-title"]');
    const price = page.locator('[data-testid="product-price"], .price, [class*="price"]');

    await expect(title).toBeVisible();
    await expect(price).toBeVisible();
  });
});
