# Pattern: Frontend E2E Tests (Playwright)

Layer F4. Full browser or API-level tests against a running backend and frontend.

---

## Two sub-types

### `api-*.spec.ts` — API contract tests
Hit the backend REST API directly (no browser UI). Prove that API endpoints respond correctly from the client's perspective. These run fast and are independent of the React frontend.

### `*.spec.ts` (no `api-` prefix) — UI flow tests
Drive a real browser through user journeys. Use the Page Object Model for all interactions.

---

## File structure

```
src/frontend/storefront/e2e/
├── pages/                          ← Page Object Model
│   ├── BasePage.ts                 ← shared navigation + wait helpers
│   ├── CartPage.ts
│   ├── CheckoutPage.ts
│   └── ProductPage.ts
├── fixtures/
│   └── auth.fixture.ts             ← authenticated test fixture
├── data/
│   └── test-data.ts                ← reusable test constants
├── api-auth.spec.ts
├── api-cart.spec.ts
├── api-catalog.spec.ts
├── auth.spec.ts
├── cart.spec.ts
└── checkout-auth.spec.ts
```

---

## Page Object Model template

```typescript
// e2e/pages/CartPage.ts
import type { Page, Locator } from '@playwright/test';
import { BasePage } from './BasePage';

export class CartPage extends BasePage {
    private readonly cartCount: Locator;
    private readonly cartItems: Locator;
    private readonly checkoutButton: Locator;

    constructor(page: Page) {
        super(page);
        this.cartCount = page.getByTestId('cart-count');
        this.cartItems = page.getByTestId('cart-item');
        this.checkoutButton = page.getByRole('button', { name: /checkout/i });
    }

    async goto() {
        await this.page.goto('/cart');
        await this.page.waitForLoadState('networkidle');
    }

    async expectItemCount(count: number) {
        await expect(this.cartItems).toHaveCount(count);
    }

    async expectCartBadge(count: number) {
        await expect(this.cartCount).toHaveText(String(count));
    }

    async removeItem(productName: string) {
        const item = this.page.getByTestId('cart-item').filter({ hasText: productName });
        await item.getByRole('button', { name: /remove/i }).click();
    }

    async proceedToCheckout() {
        await this.checkoutButton.click();
        await this.page.waitForURL('**/checkout');
    }
}
```

---

## UI flow spec template

```typescript
// e2e/cart.spec.ts
import { test, expect } from '@playwright/test';
import { CartPage } from './pages/CartPage';
import { ProductPage } from './pages/ProductPage';

test.describe('Cart flow', () => {
    test('user_CanAddProduct_AndSeeItInCart', async ({ page }) => {
        // Arrange — navigate to a product
        const productPage = new ProductPage(page);
        await productPage.goto('widget-pro'); // slug

        // Act
        await productPage.addToCart();

        // Assert — badge updates
        const cartPage = new CartPage(page);
        await cartPage.expectCartBadge(1);

        // Act — navigate to cart
        await cartPage.goto();

        // Assert — item appears in cart
        await cartPage.expectItemCount(1);
        await expect(page.getByText('Widget Pro')).toBeVisible();
    });

    test('user_CanRemoveItem_AndCartBecomesEmpty', async ({ page }) => {
        // Arrange — add a product first
        const productPage = new ProductPage(page);
        await productPage.goto('widget-pro');
        await productPage.addToCart();

        const cartPage = new CartPage(page);
        await cartPage.goto();
        await cartPage.expectItemCount(1);

        // Act
        await cartPage.removeItem('Widget Pro');

        // Assert
        await cartPage.expectItemCount(0);
        await expect(page.getByText('Your cart is empty')).toBeVisible();
    });

    test('guest_CannotCheckout_WithoutLogin', async ({ page }) => {
        // Arrange — add item as guest
        const productPage = new ProductPage(page);
        await productPage.goto('widget-pro');
        await productPage.addToCart();

        const cartPage = new CartPage(page);
        await cartPage.goto();

        // Act
        await cartPage.proceedToCheckout();

        // Assert — redirected to login
        await expect(page).toHaveURL(/\/login/);
    });
});
```

---

## API contract spec template

```typescript
// e2e/api-cart.spec.ts
import { test, expect } from '@playwright/test';

const BASE_URL = 'http://localhost:5000/api';

test.describe('Cart API', () => {
    let authToken: string;

    test.beforeAll(async ({ request }) => {
        const response = await request.post(`${BASE_URL}/auth/login`, {
            data: { email: 'integration@test.com', password: 'TestPassword123!' },
        });
        const body = await response.json();
        authToken = body.data.token;
    });

    test('GET_/cart_Returns200_WhenAuthenticated', async ({ request }) => {
        // Act
        const response = await request.get(`${BASE_URL}/cart`, {
            headers: { Authorization: `Bearer ${authToken}` },
        });

        // Assert
        expect(response.status()).toBe(200);
        const body = await response.json();
        expect(body.success).toBe(true);
        expect(body.data).toBeDefined();
        expect(Array.isArray(body.data.items)).toBe(true);
    });

    test('GET_/cart_Returns401_WhenUnauthenticated', async ({ request }) => {
        // Act
        const response = await request.get(`${BASE_URL}/cart`);

        // Assert
        expect(response.status()).toBe(401);
    });

    test('POST_/cart/items_AddsItem_Returns200', async ({ request }) => {
        // Arrange
        const body = { productId: '00000000-0000-0000-0000-000000000001', quantity: 1 };

        // Act
        const response = await request.post(`${BASE_URL}/cart/items`, {
            data: body,
            headers: { Authorization: `Bearer ${authToken}` },
        });

        // Assert
        expect(response.status()).toBe(200);
        const json = await response.json();
        expect(json.success).toBe(true);
    });
});
```

---

## Authenticated test fixture

```typescript
// e2e/fixtures/auth.fixture.ts
import { test as base } from '@playwright/test';

type AuthFixtures = {
    authenticatedPage: { page: import('@playwright/test').Page; token: string };
};

export const test = base.extend<AuthFixtures>({
    authenticatedPage: async ({ page, request }, use) => {
        const response = await request.post('http://localhost:5000/api/auth/login', {
            data: { email: 'integration@test.com', password: 'TestPassword123!' },
        });
        const body = await response.json();
        const token = body.data.token;

        await page.addInitScript((t) => {
            localStorage.setItem('token', t);
        }, token);

        await use({ page, token });
    },
});

export { expect } from '@playwright/test';
```

---

## Rules

1. **All element selectors belong in Page Objects.** Spec files call Page Object methods only — no raw `page.click('...')` in spec files.

2. **`waitForLoadState('networkidle')` before assertions** after navigation. The dev server keeps connections open; prefer `waitForLoadState('load')` in preview mode.

3. **`api-*.spec.ts` files do not use a browser** — they use `request` from Playwright's API testing mode. They run faster and can run without a frontend server.

4. **Never hard-code product IDs or user IDs** in spec files — use `test-data.ts` constants or create data in `beforeAll`.

5. **Tests must be independent.** No test may depend on state created by another test. Use `beforeAll` to set up shared prerequisites, not inter-test ordering.

6. **Keep E2E tests for critical journeys only.** Unit and integration tests cover edge cases — E2E proves the happy path works end-to-end.
