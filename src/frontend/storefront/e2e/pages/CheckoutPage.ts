import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * Checkout Page Object Model
 * Encapsulates checkout flow functionality
 */
export class CheckoutPage extends BasePage {
  constructor(page: Page) {
    super(page);
  }

  // Address Locators
  readonly firstNameInput = (): Locator =>
    this.page
      .getByTestId('first-name')
      .or(this.page.locator('input[name*="firstName"], input[name*="first_name"]'));

  readonly lastNameInput = (): Locator =>
    this.page
      .getByTestId('last-name')
      .or(this.page.locator('input[name*="lastName"], input[name*="last_name"]'));

  readonly emailInput = (): Locator =>
    this.page
      .getByTestId('checkout-email')
      .or(this.page.locator('input[name*="email"], input[type="email"]'));

  readonly phoneInput = (): Locator =>
    this.page.getByTestId('phone').or(this.page.locator('input[name*="phone"], input[type="tel"]'));

  readonly addressInput = (): Locator =>
    this.page
      .getByTestId('address')
      .or(this.page.locator('input[name*="address"], input[placeholder*="address" i]'));

  readonly cityInput = (): Locator =>
    this.page.getByTestId('city').or(this.page.locator('input[name*="city"]'));

  readonly stateInput = (): Locator =>
    this.page
      .getByTestId('state')
      .or(this.page.locator('input[name*="state"], select[name*="state"]'));

  readonly zipInput = (): Locator =>
    this.page.getByTestId('zip').or(this.page.locator('input[name*="zip"], input[name*="postal"]'));

  readonly countrySelect = (): Locator =>
    this.page.getByTestId('country').or(this.page.locator('select[name*="country"]'));

  // Payment Locators
  readonly cardNumberInput = (): Locator =>
    this.page
      .getByTestId('card-number')
      .or(this.page.locator('input[name*="card"], input[placeholder*="card" i]'));

  readonly expiryInput = (): Locator =>
    this.page
      .getByTestId('expiry')
      .or(this.page.locator('input[name*="expir"], input[placeholder*="expir" i]'));

  readonly cvvInput = (): Locator =>
    this.page.getByTestId('cvv').or(this.page.locator('input[name*="cvv"], input[name*="cvc"]'));

  readonly cardNameInput = (): Locator =>
    this.page
      .getByTestId('card-name')
      .or(this.page.locator('input[name*="cardName"], input[name*="name_on_card"]'));

  // Payment Method Locators
  readonly creditCardOption = (): Locator =>
    this.page
      .getByTestId('credit-card-option')
      .or(this.page.locator('input[value*="credit"], label:has-text("Credit Card")'));

  readonly paypalOption = (): Locator =>
    this.page
      .getByTestId('paypal-option')
      .or(this.page.locator('input[value*="paypal"], label:has-text("PayPal")'));

  // Order Summary Locators
  readonly orderSummary = (): Locator =>
    this.page
      .getByTestId('order-summary')
      .or(this.page.locator('[class*="order-summary"], [class*="summary"]'));

  readonly orderTotal = (): Locator =>
    this.page
      .getByTestId('order-total')
      .or(this.page.locator('[class*="order-total"], [class*="total"]'));

  readonly placeOrderButton = (): Locator =>
    this.page
      .getByTestId('place-order')
      .or(this.page.locator('button:has-text("Place Order"), button:has-text("Complete")'));

  // Shipping Method Locators
  readonly shippingMethods = (): Locator =>
    this.page
      .getByTestId('shipping-methods')
      .or(this.page.locator('[class*="shipping-method"], [class*="delivery-option"]'));

  readonly standardShipping = (): Locator =>
    this.page
      .getByTestId('standard-shipping')
      .or(this.page.locator('input[value*="standard"], label:has-text("Standard")'));

  readonly expressShipping = (): Locator =>
    this.page
      .getByTestId('express-shipping')
      .or(this.page.locator('input[value*="express"], label:has-text("Express")'));

  // Navigation
  async navigate(): Promise<void> {
    await this.goto('/checkout');
    await this.waitForCheckoutToLoad();
  }

  async waitForCheckoutToLoad(): Promise<void> {
    await this.page.waitForLoadState('networkidle');
    await this.loadingSpinner()
      .waitFor({ state: 'hidden', timeout: 5000 })
      .catch(() => {});
  }

  // Address Actions
  async fillShippingAddress(data: {
    firstName: string;
    lastName: string;
    email: string;
    phone?: string;
    address: string;
    city: string;
    state: string;
    zip: string;
    country?: string;
  }): Promise<void> {
    await this.fillInput(this.firstNameInput(), data.firstName);
    await this.fillInput(this.lastNameInput(), data.lastName);
    await this.fillInput(this.emailInput(), data.email);

    if (data.phone) {
      await this.fillInput(this.phoneInput(), data.phone);
    }

    await this.fillInput(this.addressInput(), data.address);
    await this.fillInput(this.cityInput(), data.city);
    await this.fillInput(this.stateInput(), data.state);
    await this.fillInput(this.zipInput(), data.zip);

    if (data.country && (await this.countrySelect().count()) > 0) {
      await this.countrySelect().selectOption(data.country);
    }
  }

  // Payment Actions
  async selectCreditCard(): Promise<void> {
    await this.creditCardOption().click();
  }

  async selectPayPal(): Promise<void> {
    await this.paypalOption().click();
  }

  async fillCreditCardDetails(data: {
    cardNumber: string;
    expiry: string;
    cvv: string;
    nameOnCard: string;
  }): Promise<void> {
    await this.fillInput(this.cardNumberInput(), data.cardNumber);
    await this.fillInput(this.expiryInput(), data.expiry);
    await this.fillInput(this.cvvInput(), data.cvv);
    await this.fillInput(this.cardNameInput(), data.nameOnCard);
  }

  // Shipping Actions
  async selectStandardShipping(): Promise<void> {
    await this.standardShipping().click();
  }

  async selectExpressShipping(): Promise<void> {
    await this.expressShipping().click();
  }

  // Order Actions
  async placeOrder(): Promise<void> {
    await this.clickElement(this.placeOrderButton());
    await this.page.waitForURL(/\/(confirmation|success|thank-you)/, { timeout: 30000 });
  }

  async completeCheckout(
    address: Record<string, string>,
    payment: Record<string, string>
  ): Promise<void> {
    await this.fillShippingAddress(address);
    await this.fillCreditCardDetails(payment);
    await this.placeOrder();
  }

  // Getters
  async getOrderTotal(): Promise<number> {
    const totalText = await this.orderTotal().textContent();
    const match = totalText?.match(/[\d.]+/);
    return match ? parseFloat(match[0]) : 0;
  }

  async isPlaceOrderButtonEnabled(): Promise<boolean> {
    return await this.placeOrderButton().isEnabled();
  }

  // Assertions
  async expectCheckoutFormVisible(): Promise<void> {
    await this.expectToBeVisible(this.emailInput());
    await this.expectToBeVisible(this.addressInput());
  }

  async expectPaymentMethodsVisible(): Promise<void> {
    await this.expectToBeVisible(this.creditCardOption());
  }

  async expectOrderSummaryVisible(): Promise<void> {
    await this.expectToBeVisible(this.orderSummary());
  }

  async expectPlaceOrderEnabled(): Promise<void> {
    await expect(this.placeOrderButton()).toBeEnabled();
  }

  async expectPlaceOrderDisabled(): Promise<void> {
    await expect(this.placeOrderButton()).toBeDisabled();
  }

  async expectValidationErrors(): Promise<void> {
    const errors = this.page.locator('[class*="error"], [class*="invalid"]');
    const count = await errors.count();
    expect(count).toBeGreaterThan(0);
  }

  async expectOnCheckoutPage(): Promise<void> {
    await this.expectUrlToContain('/checkout');
  }

  async expectOnConfirmationPage(): Promise<void> {
    await expect(this.page).toHaveURL(/\/(confirmation|success|thank-you)/);
  }
}
