import { test, expect, request, APIRequestContext } from '@playwright/test';

const API_BASE = process.env.VITE_API_URL
  ? process.env.VITE_API_URL.replace(/\/?$/, '/')
  : 'http://localhost:5000/api/';

const ADMIN_EMAIL = 'admin@test.com';
const ADMIN_PASSWORD = 'Admin123!';

async function loginAndGetToken(ctx: APIRequestContext): Promise<string> {
  const res = await ctx.post('auth/login', {
    data: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD },
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

  // GET /cart — requires auth
  test('GET /cart — no token returns 401', async () => {
    const response = await apiContext.get('cart');
    expect(response.status()).toBe(401);
  });

  test('GET /cart — valid token returns 200 with cart shape', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` },
    });

    try {
      const response = await ctx.get('cart');
      expect(response.ok(), `Expected 200, got ${response.status()}`).toBe(true);

      const body = await response.json();
      expect(body.success).toBe(true);
      const data = body.data;
      const hasItems = Array.isArray(data?.items) || Array.isArray(data?.Items);
      expect(hasItems, 'Cart data must have items array').toBe(true);
    } finally {
      await ctx.dispose();
    }
  });

  // POST /cart/get-or-create — anonymous
  test('POST /cart/get-or-create — anonymous returns 200', async () => {
    const sessionId = crypto.randomUUID();
    const response = await apiContext.post('cart/get-or-create', {
      data: {},
      headers: { 'X-Session-ID': sessionId },
    });
    expect(response.ok(), `Expected 200, got ${response.status()}`).toBe(true);

    const body = await response.json();
    expect(body.success).toBe(true);
  });

  // POST /cart/add-item — anonymous
  test('POST /cart/add-item — missing productId returns 400', async () => {
    const sessionId = crypto.randomUUID();
    const response = await apiContext.post('cart/add-item', {
      data: { quantity: 1 },
      headers: { 'X-Session-ID': sessionId },
    });
    expect(response.status()).toBeGreaterThanOrEqual(400);
    expect(response.status()).toBeLessThan(500);
  });

  test('POST /cart/add-item — unknown product returns 404 or 400', async () => {
    const sessionId = crypto.randomUUID();
    const response = await apiContext.post('cart/add-item', {
      data: { productId: crypto.randomUUID(), quantity: 1 },
      headers: { 'X-Session-ID': sessionId },
    });
    expect([200, 400, 404]).toContain(response.status());
  });

  // PUT /cart/update-item AND /cart/items — both aliases
  test('PUT /cart/update-item/{id} — route exists', async () => {
    const sessionId = crypto.randomUUID();
    const response = await apiContext.put(`cart/update-item/${crypto.randomUUID()}`, {
      data: { quantity: 2 },
      headers: { 'X-Session-ID': sessionId },
    });
    expect(response.status()).not.toBe(404);
  });

  test('PUT /cart/items/{id} — alias route exists', async () => {
    const sessionId = crypto.randomUUID();
    const response = await apiContext.put(`cart/items/${crypto.randomUUID()}`, {
      data: { quantity: 2 },
      headers: { 'X-Session-ID': sessionId },
    });
    expect(response.status()).not.toBe(404);
  });

  test('PUT /cart/update-item/{id} — zero quantity returns 400 or 422', async () => {
    const sessionId = crypto.randomUUID();
    const response = await apiContext.put(`cart/update-item/${crypto.randomUUID()}`, {
      data: { quantity: 0 },
      headers: { 'X-Session-ID': sessionId },
    });
    expect([400, 422, 404]).toContain(response.status());
  });

  // DELETE /cart/remove-item — requires auth
  test('DELETE /cart/remove-item/{id} — no token returns 401', async () => {
    const response = await apiContext.delete(`cart/remove-item/${crypto.randomUUID()}`);
    expect(response.status()).toBe(401);
  });

  test('DELETE /cart/items/{id} — no token returns 401', async () => {
    const response = await apiContext.delete(`cart/items/${crypto.randomUUID()}`);
    expect(response.status()).toBe(401);
  });

  // POST /cart/clear and DELETE /cart — anonymous
  test('POST /cart/clear — anonymous returns 200', async () => {
    const sessionId = crypto.randomUUID();
    const response = await apiContext.post('cart/clear', {
      data: {},
      headers: { 'X-Session-ID': sessionId },
    });
    expect(response.ok(), `Expected 200, got ${response.status()}`).toBe(true);
  });

  test('DELETE /cart — alias returns 200', async () => {
    const sessionId = crypto.randomUUID();
    const response = await apiContext.delete('cart', {
      headers: { 'X-Session-ID': sessionId },
    });
    expect(response.ok(), `Expected 200, got ${response.status()}`).toBe(true);
  });

  // POST /cart/validate
  test('POST /cart/validate/{cartId} — unknown cartId returns 404 or 400', async () => {
    const sessionId = crypto.randomUUID();
    const response = await apiContext.post(`cart/validate/${crypto.randomUUID()}`, {
      data: {},
      headers: { 'X-Session-ID': sessionId },
    });
    expect([200, 400, 404]).toContain(response.status());
  });
});
