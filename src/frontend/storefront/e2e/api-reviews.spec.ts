import { test, expect, request, APIRequestContext } from '@playwright/test';

const API_BASE = process.env.VITE_API_URL
  ? process.env.VITE_API_URL.replace(/\/?$/, '/')
  : 'http://localhost:5000/api/';

const ADMIN_EMAIL = `admin-${crypto.randomUUID()}@example.com`;
const ADMIN_PASSWORD = 'Admin123';
const CUSTOMER_PASSWORD = 'Customer123!';

async function loginAsAdmin(ctx: APIRequestContext): Promise<void> {
  const response = await ctx.post('auth/login', {
    data: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD },
  });

  expect(response.ok(), `Admin login failed with status ${response.status()}`).toBe(true);
  await ctx.get('auth/me');
}

async function createCustomerContext(): Promise<{ ctx: APIRequestContext; email: string }> {
  const ctx = await request.newContext({ baseURL: API_BASE });
  const email = `reviews-e2e-${crypto.randomUUID()}@example.com`;

  const registerResponse = await ctx.post('auth/register', {
    data: {
      email,
      password: CUSTOMER_PASSWORD,
      firstName: 'Reviews',
      lastName: 'E2E',
    },
  });

  expect(
    registerResponse.ok(),
    `Customer registration failed with status ${registerResponse.status()}`
  ).toBe(true);

  const loginResponse = await ctx.post('auth/login', {
    data: { email, password: CUSTOMER_PASSWORD },
  });

  expect(loginResponse.ok(), `Customer login failed with status ${loginResponse.status()}`).toBe(
    true
  );
  await ctx.get('auth/me');

  return { ctx, email };
}

async function getCsrfHeaders(ctx: APIRequestContext): Promise<Record<string, string>> {
  const state = await ctx.storageState();
  const token = state.cookies.find((cookie) => cookie.name === 'XSRF-TOKEN')?.value ?? '';
  return { 'X-XSRF-TOKEN': token };
}

async function resolveProductId(ctx: APIRequestContext): Promise<string> {
  const response = await ctx.get('products?page=1&pageSize=1');

  expect(response.ok(), `Catalog lookup failed with status ${response.status()}`).toBe(true);

  const body = await response.json();
  const product = body.data?.items?.[0];
  const productId = product?.id ?? product?.Id;

  expect(productId, 'Expected the catalog API to return at least one seeded product').toBeTruthy();
  return productId as string;
}

async function createReview(
  ctx: APIRequestContext,
  productId: string,
  overrides: Record<string, unknown> = {}
) {
  const response = await ctx.post('reviews', {
    data: {
      productId,
      rating: 5,
      title: 'Great Product',
      comment: 'Excellent quality and fast delivery',
      ...overrides,
    },
    headers: await getCsrfHeaders(ctx),
  });

  expect(response.status(), await response.text()).toBe(201);
  const body = await response.json();
  return body.data;
}

function randomGuid(): string {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (character) => {
    const random = (Math.random() * 16) | 0;
    return (character === 'x' ? random : (random & 0x3) | 0x8).toString(16);
  });
}

test.describe('Reviews API', () => {
  test('GET /reviews/product/{productId} returns approved review list shape', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });

    try {
      const productId = await resolveProductId(ctx);
      const response = await ctx.get(`reviews/product/${productId}?page=1&pageSize=10`);

      expect(response.status(), await response.text()).toBe(200);
      const body = await response.json();

      expect(body.success).toBe(true);
      expect(body.data).toBeDefined();
      expect(Array.isArray(body.data.items)).toBe(true);
      expect(typeof body.data.totalCount).toBe('number');
    } finally {
      await ctx.dispose();
    }
  });

  test('GET /reviews/product/{productId} unknown product returns 404 PRODUCT_NOT_FOUND', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });

    try {
      const response = await ctx.get(`reviews/product/${randomGuid()}?page=1&pageSize=10`);

      expect(response.status()).toBe(404);
      const body = await response.json();
      expect(body.errorDetails?.code).toBe('PRODUCT_NOT_FOUND');
    } finally {
      await ctx.dispose();
    }
  });

  test('GET /reviews/product/{productId}/rating returns a numeric rating', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });

    try {
      const productId = await resolveProductId(ctx);
      const response = await ctx.get(`reviews/product/${productId}/rating`);

      expect(response.status(), await response.text()).toBe(200);
      const body = await response.json();
      expect(body.success).toBe(true);
      expect(typeof body.data).toBe('number');
    } finally {
      await ctx.dispose();
    }
  });

  test('POST /reviews creates a pending review for an authenticated customer', async () => {
    const { ctx } = await createCustomerContext();
    let reviewId: string | null = null;

    try {
      const productId = await resolveProductId(ctx);
      const response = await ctx.post('reviews', {
        data: {
          productId,
          rating: 5,
          title: 'Great Product',
          comment: 'Excellent quality and fast delivery',
        },
        headers: await getCsrfHeaders(ctx),
      });

      expect(response.status(), await response.text()).toBe(201);
      expect(response.headers()['location']).toContain('/api/Reviews/');

      const body = await response.json();
      reviewId = body.data?.id ?? null;
      expect(body.success).toBe(true);
      expect(body.data.productId).toBe(productId);
      expect(body.data.rating).toBe(5);
      expect(body.data.isApproved).toBe(false);
      expect(body.data.isVerified).toBe(false);
    } finally {
      if (reviewId) {
        await ctx.delete(`reviews/${reviewId}`, {
          headers: await getCsrfHeaders(ctx),
        });
      }

      await ctx.dispose();
    }
  });

  test('POST /reviews duplicate user and product returns 409 DUPLICATE_REVIEW', async () => {
    const { ctx } = await createCustomerContext();
    let reviewId: string | null = null;

    try {
      const productId = await resolveProductId(ctx);
      const firstResponse = await ctx.post('reviews', {
        data: {
          productId,
          rating: 4,
          title: 'First Review',
          comment: 'This is a unique review body for the product',
        },
        headers: await getCsrfHeaders(ctx),
      });

      expect(firstResponse.status(), await firstResponse.text()).toBe(201);
      reviewId = (await firstResponse.json()).data?.id ?? null;

      const secondResponse = await ctx.post('reviews', {
        data: {
          productId,
          rating: 3,
          title: 'Second Review',
          comment: 'Second attempt for the same product',
        },
        headers: await getCsrfHeaders(ctx),
      });

      expect(secondResponse.status()).toBe(409);
      const body = await secondResponse.json();
      expect(body.errorDetails?.code).toBe('DUPLICATE_REVIEW');
    } finally {
      if (reviewId) {
        await ctx.delete(`reviews/${reviewId}`, {
          headers: await getCsrfHeaders(ctx),
        });
      }

      await ctx.dispose();
    }
  });

  test('POST /reviews invalid rating returns 400 VALIDATION_FAILED', async () => {
    const { ctx } = await createCustomerContext();

    try {
      const productId = await resolveProductId(ctx);
      const response = await ctx.post('reviews', {
        data: {
          productId,
          rating: 6,
          title: 'Bad Rating',
          comment: 'Invalid rating value',
        },
        headers: await getCsrfHeaders(ctx),
      });

      expect(response.status()).toBe(400);
    } finally {
      await ctx.dispose();
    }
  });

  test('POST /reviews empty comment returns 400 VALIDATION_FAILED', async () => {
    const { ctx } = await createCustomerContext();

    try {
      const productId = await resolveProductId(ctx);
      const response = await ctx.post('reviews', {
        data: {
          productId,
          rating: 4,
          title: 'Empty Comment',
          comment: '   ',
        },
        headers: await getCsrfHeaders(ctx),
      });

      expect(response.status()).toBe(400);
    } finally {
      await ctx.dispose();
    }
  });

  test('GET /reviews/{reviewId} returns created review details', async () => {
    const { ctx } = await createCustomerContext();
    let reviewId: string | null = null;

    try {
      const productId = await resolveProductId(ctx);
      const created = await createReview(ctx, productId, {
        rating: 3,
        title: 'Readable Review',
        comment: 'This review should be visible by its id',
      });
      reviewId = created.id;

      const response = await ctx.get(`reviews/${reviewId}`);

      expect(response.status(), await response.text()).toBe(200);
      const body = await response.json();
      expect(body.success).toBe(true);
      expect(body.data.id).toBe(reviewId);
      expect(body.data.rating).toBe(3);
      expect(body.data.isApproved).toBe(false);
    } finally {
      if (reviewId) {
        await ctx.delete(`reviews/${reviewId}`, {
          headers: await getCsrfHeaders(ctx),
        });
      }

      await ctx.dispose();
    }
  });

  test('PUT /reviews/{reviewId} updates an owned review', async () => {
    const { ctx } = await createCustomerContext();
    let reviewId: string | null = null;

    try {
      const productId = await resolveProductId(ctx);
      const created = await createReview(ctx, productId, {
        rating: 3,
        title: 'Original Title',
        comment: 'Original review comment for update test',
      });
      reviewId = created.id;

      const response = await ctx.put(`reviews/${reviewId}`, {
        data: {
          rating: 4,
          title: 'Updated Title',
          comment: 'Updated review comment body',
        },
        headers: await getCsrfHeaders(ctx),
      });

      expect(response.status(), await response.text()).toBe(200);
      const body = await response.json();
      expect(body.success).toBe(true);
      expect(body.data.rating).toBe(4);
      expect(body.data.title).toBe('Updated Title');
      expect(body.data.comment).toBe('Updated review comment body');
    } finally {
      if (reviewId) {
        await ctx.delete(`reviews/${reviewId}`, {
          headers: await getCsrfHeaders(ctx),
        });
      }

      await ctx.dispose();
    }
  });

  test('PUT /reviews/{reviewId} unknown id returns 404 REVIEW_NOT_FOUND', async () => {
    const { ctx } = await createCustomerContext();

    try {
      const response = await ctx.put(`reviews/${randomGuid()}`, {
        data: {
          rating: 4,
          title: 'Updated Title',
          comment: 'Updated review comment body',
        },
        headers: await getCsrfHeaders(ctx),
      });

      expect(response.status()).toBe(404);
      const body = await response.json();
      expect(body.errorDetails?.code).toBe('REVIEW_NOT_FOUND');
    } finally {
      await ctx.dispose();
    }
  });

  test('DELETE /reviews/{reviewId} deletes an owned review', async () => {
    const { ctx } = await createCustomerContext();

    try {
      const productId = await resolveProductId(ctx);
      const created = await createReview(ctx, productId, {
        rating: 5,
        title: 'Delete Me',
        comment: 'This review will be deleted in the test',
      });

      const response = await ctx.delete(`reviews/${created.id}`, {
        headers: await getCsrfHeaders(ctx),
      });

      expect(response.status(), await response.text()).toBe(200);
    } finally {
      await ctx.dispose();
    }
  });

  test('DELETE /reviews/{reviewId} unknown id returns 404 REVIEW_NOT_FOUND', async () => {
    const { ctx } = await createCustomerContext();

    try {
      const response = await ctx.delete(`reviews/${randomGuid()}`, {
        headers: await getCsrfHeaders(ctx),
      });

      expect(response.status()).toBe(404);
      const body = await response.json();
      expect(body.errorDetails?.code).toBe('REVIEW_NOT_FOUND');
    } finally {
      await ctx.dispose();
    }
  });

  test('POST /reviews/{reviewId}/approve non-admin returns 403', async () => {
    const { ctx } = await createCustomerContext();

    try {
      const response = await ctx.post(`reviews/${randomGuid()}/approve`, {
        data: {},
        headers: await getCsrfHeaders(ctx),
      });

      expect(response.status()).toBe(403);
    } finally {
      await ctx.dispose();
    }
  });

  test('GET /reviews/admin/pending non-admin returns 403', async () => {
    const { ctx } = await createCustomerContext();

    try {
      const response = await ctx.get('reviews/admin/pending');

      expect(response.status()).toBe(403);
    } finally {
      await ctx.dispose();
    }
  });

  test('GET /reviews/admin/pending admin sees pending review', async () => {
    const { ctx: customerCtx } = await createCustomerContext();
    const adminCtx = await request.newContext({ baseURL: API_BASE });
    let reviewId: string | null = null;

    try {
      const productId = await resolveProductId(customerCtx);
      const created = await createReview(customerCtx, productId, {
        rating: 4,
        title: 'Pending Baseline',
        comment: 'Pending review body that should appear for admins',
      });
      reviewId = created.id;

      await loginAsAdmin(adminCtx);
      const response = await adminCtx.get('reviews/admin/pending?page=1&pageSize=10');

      expect(response.status(), await response.text()).toBe(200);
      const body = await response.json();
      expect(body.success).toBe(true);
      expect(Array.isArray(body.data.items)).toBe(true);
      expect(body.data.items.some((review: { id: string }) => review.id === reviewId)).toBe(true);
    } finally {
      if (reviewId) {
        await customerCtx.delete(`reviews/${reviewId}`, {
          headers: await getCsrfHeaders(customerCtx),
        });
      }

      await customerCtx.dispose();
      await adminCtx.dispose();
    }
  });
});
