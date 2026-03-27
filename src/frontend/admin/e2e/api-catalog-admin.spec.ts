import { test, expect, request, APIRequestContext } from '@playwright/test';

/**
 * Admin API E2E tests - Catalog
 *
 * These tests exercise the Catalog admin endpoints at the HTTP level.
 * Run with (from the admin folder):
 *   npx playwright test --config playwright.api.config.ts
 */

// Ensure trailing slash on base URL
const API_BASE = (process.env.VITE_API_URL ?? 'http://localhost:5000/api').replace(/\/?$/, '/');

const ANY_GUID = '00000000-0000-0000-0000-000000000001';
const NONEXISTENT_GUID = '00000000-0000-0000-0000-999999999999';

// ===========================================================================
// Test group 1: Unauthenticated (no token)
// ===========================================================================

test.describe('Admin Catalog API — Unauthenticated (no token)', () => {
  let apiContext: APIRequestContext;

  test.beforeEach(async () => {
    apiContext = await request.newContext({ baseURL: API_BASE });
  });

  test.afterEach(async () => {
    await apiContext.dispose();
  });

  // Products - write endpoints must reject anonymous
  test('POST catalog/products returns 401', async () => {
    const response = await apiContext.post('catalog/products', { data: {} });
    expect(response.status()).toBe(401);
  });

  test('PUT catalog/products/{id} returns 401', async () => {
    const response = await apiContext.put(`catalog/products/${ANY_GUID}`, { data: {} });
    expect(response.status()).toBe(401);
  });

  test('DELETE catalog/products/{id} returns 401', async () => {
    const response = await apiContext.delete(`catalog/products/${ANY_GUID}`, { data: {} });
    expect(response.status()).toBe(401);
  });

  test('PUT catalog/products/{id}/stock returns 401', async () => {
    const response = await apiContext.put(`catalog/products/${ANY_GUID}/stock`, {
      data: { quantity: 10, reason: 'test' },
    });
    expect(response.status()).toBe(401);
  });

  test('POST catalog/products/{id}/activate returns 401', async () => {
    const response = await apiContext.post(`catalog/products/${ANY_GUID}/activate`, { data: {} });
    expect(response.status()).toBe(401);
  });

  test('POST catalog/products/{id}/deactivate returns 401', async () => {
    const response = await apiContext.post(`catalog/products/${ANY_GUID}/deactivate`, { data: {} });
    expect(response.status()).toBe(401);
  });

  // Categories - write endpoints must reject anonymous
  test('POST catalog/categories returns 401', async () => {
    const response = await apiContext.post('catalog/categories', { data: {} });
    expect(response.status()).toBe(401);
  });

  test('PUT catalog/categories/{id} returns 401', async () => {
    const response = await apiContext.put(`catalog/categories/${ANY_GUID}`, { data: {} });
    expect(response.status()).toBe(401);
  });

  test('DELETE catalog/categories/{id} returns 401', async () => {
    const response = await apiContext.delete(`catalog/categories/${ANY_GUID}`, { data: {} });
    expect(response.status()).toBe(401);
  });
});

// ===========================================================================
// Test group 2: Authenticated (admin token)
// ===========================================================================

test.describe.serial('Admin Catalog API — Authenticated (admin token)', () => {
  let apiContext: APIRequestContext;
  let adminToken: string;
  let createdCategoryId: string;
  let createdProductId: string;

  test.beforeAll(async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });
    const res = await ctx.post('auth/login', {
      data: {
        email: process.env.ADMIN_EMAIL ?? 'admin@ecommerce.com',
        password: process.env.ADMIN_PASSWORD ?? 'Admin123!',
      },
    });

    expect(res.ok(), `Admin login failed: ${res.status()}`).toBe(true);
    const body = await res.json();
    adminToken = body.data.accessToken;
    await ctx.dispose();
  });

  test.beforeEach(async () => {
    apiContext = await request.newContext({ baseURL: API_BASE });
  });

  test.afterEach(async () => {
    await apiContext.dispose();
  });

  function authHeaders() {
    return { Authorization: `Bearer ${adminToken}` };
  }

  // ----------------------
  // Category lifecycle
  // ----------------------

  test('POST catalog/categories creates a category', async () => {
    const response = await apiContext.post('catalog/categories', {
      headers: authHeaders(),
      data: { name: 'E2E Test Category' },
    });

    expect(response.status()).toBe(201);

    const body = await response.json();
    expect(body.success).toBe(true);
    expect(typeof body.data.id).toBe('string');
    createdCategoryId = body.data.id;
    expect(body.data.name).toBe('E2E Test Category');
    expect(typeof body.data.slug).toBe('string');
  });

  test('GET catalog/categories/{id} reads the created category', async () => {
    const response = await apiContext.get(`catalog/categories/${createdCategoryId}`);
    expect(response.status()).toBe(200);

    const body = await response.json();
    expect(body.success).toBe(true);
    expect(body.data.name).toBe('E2E Test Category');
  });

  test('PUT catalog/categories/{id} updates the category', async () => {
    const response = await apiContext.put(`catalog/categories/${createdCategoryId}`, {
      headers: authHeaders(),
      data: { name: 'E2E Test Category Updated' },
    });

    expect(response.status()).toBe(200);

    const body = await response.json();
    expect(body.data.name).toBe('E2E Test Category Updated');
  });

  test('DELETE catalog/categories/{nonexistent} returns 404', async () => {
    const response = await apiContext.delete(`catalog/categories/${NONEXISTENT_GUID}`, {
      headers: authHeaders(),
    });
    expect(response.status()).toBe(404);

    const body = await response.json();
    expect(body.errorDetails.code).toBe('CATEGORY_NOT_FOUND');
  });

  // ----------------------
  // Product lifecycle
  // ----------------------

  test('POST catalog/products creates a product', async () => {
    const response = await apiContext.post('catalog/products', {
      headers: authHeaders(),
      data: {
        name: 'E2E Test Product',
        sku: 'E2E-SKU-001',
        price: 99.99,
        categoryId: createdCategoryId ?? null,
        stockQuantity: 50,
      },
    });

    expect(response.status()).toBe(201);

    const body = await response.json();
    expect(body.success).toBe(true);
    expect(typeof body.data.id).toBe('string');
    createdProductId = body.data.id;
    expect(body.data.name).toBe('E2E Test Product');
    expect(typeof body.data.slug).toBe('string');
  });

  test('GET catalog/products/{id} reads the created product', async () => {
    const response = await apiContext.get(`catalog/products/${createdProductId}`);
    expect(response.status()).toBe(200);

    const body = await response.json();
    expect(body.success).toBe(true);
    expect(body.data.id).toBe(createdProductId);
    expect(body.data.name).toBe('E2E Test Product');
  });

  test('DELETE catalog/categories/{id} returns 409 when category has products', async () => {
    // Category has products because we just created one
    const response = await apiContext.delete(`catalog/categories/${createdCategoryId}`, {
      headers: authHeaders(),
    });
    expect(response.status()).toBe(409);
    const body = await response.json();
    expect(body.errorDetails.code).toBe('CATEGORY_HAS_PRODUCTS');
  });

  test('POST catalog/products/{id}/activate activates the product', async () => {
    const response = await apiContext.post(`catalog/products/${createdProductId}/activate`, {
      headers: authHeaders(),
      data: {},
    });

    expect(response.status()).toBe(200);
  });

  test('POST catalog/products/{id}/deactivate deactivates the product', async () => {
    const response = await apiContext.post(`catalog/products/${createdProductId}/deactivate`, {
      headers: authHeaders(),
      data: {},
    });

    expect(response.status()).toBe(200);
  });

  test('PUT catalog/products/{id}/stock updates product stock', async () => {
    const response = await apiContext.put(`catalog/products/${createdProductId}/stock`, {
      headers: authHeaders(),
      data: { quantity: 25, reason: 'E2E test adjustment' },
    });

    expect(response.status()).toBe(200);

    const body = await response.json();
    expect(body.success).toBe(true);
  });

  test('PUT catalog/products/{id} updates the product', async () => {
    const response = await apiContext.put(`catalog/products/${createdProductId}`, {
      headers: authHeaders(),
      data: { name: 'E2E Test Product Updated', price: 149.99 },
    });

    expect(response.status()).toBe(200);

    const body = await response.json();
    expect(body.data.name).toBe('E2E Test Product Updated');
  });

  test('Business validation: POST catalog/categories without name returns 422/400', async () => {
    const response = await apiContext.post('catalog/categories', {
      headers: authHeaders(),
      data: {},
    });
    expect([400, 422]).toContain(response.status());
    const body = await response.json();
    expect(body.success).toBe(false);
  });

  test('Business validation: POST catalog/products with negative price returns 422/400', async () => {
    const response = await apiContext.post('catalog/products', {
      headers: authHeaders(),
      data: { name: 'Bad', sku: 'BAD-001', price: -1 },
    });
    expect([400, 422]).toContain(response.status());
    const body = await response.json();
    expect(body.success).toBe(false);
  });

  test('DELETE catalog/products/{id} deletes the product', async () => {
    const response = await apiContext.delete(`catalog/products/${createdProductId}`, {
      headers: authHeaders(),
    });
    expect(response.status()).toBe(200);
    const body = await response.json();
    expect(body.success).toBe(true);
  });

  test('GET catalog/products/{id} returns 404 after deletion', async () => {
    const response = await apiContext.get(`catalog/products/${createdProductId}`);
    expect(response.status()).toBe(404);
    const body = await response.json();
    expect(body.errorDetails.code).toBe('PRODUCT_NOT_FOUND');
  });

  test('DELETE catalog/categories/{createdCategoryId} deletes the created category', async () => {
    const response = await apiContext.delete(`catalog/categories/${createdCategoryId}`, {
      headers: authHeaders(),
    });
    expect(response.status()).toBe(200);
    const body = await response.json();
    expect(body.success).toBe(true);
  });
});
