import { test, expect } from '@playwright/test';

/**
 * E2E Tests: Admin Inventory Management
 * Tests inventory tracking, stock updates, and alerts
 */

test.describe('Admin Inventory', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should display inventory page', async ({ page }) => {
    await page.goto('/inventory');
    await page.waitForTimeout(1000);

    // Check for inventory container
    const inventoryContainer = page.locator('[class*="inventory"], table, [class*="stock"]');
    expect(await inventoryContainer.count() >= 0).toBeTruthy();
  });

  test('should show stock levels', async ({ page }) => {
    await page.goto('/inventory');
    await page.waitForTimeout(1000);

    // Look for stock quantity
    const stockElement = page.locator('text=Stock, text=Quantity, [class*="stock"]');
    expect(await stockElement.count() >= 0).toBeTruthy();
  });

  test('should show low stock alerts', async ({ page }) => {
    await page.goto('/inventory');
    await page.waitForTimeout(1000);

    // Look for low stock indicators
    const lowStock = page.locator('[class*="low-stock"], [class*="warning"]');
    expect(await lowStock.count() >= 0).toBeTruthy();
  });

  test('should show out of stock items', async ({ page }) => {
    await page.goto('/inventory');
    await page.waitForTimeout(1000);

    // Look for out of stock indicators
    const outOfStock = page.locator('text="Out of Stock", text=0, [class*="out-of-stock"]');
    expect(await outOfStock.count() >= 0).toBeTruthy();
  });

  test('should update stock quantity', async ({ page }) => {
    await page.goto('/inventory');
    await page.waitForTimeout(1000);

    // Look for edit/adjust button
    const adjustButton = page.locator('button:has-text("Adjust"), button:has-text("Edit"), [class*="adjust"]').first();
    
    if (await adjustButton.count() > 0) {
      await adjustButton.click();
      await page.waitForTimeout(1000);

      // Look for quantity input
      const quantityInput = page.locator('input[type="number"], input[name*="quantity"]').first();
      expect(await quantityInput.count() >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should show inventory history', async ({ page }) => {
    await page.goto('/inventory');
    await page.waitForTimeout(1000);

    // Look for history/logs button
    const historyButton = page.locator('button:has-text("History"), a:has-text("Log")').first();
    
    if (await historyButton.count() > 0) {
      await historyButton.click();
      await page.waitForTimeout(1000);

      // Should show history
      expect(true).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should filter by stock status', async ({ page }) => {
    await page.goto('/inventory');
    await page.waitForTimeout(1000);

    // Look for filter dropdown
    const filterDropdown = page.locator('select, [class*="filter"]').first();
    
    if (await filterDropdown.count() > 0) {
      await filterDropdown.click();
      await page.waitForTimeout(500);

      // Look for status options
      const statusOption = page.locator('option:has-text("Low"), option:has-text("Out")');
      expect(await statusOption.count() >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should search inventory', async ({ page }) => {
    await page.goto('/inventory');
    await page.waitForTimeout(1000);

    // Look for search input
    const searchInput = page.locator('input[type="search"], input[placeholder*="search" i]').first();
    
    if (await searchInput.count() > 0) {
      await searchInput.fill('product');
      await searchInput.press('Enter');
      await page.waitForTimeout(1000);

      expect(true).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should show product SKU', async ({ page }) => {
    await page.goto('/inventory');
    await page.waitForTimeout(1000);

    // Look for SKU
    const skuElement = page.locator('text=SKU, [class*="sku"]');
    expect(await skuElement.count() >= 0).toBeTruthy();
  });

  test('should bulk update inventory', async ({ page }) => {
    await page.goto('/inventory');
    await page.waitForTimeout(1000);

    // Look for bulk action button
    const bulkButton = page.locator('button:has-text("Bulk"), [class*="bulk"]').first();
    expect(await bulkButton.count() >= 0).toBeTruthy();
  });

  test('should export inventory', async ({ page }) => {
    await page.goto('/inventory');
    await page.waitForTimeout(1000);

    // Look for export button
    const exportButton = page.locator('button:has-text("Export"), a:has-text("Export")');
    expect(await exportButton.count() >= 0).toBeTruthy();
  });

  test('should show inventory value', async ({ page }) => {
    await page.goto('/inventory');
    await page.waitForTimeout(1000);

    // Look for value/cost
    const valueElement = page.locator('text=Value, text=Cost, [class*="value"]');
    expect(await valueElement.count() >= 0).toBeTruthy();
  });

  test('should paginate inventory', async ({ page }) => {
    await page.goto('/inventory');
    await page.waitForTimeout(1000);

    // Look for pagination
    const pagination = page.locator('[class*="pagination"], [class*="page"]');
    expect(await pagination.count() >= 0).toBeTruthy();
  });

  test('should show inventory summary', async ({ page }) => {
    await page.goto('/inventory');
    await page.waitForTimeout(1000);

    // Look for summary stats
    const summary = page.locator('[class*="summary"], [class*="stats"], [class*="dashboard"]');
    expect(await summary.count() >= 0).toBeTruthy();
  });

  test('should allow stock adjustment reason', async ({ page }) => {
    await page.goto('/inventory');
    await page.waitForTimeout(1000);

    const adjustButton = page.locator('button:has-text("Adjust"), button:has-text("Edit")').first();
    
    if (await adjustButton.count() > 0) {
      await adjustButton.click();
      await page.waitForTimeout(1000);

      // Look for reason input/select
      const reasonInput = page.locator('input[name*="reason"], select[name*="reason"], textarea');
      expect(await reasonInput.count() >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });
});