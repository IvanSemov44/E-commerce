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
  const email = `orders-e2e-${crypto.randomUUID()}@example.com`;

  const registerResponse = await ctx.post('auth/register', {
    data: {
      email,
      password: CUSTOMER_PASSWORD,
      firstName: 'Orders',
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

async function getIdempotencyHeaders(): Promise<Record<string, string>> {
  return { 'Idempotency-Key': crypto.randomUUID() };
}

async function resolveProductId(ctx: APIRequestContext): Promise<string> {
  const response = await ctx.get('products?page=1&pageSize=1');

  expect(response.ok(), `Catalog lookup failed with status ${response.status()}`).toBe(true);

  const body = await response.json();
  const product = body.data?.items?.[0];
  const productId = product?.id ?? product?.Id;

  expect(productId, 'Expected at least one seeded product').toBeTruthy();
  return productId as string;
}

async function createOrder(
  ctx: APIRequestContext,
  productId: string,
  overrides: Record<string, unknown> = {},
  idempotencyKey?: string
) {
  const response = await ctx.post('orders', {
    data: {
      items: [{ productId, quantity: 1 }],
      shippingAddress: {
        firstName: 'Jordan',
        lastName: 'Casey',
        phone: '555-0100',
        streetLine1: '123 Market St',
        city: 'Austin',
        state: 'TX',
        postalCode: '73301',
        country: 'US',
      },
      paymentMethod: 'credit_card',
      guestEmail: 'guest@example.com',
      ...overrides,
    },
    headers: {
      ...(await getIdempotencyHeaders()),
      ...(await getCsrfHeaders(ctx)),
      ...(idempotencyKey ? { 'Idempotency-Key': idempotencyKey } : {}),
    },
  });

  expect(response.status(), await response.text()).toBe(201);
  const body = await response.json();
  return { response, data: body.data as { id: string; orderNumber: string } };
}

function randomGuid(): string {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (character) => {
    const random = (Math.random() * 16) | 0;
    return (character === 'x' ? random : (random & 0x3) | 0x8).toString(16);
  });
}

test.describe('Orders API', () => {
  test('GET /orders/my-orders returns a paginated list for authenticated user', async () => {
    const { ctx } = await createCustomerContext();

    try {
      const response = await ctx.get('orders/my-orders?page=1&pageSize=10');

      expect(response.status(), await response.text()).toBe(200);
      const body = await response.json();
      expect(body.success).toBe(true);
      expect(Array.isArray(body.data.items)).toBe(true);
      expect(typeof body.data.totalCount).toBe('number');
    } finally {
      await ctx.dispose();
    }
  });

  test('GET /orders/{id} unknown order returns 404', async () => {
    const { ctx } = await createCustomerContext();

    try {
      const response = await ctx.get(`orders/${randomGuid()}`);

      expect(response.status()).toBe(404);
      const body = await response.json();
      expect(body.errorDetails?.code).toBe('ORDER_NOT_FOUND');
    } finally {
      await ctx.dispose();
    }
  });

  test('GET /orders/number/{orderNumber} unknown order returns 404', async () => {
    const { ctx } = await createCustomerContext();

    try {
      const response = await ctx.get('orders/number/DOES-NOT-EXIST');

      expect(response.status()).toBe(404);
      const body = await response.json();
      expect(body.errorDetails?.code).toBe('ORDER_NOT_FOUND');
    } finally {
      await ctx.dispose();
    }
  });

  test('POST /orders creates an order and returns 201', async () => {
    const { ctx } = await createCustomerContext();

    try {
      const productId = await resolveProductId(ctx);
      const response = await ctx.post('orders', {
        data: {
          items: [{ productId, quantity: 1 }],
          shippingAddress: {
            firstName: 'Jordan',
            lastName: 'Casey',
            phone: '555-0100',
            streetLine1: '123 Market St',
            city: 'Austin',
            state: 'TX',
            postalCode: '73301',
            country: 'US',
          },
          paymentMethod: 'credit_card',
          guestEmail: 'guest@example.com',
        },
        headers: {
          ...(await getCsrfHeaders(ctx)),
          ...(await getIdempotencyHeaders()),
        },
      });

      expect(response.status(), await response.text()).toBe(201);
      const body = await response.json();
      expect(body.success).toBe(true);
      expect(body.data.orderNumber).toBeTruthy();
      expect(body.data.items.length).toBe(1);
    } finally {
      await ctx.dispose();
    }
  });

  test('POST /orders same idempotency key creates another order on the live backend', async () => {
    const { ctx } = await createCustomerContext();

    try {
      const productId = await resolveProductId(ctx);
      const idempotencyKey = crypto.randomUUID();

      const first = await createOrder(ctx, productId, {}, idempotencyKey);
      const second = await ctx.post('orders', {
        data: {
          items: [{ productId, quantity: 1 }],
          shippingAddress: {
            firstName: 'Jordan',
            lastName: 'Casey',
            phone: '555-0100',
            streetLine1: '123 Market St',
            city: 'Austin',
            state: 'TX',
            postalCode: '73301',
            country: 'US',
          },
          paymentMethod: 'credit_card',
          guestEmail: 'guest@example.com',
        },
        headers: {
          ...(await getCsrfHeaders(ctx)),
          'Idempotency-Key': idempotencyKey,
        },
      });

      expect(second.status(), await second.text()).toBe(201);
      const body = await second.json();
      expect(body.data.id).not.toBe(first.data.id);
      expect(body.data.orderNumber).not.toBe(first.data.orderNumber);
    } finally {
      await ctx.dispose();
    }
  });

  test('GET /orders/{id} returns created order details', async () => {
    const { ctx } = await createCustomerContext();

    try {
      const productId = await resolveProductId(ctx);
      const created = await createOrder(ctx, productId);
      const response = await ctx.get(`orders/${created.data.id}`);

      expect(response.status(), await response.text()).toBe(200);
      const body = await response.json();
      expect(body.success).toBe(true);
      expect(body.data.id).toBe(created.data.id);
      expect(body.data.orderNumber).toBe(created.data.orderNumber);
    } finally {
      await ctx.dispose();
    }
  });

  test('GET /orders/number/{orderNumber} returns created order details', async () => {
    const { ctx } = await createCustomerContext();

    try {
      const productId = await resolveProductId(ctx);
      const created = await createOrder(ctx, productId);
      const response = await ctx.get(`orders/number/${created.data.orderNumber}`);

      expect(response.status(), await response.text()).toBe(200);
      const body = await response.json();
      expect(body.success).toBe(true);
      expect(body.data.id).toBe(created.data.id);
      expect(body.data.orderNumber).toBe(created.data.orderNumber);
    } finally {
      await ctx.dispose();
    }
  });

  test('PUT /orders/{id}/status non-admin returns 403', async () => {
    const { ctx } = await createCustomerContext();

    try {
      const response = await ctx.put(`orders/${randomGuid()}/status`, {
        data: { status: 'confirmed' },
        headers: await getCsrfHeaders(ctx),
      });

      expect(response.status()).toBe(403);
    } finally {
      await ctx.dispose();
    }
  });

  test('GET /orders admin returns a paginated list', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });

    try {
      await loginAsAdmin(ctx);
      const response = await ctx.get('orders?page=1&pageSize=10');

      expect(response.status(), await response.text()).toBe(200);
      const body = await response.json();
      expect(body.success).toBe(true);
      expect(Array.isArray(body.data.items)).toBe(true);
      expect(typeof body.data.totalCount).toBe('number');
    } finally {
      await ctx.dispose();
    }
  });

  test('PUT /orders/{id}/status admin can update an order', async () => {
    const { ctx } = await createCustomerContext();
    const adminCtx = await request.newContext({ baseURL: API_BASE });

    try {
      const productId = await resolveProductId(ctx);
      const created = await createOrder(ctx, productId);

      await loginAsAdmin(adminCtx);
      const response = await adminCtx.put(`orders/${created.data.id}/status`, {
        data: { status: 'confirmed' },
        headers: await getCsrfHeaders(adminCtx),
      });

      expect(response.status(), await response.text()).toBe(200);
      const body = await response.json();
      expect(body.success).toBe(true);
      expect(body.data.id).toBe(created.data.id);
      expect(body.data.status).toBeTruthy();
    } finally {
      await ctx.dispose();
      await adminCtx.dispose();
    }
  });

  test('POST /orders/{id}/cancel customer can cancel an order', async () => {
    const { ctx } = await createCustomerContext();

    try {
      const productId = await resolveProductId(ctx);
      const created = await createOrder(ctx, productId);
      const response = await ctx.post(`orders/${created.data.id}/cancel`, {
        headers: {
          ...(await getCsrfHeaders(ctx)),
          ...(await getIdempotencyHeaders()),
        },
      });

      expect(response.status(), await response.text()).toBe(200);
      const body = await response.json();
      expect(body.success).toBe(true);
    } finally {
      await ctx.dispose();
    }
  });
});
