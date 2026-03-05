import { test as base, Page } from '@playwright/test';
import { LoginPage } from '../pages/LoginPage';

/**
 * Authentication Fixtures
 * Provides pre-authenticated page instances for tests
 */

// Environment variables for test credentials
const TEST_USER_EMAIL = process.env.E2E_TEST_USER_EMAIL || 'test@example.com';
const TEST_USER_PASSWORD = process.env.E2E_TEST_USER_PASSWORD || 'TestPassword123!';
const ADMIN_USER_EMAIL = process.env.E2E_ADMIN_USER_EMAIL || 'admin@example.com';
const ADMIN_USER_PASSWORD = process.env.E2E_ADMIN_USER_PASSWORD || 'AdminPassword123!';

// Define fixture types
type AuthFixtures = {
  authenticatedPage: Page;
  adminPage: Page;
  loginPage: LoginPage;
};

// Extend base test with authentication fixtures
export const test = base.extend<AuthFixtures>({
  // Regular authenticated user page
  authenticatedPage: async ({ page }, use) => {
    const loginPage = new LoginPage(page);
    await loginPage.navigate();
    await loginPage.login(TEST_USER_EMAIL, TEST_USER_PASSWORD);
    
    // Wait for successful login (redirect away from login page)
    await page.waitForURL(url => !url.pathname.includes('/login'), { timeout: 10000 });
    
    // eslint-disable-next-line react-hooks/rules-of-hooks
    await use(page);
  },

  // Admin authenticated page
  adminPage: async ({ page }, use) => {
    const loginPage = new LoginPage(page);
    await loginPage.navigate();
    await loginPage.login(ADMIN_USER_EMAIL, ADMIN_USER_PASSWORD);
    
    // Wait for admin dashboard
    await page.waitForURL(url => url.pathname.includes('/admin') || url.pathname.includes('/dashboard'), { 
      timeout: 10000 
    });
    
    // eslint-disable-next-line react-hooks/rules-of-hooks
    await use(page);
  },

  // Login page instance
  loginPage: async ({ page }, use) => {
    const loginPage = new LoginPage(page);
    // eslint-disable-next-line react-hooks/rules-of-hooks
    await use(loginPage);
  },
});

// Export expect for convenience
export { expect } from '@playwright/test';

// Helper function to manually authenticate a page
export async function authenticatePage(page: Page, email?: string, password?: string): Promise<void> {
  const loginPage = new LoginPage(page);
  await loginPage.navigate();
  await loginPage.login(email || TEST_USER_EMAIL, password || TEST_USER_PASSWORD);
  await page.waitForURL(url => !url.pathname.includes('/login'), { timeout: 10000 });
}

// Helper to check if user is authenticated
export async function isAuthenticated(page: Page): Promise<boolean> {
  // Check for authenticated state indicators
  const userMenu = page.locator('[data-testid="user-menu"], [class*="user-dropdown"], [aria-label*="account" i]');
  const loginLink = page.locator('a[href*="login"], a:has-text("Login")');
  
  const hasUserMenu = await userMenu.count() > 0;
  const hasLoginLink = await loginLink.count() > 0;
  
  return hasUserMenu && !hasLoginLink;
}

// Helper to logout
export async function logout(page: Page): Promise<void> {
  const logoutButton = page.locator('button:has-text("Logout"), a:has-text("Logout"), button:has-text("Sign Out")');
  
  if (await logoutButton.count() > 0) {
    await logoutButton.click();
    await page.waitForURL('/login', { timeout: 5000 }).catch(() => {});
  }
}
