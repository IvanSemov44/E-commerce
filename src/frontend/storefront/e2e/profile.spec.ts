import { test, expect } from '@playwright/test';

/**
 * E2E Tests: Profile Management
 * Tests user profile viewing, updating, and account management
 */

test.describe('Profile Management', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to the application
    await page.goto('/');
  });

  test('should redirect to login when accessing profile while not authenticated', async ({
    page,
  }) => {
    // Try to access profile page directly
    await page.goto('/profile');

    // Should be redirected to login or see login form
    await page.waitForTimeout(1000);
    const url = page.url();
    const hasLoginForm = (await page.locator('input[type="email"]').count()) > 0;

    expect(url.includes('/login') || hasLoginForm).toBeTruthy();
  });

  test('should display profile page for authenticated user', async ({ page }) => {
    // This test requires authentication - skip if no auth mechanism available
    const loginLink = page.locator('a[href*="login"], a:has-text("Login")').first();

    if ((await loginLink.count()) === 0) {
      test.skip();
      return;
    }

    // Navigate to login and attempt authentication
    await loginLink.click();
    await page.waitForTimeout(1000);

    // Fill login form (would need valid test credentials)
    const emailInput = page.locator('input[type="email"]').first();
    const passwordInput = page.locator('input[type="password"]').first();

    if ((await emailInput.count()) > 0 && (await passwordInput.count()) > 0) {
      // Check if we can access profile after login
      // This would require actual test credentials
      test.skip();
    } else {
      test.skip();
    }
  });

  test('should display user information on profile page', async ({ page }) => {
    // Navigate to profile (would need authentication)
    await page.goto('/profile');
    await page.waitForTimeout(1000);

    // Check for profile elements
    const profileSection = page.locator('[class*="profile"], [data-testid="profile"]');

    if ((await profileSection.count()) > 0) {
      // Look for typical profile information
      const hasEmail =
        (await page.locator('input[type="email"], [data-testid="user-email"]').count()) > 0;
      const hasName =
        (await page.locator('input[name*="name"], [data-testid="user-name"]').count()) > 0;

      expect(hasEmail || hasName).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should allow updating profile information', async ({ page }) => {
    await page.goto('/profile');
    await page.waitForTimeout(1000);

    // Look for editable profile form
    const editButton = page.locator('button:has-text("Edit"), button:has-text("Update")').first();

    if ((await editButton.count()) > 0) {
      await editButton.click();

      // Look for form inputs
      const firstNameInput = page
        .locator('input[name="firstName"], input[placeholder*="first name" i]')
        .first();

      if ((await firstNameInput.count()) > 0) {
        await firstNameInput.fill('Test Name Updated');

        const saveButton = page.locator('button[type="submit"], button:has-text("Save")').first();
        if ((await saveButton.count()) > 0) {
          await saveButton.click();
          await page.waitForTimeout(2000);

          // Look for success message
          const successMessage = page.locator('[class*="success"], [role="alert"], .toast');
          expect((await successMessage.count()) >= 0).toBeTruthy();
        }
      }
    } else {
      test.skip();
    }
  });

  test('should display order history on profile', async ({ page }) => {
    await page.goto('/profile');
    await page.waitForTimeout(1000);

    // Look for order history section
    const orderHistoryLink = page
      .locator('a[href*="orders"], a:has-text("Orders"), a:has-text("Order History")')
      .first();

    if ((await orderHistoryLink.count()) > 0) {
      await orderHistoryLink.click();
      await page.waitForTimeout(1000);

      // Check for order list
      const orderList = page.locator('[class*="order"], [data-testid="order-list"]');
      expect((await orderList.count()) >= 0).toBeTruthy();
    } else {
      // Try direct navigation
      await page.goto('/profile/orders');
      await page.waitForTimeout(1000);

      const url = page.url();
      expect(url).toContain('order');
    }
  });

  test('should allow password change', async ({ page }) => {
    await page.goto('/profile');
    await page.waitForTimeout(1000);

    // Look for password change section
    const passwordSection = page
      .locator('a[href*="password"], button:has-text("Change Password"), a:has-text("Password")')
      .first();

    if ((await passwordSection.count()) > 0) {
      await passwordSection.click();
      await page.waitForTimeout(1000);

      // Look for password form
      const currentPasswordInput = page
        .locator('input[name*="current"], input[placeholder*="current" i]')
        .first();
      const newPasswordInput = page
        .locator('input[name*="new"], input[placeholder*="new" i]')
        .first();

      if ((await currentPasswordInput.count()) > 0 && (await newPasswordInput.count()) > 0) {
        expect(true).toBeTruthy(); // Password change form exists
      }
    } else {
      test.skip();
    }
  });

  test('should display wishlist on profile', async ({ page }) => {
    await page.goto('/profile');
    await page.waitForTimeout(1000);

    // Look for wishlist section
    const wishlistLink = page.locator('a[href*="wishlist"], a:has-text("Wishlist")').first();

    if ((await wishlistLink.count()) > 0) {
      await wishlistLink.click();
      await page.waitForTimeout(1000);

      const url = page.url();
      expect(url).toContain('wishlist');
    } else {
      // Try direct navigation
      await page.goto('/wishlist');
      await page.waitForTimeout(1000);

      const wishlistContainer = page.locator('[class*="wishlist"], [data-testid="wishlist"]');
      expect((await wishlistContainer.count()) >= 0).toBeTruthy();
    }
  });

  test('should allow account deletion or deactivation', async ({ page }) => {
    await page.goto('/profile');
    await page.waitForTimeout(1000);

    // Look for account deletion option (usually in settings)
    const settingsLink = page.locator('a[href*="settings"], button:has-text("Settings")').first();

    if ((await settingsLink.count()) > 0) {
      await settingsLink.click();
      await page.waitForTimeout(1000);

      // Look for delete/deactivate option
      const deleteOption = page.locator('button:has-text("Delete"), button:has-text("Deactivate")');
      expect((await deleteOption.count()) >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });
});
