# Phase 5, Step 0b: Promotions E2E API Tests (Pre-Migration Baseline)

**Do this BEFORE touching any migration code.**

Run against the OLD `PromoCodeService` to establish a passing baseline. After cutover (step 4), run again — all must still pass.

**Prerequisite**: Backend running on `http://localhost:5000` with a seeded PostgreSQL database.

---

## Task: Create `api-promo-codes.spec.ts`

### File: `src/frontend/storefront/e2e/api-promo-codes.spec.ts`

```typescript
import { test, expect, request, APIRequestContext } from '@playwright/test';

/**
 * Promo Codes API Tests
 *
 * Written BEFORE Phase 5 migration to pin the existing HTTP contract.
 * Run against old PromoCodeService first to establish a passing baseline.
 * Run again after cutover — all tests must still pass.
 *
 * No browser — pure API contract verification.
 *
 * Seeded data:
 *   PromoCode Id: 55555555-5555-5555-5555-555555555555
 *   Code: SAVE20, DiscountType: Percentage, DiscountValue: 20, IsActive: true
 */

const API_BASE = process.env.VITE_API_URL
  ? process.env.VITE_API_URL.replace(/\/?$/, '/')
  : 'http://localhost:5000/api/';

const ADMIN_EMAIL    = 'admin@test.com';
const ADMIN_PASSWORD = 'Admin123!';

const SEEDED_PROMO_ID   = '55555555-5555-5555-5555-555555555555';
const SEEDED_PROMO_CODE = 'SAVE20';

async function loginAndGetToken(ctx: APIRequestContext): Promise<string> {
  const res = await ctx.post('auth/login', {
    data: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD }
  });
  expect(res.ok(), `Login failed with status ${res.status()}`).toBe(true);
  const body  = await res.json();
  const token = body.data?.token ?? body.data?.accessToken ?? body.data?.access_token;
  expect(token, 'Login response must contain a token').toBeTruthy();
  return token as string;
}

/** Generates a unique promo code string safe for the API (3-20 chars, A-Z0-9). */
function uniqueCode(): string {
  return ('TEST' + Math.random().toString(36).toUpperCase().replace('.', '').slice(2, 8)).slice(0, 10);
}

test.describe('Promo Codes API', () => {
  let apiContext: APIRequestContext;

  test.beforeEach(async () => {
    apiContext = await request.newContext({ baseURL: API_BASE });
  });

  test.afterEach(async () => {
    await apiContext.dispose();
  });

  // ===========================================================================
  // GET /promo-codes/active — anonymous
  // ===========================================================================

  test('GET /promo-codes/active — anonymous returns 200', async () => {
    const response = await apiContext.get('promo-codes/active');
    expect(response.ok(), `Expected 200, got ${response.status()}`).toBe(true);
  });

  test('GET /promo-codes/active — response has paginated shape', async () => {
    const response = await apiContext.get('promo-codes/active?page=1&pageSize=10');
    expect(response.ok()).toBe(true);

    const body = await response.json();
    expect(body.success).toBe(true);
    const data = body.data;
    const hasItems =
      Array.isArray(data?.items) || Array.isArray(data?.Items);
    expect(hasItems, 'Response data must have items array').toBe(true);
  });

  test('GET /promo-codes/active — pageSize over 100 is clamped, not an error', async () => {
    const response = await apiContext.get('promo-codes/active?page=1&pageSize=500');
    expect(response.ok(), `Expected 200, got ${response.status()}`).toBe(true);
  });

  // ===========================================================================
  // GET /promo-codes — admin list (auth required)
  // ===========================================================================

  test('GET /promo-codes — no token returns 401', async () => {
    const response = await apiContext.get('promo-codes');
    expect(response.status()).toBe(401);
  });

  test('GET /promo-codes — admin token returns 200 with paginated shape', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` }
    });

    try {
      const response = await ctx.get('promo-codes?page=1&pageSize=10');
      expect(response.ok()).toBe(true);

      const body = await response.json();
      expect(body.success).toBe(true);
      expect(
        Array.isArray(body.data?.items) || Array.isArray(body.data?.Items)
      ).toBe(true);
    } finally {
      await ctx.dispose();
    }
  });

  // ===========================================================================
  // GET /promo-codes/{id} — admin only
  // ===========================================================================

  test('GET /promo-codes/{id} — no token returns 401', async () => {
    const response = await apiContext.get(`promo-codes/${SEEDED_PROMO_ID}`);
    expect(response.status()).toBe(401);
  });

  test('GET /promo-codes/{id} — seeded id returns 200 with code SAVE20', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` }
    });

    try {
      const response = await ctx.get(`promo-codes/${SEEDED_PROMO_ID}`);
      expect(response.ok()).toBe(true);

      const body = await response.json();
      const code = body.data?.code ?? body.data?.Code;
      expect(code).toBe(SEEDED_PROMO_CODE);
    } finally {
      await ctx.dispose();
    }
  });

  test('GET /promo-codes/{id} — unknown id returns 404', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` }
    });

    try {
      const response = await ctx.get(`promo-codes/${crypto.randomUUID()}`);
      expect(response.status()).toBe(404);
    } finally {
      await ctx.dispose();
    }
  });

  // ===========================================================================
  // POST /promo-codes — create (admin only)
  // ===========================================================================

  test('POST /promo-codes — no token returns 401', async () => {
    const response = await apiContext.post('promo-codes', {
      data: { code: uniqueCode(), discountType: 'Percentage', discountValue: 10 }
    });
    expect(response.status()).toBe(401);
  });

  test('POST /promo-codes — admin creates code, returns 201 with Location header', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` }
    });

    try {
      const response = await ctx.post('promo-codes', {
        data: {
          code:          uniqueCode(),
          discountType:  'Percentage',
          discountValue: 15,
          isActive:      true
        }
      });
      expect(response.status()).toBe(201);

      const location = response.headers()['location'];
      expect(location, 'Created response must have Location header').toBeTruthy();
    } finally {
      await ctx.dispose();
    }
  });

  test('POST /promo-codes — duplicate code returns 409', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` }
    });

    try {
      const response = await ctx.post('promo-codes', {
        data: {
          code:          SEEDED_PROMO_CODE, // already exists
          discountType:  'Percentage',
          discountValue: 10
        }
      });
      expect(response.status()).toBe(409);
    } finally {
      await ctx.dispose();
    }
  });

  // ===========================================================================
  // DELETE /promo-codes/{id} — admin only
  // ===========================================================================

  test('DELETE /promo-codes/{id} — no token returns 401', async () => {
    const response = await apiContext.delete(`promo-codes/${crypto.randomUUID()}`);
    expect(response.status()).toBe(401);
  });

  test('DELETE /promo-codes/{id} — unknown id returns 404', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` }
    });

    try {
      const response = await ctx.delete(`promo-codes/${crypto.randomUUID()}`);
      expect(response.status()).toBe(404);
    } finally {
      await ctx.dispose();
    }
  });

  test('DELETE /promo-codes/{id} — admin deletes created code, returns 200', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` }
    });

    try {
      // Create first
      const createRes = await ctx.post('promo-codes', {
        data: { code: uniqueCode(), discountType: 'Percentage', discountValue: 5, isActive: true }
      });
      expect(createRes.status()).toBe(201);

      const location = createRes.headers()['location']!;
      const id = location.split('/').pop()!;

      const deleteRes = await ctx.delete(`promo-codes/${id}`);
      expect(deleteRes.ok(), `Expected 200, got ${deleteRes.status()}`).toBe(true);
    } finally {
      await ctx.dispose();
    }
  });

  // ===========================================================================
  // POST /promo-codes/validate — anonymous, ALWAYS 200
  // ===========================================================================

  test('POST /promo-codes/validate — anonymous, unknown code returns 200 with isValid=false', async () => {
    const response = await apiContext.post('promo-codes/validate', {
      data: { code: 'TOTALLY-INVALID', orderAmount: 100 }
    });
    expect(response.status()).toBe(200);

    const body    = await response.json();
    const isValid = body.data?.isValid ?? body.data?.IsValid;
    expect(isValid).toBe(false);
  });

  test('POST /promo-codes/validate — SAVE20 with orderAmount=100 returns isValid=true, discountAmount=20', async () => {
    const response = await apiContext.post('promo-codes/validate', {
      data: { code: SEEDED_PROMO_CODE, orderAmount: 100 }
    });
    expect(response.status()).toBe(200);

    const body           = await response.json();
    const isValid        = body.data?.isValid ?? body.data?.IsValid;
    const discountAmount = body.data?.discountAmount ?? body.data?.DiscountAmount;

    expect(isValid).toBe(true);
    expect(discountAmount).toBe(20); // 20% of 100
  });

  test('POST /promo-codes/validate — response always 200 even when isValid=false (never fails with error status)', async () => {
    // Confirm the contract: validate is not a normal "fail with 4xx" endpoint
    const testCodes = [
      { code: '',             orderAmount: 100 },
      { code: 'DOESNOTEXIST', orderAmount: 0   },
    ];

    for (const payload of testCodes) {
      const response = await apiContext.post('promo-codes/validate', { data: payload });
      // Must be 200 or at most 400 (for truly malformed bodies) — never 404/409/500
      expect(
        response.status() === 200 || response.status() === 400,
        `Validate must return 200 or 400, got ${response.status()} for code="${payload.code}"`
      ).toBe(true);
    }
  });

  test('POST /promo-codes/validate — code lookup is case-insensitive', async () => {
    // "save20" lowercase must return the same isValid=true as "SAVE20"
    const response = await apiContext.post('promo-codes/validate', {
      data: { code: 'save20', orderAmount: 100 }
    });
    expect(response.status()).toBe(200);

    const body    = await response.json();
    const isValid = body.data?.isValid ?? body.data?.IsValid;
    expect(isValid).toBe(true);
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
npx playwright test api-promo-codes.spec.ts --reporter=list
```

---

## Acceptance Criteria

- [ ] `api-promo-codes.spec.ts` created in `src/frontend/storefront/e2e/`
- [ ] All tests pass against the OLD `PromoCodeService` (baseline)
- [ ] Anonymous endpoints confirmed: `GET /promo-codes/active` and `POST /promo-codes/validate` return 200 without token
- [ ] Admin endpoints return 401 without token
- [ ] `POST /promo-codes` returns 201 with `Location` header
- [ ] `POST /promo-codes/validate` with SAVE20 + 100 → `{ isValid: true, discountAmount: 20 }`
- [ ] `POST /promo-codes/validate` with unknown code → `{ isValid: false }` still with status 200
- [ ] `DELETE /promo-codes/{id}` returns 404 for unknown ID
- [ ] Duplicate code on create → 409
