import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * Products Page Object Model
 * Encapsulates product listing and browsing functionality
 */
export class ProductsPage extends BasePage {
  constructor(page: Page) {
    super(page);
  }

  // Locators
  readonly productCard = (): Locator =>
    this.page
      .getByTestId('product-card')
      .or(this.page.locator('[class*="product-card"], [class*="product-item"]'));

  readonly productGrid = (): Locator =>
    this.page
      .getByTestId('product-grid')
      .or(this.page.locator('[class*="product-grid"], [class*="products-container"]'));

  readonly searchInput = (): Locator =>
    this.page
      .getByTestId('search-input')
      .or(this.page.locator('input[type="search"], input[placeholder*="search" i]'));

  readonly searchButton = (): Locator =>
    this.page
      .getByTestId('search-button')
      .or(this.page.locator('button[type="submit"], button:has-text("Search")'));

  readonly categoryFilter = (): Locator =>
    this.page
      .getByTestId('category-filter')
      .or(this.page.locator('select[name*="category"], [class*="category-filter"]'));

  readonly priceFilter = (): Locator =>
    this.page
      .getByTestId('price-filter')
      .or(this.page.locator('input[type="range"], [class*="price-filter"]'));

  readonly sortDropdown = (): Locator =>
    this.page
      .getByTestId('sort-dropdown')
      .or(this.page.locator('select[name*="sort"], [class*="sort"]'));

  readonly pagination = (): Locator =>
    this.page
      .getByTestId('pagination')
      .or(this.page.locator('[class*="pagination"], nav[aria-label*="pagination"]'));

  readonly nextPageButton = (): Locator =>
    this.page
      .getByTestId('next-page')
      .or(this.page.locator('button:has-text("Next"), a:has-text("Next"), [aria-label*="next" i]'));

  readonly previousPageButton = (): Locator =>
    this.page
      .getByTestId('previous-page')
      .or(
        this.page.locator(
          'button:has-text("Previous"), a:has-text("Previous"), [aria-label*="previous" i]'
        )
      );

  readonly noResultsMessage = (): Locator =>
    this.page
      .getByTestId('no-results')
      .or(this.page.locator('[class*="no-results"], :text("No products found")'));

  readonly productCount = (): Locator =>
    this.page
      .getByTestId('product-count')
      .or(this.page.locator('[class*="product-count"], [class*="results-count"]'));

  // Navigation
  async navigate(): Promise<void> {
    await this.goto('/products');
    await this.waitForProductsToLoad();
  }

  async waitForProductsToLoad(): Promise<void> {
    await this.productGrid().waitFor({ state: 'visible', timeout: 10000 });
    await this.loadingSpinner()
      .waitFor({ state: 'hidden', timeout: 5000 })
      .catch(() => {});
  }

  // Actions
  async searchForProduct(query: string): Promise<void> {
    await this.fillInput(this.searchInput(), query);
    await this.searchButton().click();
    await this.waitForProductsToLoad();
  }

  async selectCategory(category: string): Promise<void> {
    await this.categoryFilter().selectOption(category);
    await this.waitForProductsToLoad();
  }

  async sortBy(option: string): Promise<void> {
    await this.sortDropdown().selectOption(option);
    await this.waitForProductsToLoad();
  }

  async clickProduct(index: number = 0): Promise<void> {
    const products = this.productCard();
    const count = await products.count();
    if (count > index) {
      await products.nth(index).click();
    } else {
      throw new Error(`Product at index ${index} not found. Only ${count} products available.`);
    }
  }

  async goToNextPage(): Promise<void> {
    const nextButton = this.nextPageButton();
    if (await nextButton.isEnabled()) {
      await nextButton.click();
      await this.waitForProductsToLoad();
    }
  }

  async goToPreviousPage(): Promise<void> {
    const prevButton = this.previousPageButton();
    if (await prevButton.isEnabled()) {
      await prevButton.click();
      await this.waitForProductsToLoad();
    }
  }

  async setPriceRange(min: number, max: number): Promise<void> {
    const minInput = this.page.locator('input[name*="min-price"], input[placeholder*="min" i]');
    const maxInput = this.page.locator('input[name*="max-price"], input[placeholder*="max" i]');

    if ((await minInput.count()) > 0) {
      await this.fillInput(minInput, min.toString());
    }
    if ((await maxInput.count()) > 0) {
      await this.fillInput(maxInput, max.toString());
    }

    await this.waitForProductsToLoad();
  }

  // Getters
  async getProductCount(): Promise<number> {
    return await this.productCard().count();
  }

  async getProductNames(): Promise<string[]> {
    const products = this.productCard();
    const count = await products.count();
    const names: string[] = [];

    for (let i = 0; i < count; i++) {
      const name = await products.nth(i).locator('[class*="product-name"], h3, h4').textContent();
      if (name) names.push(name);
    }

    return names;
  }

  async getProductPrice(index: number): Promise<number | null> {
    const product = this.productCard().nth(index);
    const priceElement = product.locator('[class*="price"], [data-testid="price"]');

    if ((await priceElement.count()) > 0) {
      const priceText = await priceElement.textContent();
      const match = priceText?.match(/[\d.]+/);
      return match ? parseFloat(match[0]) : null;
    }
    return null;
  }

  async getCurrentPage(): Promise<number> {
    const activePage = this.pagination().locator('[aria-current="page"], .active');
    if ((await activePage.count()) > 0) {
      const text = await activePage.textContent();
      return parseInt(text || '1');
    }
    return 1;
  }

  async getTotalPages(): Promise<number> {
    const pageLinks = this.pagination().locator('a, button');
    const count = await pageLinks.count();
    return count > 0 ? count : 1;
  }

  // Assertions
  async expectProductsVisible(count?: number): Promise<void> {
    const products = this.productCard();
    if (count !== undefined) {
      await expect(products).toHaveCount(count);
    } else {
      const productCount = await products.count();
      expect(productCount).toBeGreaterThan(0);
    }
  }

  async expectNoResults(): Promise<void> {
    await this.expectToBeVisible(this.noResultsMessage());
  }

  async expectProductCountText(text: string): Promise<void> {
    await this.expectToContainText(this.productCount(), text);
  }

  async expectProductsSortedByPrice(ascending: boolean = true): Promise<void> {
    const prices: number[] = [];
    const count = await this.getProductCount();

    for (let i = 0; i < count; i++) {
      const price = await this.getProductPrice(i);
      if (price !== null) prices.push(price);
    }

    const sortedPrices = [...prices].sort((a, b) => (ascending ? a - b : b - a));
    expect(prices).toEqual(sortedPrices);
  }

  async expectPaginationVisible(): Promise<void> {
    await this.expectToBeVisible(this.pagination());
  }

  async expectOnProductsPage(): Promise<void> {
    await this.expectUrlToContain('/products');
  }
}
