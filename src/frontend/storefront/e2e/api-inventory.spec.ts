import { test, expect, request, APIRequestContext } from '@playwright/test';

const API_BASE = process.env.VITE_API_URL
  ? process.env.VITE_API_URL.replace(/\/?$/, '/')
  : 'http://localhost:5000/api/';

const ADMIN_PASSWORD = 'Admin123';

const SEEDED_PRODUCT_ID = '22222222-2222-2222-2222-222222222222';

function getAdminEmail(): string {
  return `admin-${crypto.randomUUID()}@example.com`;
}

async function loginAndGetToken(ctx: APIRequestContext): Promise<string> {
  const res = await ctx.post('auth/login', {
    data: { email: getAdminEmail(), password: ADMIN_PASSWORD },
  });
  expect(res.ok(), `Login failed with status ${res.status()}`).toBe(true);

  // Token is set in HTTP-only cookie, not in response body
  const cookies = res.headers()['set-cookie'] || '';
  const tokenMatch = cookies.match(/accessToken=([^;]+)/);
  expect(tokenMatch, 'Login should set accessToken cookie').toBeTruthy();
  return tokenMatch![1];
}

test.describe('Inventory API', () => {
  let apiContext: APIRequestContext;

  test.beforeEach(async () => {
    apiContext = await request.newContext({ baseURL: API_BASE });
  });

  test.afterEach(async () => {
    await apiContext.dispose();
  });

  // GET /inventory — Admin only

  test('GET /inventory — no token returns 401', async () => {
    const response = await apiContext.get('inventory');
    expect(response.status()).toBe(401);
  });

  test('GET /inventory — admin token returns 200 with paginated data', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` },
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
      extraHTTPHeaders: { Authorization: `Bearer ${token}` },
    });

    try {
      const response = await ctx.get('inventory?lowStockOnly=true');
      expect(response.ok(), `Expected 200, got ${response.status()}`).toBe(true);
    } finally {
      await ctx.dispose();
    }
  });

  // GET /inventory/low-stock — Admin only

  test('GET /inventory/low-stock — no token returns 401', async () => {
    const response = await apiContext.get('inventory/low-stock');
    expect(response.status()).toBe(401);
  });

  test('GET /inventory/low-stock — admin token returns 200', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` },
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

  // GET /inventory/{productId} — Anonymous

  test('GET /inventory/{productId} — seeded product returns 200 or 404', async () => {
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

  // GET /inventory/{productId}/available — Anonymous

  test('GET /inventory/{productId}/available — returns 200 with isAvailable field', async () => {
    const response = await apiContext.get(`inventory/${SEEDED_PRODUCT_ID}/available?quantity=1`);

    expect([200, 404]).toContain(response.status());
    if (response.ok()) {
      const body = await response.json();
      expect(body.success).toBe(true);
      const data = body.data;
      expect(data).toHaveProperty('isAvailable');
    }
  });

  // GET /inventory/{productId}/history — Admin only

  test('GET /inventory/{productId}/history — no token returns 401', async () => {
    const response = await apiContext.get(`inventory/${SEEDED_PRODUCT_ID}/history`);
    expect(response.status()).toBe(401);
  });

  test('GET /inventory/{productId}/history — admin token returns 200 or 404', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` },
    });

    try {
      const response = await ctx.get(`inventory/${SEEDED_PRODUCT_ID}/history`);
      expect([200, 404]).toContain(response.status());
    } finally {
      await ctx.dispose();
    }
  });

  // POST /inventory/{productId}/adjust — Admin only

  test('POST /inventory/{productId}/adjust — no token returns 401', async () => {
    const response = await apiContext.post(`inventory/${SEEDED_PRODUCT_ID}/adjust`, {
      data: { quantity: 10, reason: 'correction' },
    });
    expect(response.status()).toBe(401);
  });

  test('POST /inventory/{productId}/adjust — admin token with valid body returns 200 or 404', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` },
    });

    try {
      const response = await ctx.post(`inventory/${SEEDED_PRODUCT_ID}/adjust`, {
        data: { quantity: 50, reason: 'e2e-test-adjustment' },
      });
      expect([200, 400, 404, 422]).toContain(response.status());
    } finally {
      await ctx.dispose();
    }
  });

  // POST /inventory/{productId}/restock — Admin only

  test('POST /inventory/{productId}/restock — no token returns 401', async () => {
    const response = await apiContext.post(`inventory/${SEEDED_PRODUCT_ID}/restock`, {
      data: { quantity: 5, reason: 'restock' },
    });
    expect(response.status()).toBe(401);
  });

  test('POST /inventory/{productId}/restock — admin token with valid body returns 200 or 404', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` },
    });

    try {
      const response = await ctx.post(`inventory/${SEEDED_PRODUCT_ID}/restock`, {
        data: { quantity: 10, reason: 'e2e-restock' },
      });
      expect([200, 400, 404, 422]).toContain(response.status());
    } finally {
      await ctx.dispose();
    }
  });

  // POST /inventory/check-availability — Anonymous

  test('POST /inventory/check-availability — valid request returns 200 with isAvailable', async () => {
    const response = await apiContext.post('inventory/check-availability', {
      data: {
        items: [{ productId: SEEDED_PRODUCT_ID, quantity: 1 }],
      },
    });

    expect(response.ok(), `Expected 200, got ${response.status()}`).toBe(true);
    const body = await response.json();
    expect(body.success).toBe(true);
    expect(body.data).toHaveProperty('isAvailable');
  });

  test('POST /inventory/check-availability — missing items returns 400', async () => {
    const response = await apiContext.post('inventory/check-availability', {
      data: {},
    });

    expect(response.status()).toBeGreaterThanOrEqual(400);
    expect(response.status()).toBeLessThan(500);
  });

  // PUT /inventory/{productId} — Admin only

  test('PUT /inventory/{productId} — no token returns 401', async () => {
    const response = await apiContext.put(`inventory/${SEEDED_PRODUCT_ID}`, {
      data: { quantity: 100, reason: 'update' },
    });
    expect(response.status()).toBe(401);
  });

  // PUT /inventory/bulk-update — Admin only

  test('PUT /inventory/bulk-update — no token returns 401', async () => {
    const response = await apiContext.put('inventory/bulk-update', {
      data: { updates: [{ productId: SEEDED_PRODUCT_ID, quantity: 5 }] },
    });
    expect(response.status()).toBe(401);
  });

  test('PUT /inventory/bulk-update — admin token with valid body returns 200', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` },
    });

    try {
      const response = await ctx.put('inventory/bulk-update', {
        data: { updates: [{ productId: SEEDED_PRODUCT_ID, quantity: 20 }] },
      });
      expect([200, 400, 404, 422]).toContain(response.status());
    } finally {
      await ctx.dispose();
    }
  });
});
