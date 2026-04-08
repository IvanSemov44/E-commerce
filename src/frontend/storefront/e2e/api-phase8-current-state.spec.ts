import { test, expect, request, APIRequestContext } from '@playwright/test';

const API_BASE = process.env.VITE_API_URL
  ? process.env.VITE_API_URL.replace(/\/?$/, '/')
  : 'http://localhost:5000/api/';

const CUSTOMER_PASSWORD = 'Customer123!';

async function createCustomerContext(): Promise<APIRequestContext> {
  const ctx = await request.newContext({ baseURL: API_BASE });
  const email = `phase8-sync-${crypto.randomUUID()}@example.com`;

  const registerResponse = await ctx.post('auth/register', {
    data: {
      email,
      password: CUSTOMER_PASSWORD,
      firstName: 'Phase',
      lastName: 'Eight',
    },
  });

  expect(registerResponse.ok(), await registerResponse.text()).toBe(true);

  const loginResponse = await ctx.post('auth/login', {
    data: { email, password: CUSTOMER_PASSWORD },
  });

  expect(loginResponse.ok(), await loginResponse.text()).toBe(true);
  await ctx.get('auth/me');

  return ctx;
}

async function getCsrfHeaders(ctx: APIRequestContext): Promise<Record<string, string>> {
  const state = await ctx.storageState();
  const token = state.cookies.find((cookie) => cookie.name === 'XSRF-TOKEN')?.value ?? '';
  return { 'X-XSRF-TOKEN': token };
}

async function resolveProductId(ctx: APIRequestContext): Promise<string> {
  const response = await ctx.get('products?page=1&pageSize=1');
  expect(response.ok(), await response.text()).toBe(true);

  const body = await response.json();
  const product = body.data?.items?.[0];
  const productId = product?.id ?? product?.Id;
  expect(productId, 'Expected seeded product from catalog').toBeTruthy();
  return productId as string;
}

function createOrderPayload(productId: string, quantity: number) {
  return {
    items: [{ productId, quantity }],
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
  };
}

test.describe('Phase 8 Current State API Baseline', () => {
  test('PlaceOrder returns synchronously with either created or address-resolution failure', async () => {
    const ctx = await createCustomerContext();

    try {
      const productId = await resolveProductId(ctx);

      const response = await ctx.post('orders', {
        data: createOrderPayload(productId, 1),
        headers: {
          ...(await getCsrfHeaders(ctx)),
          'Idempotency-Key': crypto.randomUUID(),
        },
      });

      const body = await response.json();

      if (response.status() === 201) {
        expect(body.success).toBe(true);
        expect(body.data?.id).toBeTruthy();
      } else {
        expect(response.status(), JSON.stringify(body)).toBe(400);
        expect(body.errorDetails?.code).toBe('ADDRESS_NOT_FOUND');
      }
    } finally {
      await ctx.dispose();
    }
  });

  test('Insufficient stock request is rejected in current synchronous flow', async () => {
    const ctx = await createCustomerContext();

    try {
      const productId = await resolveProductId(ctx);
      const tooLargeQuantity = 999999;

      const response = await ctx.post('orders', {
        data: createOrderPayload(productId, tooLargeQuantity),
        headers: {
          ...(await getCsrfHeaders(ctx)),
          'Idempotency-Key': crypto.randomUUID(),
        },
      });

      const body = await response.json();
      expect([400, 409, 422]).toContain(response.status());
      expect(['INSUFFICIENT_STOCK', 'ADDRESS_NOT_FOUND']).toContain(body.errorDetails?.code);
    } finally {
      await ctx.dispose();
    }
  });

  test('Add-to-cart endpoint returns immediate success payload', async () => {
    const ctx = await request.newContext({ baseURL: API_BASE });

    try {
      const productId = await resolveProductId(ctx);
      const sessionId = crypto.randomUUID();

      const response = await ctx.post('cart/add-item', {
        data: { productId, quantity: 1 },
        headers: { 'X-Session-ID': sessionId },
      });

      expect(response.status(), await response.text()).toBe(200);
      const body = await response.json();
      expect(body.success).toBe(true);
      expect(body.data).toBeTruthy();
    } finally {
      await ctx.dispose();
    }
  });

  test('If order is created, it is readable immediately', async () => {
    const ctx = await createCustomerContext();

    try {
      const productId = await resolveProductId(ctx);
      const response = await ctx.post('orders', {
        data: createOrderPayload(productId, 1),
        headers: {
          ...(await getCsrfHeaders(ctx)),
          'Idempotency-Key': crypto.randomUUID(),
        },
      });

      const body = await response.json();
      if (response.status() === 201) {
        const orderId = body.data.id;
        const getResponse = await ctx.get(`orders/${orderId}`);
        expect(getResponse.status(), await getResponse.text()).toBe(200);
      } else {
        expect(response.status()).toBe(400);
        expect(body.errorDetails?.code).toBe('ADDRESS_NOT_FOUND');
      }
    } finally {
      await ctx.dispose();
    }
  });
});
