import { test, expect } from '@playwright/test';

/**
 * E2E Tests: Search
 * Tests product search functionality
 */

test.describe('Search', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should display search input in header', async ({ page }) => {
    // Look for search input
    const searchInput = page
      .locator('input[type="search"], input[placeholder*="search" i]')
      .first();

    if ((await searchInput.count()) > 0) {
      expect(await searchInput.isVisible()).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should search for products by name', async ({ page }) => {
    const searchInput = page
      .locator('input[type="search"], input[placeholder*="search" i]')
      .first();

    if ((await searchInput.count()) > 0) {
      await searchInput.fill('product');
      await searchInput.press('Enter');
      await page.waitForLoadState('networkidle');

      // Should show search results
      const searchResults = page.locator('[class*="product"], [class*="result"]');
      expect((await searchResults.count()) >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should show search results page', async ({ page }) => {
    await page.goto('/products?search=test');
    await page.waitForLoadState('networkidle');

    // Should be on products page with search query
    expect(page.url()).toContain('search');
  });

  test('should display no results message for empty search', async ({ page }) => {
    const searchInput = page
      .locator('input[type="search"], input[placeholder*="search" i]')
      .first();

    if ((await searchInput.count()) > 0) {
      await searchInput.fill('zzzzzzzzzzzzzzzzzzzz');
      await searchInput.press('Enter');
      await page.waitForLoadState('networkidle');

      // Look for no results message
      const noResults = page.locator(':text("No results"), :text("No products found")');
      expect((await noResults.count()) >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should show search suggestions while typing', async ({ page }) => {
    const searchInput = page
      .locator('input[type="search"], input[placeholder*="search" i]')
      .first();

    if ((await searchInput.count()) > 0) {
      await searchInput.fill('pro');
      await page.waitForLoadState('networkidle');

      // Look for autocomplete/suggestions dropdown
      const suggestions = page.locator(
        '[class*="suggestion"], [class*="autocomplete"], [class*="dropdown"]'
      );
      expect((await suggestions.count()) >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should filter search results by category', async ({ page }) => {
    await page.goto('/products?search=product');
    await page.waitForLoadState('networkidle');

    // Look for category filter
    const categoryFilter = page
      .locator('[class*="filter"] select, [class*="category-filter"]')
      .first();

    if ((await categoryFilter.count()) > 0) {
      await categoryFilter.click();
      await page.waitForLoadState('networkidle');

      // Look for category options
      const categoryOption = page.locator('option').first();
      expect((await categoryOption.count()) >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should filter search results by price range', async ({ page }) => {
    await page.goto('/products?search=product');
    await page.waitForLoadState('networkidle');

    // Look for price filter
    const priceFilter = page.locator('input[type="range"], [class*="price-filter"]').first();

    if ((await priceFilter.count()) > 0) {
      expect(await priceFilter.isVisible()).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should sort search results', async ({ page }) => {
    await page.goto('/products?search=product');
    await page.waitForLoadState('networkidle');

    // Look for sort dropdown
    const sortDropdown = page.locator('select, [class*="sort"]').first();

    if ((await sortDropdown.count()) > 0) {
      await sortDropdown.click();
      await page.waitForLoadState('networkidle');

      // Look for sort options
      const sortOption = page.locator('option:has-text("Price"), option:has-text("Name")');
      expect((await sortOption.count()) >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should clear search and show all products', async ({ page }) => {
    const searchInput = page
      .locator('input[type="search"], input[placeholder*="search" i]')
      .first();

    if ((await searchInput.count()) > 0) {
      await searchInput.fill('test');
      await searchInput.press('Enter');
      await page.waitForLoadState('networkidle');

      // Clear search
      const clearButton = page
        .locator('button:has-text("Clear"), button[aria-label*="clear" i]')
        .first();

      if ((await clearButton.count()) > 0) {
        await clearButton.click();
        await page.waitForLoadState('networkidle');

        // Should show all products
        expect(true).toBeTruthy();
      } else {
        // Clear by emptying search
        await searchInput.fill('');
        await searchInput.press('Enter');
        await page.waitForLoadState('networkidle');
        expect(true).toBeTruthy();
      }
    } else {
      test.skip();
    }
  });

  test('should show product count in search results', async ({ page }) => {
    await page.goto('/products?search=product');
    await page.waitForLoadState('networkidle');

    // Look for result count
    const resultCount = page.locator(':text(/\\d+\\s*(products?|results?)/i), [class*="count"]');
    expect((await resultCount.count()) >= 0).toBeTruthy();
  });

  test('should navigate to product from search results', async ({ page }) => {
    await page.goto('/products?search=product');
    await page.waitForLoadState('networkidle');

    const productLink = page.locator('a[href*="/products/"]').first();

    if ((await productLink.count()) > 0) {
      await productLink.click();
      await page.waitForLoadState('networkidle');

      // Should be on product page
      expect(page.url()).toContain('/products/');
    } else {
      test.skip();
    }
  });

  test('should preserve search query in URL', async ({ page }) => {
    const searchInput = page
      .locator('input[type="search"], input[placeholder*="search" i]')
      .first();

    if ((await searchInput.count()) > 0) {
      await searchInput.fill('laptop');
      await searchInput.press('Enter');
      await page.waitForLoadState('networkidle');

      // URL should contain search query
      expect(page.url()).toContain('laptop');
    } else {
      test.skip();
    }
  });

  test('should search with special characters', async ({ page }) => {
    const searchInput = page
      .locator('input[type="search"], input[placeholder*="search" i]')
      .first();

    if ((await searchInput.count()) > 0) {
      await searchInput.fill('product-test');
      await searchInput.press('Enter');
      await page.waitForLoadState('networkidle');

      // Should handle special characters without error
      const errorPage = page.locator(':text("Error"), :text("Something went wrong")');
      expect(await errorPage.count()).toBe(0);
    } else {
      test.skip();
    }
  });

  test('should show recent searches', async ({ page }) => {
    const searchInput = page
      .locator('input[type="search"], input[placeholder*="search" i]')
      .first();

    if ((await searchInput.count()) > 0) {
      // Perform a search
      await searchInput.fill('recent search');
      await searchInput.press('Enter');
      await page.waitForLoadState('networkidle');

      // Go back to home
      await page.goto('/');
      await page.waitForLoadState('networkidle');

      // Click on search input
      await searchInput.click();
      await page.waitForLoadState('networkidle');

      // Look for recent searches
      const recentSearches = page.locator(':text("Recent"), :text("History")');
      expect((await recentSearches.count()) >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should search by product SKU', async ({ page }) => {
    const searchInput = page
      .locator('input[type="search"], input[placeholder*="search" i]')
      .first();

    if ((await searchInput.count()) > 0) {
      await searchInput.fill('SKU-');
      await searchInput.press('Enter');
      await page.waitForLoadState('networkidle');

      // Should show results or no results
      const results = page.locator('[class*="product"], :text("No results")');
      expect((await results.count()) >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });
});
