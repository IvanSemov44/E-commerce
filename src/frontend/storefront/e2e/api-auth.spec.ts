import { test, expect, request, APIRequestContext } from '@playwright/test';

/**
 * Identity API Tests — Auth & Profile Endpoints
 *
 * Written BEFORE Phase 2 migration to pin the existing HTTP contract.
 * Run against old AuthService/UserService first to establish a passing baseline.
 * Run again after cutover — all tests must still pass.
 *
 * No browser, no frontend — pure API contract verification.
 *
 * Run with:
 *   npx playwright test api-auth.spec.ts --config=playwright.api.config.ts --reporter=list
 *   (from src/frontend/storefront/)
 *
 * ApiResponse shape (success):  { success: true,  data: T,    errorDetails: null }
 * ApiResponse shape (failure):  { success: false, data: null, errorDetails: { code, message } }
 */

const API_BASE = process.env.VITE_API_URL
  ? process.env.VITE_API_URL.replace(/\/?$/, '/')
  : 'http://localhost:5000/api/';

// Credentials for the seeded admin user (must match UserSeeder)
const ADMIN_EMAIL = 'admin@example.com';
const ADMIN_PASSWORD = 'Admin123';

/**
 * Login using cookie-based auth. Cookies are stored automatically in the
 * APIRequestContext — subsequent requests on the same context are authenticated.
 * Also fires a safe GET to trigger XSRF-TOKEN cookie generation (needed for mutations).
 */
async function loginWithCookies(ctx: APIRequestContext): Promise<void> {
  const res = await ctx.post('auth/login', {
    data: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD },
  });
  expect(res.ok(), `Login failed with status ${res.status()}`).toBe(true);
  const body = await res.json();
  expect(body.success, 'Login response should have success=true').toBe(true);
  // Fire a safe GET so the CSRF middleware sets the XSRF-TOKEN cookie
  await ctx.get('auth/me');
}

/**
 * Returns headers required for mutating requests (PUT/POST/DELETE).
 * Reads the XSRF-TOKEN cookie that was set by a prior GET request.
 */
async function getCsrfHeaders(ctx: APIRequestContext): Promise<{ 'X-XSRF-TOKEN': string }> {
  const state = await ctx.storageState();
  const xsrf = state.cookies.find((c) => c.name === 'XSRF-TOKEN')?.value ?? '';
  return { 'X-XSRF-TOKEN': xsrf };
}

test.describe('Identity API — Auth & Profile', () => {
  let apiContext: APIRequestContext;

  test.beforeEach(async () => {
    apiContext = await request.newContext({ baseURL: API_BASE });
  });

  test.afterEach(async () => {
    await apiContext.dispose();
  });

  // ===========================================================================
  // POST /auth/register
  // ===========================================================================

  test('POST /auth/register — valid data returns 200', async () => {
    const uniqueEmail = `e2e-${Date.now()}@example.com`;

    const response = await apiContext.post('auth/register', {
      data: { email: uniqueEmail, password: 'SecurePass1!', firstName: 'E2E', lastName: 'Test' },
    });

    expect(response.ok(), `Expected 200 OK, got ${response.status()}`).toBe(true);
    const body = await response.json();
    expect(body.success).toBe(true);
    expect(body.data).toBeTruthy();
  });

  test('POST /auth/register — duplicate email returns 409 or 422', async () => {
    const response = await apiContext.post('auth/register', {
      data: { email: ADMIN_EMAIL, password: 'SecurePass1!', firstName: 'Dupe', lastName: 'User' },
    });

    expect(response.status()).toBeGreaterThanOrEqual(400);
    expect(response.status()).toBeLessThan(500);

    const body = await response.json();
    expect(body.success).toBe(false);
  });

  test('POST /auth/register — missing email returns 400', async () => {
    const response = await apiContext.post('auth/register', {
      data: { password: 'SecurePass1!', firstName: 'No', lastName: 'Email' },
    });

    expect(response.status()).toBe(400);
  });

  test('POST /auth/register — weak password returns 400', async () => {
    const response = await apiContext.post('auth/register', {
      data: { email: 'weak@example.com', password: 'abc', firstName: 'Weak', lastName: 'Pass' },
    });

    expect(response.status()).toBe(400);
  });

  // ===========================================================================
  // POST /auth/login
  // ===========================================================================

  test('POST /auth/login — valid credentials returns 200 with user data', async () => {
    // Tokens are set as httpOnly cookies, not returned in the body.
    // The response body contains user profile data.
    const response = await apiContext.post('auth/login', {
      data: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD },
    });

    expect(response.ok(), `Expected 200 OK, got ${response.status()}`).toBe(true);
    const body = await response.json();
    expect(body.success).toBe(true);
    expect(body.data).toHaveProperty('email');
    expect(body.data.email).toBe(ADMIN_EMAIL);
  });

  test('POST /auth/login — wrong password returns 401', async () => {
    const response = await apiContext.post('auth/login', {
      data: { email: ADMIN_EMAIL, password: 'WrongPassword999' },
    });

    expect(response.status()).toBe(401);
    const body = await response.json();
    expect(body.success).toBe(false);
  });

  test('POST /auth/login — nonexistent email returns 401, NOT 404', async () => {
    // Security invariant: must never reveal whether an email is registered.
    // This must remain 401 both before AND after migration.
    const response = await apiContext.post('auth/login', {
      data: { email: 'ghost@nowhere.com', password: 'SomePass1!' },
    });

    expect(response.status()).toBe(401);
  });

  test('POST /auth/login — missing password returns 400', async () => {
    const response = await apiContext.post('auth/login', {
      data: { email: ADMIN_EMAIL },
    });

    expect(response.status()).toBe(400);
  });

  // ===========================================================================
  // POST /auth/forgot-password
  // ===========================================================================

  test('POST /auth/forgot-password — always returns 200 regardless of email', async () => {
    // Security invariant: same response for existing AND nonexistent email.
    const [existing, nonexistent] = await Promise.all([
      apiContext.post('auth/forgot-password', { data: { email: ADMIN_EMAIL } }),
      apiContext.post('auth/forgot-password', { data: { email: 'nobody@nowhere.com' } }),
    ]);

    expect(existing.ok(), `Expected 200 for existing email, got ${existing.status()}`).toBe(true);
    expect(
      nonexistent.ok(),
      `Expected 200 for nonexistent email, got ${nonexistent.status()}`
    ).toBe(true);
  });

  test('POST /auth/forgot-password — missing email returns 400', async () => {
    const response = await apiContext.post('auth/forgot-password', { data: {} });

    expect(response.status()).toBe(400);
  });

  // ===========================================================================
  // GET /auth/me (requires authentication)
  // ===========================================================================

  test('GET /auth/me — no token returns 401', async () => {
    const response = await apiContext.get('auth/me');

    expect(response.status()).toBe(401);
  });

  test('GET /auth/me — valid token returns 200 with email field', async () => {
    await loginWithCookies(apiContext);

    const response = await apiContext.get('auth/me');
    expect(response.ok(), `Expected 200, got ${response.status()}`).toBe(true);

    const body = await response.json();
    expect(body.success).toBe(true);
    expect(body.data).toHaveProperty('email');
  });

  // ===========================================================================
  // GET /profile (requires authentication)
  // ===========================================================================

  test('GET /profile — no token returns 401', async () => {
    const response = await apiContext.get('profile');

    expect(response.status()).toBe(401);
  });

  test('GET /profile — valid token returns 200 with expected shape', async () => {
    await loginWithCookies(apiContext);

    const response = await apiContext.get('profile');
    expect(response.ok(), `Expected 200, got ${response.status()}`).toBe(true);

    const body = await response.json();
    expect(body.success).toBe(true);
    // Profile must expose at minimum: email
    const data = body.data;
    const hasEmail = data?.email ?? data?.Email;
    expect(hasEmail).toBeTruthy();
  });

  // ===========================================================================
  // PUT /profile (requires authentication)
  // ===========================================================================

  test('PUT /profile — no token returns 401', async () => {
    const response = await apiContext.put('profile', {
      data: { firstName: 'Test', lastName: 'User' },
    });

    expect(response.status()).toBe(401);
  });

  test('PUT /profile — valid token and data returns 200', async () => {
    await loginWithCookies(apiContext);

    const response = await apiContext.put('profile', {
      data: { firstName: 'Updated', lastName: 'Admin' },
      headers: await getCsrfHeaders(apiContext),
    });

    expect(response.ok(), `Expected 200, got ${response.status()}`).toBe(true);
  });

  test('PUT /profile — missing required fields returns 400', async () => {
    await loginWithCookies(apiContext);

    const response = await apiContext.put('profile', {
      data: {}, // missing firstName and lastName
      headers: await getCsrfHeaders(apiContext),
    });

    expect(response.status()).toBe(400);
  });

  // ===========================================================================
  // POST /profile/change-password (requires authentication)
  // ===========================================================================

  test('POST /profile/change-password — no token returns 401', async () => {
    const response = await apiContext.post('profile/change-password', {
      data: { oldPassword: 'Old1', newPassword: 'New1' },
    });

    expect(response.status()).toBe(401);
  });

  test('POST /profile/change-password — missing fields returns 400', async () => {
    await loginWithCookies(apiContext);

    const response = await apiContext.post('profile/change-password', {
      data: {},
      headers: await getCsrfHeaders(apiContext),
    });
    expect(response.status()).toBe(400);
  });

  test('POST /profile/change-password — wrong old password returns 4xx', async () => {
    await loginWithCookies(apiContext);

    const response = await apiContext.post('profile/change-password', {
      data: { oldPassword: 'DefinitelyWrong999', newPassword: 'NewPass1!' },
      headers: await getCsrfHeaders(apiContext),
    });

    expect(response.status()).toBeGreaterThanOrEqual(400);
    expect(response.status()).toBeLessThan(500);
  });
});
