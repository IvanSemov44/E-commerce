import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * Registration Page Object Model
 * Encapsulates registration page functionality and locators
 */
export class RegisterPage extends BasePage {
  constructor(page: Page) {
    super(page);
  }

  // Locators
  readonly firstNameInput = (): Locator =>
    this.page
      .getByTestId('first-name-input')
      .or(
        this.page.locator(
          'input[name*="firstName"], input[name*="first_name"], input[placeholder*="first" i]'
        )
      );

  readonly lastNameInput = (): Locator =>
    this.page
      .getByTestId('last-name-input')
      .or(
        this.page.locator(
          'input[name*="lastName"], input[name*="last_name"], input[placeholder*="last" i]'
        )
      );

  readonly emailInput = (): Locator =>
    this.page
      .getByTestId('email-input')
      .or(this.page.locator('input[type="email"], input[name*="email"]'));

  readonly passwordInput = (): Locator =>
    this.page
      .getByTestId('password-input')
      .or(this.page.locator('input[type="password"], input[name*="password"]'));

  readonly confirmPasswordInput = (): Locator =>
    this.page
      .getByTestId('confirm-password-input')
      .or(this.page.locator('input[name*="confirm"], input[placeholder*="confirm" i]'));

  readonly registerButton = (): Locator =>
    this.page
      .getByTestId('register-button')
      .or(
        this.page.locator(
          'button[type="submit"], button:has-text("Register"), button:has-text("Sign Up")'
        )
      );

  readonly loginLink = (): Locator =>
    this.page
      .getByTestId('login-link')
      .or(this.page.locator('a[href*="login"], a:has-text("Login"), a:has-text("Sign In")'));

  readonly termsCheckbox = (): Locator =>
    this.page
      .getByTestId('terms-checkbox')
      .or(this.page.locator('input[type="checkbox"][name*="terms"]'));

  // Navigation
  async navigate(): Promise<void> {
    await this.goto('/register');
    await this.waitForFormToLoad();
  }

  async waitForFormToLoad(): Promise<void> {
    await this.emailInput().waitFor({ state: 'visible', timeout: 10000 });
  }

  // Actions
  async register(data: {
    firstName: string;
    lastName: string;
    email: string;
    password: string;
    confirmPassword?: string;
    acceptTerms?: boolean;
  }): Promise<void> {
    await this.fillInput(this.firstNameInput(), data.firstName);
    await this.fillInput(this.lastNameInput(), data.lastName);
    await this.fillInput(this.emailInput(), data.email);
    await this.fillInput(this.passwordInput(), data.password);

    if (data.confirmPassword) {
      await this.fillInput(this.confirmPasswordInput(), data.confirmPassword);
    } else {
      await this.fillInput(this.confirmPasswordInput(), data.password);
    }

    if (data.acceptTerms !== false) {
      const checkbox = this.termsCheckbox();
      if ((await checkbox.count()) > 0) {
        await checkbox.check();
      }
    }

    await this.clickElement(this.registerButton());
  }

  async navigateToLogin(): Promise<void> {
    await this.clickElement(this.loginLink());
  }

  // Form validation helpers
  async submitEmptyForm(): Promise<void> {
    await this.registerButton().click();
  }

  async fillEmailOnly(email: string): Promise<void> {
    await this.fillInput(this.emailInput(), email);
    await this.registerButton().click();
  }

  // Assertions
  async expectRegisterFormVisible(): Promise<void> {
    await this.expectToBeVisible(this.emailInput());
    await this.expectToBeVisible(this.passwordInput());
    await this.expectToBeVisible(this.registerButton());
  }

  async expectPasswordMismatchError(): Promise<void> {
    const error = this.page.locator(
      '[data-testid="confirm-password-error"], [class*="password-match"], [class*="mismatch"]'
    );
    await this.expectToBeVisible(error);
  }

  async expectWeakPasswordError(): Promise<void> {
    const error = this.page.locator(
      '[data-testid="password-strength"], [class*="weak"], [class*="strength"]'
    );
    await this.expectToBeVisible(error);
  }

  async expectEmailAlreadyExistsError(): Promise<void> {
    await this.expectErrorMessage('already exists');
  }

  async expectSuccessfulRegistration(): Promise<void> {
    // Should redirect or show success message
    const currentUrl = this.page.url();
    const hasSuccessMessage = (await this.successMessage().count()) > 0;
    const redirectedFromRegister = !currentUrl.includes('/register');

    expect(hasSuccessMessage || redirectedFromRegister).toBeTruthy();
  }

  async expectOnRegisterPage(): Promise<void> {
    await this.expectUrlToContain('/register');
  }

  // Password strength indicator
  async getPasswordStrength(): Promise<string | null> {
    const strengthIndicator = this.page.locator(
      '[data-testid="password-strength"], [class*="strength-indicator"]'
    );
    if ((await strengthIndicator.count()) > 0) {
      return (
        (await strengthIndicator.getAttribute('data-strength')) ||
        (await strengthIndicator.textContent())
      );
    }
    return null;
  }
}
