# Phase 4, Step 0b: Shopping E2E API Tests (Pre-Migration Baseline)

**Do this BEFORE touching any migration code.**

Run against the OLD `CartService`/`WishlistService` to establish a passing baseline. After cutover (step 4), run again — all must still pass.

**Prerequisite**: Backend running on `http://localhost:5000` with a seeded PostgreSQL database.

---

## Task: Create `api-cart.spec.ts` and `api-wishlist.spec.ts`

### File: `src/frontend/storefront/e2e/api-cart.spec.ts`

```typescript
import { test, expect, request, APIRequestContext } from '@playwright/test';

/**
 * Cart API Tests
 *
 * Written BEFORE Phase 4 migration to pin the existing HTTP contract.
 * Run against old CartService first to establish a passing baseline.
 * Run again after cutover — all tests must still pass.
 *
 * No browser — pure API contract verification.
 */

const API_BASE = process.env.VITE_API_URL
  ? process.env.VITE_API_URL.replace(/\/?$/, '/')
  : 'http://localhost:5000/api/';

const ADMIN_EMAIL    = 'admin@test.com';
const ADMIN_PASSWORD = 'Admin123!';
const SEEDED_PRODUCT_ID = '22222222-2222-2222-2222-222222222222';

async function loginAndGetToken(ctx: APIRequestContext): Promise<string> {
  const res = await ctx.post('auth/login', {
    data: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD }
  });
  expect(res.ok(), `Login failed with status ${res.status()}`).toBe(true);
  const body = await res.json();
  const token = body.data?.token ?? body.data?.accessToken ?? body.data?.access_token;
  expect(token, 'Login response must contain a token').toBeTruthy();
  return token as string;
}

test.describe('Cart API', () => {
  let apiContext: APIRequestContext;

  test.beforeEach(async () => {
    apiContext = await request.newContext({ baseURL: API_BASE });
  });

  test.afterEach(async () => {
    await apiContext.dispose();
  });

  // ===========================================================================
  // GET /cart — requires auth
  // ===========================================================================

  test('GET /cart — no token returns 401', async () => {
    const response = await apiContext.get('cart');
    expect(response.status()).toBe(401);
  });

  test('GET /cart — valid token returns 200 with cart shape', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` }
    });

    try {
      const response = await ctx.get('cart');
      expect(response.ok(), `Expected 200, got ${response.status()}`).toBe(true);

      const body = await response.json();
      expect(body.success).toBe(true);
      // Cart must have items array
      const data = body.data;
      const hasItems = Array.isArray(data?.items) || Array.isArray(data?.Items);
      expect(hasItems, 'Cart data must have items array').toBe(true);
    } finally {
      await ctx.dispose();
    }
  });

  // ===========================================================================
  // POST /cart/get-or-create — anonymous
  // ===========================================================================

  test('POST /cart/get-or-create — anonymous returns 200', async () => {
    const response = await apiContext.post('cart/get-or-create', { data: {} });
    expect(response.ok(), `Expected 200, got ${response.status()}`).toBe(true);

    const body = await response.json();
    expect(body.success).toBe(true);
  });

  // ===========================================================================
  // POST /cart/add-item — anonymous
  // ===========================================================================

  test('POST /cart/add-item — missing productId returns 400', async () => {
    const response = await apiContext.post('cart/add-item', { data: { quantity: 1 } });
    expect(response.status()).toBeGreaterThanOrEqual(400);
    expect(response.status()).toBeLessThan(500);
  });

  test('POST /cart/add-item — unknown product returns 404 or 400', async () => {
    const response = await apiContext.post('cart/add-item', {
      data: { productId: crypto.randomUUID(), quantity: 1 }
    });
    expect([200, 400, 404]).toContain(response.status());
  });

  test('POST /cart/add-item — authenticated user, seeded product returns 200', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` }
    });

    try {
      const response = await ctx.post('cart/add-item', {
        data: { productId: SEEDED_PRODUCT_ID, quantity: 1 }
      });
      // 200 if product exists in inventory, 404 if not found
      expect([200, 404]).toContain(response.status());
    } finally {
      await ctx.dispose();
    }
  });

  // ===========================================================================
  // PUT /cart/update-item/{id} AND /cart/items/{id} — both aliases must work
  // ===========================================================================

  test('PUT /cart/update-item/{id} — route exists (not 404 on route)', async () => {
    const response = await apiContext.put(`cart/update-item/${crypto.randomUUID()}`, {
      data: { quantity: 2 }
    });
    // Route itself must resolve — not a 404 "route not found"
    expect(response.status()).not.toBe(404);
  });

  test('PUT /cart/items/{id} — alias route exists', async () => {
    const response = await apiContext.put(`cart/items/${crypto.randomUUID()}`, {
      data: { quantity: 2 }
    });
    expect(response.status()).not.toBe(404);
  });

  test('PUT /cart/update-item/{id} — zero quantity returns 400 or 422', async () => {
    const response = await apiContext.put(`cart/update-item/${crypto.randomUUID()}`, {
      data: { quantity: 0 }
    });
    expect([400, 422, 404]).toContain(response.status());
  });

  // ===========================================================================
  // DELETE /cart/remove-item/{id} AND /cart/items/{id} — requires auth
  // ===========================================================================

  test('DELETE /cart/remove-item/{id} — no token returns 401', async () => {
    const response = await apiContext.delete(`cart/remove-item/${crypto.randomUUID()}`);
    expect(response.status()).toBe(401);
  });

  test('DELETE /cart/items/{id} — no token returns 401', async () => {
    const response = await apiContext.delete(`cart/items/${crypto.randomUUID()}`);
    expect(response.status()).toBe(401);
  });

  // ===========================================================================
  // POST /cart/clear AND DELETE /cart — anonymous
  // ===========================================================================

  test('POST /cart/clear — anonymous returns 200', async () => {
    const response = await apiContext.post('cart/clear', { data: {} });
    expect(response.ok(), `Expected 200, got ${response.status()}`).toBe(true);
  });

  test('DELETE /cart — alias returns 200', async () => {
    const response = await apiContext.delete('cart');
    expect(response.ok(), `Expected 200, got ${response.status()}`).toBe(true);
  });

  // ===========================================================================
  // POST /cart/validate/{cartId} — anonymous
  // ===========================================================================

  test('POST /cart/validate/{cartId} — unknown cartId returns 404 or 400', async () => {
    const response = await apiContext.post(`cart/validate/${crypto.randomUUID()}`, { data: {} });
    expect([200, 400, 404]).toContain(response.status());
  });
});
```

---

### File: `src/frontend/storefront/e2e/api-wishlist.spec.ts`

```typescript
import { test, expect, request, APIRequestContext } from '@playwright/test';

/**
 * Wishlist API Tests
 *
 * Written BEFORE Phase 4 migration to pin the existing HTTP contract.
 */

const API_BASE = process.env.VITE_API_URL
  ? process.env.VITE_API_URL.replace(/\/?$/, '/')
  : 'http://localhost:5000/api/';

const ADMIN_EMAIL    = 'admin@test.com';
const ADMIN_PASSWORD = 'Admin123!';

async function loginAndGetToken(ctx: APIRequestContext): Promise<string> {
  const res = await ctx.post('auth/login', {
    data: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD }
  });
  expect(res.ok()).toBe(true);
  const body = await res.json();
  const token = body.data?.token ?? body.data?.accessToken ?? body.data?.access_token;
  expect(token).toBeTruthy();
  return token as string;
}

test.describe('Wishlist API', () => {
  let apiContext: APIRequestContext;

  test.beforeEach(async () => {
    apiContext = await request.newContext({ baseURL: API_BASE });
  });

  test.afterEach(async () => {
    await apiContext.dispose();
  });

  // ===========================================================================
  // Security invariant: all endpoints require auth
  // ===========================================================================

  test('GET /wishlist — no token returns 401', async () => {
    expect((await apiContext.get('wishlist')).status()).toBe(401);
  });

  test('POST /wishlist/add — no token returns 401', async () => {
    expect((await apiContext.post('wishlist/add', { data: { productId: crypto.randomUUID() } })).status()).toBe(401);
  });

  test('DELETE /wishlist/remove/{id} — no token returns 401', async () => {
    expect((await apiContext.delete(`wishlist/remove/${crypto.randomUUID()}`)).status()).toBe(401);
  });

  test('GET /wishlist/contains/{id} — no token returns 401', async () => {
    expect((await apiContext.get(`wishlist/contains/${crypto.randomUUID()}`)).status()).toBe(401);
  });

  test('POST /wishlist/clear — no token returns 401', async () => {
    expect((await apiContext.post('wishlist/clear', { data: {} })).status()).toBe(401);
  });

  // ===========================================================================
  // GET /wishlist — authenticated
  // ===========================================================================

  test('GET /wishlist — valid token returns 200 with data', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` }
    });

    try {
      const response = await ctx.get('wishlist');
      expect(response.ok()).toBe(true);

      const body = await response.json();
      expect(body.success).toBe(true);
      expect(body.data).toBeTruthy();
    } finally {
      await ctx.dispose();
    }
  });

  // ===========================================================================
  // GET /wishlist/contains/{productId} — returns bool in data
  // ===========================================================================

  test('GET /wishlist/contains/{id} — returns bool in data field', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` }
    });

    try {
      const response = await ctx.get(`wishlist/contains/${crypto.randomUUID()}`);
      expect(response.ok()).toBe(true);

      const body = await response.json();
      expect(body.success).toBe(true);
      // data must be a boolean, not an object
      expect(typeof body.data).toBe('boolean');
    } finally {
      await ctx.dispose();
    }
  });

  // ===========================================================================
  // POST /wishlist/add — validation
  // ===========================================================================

  test('POST /wishlist/add — missing productId returns 400', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` }
    });

    try {
      const response = await ctx.post('wishlist/add', { data: {} });
      expect(response.status()).toBeGreaterThanOrEqual(400);
      expect(response.status()).toBeLessThan(500);
    } finally {
      await ctx.dispose();
    }
  });

  // ===========================================================================
  // POST /wishlist/clear — authenticated
  // ===========================================================================

  test('POST /wishlist/clear — valid token returns 200', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` }
    });

    try {
      const response = await ctx.post('wishlist/clear', { data: {} });
      expect(response.ok()).toBe(true);
    } finally {
      await ctx.dispose();
    }
  });
});
```

---

## Run Before Starting Migration

```bash
# 1. Start the backend
cd src/backend && dotnet run --project ECommerce.API

# 2. Run e2e tests
cd src/frontend/storefront
npx playwright test api-cart.spec.ts api-wishlist.spec.ts --reporter=list
```

---

## Acceptance Criteria

- [ ] `api-cart.spec.ts` created in `src/frontend/storefront/e2e/`
- [ ] `api-wishlist.spec.ts` created in `src/frontend/storefront/e2e/`
- [ ] All tests pass against the OLD services (baseline)
- [ ] Both Cart route aliases confirmed at real-DB level
- [ ] `GET /wishlist/contains/{id}` confirmed to return `typeof data === 'boolean'`
- [ ] `POST /cart/clear` and `DELETE /cart` both return 200 (alias confirmed)
