import { test, expect, request, APIRequestContext } from '@playwright/test';

/**
 * API Migration Tests - Catalog Endpoints
 *
 * These tests verify that the Catalog API endpoints work correctly
 * after the DDD/CQRS migration. They test the API directly,
 * making them suitable for CI/CD pipelines and Docker environments.
 *
 * Run with:
 *   npm run test:e2e:api
 *   npm run test:e2e:api:ci (for CI environments)
 *
 * ApiResponse shape (success):  { success: true,  data: T,    errorDetails: null }
 * ApiResponse shape (failure):  { success: false, data: null, errorDetails: { code, message, errors } }
 */

// Trailing slash is required — Playwright resolves '/products' against origin only,
// so 'http://localhost:5000/api/' + 'products' → correct, while '/products' → 'http://localhost:5000/products'
const API_BASE = process.env.VITE_API_URL
  ? process.env.VITE_API_URL.replace(/\/?$/, '/')
  : 'http://localhost:5000/api/';

// Fake GUIDs — guaranteed not to exist in any seeded DB
const NONEXISTENT_GUID = '00000000-0000-0000-0000-999999999999';

test.describe('Catalog API Migration', () => {
  let apiContext: APIRequestContext;

  test.beforeEach(async () => {
    apiContext = await request.newContext({ baseURL: API_BASE });
  });

  test.afterEach(async () => {
    await apiContext.dispose();
  });

  // ===========================================================================
  // GET /products - Product Listing
  // ===========================================================================

  test('GET /products returns 200 with valid response shape', async () => {
    const response = await apiContext.get('products');

    expect(response.ok(), `Expected 200 OK, got ${response.status()}`).toBe(true);

    const body = await response.json();
    expect(body.success).toBe(true);
    expect(body.data).toHaveProperty('items');
    expect(body.data).toHaveProperty('totalCount');
    expect(body.data).toHaveProperty('page');
    expect(body.data).toHaveProperty('pageSize');
    expect(Array.isArray(body.data.items)).toBe(true);
  });

  test('GET /products supports pagination', async () => {
    const response = await apiContext.get('products?page=1&pageSize=5');

    expect(response.ok()).toBe(true);

    const body = await response.json();
    expect(body.data.page).toBe(1);
    expect(body.data.pageSize).toBe(5);
  });

  test('GET /products rejects invalid page parameters', async () => {
    const response = await apiContext.get('products?page=-1&pageSize=0');

    expect(response.status()).toBeGreaterThanOrEqual(400);
  });

  // ===========================================================================
  // GET /products/{id} - Product by ID
  // ===========================================================================

  test('GET /products/{id} returns 404 for non-existent product', async () => {
    const response = await apiContext.get(`products/${NONEXISTENT_GUID}`);

    expect(response.status()).toBe(404);

    const body = await response.json();
    expect(body.success).toBe(false);
    expect(body.errorDetails.code).toBe('PRODUCT_NOT_FOUND');
  });

  test('GET /products/{id} returns 400 for invalid ID format', async () => {
    const response = await apiContext.get('products/not-a-guid');

    expect(response.status()).toBe(400);
  });

  // ===========================================================================
  // GET /products/slug/{slug} - Product by Slug
  // ===========================================================================

  test('GET /products/slug/{slug} returns 404 for non-existent slug', async () => {
    const response = await apiContext.get('products/slug/non-existent-product-zzz');

    expect(response.status()).toBe(404);
  });

  // ===========================================================================
  // GET /products/featured - Featured Products
  // ===========================================================================

  test('GET /products/featured returns featured products list', async () => {
    const response = await apiContext.get('products/featured');

    expect(response.ok()).toBe(true);

    const body = await response.json();
    expect(body.success).toBe(true);
    expect(Array.isArray(body.data.items)).toBe(true);
    expect(body.data).toHaveProperty('totalCount');
    expect(body.data).toHaveProperty('page');
    expect(body.data).toHaveProperty('pageSize');
  });

  test('GET /products/featured respects limit parameter', async () => {
    const response = await apiContext.get('products/featured?pageSize=5');

    expect(response.ok()).toBe(true);

    const body = await response.json();
    expect(body.data.items.length).toBeLessThanOrEqual(5);
  });

  // ===========================================================================
  // GET /products/search - Product Search
  // ===========================================================================

  test('GET /products/search returns paged results', async () => {
    const response = await apiContext.get('products/search?q=laptop');

    expect(response.ok()).toBe(true);

    const body = await response.json();
    expect(body.success).toBe(true);
    expect(body.data).toHaveProperty('items');
    expect(body.data).toHaveProperty('totalCount');
  });

  test('GET /products/search handles empty query', async () => {
    const response = await apiContext.get('products/search?q=');

    expect(response.ok()).toBe(true);
  });

  test('GET /products/search returns empty results for non-matching query', async () => {
    const response = await apiContext.get('products/search?q=zzzznonexistentzzz');

    expect(response.ok()).toBe(true);

    const body = await response.json();
    expect(body.data.totalCount).toBe(0);
    expect(body.data.items).toHaveLength(0);
  });

  // ===========================================================================
  // GET /products/by-category/{categoryId} - Products by Category
  // ===========================================================================

  test('GET /products/by-category/{categoryId} returns 400 for invalid ID', async () => {
    const response = await apiContext.get('products/by-category/not-a-guid');

    expect(response.status()).toBe(400);
  });

  // ===========================================================================
  // GET /products/by-price - Products by Price Range
  // ===========================================================================

  test('GET /products/by-price returns paged products in range', async () => {
    const response = await apiContext.get('products/by-price?min=0&max=1000');

    expect(response.ok()).toBe(true);

    const body = await response.json();
    expect(Array.isArray(body.data.items)).toBe(true);
  });

  test('GET /products/by-price returns 400 when min/max are missing', async () => {
    const response = await apiContext.get('products/by-price');

    expect(response.status()).toBeGreaterThanOrEqual(400);
  });

  // ===========================================================================
  // GET /products/low-stock - Low Stock Products
  // ===========================================================================

  test('GET /products/low-stock returns list', async () => {
    const response = await apiContext.get('products/low-stock?threshold=10');

    expect(response.ok()).toBe(true);

    const body = await response.json();
    expect(body.success).toBe(true);
    expect(Array.isArray(body.data)).toBe(true);
  });

  // ===========================================================================
  // GET /categories - Categories
  // ===========================================================================

  test('GET /categories returns category list', async () => {
    const response = await apiContext.get('categories');

    expect(response.ok()).toBe(true);

    const body = await response.json();
    expect(body.success).toBe(true);
    expect(Array.isArray(body.data.items)).toBe(true);
  });

  test('GET /categories items have expected shape', async () => {
    const response = await apiContext.get('categories');

    expect(response.ok()).toBe(true);

    const body = await response.json();
    if (body.data.items.length > 0) {
      const category = body.data.items[0];
      expect(category).toHaveProperty('id');
      expect(category).toHaveProperty('name');
      expect(category).toHaveProperty('slug');
    }
  });

  // ===========================================================================
  // GET /categories/{id} - Category by ID
  // ===========================================================================

  test('GET /categories/{id} returns 404 for non-existent category', async () => {
    const response = await apiContext.get(`categories/${NONEXISTENT_GUID}`);

    expect(response.status()).toBe(404);

    const body = await response.json();
    expect(body.success).toBe(false);
    expect(body.errorDetails.code).toBe('CATEGORY_NOT_FOUND');
  });

  // ===========================================================================
  // GET /categories/slug/{slug} - Category by Slug
  // ===========================================================================

  test('GET /categories/slug/{slug} returns 404 for non-existent slug', async () => {
    const response = await apiContext.get('categories/slug/zzz-non-existent-zzz');

    expect(response.status()).toBe(404);
  });
});

/* Authentication-required write endpoint tests removed from storefront spec.
   These admin/write tests are covered in src/frontend/admin/e2e/api-catalog-admin.spec.ts */

test.describe('Catalog API - Response Format Consistency', () => {
  let apiContext: APIRequestContext;

  test.beforeEach(async () => {
    apiContext = await request.newContext({ baseURL: API_BASE });
  });

  test.afterEach(async () => {
    await apiContext.dispose();
  });

  test('successful GET endpoints return { success, data }', async () => {
    const endpoints = ['products', 'products/featured', 'categories'];

    for (const endpoint of endpoints) {
      const response = await apiContext.get(endpoint);

      expect(response.ok(), `${endpoint} returned ${response.status()}`).toBe(true);

      const body = await response.json();
      expect(body, `${endpoint} missing 'success'`).toHaveProperty('success', true);
      expect(body, `${endpoint} missing 'data'`).toHaveProperty('data');
    }
  });

  test('error responses include errorDetails.code and errorDetails.message', async () => {
    const response = await apiContext.get(`products/${NONEXISTENT_GUID}`);

    expect(response.status()).toBe(404);

    const body = await response.json();
    expect(body.success).toBe(false);
    expect(body.errorDetails).toHaveProperty('code');
    expect(body.errorDetails).toHaveProperty('message');
  });
});

test.describe('Catalog Gap Coverage', () => {
  let apiContext: APIRequestContext;

  test.beforeEach(async () => {
    apiContext = await request.newContext({ baseURL: API_BASE });
  });

  test.afterEach(async () => {
    await apiContext.dispose();
  });

  // ===========================================================================
  // Group 1 — GET /catalog/categories/top-level
  // ===========================================================================

  test('GET /catalog/categories/top-level returns 200 with array of categories', async () => {
    const response = await apiContext.get('catalog/categories/top-level');

    expect(response.status()).toBe(200);

    const body = await response.json();
    expect(body.success).toBe(true);
    expect(Array.isArray(body.data.items)).toBe(true);
    expect(body.data).toHaveProperty('totalCount');

    if (body.data.items.length > 0) {
      for (const category of body.data.items) {
        expect(category).toHaveProperty('id');
        expect(category).toHaveProperty('name');
        expect(category).toHaveProperty('slug');
        expect(category.parentId).toBeNull();
      }
    }
  });

  // Group 2 — PUT /catalog/products/{id}/stock (moved to admin tests)

  // ===========================================================================
  // Group 3 — GET /catalog/products filter params
  // ===========================================================================

  test('GET /catalog/products?categoryId= returns 200 with filtered array', async () => {
    const response = await apiContext.get(`catalog/products?categoryId=${NONEXISTENT_GUID}`);

    expect(response.ok()).toBe(true);

    const body = await response.json();
    expect(body.success).toBe(true);
    expect(Array.isArray(body.data.items)).toBe(true);
    expect(body.data).toHaveProperty('totalCount');
    expect(body.data).toHaveProperty('page');
    expect(body.data).toHaveProperty('pageSize');
  });

  test('GET /catalog/products?search= returns 200', async () => {
    const response = await apiContext.get('catalog/products?search=nonexistentproductxyz');

    expect(response.ok()).toBe(true);

    const body = await response.json();
    expect(Array.isArray(body.data.items)).toBe(true);
  });

  test('GET /catalog/products?minPrice=&maxPrice= returns 200', async () => {
    const response = await apiContext.get('catalog/products?minPrice=0&maxPrice=9999');

    expect(response.ok()).toBe(true);

    const body = await response.json();
    expect(Array.isArray(body.data.items)).toBe(true);
  });

  test('GET /catalog/products?isFeatured=true returns 200', async () => {
    const response = await apiContext.get('catalog/products?isFeatured=true');

    expect(response.ok()).toBe(true);

    const body = await response.json();
    expect(Array.isArray(body.data.items)).toBe(true);
  });

  test('GET /catalog/products?sortBy= returns 200', async () => {
    const response = await apiContext.get('catalog/products?sortBy=price_asc');

    expect(response.ok()).toBe(true);

    const body = await response.json();
    expect(Array.isArray(body.data.items)).toBe(true);
  });

  // ===========================================================================
  // Group 4 — GET /catalog/products/featured pagination
  // ===========================================================================

  test('GET /catalog/products/featured?page=1&pageSize=5 returns paginated shape', async () => {
    const response = await apiContext.get('catalog/products/featured?page=1&pageSize=5');

    expect(response.ok()).toBe(true);

    const body = await response.json();
    expect(body.success).toBe(true);
    expect(Array.isArray(body.data.items)).toBe(true);
    expect(typeof body.data.totalCount).toBe('number');
    expect(typeof body.data.page).toBe('number');
    expect(typeof body.data.pageSize).toBe('number');
    expect(body.data.page).toBe(1);
    expect(body.data.pageSize).toBe(5);
  });

  test('GET /catalog/products/featured?page=2&pageSize=5 page 2 returns consistent shape', async () => {
    const response = await apiContext.get('catalog/products/featured?page=2&pageSize=5');

    expect(response.ok()).toBe(true);

    const body = await response.json();
    expect(body.data.page).toBe(2);
  });
});
