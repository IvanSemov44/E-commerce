import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * Cart Page Object Model
 * Encapsulates shopping cart functionality
 */
export class CartPage extends BasePage {
  constructor(page: Page) {
    super(page);
  }

  // Locators
  readonly cartItem = (): Locator => this.page.getByTestId('cart-item').or(
    this.page.locator('[class*="cart-item"], [class*="cart-row"]')
  );
  
  readonly emptyCartMessage = (): Locator => this.page.getByTestId('empty-cart').or(
    this.page.locator('[class*="empty-cart"], :text("Your cart is empty")')
  );
  
  readonly checkoutButton = (): Locator => this.page.getByTestId('checkout-button').or(
    this.page.locator('button:has-text("Checkout"), a:has-text("Checkout")')
  );
  
  readonly continueShoppingButton = (): Locator => this.page.getByTestId('continue-shopping').or(
    this.page.locator('a:has-text("Continue Shopping"), button:has-text("Continue Shopping")')
  );
  
  readonly cartTotal = (): Locator => this.page.getByTestId('cart-total').or(
    this.page.locator('[class*="cart-total"], [class*="order-total"]')
  );
  
  readonly cartSubtotal = (): Locator => this.page.getByTestId('cart-subtotal').or(
    this.page.locator('[class*="subtotal"]')
  );
  
  readonly shippingCost = (): Locator => this.page.getByTestId('shipping-cost').or(
    this.page.locator('[class*="shipping"]')
  );
  
  readonly taxAmount = (): Locator => this.page.getByTestId('tax-amount').or(
    this.page.locator('[class*="tax"]')
  );
  
  readonly promoCodeInput = (): Locator => this.page.getByTestId('promo-code-input').or(
    this.page.locator('input[name*="promo"], input[placeholder*="promo" i]')
  );
  
  readonly applyPromoButton = (): Locator => this.page.getByTestId('apply-promo').or(
    this.page.locator('button:has-text("Apply")')
  );
  
  readonly discountAmount = (): Locator => this.page.getByTestId('discount-amount').or(
    this.page.locator('[class*="discount"]')
  );

  // Navigation
  async navigate(): Promise<void> {
    await this.goto('/cart');
    await this.waitForCartToLoad();
  }

  async waitForCartToLoad(): Promise<void> {
    await this.page.waitForLoadState('networkidle');
    await this.loadingSpinner().waitFor({ state: 'hidden', timeout: 5000 }).catch(() => {});
  }

  // Actions
  async removeItem(index: number): Promise<void> {
    const item = this.cartItem().nth(index);
    const removeButton = item.locator('[data-testid="remove-item"], button:has-text("Remove"), [class*="remove"]');
    await removeButton.click();
    await this.waitForCartToLoad();
  }

  async updateQuantity(index: number, quantity: number): Promise<void> {
    const item = this.cartItem().nth(index);
    const quantityInput = item.locator('input[type="number"], select[name*="quantity"]');
    
    if (await quantityInput.count() > 0) {
      await quantityInput.fill(quantity.toString());
      await this.waitForCartToLoad();
    }
  }

  async incrementQuantity(index: number): Promise<void> {
    const item = this.cartItem().nth(index);
    const incrementButton = item.locator('[data-testid="increment"], button:has-text("+"), [aria-label*="increase" i]');
    
    if (await incrementButton.count() > 0) {
      await incrementButton.click();
      await this.waitForCartToLoad();
    }
  }

  async decrementQuantity(index: number): Promise<void> {
    const item = this.cartItem().nth(index);
    const decrementButton = item.locator('[data-testid="decrement"], button:has-text("-"), [aria-label*="decrease" i]');
    
    if (await decrementButton.count() > 0) {
      await decrementButton.click();
      await this.waitForCartToLoad();
    }
  }

  async applyPromoCode(code: string): Promise<void> {
    await this.fillInput(this.promoCodeInput(), code);
    await this.applyPromoButton().click();
    await this.waitForCartToLoad();
  }

  async proceedToCheckout(): Promise<void> {
    await this.clickElement(this.checkoutButton());
  }

  async continueShopping(): Promise<void> {
    await this.clickElement(this.continueShoppingButton());
  }

  async clearCart(): Promise<void> {
    const itemCount = await this.getItemCount();
    for (let i = 0; i < itemCount; i++) {
      await this.removeItem(0);
    }
  }

  // Getters
  async getItemCount(): Promise<number> {
    return await this.cartItem().count();
  }

  async getTotal(): Promise<number> {
    const totalText = await this.cartTotal().textContent();
    const match = totalText?.match(/[\d.]+/);
    return match ? parseFloat(match[0]) : 0;
  }

  async getSubtotal(): Promise<number> {
    const subtotalText = await this.cartSubtotal().textContent();
    const match = subtotalText?.match(/[\d.]+/);
    return match ? parseFloat(match[0]) : 0;
  }

  async getShipping(): Promise<number> {
    const shippingText = await this.shippingCost().textContent();
    const match = shippingText?.match(/[\d.]+/);
    return match ? parseFloat(match[0]) : 0;
  }

  async getTax(): Promise<number> {
    const taxText = await this.taxAmount().textContent();
    const match = taxText?.match(/[\d.]+/);
    return match ? parseFloat(match[0]) : 0;
  }

  async getDiscount(): Promise<number> {
    const discountText = await this.discountAmount().textContent();
    const match = discountText?.match(/[\d.]+/);
    return match ? parseFloat(match[0]) : 0;
  }

  async getItemQuantity(index: number): Promise<number> {
    const item = this.cartItem().nth(index);
    const quantityInput = item.locator('input[type="number"]');
    const quantitySelect = item.locator('select');
    
    if (await quantityInput.count() > 0) {
      const value = await quantityInput.inputValue();
      return parseInt(value) || 1;
    } else if (await quantitySelect.count() > 0) {
      const value = await quantitySelect.inputValue();
      return parseInt(value) || 1;
    }
    return 1;
  }

  async getItemName(index: number): Promise<string | null> {
    const item = this.cartItem().nth(index);
    const nameElement = item.locator('[class*="product-name"], [class*="item-name"], a');
    return await nameElement.textContent();
  }

  async getItemPrice(index: number): Promise<number> {
    const item = this.cartItem().nth(index);
    const priceElement = item.locator('[class*="price"]');
    const priceText = await priceElement.textContent();
    const match = priceText?.match(/[\d.]+/);
    return match ? parseFloat(match[0]) : 0;
  }

  async isCartEmpty(): Promise<boolean> {
    return (await this.getItemCount()) === 0 || (await this.emptyCartMessage().count()) > 0;
  }

  // Assertions
  async expectCartNotEmpty(): Promise<void> {
    const itemCount = await this.getItemCount();
    expect(itemCount).toBeGreaterThan(0);
  }

  async expectCartEmpty(): Promise<void> {
    await this.expectToBeVisible(this.emptyCartMessage());
  }

  async expectItemCount(count: number): Promise<void> {
    await expect(this.cartItem()).toHaveCount(count);
  }

  async expectTotalToBe(expected: number): Promise<void> {
    const total = await this.getTotal();
    expect(total).toBeCloseTo(expected, 2);
  }

  async expectPromoCodeApplied(): Promise<void> {
    await this.expectToBeVisible(this.discountAmount());
  }

  async expectPromoCodeError(message: string): Promise<void> {
    const error = this.page.locator('[class*="promo-error"], [class*="invalid-promo"]');
    await this.expectToContainText(error, message);
  }

  async expectQuantityUpdated(index: number, expectedQuantity: number): Promise<void> {
    const quantity = await this.getItemQuantity(index);
    expect(quantity).toBe(expectedQuantity);
  }

  async expectCheckoutButtonEnabled(): Promise<void> {
    await expect(this.checkoutButton()).toBeEnabled();
  }

  async expectCheckoutButtonDisabled(): Promise<void> {
    await expect(this.checkoutButton()).toBeDisabled();
  }

  async expectOnCartPage(): Promise<void> {
    await this.expectUrlToContain('/cart');
  }
}
