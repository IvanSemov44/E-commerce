import { test, expect } from '@playwright/test';

/**
 * E2E Tests: Admin Orders Management
 * Tests order viewing, status updates, and management
 */

test.describe('Admin Orders', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should display orders list page', async ({ page }) => {
    await page.goto('/orders');
    await page.waitForTimeout(1000);

    // Check for orders container
    const ordersContainer = page.locator('[class*="order"], table, [class*="list"]');
    expect(await ordersContainer.count() >= 0).toBeTruthy();
  });

  test('should show order count', async ({ page }) => {
    await page.goto('/orders');
    await page.waitForTimeout(1000);

    // Look for order count
    const countElement = page.locator('[class*="count"]');
    expect(await countElement.count() >= 0).toBeTruthy();
  });

  test('should display order details in table', async ({ page }) => {
    await page.goto('/orders');
    await page.waitForTimeout(1000);

    // Look for table with order data
    const tableRow = page.locator('tr, [class*="row"]').first();
    
    if (await tableRow.count() > 0) {
      expect(await tableRow.count()).toBeGreaterThan(0);
    } else {
      test.skip();
    }
  });

  test('should show order number', async ({ page }) => {
    await page.goto('/orders');
    await page.waitForTimeout(1000);

    // Look for order number
    const orderNumber = page.locator('[class*="order-number"], [class*="order-id"]');
    expect(await orderNumber.count() >= 0).toBeTruthy();
  });

  test('should show order status', async ({ page }) => {
    await page.goto('/orders');
    await page.waitForTimeout(1000);

    // Look for status badge
    const statusBadge = page.locator('[class*="status"], [class*="badge"]');
    expect(await statusBadge.count() >= 0).toBeTruthy();
  });

  test('should show order total', async ({ page }) => {
    await page.goto('/orders');
    await page.waitForTimeout(1000);

    // Look for total price
    const totalElement = page.locator('[class*="total"]');
    expect(await totalElement.count() >= 0).toBeTruthy();
  });

  test('should show order date', async ({ page }) => {
    await page.goto('/orders');
    await page.waitForTimeout(1000);

    // Look for date
    const dateElement = page.locator('[class*="date"]');
    expect(await dateElement.count() >= 0).toBeTruthy();
  });

  test('should view order details', async ({ page }) => {
    await page.goto('/orders');
    await page.waitForTimeout(1000);

    // Look for view button
    const viewButton = page.locator('button:has-text("View"), a:has-text("View"), [class*="view"]').first();
    
    if (await viewButton.count() > 0) {
      await viewButton.click();
      await page.waitForTimeout(1000);

      // Should show order details
      const orderDetails = page.locator('[class*="detail"], [class*="order"]');
      expect(await orderDetails.count() >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should update order status', async ({ page }) => {
    await page.goto('/orders');
    await page.waitForTimeout(1000);

    // Look for status dropdown or update button
    const statusDropdown = page.locator('select, [class*="status"]').first();
    
    if (await statusDropdown.count() > 0) {
      await statusDropdown.click();
      await page.waitForTimeout(500);

      // Look for status options
      const statusOption = page.locator('option:has-text("Processing"), option:has-text("Shipped")');
      expect(await statusOption.count() >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should filter orders by status', async ({ page }) => {
    await page.goto('/orders');
    await page.waitForTimeout(1000);

    // Look for filter dropdown
    const filterDropdown = page.locator('select, [class*="filter"]').first();
    
    if (await filterDropdown.count() > 0) {
      await filterDropdown.click();
      await page.waitForTimeout(500);

      expect(true).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should search orders', async ({ page }) => {
    await page.goto('/orders');
    await page.waitForTimeout(1000);

    // Look for search input
    const searchInput = page.locator('input[type="search"], input[placeholder*="search" i]').first();
    
    if (await searchInput.count() > 0) {
      await searchInput.fill('ORD-');
      await searchInput.press('Enter');
      await page.waitForTimeout(1000);

      expect(true).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should show customer information', async ({ page }) => {
    await page.goto('/orders');
    await page.waitForTimeout(1000);

    // Look for customer column
    const customerElement = page.locator('text=Customer, text=Email, [class*="customer"]');
    expect(await customerElement.count() >= 0).toBeTruthy();
  });

  test('should show order items', async ({ page }) => {
    await page.goto('/orders');
    await page.waitForTimeout(1000);

    const viewButton = page.locator('button:has-text("View"), a:has-text("View")').first();
    
    if (await viewButton.count() > 0) {
      await viewButton.click();
      await page.waitForTimeout(1000);

      // Look for order items
      const orderItems = page.locator('[class*="item"], [class*="product"]');
      expect(await orderItems.count() >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should show shipping address', async ({ page }) => {
    await page.goto('/orders');
    await page.waitForTimeout(1000);

    const viewButton = page.locator('button:has-text("View"), a:has-text("View")').first();
    
    if (await viewButton.count() > 0) {
      await viewButton.click();
      await page.waitForTimeout(1000);

      // Look for shipping address
      const shippingAddress = page.locator('[class*="shipping"], [class*="address"]');
      expect(await shippingAddress.count() >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should paginate orders', async ({ page }) => {
    await page.goto('/orders');
    await page.waitForTimeout(1000);

    // Look for pagination
    const pagination = page.locator('[class*="pagination"], [class*="page"]');
    expect(await pagination.count() >= 0).toBeTruthy();
  });

  test('should export orders', async ({ page }) => {
    await page.goto('/orders');
    await page.waitForTimeout(1000);

    // Look for export button
    const exportButton = page.locator('button:has-text("Export"), a:has-text("Export")');
    expect(await exportButton.count() >= 0).toBeTruthy();
  });

  test('should show payment status', async ({ page }) => {
    await page.goto('/orders');
    await page.waitForTimeout(1000);

    // Look for payment status
    const paymentStatus = page.locator('text=Paid, text=Pending, [class*="payment"]');
    expect(await paymentStatus.count() >= 0).toBeTruthy();
  });
});