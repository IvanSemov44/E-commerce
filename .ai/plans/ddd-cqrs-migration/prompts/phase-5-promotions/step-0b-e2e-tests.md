# Phase 5, Step 0b: Promotions E2E API Tests (Pre-Migration Baseline)

**Do this BEFORE touching any migration code.**

Run against the OLD `PromoCodeService` to establish a passing baseline. After cutover (step 4), run again — all must still pass.

**Prerequisite**: Backend running on `http://localhost:5000` with a seeded PostgreSQL database (SAVE20 code seeded with Id `55555555-5555-5555-5555-555555555555`).

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
 */

const API_BASE = process.env.VITE_API_URL
  ? process.env.VITE_API_URL.replace(/\/?$/, '/')
  : 'http://localhost:5000/api/';

const ADMIN_EMAIL    = 'admin@test.com';
const ADMIN_PASSWORD = 'Admin123!';
const SEEDED_PROMO_ID = '55555555-5555-5555-5555-555555555555';
const SEEDED_CODE     = 'SAVE20';

async function loginAndGetToken(ctx: APIRequestContext): Promise<string> {
  const res = await ctx.post('auth/login', {
    data: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD }
  });
  expect(res.ok(), `Login failed with status ${res.status()}`).toBe(true);
  const body = await res.json();
  return body.data?.token ?? body.token;
}

function uniqueCode(): string {
  return ('T' + Math.random().toString(36).slice(2, 13)).toUpperCase();
}

function randomGuid(): string {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
    const r = Math.random() * 16 | 0;
    return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
  });
}

// ─────────────────────────────────────────────────────────
// GET /api/promo-codes/active  (anonymous)
// ─────────────────────────────────────────────────────────

test.describe('GET /api/promo-codes/active', () => {
  test('anonymous user can access, returns 200', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    const res = await ctx.get('promo-codes/active');
    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(body.data).toBeDefined();
    expect(Array.isArray(body.data.items)).toBe(true);
    expect(typeof body.data.totalCount).toBe('number');
    await ctx.dispose();
  });

  test('pageSize is clamped to 100', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    const res = await ctx.get('promo-codes/active?page=1&pageSize=500');
    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(body.data.items.length).toBeLessThanOrEqual(100);
    await ctx.dispose();
  });
});

// ─────────────────────────────────────────────────────────
// GET /api/promo-codes  (admin only)
// ─────────────────────────────────────────────────────────

test.describe('GET /api/promo-codes', () => {
  test('anonymous returns 401', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    expect((await ctx.get('promo-codes')).status()).toBe(401);
    await ctx.dispose();
  });

  test('admin returns 200 with paginated shape', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    const token = await loginAndGetToken(ctx);
    const res = await ctx.get('promo-codes?page=1&pageSize=10', {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(Array.isArray(body.data.items)).toBe(true);
    expect(typeof body.data.totalCount).toBe('number');
    await ctx.dispose();
  });

  test('admin can filter by isActive=true', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    const token = await loginAndGetToken(ctx);
    const res = await ctx.get('promo-codes?isActive=true', {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(res.status()).toBe(200);
    await ctx.dispose();
  });

  test('admin can search by code string', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    const token = await loginAndGetToken(ctx);
    const res = await ctx.get('promo-codes?search=SAVE', {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(res.status()).toBe(200);
    await ctx.dispose();
  });
});

// ─────────────────────────────────────────────────────────
// GET /api/promo-codes/{id}  (admin only)
// ─────────────────────────────────────────────────────────

test.describe('GET /api/promo-codes/{id}', () => {
  test('anonymous returns 401', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    expect((await ctx.get(`promo-codes/${SEEDED_PROMO_ID}`)).status()).toBe(401);
    await ctx.dispose();
  });

  test('admin gets seeded code, returns correct shape', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    const token = await loginAndGetToken(ctx);
    const res = await ctx.get(`promo-codes/${SEEDED_PROMO_ID}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(res.status()).toBe(200);
    const { data } = await res.json();
    expect(data.code).toBe(SEEDED_CODE);
    expect(data.isActive).toBe(true);
    expect(typeof data.discountType).toBe('string');
    expect(typeof data.discountValue).toBe('number');
    expect(typeof data.usedCount).toBe('number');
    expect(data.createdAt).toBeDefined();
    expect(data.updatedAt).toBeDefined();
    await ctx.dispose();
  });

  test('unknown id returns 404 with PROMO_CODE_NOT_FOUND', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    const token = await loginAndGetToken(ctx);
    const res = await ctx.get(`promo-codes/${randomGuid()}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(res.status()).toBe(404);
    const body = await res.json();
    expect(body.errorCode).toBe('PROMO_CODE_NOT_FOUND');
    await ctx.dispose();
  });
});

// ─────────────────────────────────────────────────────────
// POST /api/promo-codes  (admin only, 201 Created)
// ─────────────────────────────────────────────────────────

test.describe('POST /api/promo-codes', () => {
  test('anonymous returns 401', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    const res = await ctx.post('promo-codes', {
      data: { code: uniqueCode(), discountType: 'Percentage', discountValue: 10 }
    });
    expect(res.status()).toBe(401);
    await ctx.dispose();
  });

  test('admin creates code, returns 201 with Location header', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    const token = await loginAndGetToken(ctx);
    const res = await ctx.post('promo-codes', {
      data: { code: uniqueCode(), discountType: 'Percentage', discountValue: 15 },
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(res.status()).toBe(201);
    const location = res.headers()['location'];
    expect(location).toBeDefined();
    expect(location).toContain('promo-codes');
    await ctx.dispose();
  });

  test('code stored as uppercase regardless of input case', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    const token = await loginAndGetToken(ctx);
    const rawCode = 'lower-' + Math.random().toString(36).slice(2, 7);
    const res = await ctx.post('promo-codes', {
      data: { code: rawCode, discountType: 'Percentage', discountValue: 10 },
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(res.status()).toBe(201);
    const { data } = await res.json();
    expect(data.code).toBe(rawCode.trim().toUpperCase());
    await ctx.dispose();
  });

  test('duplicate code returns 409 with DUPLICATE_PROMO_CODE', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    const token = await loginAndGetToken(ctx);
    const code = uniqueCode();
    const headers = { Authorization: `Bearer ${token}` };

    await ctx.post('promo-codes', {
      data: { code, discountType: 'Fixed', discountValue: 5 }, headers
    });

    const second = await ctx.post('promo-codes', {
      data: { code, discountType: 'Fixed', discountValue: 5 }, headers
    });
    expect(second.status()).toBe(409);
    expect((await second.json()).errorCode).toBe('DUPLICATE_PROMO_CODE');
    await ctx.dispose();
  });
});

// ─────────────────────────────────────────────────────────
// PUT /api/promo-codes/{id}  (admin only)
// ─────────────────────────────────────────────────────────

test.describe('PUT /api/promo-codes/{id}', () => {
  test('anonymous returns 401', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    expect((await ctx.put(`promo-codes/${SEEDED_PROMO_ID}`, { data: {} })).status()).toBe(401);
    await ctx.dispose();
  });

  test('admin updates existing code, returns 200', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    const token = await loginAndGetToken(ctx);
    const res = await ctx.put(`promo-codes/${SEEDED_PROMO_ID}`, {
      data: { isActive: true },
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(res.status()).toBe(200);
    await ctx.dispose();
  });

  test('unknown id returns 404', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    const token = await loginAndGetToken(ctx);
    const res = await ctx.put(`promo-codes/${randomGuid()}`, {
      data: { isActive: false },
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(res.status()).toBe(404);
    await ctx.dispose();
  });
});

// ─────────────────────────────────────────────────────────
// PUT /api/promo-codes/{id}/deactivate  (admin only)
// ─────────────────────────────────────────────────────────

test.describe('PUT /api/promo-codes/{id}/deactivate', () => {
  test('anonymous returns 401', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    expect((await ctx.put(`promo-codes/${SEEDED_PROMO_ID}/deactivate`)).status()).toBe(401);
    await ctx.dispose();
  });

  test('admin deactivates existing code, returns 200', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    const token = await loginAndGetToken(ctx);
    // Create a fresh code — do not deactivate SAVE20 as other tests rely on it being active
    const createRes = await ctx.post('promo-codes', {
      data: { code: uniqueCode(), discountType: 'Percentage', discountValue: 5 },
      headers: { Authorization: `Bearer ${token}` }
    });
    const { data } = await createRes.json();
    const res = await ctx.put(`promo-codes/${data.id}/deactivate`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(res.status()).toBe(200);
    await ctx.dispose();
  });

  test('unknown id returns 404', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    const token = await loginAndGetToken(ctx);
    expect((await ctx.put(`promo-codes/${randomGuid()}/deactivate`, {
      headers: { Authorization: `Bearer ${token}` }
    })).status()).toBe(404);
    await ctx.dispose();
  });
});

// ─────────────────────────────────────────────────────────
// DELETE /api/promo-codes/{id}  (admin only)
// ─────────────────────────────────────────────────────────

test.describe('DELETE /api/promo-codes/{id}', () => {
  test('anonymous returns 401', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    expect((await ctx.delete(`promo-codes/${randomGuid()}`)).status()).toBe(401);
    await ctx.dispose();
  });

  test('admin deletes existing code, returns 200', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    const token = await loginAndGetToken(ctx);
    const createRes = await ctx.post('promo-codes', {
      data: { code: uniqueCode(), discountType: 'Fixed', discountValue: 5 },
      headers: { Authorization: `Bearer ${token}` }
    });
    const { data } = await createRes.json();
    const res = await ctx.delete(`promo-codes/${data.id}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(res.status()).toBe(200);
    await ctx.dispose();
  });

  test('unknown id returns 404', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    const token = await loginAndGetToken(ctx);
    expect((await ctx.delete(`promo-codes/${randomGuid()}`, {
      headers: { Authorization: `Bearer ${token}` }
    })).status()).toBe(404);
    await ctx.dispose();
  });
});

// ─────────────────────────────────────────────────────────
// POST /api/promo-codes/validate  (anonymous, ALWAYS 200)
// ─────────────────────────────────────────────────────────

test.describe('POST /api/promo-codes/validate', () => {
  test('anonymous can access', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    expect((await ctx.post('promo-codes/validate', {
      data: { code: SEEDED_CODE, orderAmount: 100 }
    })).status()).toBe(200);
    await ctx.dispose();
  });

  test('valid code: isValid=true, discountAmount=20 for orderAmount=100', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    const res = await ctx.post('promo-codes/validate', {
      data: { code: SEEDED_CODE, orderAmount: 100 }
    });
    expect(res.status()).toBe(200);
    const { data } = await res.json();
    expect(data.isValid).toBe(true);
    expect(data.discountAmount).toBe(20); // 20% of 100
    expect(data.message).toBeDefined();
    await ctx.dispose();
  });

  test('unknown code returns 200 with isValid=false, discountAmount=0', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    const res = await ctx.post('promo-codes/validate', {
      data: { code: 'FAKECODE999', orderAmount: 100 }
    });
    expect(res.status()).toBe(200); // NOT 404
    const { data } = await res.json();
    expect(data.isValid).toBe(false);
    expect(data.discountAmount).toBe(0);
    await ctx.dispose();
  });

  test('lowercase code matches case-insensitively', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    const { data } = await (await ctx.post('promo-codes/validate', {
      data: { code: 'save20', orderAmount: 100 }
    })).json();
    expect(data.isValid).toBe(true);
    await ctx.dispose();
  });

  test('below minOrderAmount: isValid=false', async () => {
    const adminCtx = await request.newContext({ baseURL: API_BASE });
    const token = await loginAndGetToken(adminCtx);
    const code = uniqueCode();
    await adminCtx.post('promo-codes', {
      data: { code, discountType: 'Percentage', discountValue: 10, minOrderAmount: 50 },
      headers: { Authorization: `Bearer ${token}` }
    });
    await adminCtx.dispose();

    const ctx = await request.newContext({ baseURL: API_BASE });
    const { data } = await (await ctx.post('promo-codes/validate', {
      data: { code, orderAmount: 30 }
    })).json();
    expect(data.isValid).toBe(false);
    await ctx.dispose();
  });

  test('response shape has isValid, discountAmount, message', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    const { data } = await (await ctx.post('promo-codes/validate', {
      data: { code: SEEDED_CODE, orderAmount: 100 }
    })).json();
    expect(typeof data.isValid).toBe('boolean');
    expect(typeof data.discountAmount).toBe('number');
    expect(data.message).toBeDefined();
    await ctx.dispose();
  });
});
```

---

## Run

```bash
# Backend must be running with seeded PostgreSQL
cd src/frontend/storefront
npx playwright test api-promo-codes.spec.ts --reporter=list
```

---

## Acceptance Criteria

- [ ] All tests pass against the OLD `PromoCodeService` with real PostgreSQL
- [ ] `POST /validate` always returns 200 regardless of code validity
- [ ] SAVE20 + orderAmount=100 → discountAmount=20 confirmed against real DB
- [ ] 201 with Location header on create confirmed
- [ ] 409 with `DUPLICATE_PROMO_CODE` on duplicate confirmed
- [ ] 404 with `PROMO_CODE_NOT_FOUND` on unknown id confirmed
- [ ] Auth boundaries confirmed: anonymous → 401, Customer → 403
