import { Page, Locator, expect } from '@playwright/test';

/**
 * Base Page Object Model
 * Provides common functionality for all page objects
 */
export abstract class BasePage {
  constructor(protected page: Page) {}

  // Common locators
  readonly loadingSpinner = () =>
    this.page.locator('[data-testid="loading"], .loading, [class*="spinner"]');
  readonly toastMessage = () => this.page.locator('[data-testid="toast"], [role="alert"], .toast');
  readonly errorMessage = () =>
    this.page.locator('[data-testid="error"], [class*="error"], [role="alert"]');
  readonly successMessage = () => this.page.locator('[data-testid="success"], [class*="success"]');

  // Navigation
  async goto(path: string): Promise<void> {
    await this.page.goto(path);
    await this.waitForPageLoad();
  }

  async waitForPageLoad(): Promise<void> {
    await this.page.waitForLoadState('networkidle');
    await this.loadingSpinner()
      .waitFor({ state: 'hidden', timeout: 10000 })
      .catch(() => {});
  }

  // Common actions
  async clickElement(locator: Locator): Promise<void> {
    await locator.waitFor({ state: 'visible', timeout: 5000 });
    await locator.click();
  }

  async fillInput(locator: Locator, value: string): Promise<void> {
    await locator.waitFor({ state: 'visible', timeout: 5000 });
    await locator.clear();
    await locator.fill(value);
  }

  // Assertions
  async expectToBeVisible(locator: Locator): Promise<void> {
    await expect(locator).toBeVisible({ timeout: 5000 });
  }

  async expectToHaveText(locator: Locator, text: string): Promise<void> {
    await expect(locator).toHaveText(text, { timeout: 5000 });
  }

  async expectToContainText(locator: Locator, text: string): Promise<void> {
    await expect(locator).toContainText(text, { timeout: 5000 });
  }

  async expectUrlToContain(path: string): Promise<void> {
    await expect(this.page).toHaveURL(new RegExp(path));
  }

  async expectToastMessage(text: string): Promise<void> {
    const toast = this.toastMessage();
    await this.expectToContainText(toast, text);
  }

  async expectErrorMessage(text: string): Promise<void> {
    const error = this.errorMessage();
    await this.expectToContainText(error, text);
  }

  // Wait helpers
  async waitForResponse(url: string | RegExp): Promise<void> {
    await this.page.waitForResponse((resp) =>
      typeof url === 'string' ? resp.url().includes(url) : url.test(resp.url())
    );
  }

  async waitForNavigation(): Promise<void> {
    await this.page.waitForLoadState('networkidle');
  }

  // Screenshot helper
  async takeScreenshot(name: string): Promise<void> {
    await this.page.screenshot({ path: `test-results/screenshots/${name}.png` });
  }
}
