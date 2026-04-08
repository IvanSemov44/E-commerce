import { test, expect, request, APIRequestContext } from '@playwright/test';

const API_BASE = process.env.VITE_API_URL
  ? process.env.VITE_API_URL.replace(/\/?$/, '/')
  : 'http://localhost:5000/api/';

const ADMIN_PASSWORD = 'Admin123!';

function getAdminEmail(): string {
  return `admin-${crypto.randomUUID()}@example.com`;
}

async function loginAndGetToken(ctx: APIRequestContext): Promise<string> {
  const res = await ctx.post('auth/login', {
    data: { email: getAdminEmail(), password: ADMIN_PASSWORD },
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

  // Security: all endpoints require auth
  test('GET /wishlist — no token returns 401', async () => {
    expect((await apiContext.get('wishlist')).status()).toBe(401);
  });

  test('POST /wishlist/add — no token returns 401', async () => {
    expect(
      (await apiContext.post('wishlist/add', { data: { productId: crypto.randomUUID() } })).status()
    ).toBe(401);
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

  // GET /wishlist — authenticated
  test('GET /wishlist — valid token returns 200 with data', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` },
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

  // GET /wishlist/contains — returns bool in data
  test('GET /wishlist/contains/{id} — returns bool in data field', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` },
    });

    try {
      const response = await ctx.get(`wishlist/contains/${crypto.randomUUID()}`);
      expect(response.ok()).toBe(true);

      const body = await response.json();
      expect(body.success).toBe(true);
      expect(typeof body.data).toBe('boolean');
    } finally {
      await ctx.dispose();
    }
  });

  // POST /wishlist/add — validation
  test('POST /wishlist/add — missing productId returns 400', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` },
    });

    try {
      const response = await ctx.post('wishlist/add', { data: {} });
      expect(response.status()).toBeGreaterThanOrEqual(400);
      expect(response.status()).toBeLessThan(500);
    } finally {
      await ctx.dispose();
    }
  });

  // POST /wishlist/clear — authenticated
  test('POST /wishlist/clear — valid token returns 200', async () => {
    const token = await loginAndGetToken(apiContext);
    const ctx = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` },
    });

    try {
      const response = await ctx.post('wishlist/clear', { data: {} });
      expect(response.ok()).toBe(true);
    } finally {
      await ctx.dispose();
    }
  });
});
