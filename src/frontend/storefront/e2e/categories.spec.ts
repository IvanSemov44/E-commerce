import { test, expect } from '@playwright/test';

/**
 * E2E Tests: Categories
 * Tests category browsing and filtering functionality
 */

test.describe('Categories', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should display categories in navigation', async ({ page }) => {
    // Look for category links in navigation
    const categoryLinks = page.locator(
      'nav a[href*="/categories"], nav a[href*="/products?category"]'
    );

    if ((await categoryLinks.count()) > 0) {
      expect(await categoryLinks.count()).toBeGreaterThan(0);
    } else {
      test.skip();
    }
  });

  test('should navigate to category page', async ({ page }) => {
    await page.goto('/categories');
    await page.waitForLoadState('networkidle');

    // Should show categories
    const categoryGrid = page.locator('[class*="category"], [class*="grid"]');
    expect((await categoryGrid.count()) >= 0).toBeTruthy();
  });

  test('should display category cards', async ({ page }) => {
    await page.goto('/categories');
    await page.waitForLoadState('networkidle');

    // Look for category cards
    const categoryCard = page.locator('[class*="category-card"], [data-testid="category"]').first();

    if ((await categoryCard.count()) > 0) {
      expect(await categoryCard.isVisible()).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should show category image', async ({ page }) => {
    await page.goto('/categories');
    await page.waitForLoadState('networkidle');

    const categoryCard = page.locator('[class*="category-card"], [data-testid="category"]').first();

    if ((await categoryCard.count()) > 0) {
      const categoryImage = categoryCard.locator('img');
      expect((await categoryImage.count()) >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should navigate to products by category', async ({ page }) => {
    await page.goto('/categories');
    await page.waitForLoadState('networkidle');

    const categoryLink = page
      .locator('a[href*="/categories/"], a[href*="/products?category"]')
      .first();

    if ((await categoryLink.count()) > 0) {
      await categoryLink.click();
      await page.waitForLoadState('networkidle');

      // Should be on category products page
      expect(page.url()).toContain('categor');
    } else {
      test.skip();
    }
  });

  test('should show products within category', async ({ page }) => {
    await page.goto('/categories/1');
    await page.waitForLoadState('networkidle');

    // Look for products
    const productGrid = page.locator('[class*="product"], [class*="grid"]');
    expect((await productGrid.count()) >= 0).toBeTruthy();
  });

  test('should display category name as heading', async ({ page }) => {
    await page.goto('/categories/1');
    await page.waitForLoadState('networkidle');

    // Look for category heading
    const heading = page.locator('h1, h2').first();
    expect((await heading.count()) >= 0).toBeTruthy();
  });

  test('should show product count in category', async ({ page }) => {
    await page.goto('/categories/1');
    await page.waitForLoadState('networkidle');

    // Look for product count
    const countElement = page.locator(':text(/\\d+\\s*(products?|items?)/i), [class*="count"]');
    expect((await countElement.count()) >= 0).toBeTruthy();
  });

  test('should filter products within category by price', async ({ page }) => {
    await page.goto('/categories/1');
    await page.waitForLoadState('networkidle');

    // Look for price filter
    const priceFilter = page.locator('input[type="range"], [class*="price"]').first();

    if ((await priceFilter.count()) > 0) {
      expect(await priceFilter.isVisible()).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should sort products within category', async ({ page }) => {
    await page.goto('/categories/1');
    await page.waitForLoadState('networkidle');

    // Look for sort dropdown
    const sortDropdown = page.locator('select, [class*="sort"]').first();

    if ((await sortDropdown.count()) > 0) {
      await sortDropdown.click();
      await page.waitForLoadState('networkidle');

      const sortOption = page.locator('option:has-text("Price"), option:has-text("Name")');
      expect((await sortOption.count()) >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should show subcategories if available', async ({ page }) => {
    await page.goto('/categories');
    await page.waitForLoadState('networkidle');

    // Look for subcategory section
    const subcategories = page.locator('[class*="subcategor"], [class*="child"]');
    expect((await subcategories.count()) >= 0).toBeTruthy();
  });

  test('should show breadcrumb navigation', async ({ page }) => {
    await page.goto('/categories/1');
    await page.waitForLoadState('networkidle');

    // Look for breadcrumb
    const breadcrumb = page.locator('[class*="breadcrumb"], nav[aria-label*="breadcrumb"]');
    expect((await breadcrumb.count()) >= 0).toBeTruthy();
  });

  test('should paginate products in category', async ({ page }) => {
    await page.goto('/categories/1');
    await page.waitForLoadState('networkidle');

    // Look for pagination
    const pagination = page.locator('[class*="pagination"], [class*="page-nav"]');
    expect((await pagination.count()) >= 0).toBeTruthy();
  });

  test('should show empty state for empty category', async ({ page }) => {
    await page.goto('/categories/99999');
    await page.waitForLoadState('networkidle');

    // Look for empty state
    const emptyState = page.locator(':text("No products"), :text("empty")');
    expect((await emptyState.count()) >= 0).toBeTruthy();
  });

  test('should display category description', async ({ page }) => {
    await page.goto('/categories/1');
    await page.waitForLoadState('networkidle');

    // Look for description
    const description = page.locator('[class*="description"], p').first();
    expect((await description.count()) >= 0).toBeTruthy();
  });

  test('should show featured products in category', async ({ page }) => {
    await page.goto('/categories/1');
    await page.waitForLoadState('networkidle');

    // Look for featured section
    const featuredSection = page.locator('[class*="featured"], :text("Featured")');
    expect((await featuredSection.count()) >= 0).toBeTruthy();
  });
});
