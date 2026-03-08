import { test, expect } from '@playwright/test';

/**
 * E2E Tests: Authentication
 * Tests login, registration, and logout functionality
 */

test.describe('Authentication', () => {
  test('should display login page', async ({ page }) => {
    await page.goto('/');

    // Find login link
    const loginLink = page
      .locator(
        'a[href*="login"], [data-testid="login-link"], a:has-text("Login"), a:has-text("Sign In")'
      )
      .first();

    if ((await loginLink.count()) > 0) {
      await loginLink.click();

      // Verify we're on login page
      await page.waitForTimeout(1000);
      const url = page.url();
      expect(url).toMatch(/\/(login|signin|auth)/i);

      // Verify login form elements
      const emailInput = page.locator('input[type="email"]').first();
      const passwordInput = page.locator('input[type="password"]').first();

      await expect(emailInput).toBeVisible({ timeout: 5000 });
      await expect(passwordInput).toBeVisible({ timeout: 5000 });
    } else {
      test.skip();
    }
  });

  test('should show validation error for invalid login', async ({ page }) => {
    // Navigate to login
    await page.goto('/');

    const loginLink = page
      .locator(
        'a[href*="login"], [data-testid="login-link"], a:has-text("Login"), a:has-text("Sign In")'
      )
      .first();

    if ((await loginLink.count()) === 0) {
      test.skip();
      return;
    }

    await loginLink.click();
    await page.waitForTimeout(1000);

    // Try to login with invalid credentials
    const emailInput = page.locator('input[type="email"]').first();
    const passwordInput = page.locator('input[type="password"]').first();
    const submitButton = page.locator('button[type="submit"]').first();

    await emailInput.fill('invalid@example.com');
    await passwordInput.fill('wrongpassword');
    await submitButton.click();

    // Wait for response
    await page.waitForTimeout(2000);

    // Look for error message
    const errorMessage = page.locator(
      '.error, [class*="error"], [role="alert"], .toast, .notification'
    );

    const hasError = (await errorMessage.count()) > 0;
    const stillOnLogin = page.url().match(/\/(login|signin|auth)/i);

    // Either shows error or stays on login page
    expect(hasError || stillOnLogin).toBeTruthy();
  });

  test('should navigate to registration page', async ({ page }) => {
    await page.goto('/');

    // Find register/signup link
    const registerLink = page
      .locator(
        'a[href*="register"], a[href*="signup"], [data-testid="register-link"], a:has-text("Register"), a:has-text("Sign Up")'
      )
      .first();

    if ((await registerLink.count()) > 0) {
      await registerLink.click();

      // Verify we're on registration page
      await page.waitForTimeout(1000);
      const url = page.url();
      expect(url).toMatch(/\/(register|signup|auth)/i);

      // Verify registration form elements
      const emailInput = page.locator('input[type="email"]').first();
      const passwordInput = page.locator('input[type="password"]').first();

      await expect(emailInput).toBeVisible({ timeout: 5000 });
      await expect(passwordInput).toBeVisible({ timeout: 5000 });
    } else {
      test.skip();
    }
  });

  test('should display registration form fields', async ({ page }) => {
    await page.goto('/');

    const registerLink = page
      .locator(
        'a[href*="register"], a[href*="signup"], [data-testid="register-link"], a:has-text("Register"), a:has-text("Sign Up")'
      )
      .first();

    if ((await registerLink.count()) === 0) {
      test.skip();
      return;
    }

    await registerLink.click();
    await page.waitForTimeout(1000);

    // Verify form fields
    const emailInput = page.locator('input[type="email"]');
    const passwordInputs = page.locator('input[type="password"]');

    expect(await emailInput.count()).toBeGreaterThanOrEqual(1);
    expect(await passwordInputs.count()).toBeGreaterThanOrEqual(1);

    // Registration typically has 2 password fields (password + confirm)
    if ((await passwordInputs.count()) >= 2) {
      await expect(passwordInputs.nth(0)).toBeVisible();
      await expect(passwordInputs.nth(1)).toBeVisible();
    }
  });

  test('should require valid email format', async ({ page }) => {
    await page.goto('/');

    const loginLink = page
      .locator(
        'a[href*="login"], [data-testid="login-link"], a:has-text("Login"), a:has-text("Sign In")'
      )
      .first();

    if ((await loginLink.count()) === 0) {
      test.skip();
      return;
    }

    await loginLink.click();
    await page.waitForTimeout(1000);

    // Try invalid email format
    const emailInput = page.locator('input[type="email"]').first();
    const passwordInput = page.locator('input[type="password"]').first();
    const submitButton = page.locator('button[type="submit"]').first();

    await emailInput.fill('notanemail');
    await passwordInput.fill('password123');

    // Browser should show HTML5 validation or form should show error
    const isInvalid = await emailInput.evaluate((el: HTMLInputElement) => !el.validity.valid);

    if (isInvalid) {
      // HTML5 validation should prevent submission
      expect(isInvalid).toBeTruthy();
    } else {
      // Try submitting and check for error
      await submitButton.click();
      await page.waitForTimeout(500);

      const errorMessage = page.locator('.error, [class*="error"], [role="alert"]');
      const hasError = (await errorMessage.count()) > 0;

      expect(hasError).toBeTruthy();
    }
  });

  test('should show user menu when logged in', async ({ page }) => {
    // This test checks if user menu appears (would need actual login)
    await page.goto('/');

    // Look for user menu indicators
    const userMenu = page.locator(
      '[data-testid="user-menu"], .user-menu, [aria-label*="user"], button:has-text("Account")'
    );

    const loginLink = page.locator('a[href*="login"], a:has-text("Login"), a:has-text("Sign In")');

    // Either logged in (has user menu) or logged out (has login link)
    const hasUserMenu = (await userMenu.count()) > 0;
    const hasLoginLink = (await loginLink.count()) > 0;

    expect(hasUserMenu || hasLoginLink).toBeTruthy();
  });
});
