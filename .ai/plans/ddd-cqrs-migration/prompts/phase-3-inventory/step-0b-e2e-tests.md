# Phase 3, Step 0b: Inventory E2E API Tests (Pre-Migration Baseline)

**Do this BEFORE touching any migration code — same as step-0 characterization tests.**

These tests pin the existing API contract against a **live backend with real PostgreSQL**. Run them first against the old `InventoryService` to establish a passing baseline. After cutover (step 4), run them again — they must still pass. If any fail after migration, you have a regression.

**Prerequisite**: Backend running on `http://localhost:5000` with a seeded PostgreSQL database.

---

## Why both characterization tests AND e2e tests?

| | Characterization tests (step-0) | E2E tests (step-0b) |
|--|--|--|
| Database | InMemory (fast, no setup) | Real PostgreSQL |
| Speed | Fast — run in every PR | Slower — run before/after migration |
| Catches | Logic regressions, HTTP contract | Real DB queries, EF mappings, data migration |
| When to run | Always (CI) | Before migration + after cutover |

Both must pass at baseline. Both must still pass after cutover.

---

## Task: Create `api-inventory.spec.ts` in Storefront E2E

### File: `src/frontend/storefront/e2e/api-inventory.spec.ts`

```typescript
import { test, expect, request, APIRequestContext } from '@playwright/test';

/**
 * Inventory API Tests
 *
 * Written BEFORE Phase 3 migration to pin the existing HTTP contract.
 * Run against old InventoryService first to establish a passing baseline.
 * Run again after cutover — all tests must still pass.
 *
 * No browser, no frontend — pure API contract verification.
 *
 * Run with:
 *   npx playwright test api-inventory.spec.ts --reporter=list
 *   (from src/frontend/storefront/)
 *
 * ApiResponse shape (success):  { success: true,  data: T,    errorDetails: null }
 * ApiResponse shape (failure):  { success: false, data: null, errorDetails: { code, message } }
 */

const API_BASE = process.env.VITE_API_URL
  ? process.env.VITE_API_URL.replace(/\/?$/, '/')
  : 'http://localhost:5000/api/';

const ADMIN_EMAIL    = 'admin@test.com';
const ADMIN_PASSWORD = 'Admin123!';

// Seeded product ID — must match the seed data in DatabaseSeeder
const SEEDED_PRODUCT_ID = '22222222-2222-2222-2222-222222222222';

/** Login and return the Bearer token. Fails the test if login fails. */
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

test.describe('Inventory API', () => {
  let apiContext: APIRequestContext;

  test.beforeEach(async () => {
    apiContext = await request.newContext({ baseURL: API_BASE });
  });

  test.afterEach(async () => {
    await apiContext.dispose();
  });

  // ===========================================================================
  // GET /inventory — Admin only
  // ===========================================================================

  test('GET /inventory — no token returns 401', async () => {
    const response = await apiContext.get('inventory');
    expect(response.status()).toBe(401);
  });

  test('GET /inventory — admin token returns 200 with paginated data', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` }
    });

    try {
      const response = await ctx.get('inventory');
      expect(response.ok(), `Expected 200, got ${response.status()}`).toBe(true);

      const body = await response.json();
      expect(body.success).toBe(true);
      expect(body.data).toBeTruthy();
    } finally {
      await ctx.dispose();
    }
  });

  test('GET /inventory?lowStockOnly=true — admin token returns 200', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` }
    });

    try {
      const response = await ctx.get('inventory?lowStockOnly=true');
      expect(response.ok(), `Expected 200, got ${response.status()}`).toBe(true);
    } finally {
      await ctx.dispose();
    }
  });

  // ===========================================================================
  // GET /inventory/low-stock — Admin only
  // ===========================================================================

  test('GET /inventory/low-stock — no token returns 401', async () => {
    const response = await apiContext.get('inventory/low-stock');
    expect(response.status()).toBe(401);
  });

  test('GET /inventory/low-stock — admin token returns 200', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` }
    });

    try {
      const response = await ctx.get('inventory/low-stock');
      expect(response.ok(), `Expected 200, got ${response.status()}`).toBe(true);

      const body = await response.json();
      expect(body.success).toBe(true);
    } finally {
      await ctx.dispose();
    }
  });

  // ===========================================================================
  // GET /inventory/{productId} — Anonymous
  // ===========================================================================

  test('GET /inventory/{productId} — seeded product returns 200 or 404', async () => {
    // Anonymous — this endpoint is [AllowAnonymous]
    const response = await apiContext.get(`inventory/${SEEDED_PRODUCT_ID}`);

    expect([200, 404]).toContain(response.status());
    if (response.ok()) {
      const body = await response.json();
      expect(body.success).toBe(true);
    }
  });

  test('GET /inventory/{productId} — unknown product returns 404', async () => {
    const response = await apiContext.get(`inventory/${crypto.randomUUID()}`);

    expect([200, 404]).toContain(response.status());
  });

  // ===========================================================================
  // GET /inventory/{productId}/available — Anonymous
  // ===========================================================================

  test('GET /inventory/{productId}/available — returns 200 with isAvailable field', async () => {
    const response = await apiContext.get(
      `inventory/${SEEDED_PRODUCT_ID}/available?quantity=1`
    );

    expect([200, 404]).toContain(response.status());
    if (response.ok()) {
      const body = await response.json();
      expect(body.success).toBe(true);
      // Response must include availability info
      const data = body.data;
      expect(data).toHaveProperty('isAvailable');
    }
  });

  // ===========================================================================
  // GET /inventory/{productId}/history — Admin only
  // ===========================================================================

  test('GET /inventory/{productId}/history — no token returns 401', async () => {
    const response = await apiContext.get(`inventory/${SEEDED_PRODUCT_ID}/history`);
    expect(response.status()).toBe(401);
  });

  test('GET /inventory/{productId}/history — admin token returns 200 or 404', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` }
    });

    try {
      const response = await ctx.get(`inventory/${SEEDED_PRODUCT_ID}/history`);
      expect([200, 404]).toContain(response.status());
    } finally {
      await ctx.dispose();
    }
  });

  // ===========================================================================
  // POST /inventory/{productId}/adjust — Admin only
  // ===========================================================================

  test('POST /inventory/{productId}/adjust — no token returns 401', async () => {
    const response = await apiContext.post(`inventory/${SEEDED_PRODUCT_ID}/adjust`, {
      data: { quantity: 10, reason: 'correction' }
    });
    expect(response.status()).toBe(401);
  });

  test('POST /inventory/{productId}/adjust — admin token with valid body returns 200 or 404', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` }
    });

    try {
      const response = await ctx.post(`inventory/${SEEDED_PRODUCT_ID}/adjust`, {
        data: { quantity: 50, reason: 'e2e-test-adjustment' }
      });
      expect([200, 404, 422]).toContain(response.status());
    } finally {
      await ctx.dispose();
    }
  });

  // ===========================================================================
  // POST /inventory/{productId}/restock — Admin only
  // ===========================================================================

  test('POST /inventory/{productId}/restock — no token returns 401', async () => {
    const response = await apiContext.post(`inventory/${SEEDED_PRODUCT_ID}/restock`, {
      data: { quantity: 5, reason: 'restock' }
    });
    expect(response.status()).toBe(401);
  });

  test('POST /inventory/{productId}/restock — admin token with valid body returns 200 or 404', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` }
    });

    try {
      const response = await ctx.post(`inventory/${SEEDED_PRODUCT_ID}/restock`, {
        data: { quantity: 10, reason: 'e2e-restock' }
      });
      expect([200, 404, 422]).toContain(response.status());
    } finally {
      await ctx.dispose();
    }
  });

  // ===========================================================================
  // POST /inventory/check-availability — Anonymous
  // ===========================================================================

  test('POST /inventory/check-availability — valid request returns 200 with isAvailable', async () => {
    const response = await apiContext.post('inventory/check-availability', {
      data: {
        items: [{ productId: SEEDED_PRODUCT_ID, quantity: 1 }]
      }
    });

    expect(response.ok(), `Expected 200, got ${response.status()}`).toBe(true);
    const body = await response.json();
    expect(body.success).toBe(true);
    // Response must include isAvailable
    expect(body.data).toHaveProperty('isAvailable');
  });

  test('POST /inventory/check-availability — missing items returns 400', async () => {
    const response = await apiContext.post('inventory/check-availability', {
      data: {}
    });

    expect(response.status()).toBeGreaterThanOrEqual(400);
    expect(response.status()).toBeLessThan(500);
  });

  // ===========================================================================
  // PUT /inventory/{productId} — Admin only
  // ===========================================================================

  test('PUT /inventory/{productId} — no token returns 401', async () => {
    const response = await apiContext.put(`inventory/${SEEDED_PRODUCT_ID}`, {
      data: { quantity: 100, reason: 'update' }
    });
    expect(response.status()).toBe(401);
  });

  // ===========================================================================
  // PUT /inventory/bulk-update — Admin only
  // ===========================================================================

  test('PUT /inventory/bulk-update — no token returns 401', async () => {
    const response = await apiContext.put('inventory/bulk-update', {
      data: { updates: [{ productId: SEEDED_PRODUCT_ID, quantity: 5 }] }
    });
    expect(response.status()).toBe(401);
  });

  test('PUT /inventory/bulk-update — admin token with valid body returns 200', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` }
    });

    try {
      const response = await ctx.put('inventory/bulk-update', {
        data: { updates: [{ productId: SEEDED_PRODUCT_ID, quantity: 20 }] }
      });
      expect([200, 404, 422]).toContain(response.status());
    } finally {
      await ctx.dispose();
    }
  });
});
```

---

## Run Before Starting Migration

```bash
# 1. Start the backend (old service — no migration code yet)
cd src/backend && dotnet run --project ECommerce.API

# 2. Run e2e tests
cd src/frontend/storefront
npx playwright test api-inventory.spec.ts --reporter=list
```

**All tests must pass against the OLD service.** If any fail, fix the test (wrong assumption about the existing contract) before starting migration.

Record the passing run as your baseline. After cutover (step 4), run the exact same command again — all must still pass.

---

## Acceptance Criteria

- [ ] `api-inventory.spec.ts` created in `src/frontend/storefront/e2e/`
- [ ] All tests pass against the OLD `InventoryService` (pre-migration baseline)
- [ ] Auth invariants verified at baseline:
  - `GET /inventory` → 401 without token
  - `GET /inventory/{productId}` → accessible anonymous
  - `POST /inventory/check-availability` → accessible anonymous
- [ ] `isAvailable` field confirmed in `check-availability` response shape at baseline
- [ ] Results recorded — cutover (step 4) will run the exact same tests again
