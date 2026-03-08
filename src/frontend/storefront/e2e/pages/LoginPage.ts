import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * Login Page Object Model
 * Encapsulates login page functionality and locators
 */
export class LoginPage extends BasePage {
  constructor(page: Page) {
    super(page);
  }

  // Locators using data-testid for reliability
  readonly emailInput = (): Locator =>
    this.page
      .getByTestId('email-input')
      .or(this.page.locator('input[type="email"], input[name*="email"]'));

  readonly passwordInput = (): Locator =>
    this.page
      .getByTestId('password-input')
      .or(this.page.locator('input[type="password"], input[name*="password"]'));

  readonly loginButton = (): Locator =>
    this.page
      .getByTestId('login-button')
      .or(
        this.page.locator(
          'button[type="submit"], button:has-text("Login"), button:has-text("Sign In")'
        )
      );

  readonly registerLink = (): Locator =>
    this.page
      .getByTestId('register-link')
      .or(this.page.locator('a[href*="register"], a:has-text("Register"), a:has-text("Sign Up")'));

  readonly forgotPasswordLink = (): Locator =>
    this.page
      .getByTestId('forgot-password-link')
      .or(this.page.locator('a[href*="forgot"], a:has-text("Forgot"), a:has-text("Reset")'));

  readonly rememberMeCheckbox = (): Locator =>
    this.page
      .getByTestId('remember-me')
      .or(this.page.locator('input[type="checkbox"][name*="remember"]'));

  readonly showPasswordButton = (): Locator =>
    this.page.getByTestId('show-password').or(this.page.locator('button[aria-label*="password"]'));

  // Navigation
  async navigate(): Promise<void> {
    await this.goto('/login');
    await this.waitForFormToLoad();
  }

  async waitForFormToLoad(): Promise<void> {
    await this.emailInput().waitFor({ state: 'visible', timeout: 10000 });
  }

  // Actions
  async login(email: string, password: string): Promise<void> {
    await this.fillInput(this.emailInput(), email);
    await this.fillInput(this.passwordInput(), password);
    await this.clickElement(this.loginButton());
  }

  async loginWithRememberMe(email: string, password: string): Promise<void> {
    await this.fillInput(this.emailInput(), email);
    await this.fillInput(this.passwordInput(), password);
    await this.rememberMeCheckbox().check();
    await this.clickElement(this.loginButton());
  }

  async navigateToRegister(): Promise<void> {
    await this.clickElement(this.registerLink());
  }

  async navigateToForgotPassword(): Promise<void> {
    await this.clickElement(this.forgotPasswordLink());
  }

  async togglePasswordVisibility(): Promise<void> {
    await this.clickElement(this.showPasswordButton());
  }

  // Form validation helpers
  async fillEmailOnly(email: string): Promise<void> {
    await this.fillInput(this.emailInput(), email);
    await this.loginButton().click();
  }

  async fillPasswordOnly(password: string): Promise<void> {
    await this.fillInput(this.passwordInput(), password);
    await this.loginButton().click();
  }

  async submitEmptyForm(): Promise<void> {
    await this.loginButton().click();
  }

  // Assertions
  async expectLoginFormVisible(): Promise<void> {
    await this.expectToBeVisible(this.emailInput());
    await this.expectToBeVisible(this.passwordInput());
    await this.expectToBeVisible(this.loginButton());
  }

  async expectEmailError(message: string): Promise<void> {
    const emailError = this.page.locator(
      '[data-testid="email-error"], [class*="email-error"], input[name*="email"] + [class*="error"]'
    );
    await this.expectToContainText(emailError, message);
  }

  async expectPasswordError(message: string): Promise<void> {
    const passwordError = this.page.locator(
      '[data-testid="password-error"], [class*="password-error"], input[name*="password"] + [class*="error"]'
    );
    await this.expectToContainText(passwordError, message);
  }

  async expectLoginError(message: string): Promise<void> {
    await this.expectErrorMessage(message);
  }

  async expectSuccessfulLogin(): Promise<void> {
    // Should redirect away from login page
    await this.page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 10000 });
  }

  async expectOnLoginPage(): Promise<void> {
    await this.expectUrlToContain('/login');
  }

  async expectPasswordVisible(): Promise<void> {
    await expect(this.passwordInput()).toHaveAttribute('type', 'text');
  }

  async expectPasswordHidden(): Promise<void> {
    await expect(this.passwordInput()).toHaveAttribute('type', 'password');
  }

  // State checks
  async isLoginButtonDisabled(): Promise<boolean> {
    return await this.loginButton().isDisabled();
  }

  async isRememberMeChecked(): Promise<boolean> {
    return await this.rememberMeCheckbox().isChecked();
  }
}
