# Phase 7, Step 0b: E2E Tests (Frontend)

**Prerequisite**: Step 0 characterization tests pass. Backend running on `http://localhost:5000` with the **old** service.

Write Playwright tests that hit the Orders endpoints against a real backend.

---

## File: `src/frontend/storefront/tests/e2e/api-orders.spec.ts`

```typescript
import { test, expect } from "@playwright/test";

const API_BASE = "http://localhost:5000/api";

// Seeded IDs
const CUSTOMER_ID = "99999999-9999-9999-9999-999999999999";
const ORDER_ID = "44444444-4444-4444-4444-444444444444";
const SHIPPED_ORDER_ID = "55555555-5555-5555-5555-555555555555";
const PRODUCT_1 = "11111111-1111-1111-1111-111111111111";
const PRODUCT_2 = "11111111-1111-1111-1111-111111111112";

function randomGuid(): string {
  return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === "x" ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

test.describe("Orders API (OLD Service)", () => {
  test("POST /api/orders - create returns 201 with Location header", async ({
    request,
  }) => {
    const createPayload = {
      customerId: CUSTOMER_ID,
      items: [
        { productId: PRODUCT_1, quantity: 2, unitPrice: 50.0 },
        { productId: PRODUCT_2, quantity: 1, unitPrice: 100.0 },
      ],
      shippingAddressId: randomGuid(),
    };

    const response = await request.post(`${API_BASE}/orders`, {
      data: createPayload,
    });
    expect(response.status()).toBe(201);
    expect(response.headers()["location"]).toBeDefined();

    const data = await response.json();
    expect(data.success).toBe(true);
    expect(data.data).toHaveProperty("id");
    expect(data.data).toHaveProperty("orderNumber");
    expect(data.data.status).toBe("Pending");
  });

  test("POST /api/orders - empty cart returns 400", async ({ request }) => {
    const createPayload = {
      customerId: randomGuid(),
      items: [],
      shippingAddressId: randomGuid(),
    };

    const response = await request.post(`${API_BASE}/orders`, {
      data: createPayload,
    });
    expect(response.status()).toBe(400);

    const data = await response.json();
    expect(data.code).toBe("ORDER_EMPTY");
  });

  test("POST /api/orders - invalid quantity returns 400", async ({
    request,
  }) => {
    const createPayload = {
      customerId: randomGuid(),
      items: [{ productId: randomGuid(), quantity: 0, unitPrice: 50.0 }],
      shippingAddressId: randomGuid(),
    };

    const response = await request.post(`${API_BASE}/orders`, {
      data: createPayload,
    });
    expect(response.status()).toBe(400);

    const data = await response.json();
    expect(data.code).toBe("ORDER_INVALID_QUANTITY");
  });

  test("GET /api/orders/{id} - returns 200 with order data", async ({
    request,
  }) => {
    const response = await request.get(`${API_BASE}/orders/${ORDER_ID}`);
    expect(response.status()).toBe(200);

    const data = await response.json();
    expect(data.data).toHaveProperty("id");
    expect(data.data).toHaveProperty("orderNumber");
    expect(data.data).toHaveProperty("status");
    expect(data.data).toHaveProperty("items");
    expect(Array.isArray(data.data.items)).toBe(true);
  });

  test("GET /api/orders/{id} - unknown ID returns 404", async ({ request }) => {
    const response = await request.get(`${API_BASE}/orders/${randomGuid()}`);
    expect(response.status()).toBe(404);

    const data = await response.json();
    expect(data.code).toBe("ORDER_NOT_FOUND");
  });

  test("GET /api/customers/{customerId}/orders - returns paginated list", async ({
    request,
  }) => {
    const response = await request.get(
      `${API_BASE}/customers/${CUSTOMER_ID}/orders?page=1&pageSize=10`
    );
    expect(response.status()).toBe(200);

    const data = await response.json();
    expect(data.data).toHaveProperty("items");
    expect(data.data).toHaveProperty("page");
    expect(data.data).toHaveProperty("pageSize");
    expect(Array.isArray(data.data.items)).toBe(true);
  });

  test("POST /api/orders/{id}/confirm - pending order returns 200", async ({
    request,
  }) => {
    const response = await request.post(
      `${API_BASE}/orders/${ORDER_ID}/confirm`,
      { data: {} }
    );
    expect(response.status()).toBe(200);

    const data = await response.json();
    expect(data.data.status).toBe("Confirmed");
  });

  test("POST /api/orders/{id}/confirm - unknown ID returns 404", async ({
    request,
  }) => {
    const response = await request.post(
      `${API_BASE}/orders/${randomGuid()}/confirm`,
      { data: {} }
    );
    expect(response.status()).toBe(404);

    const data = await response.json();
    expect(data.code).toBe("ORDER_NOT_FOUND");
  });

  test("POST /api/orders/{id}/ship - confirmed order returns 200", async ({
    request,
  }) => {
    const shipPayload = {
      trackingNumber: "TRK" + Math.random().toString(36).substring(2, 11),
    };

    const response = await request.post(
      `${API_BASE}/orders/${ORDER_ID}/ship`,
      { data: shipPayload }
    );
    expect(response.status()).toBe(200);

    const data = await response.json();
    expect(data.data.status).toBe("Shipped");
  });

  test("POST /api/orders/{id}/cancel - pending order returns 200", async ({
    request,
  }) => {
    const cancelPayload = { reason: "Customer requested" };

    const response = await request.post(
      `${API_BASE}/orders/${ORDER_ID}/cancel`,
      { data: cancelPayload }
    );
    expect(response.status()).toBe(200);

    const data = await response.json();
    expect(data.data.status).toBe("Cancelled");
  });

  test("POST /api/orders/{id}/cancel - shipped order returns 422", async ({
    request,
  }) => {
    const cancelPayload = { reason: "Too late" };

    const response = await request.post(
      `${API_BASE}/orders/${SHIPPED_ORDER_ID}/cancel`,
      { data: cancelPayload }
    );
    expect(response.status()).toBe(422);

    const data = await response.json();
    expect(data.code).toBe("ORDER_CANNOT_CANCEL_SHIPPED");
  });

  test("GET /api/orders/{id} - total calculation is correct", async ({
    request,
  }) => {
    const response = await request.get(`${API_BASE}/orders/${ORDER_ID}`);

    const data = await response.json();
    const order = data.data;
    const expected = order.subtotal + order.tax + order.shippingCost;
    expect(order.total).toBe(expected);
  });

  test("GET /api/orders/admin/pending - non-admin returns 403", async ({
    request,
  }) => {
    const response = await request.get(`${API_BASE}/orders/admin/pending`);
    expect(response.status()).toBe(403);
  });

  test("Order line items - include productId, quantity, unitPrice", async ({
    request,
  }) => {
    const response = await request.get(`${API_BASE}/orders/${ORDER_ID}`);

    const data = await response.json();
    const items = data.data.items;
    expect(items.length).toBeGreaterThan(0);

    for (const item of items) {
      expect(item).toHaveProperty("productId");
      expect(item).toHaveProperty("quantity");
      expect(item).toHaveProperty("unitPrice");
      expect(item.quantity).toBeGreaterThan(0);
      expect(item.unitPrice).toBeGreaterThan(0);
    }
  });
});
```

---

## Run Against OLD Service

```bash
cd src/backend
dotnet run --project ECommerce.API &
sleep 3

cd ../../src/frontend/storefront
npx playwright test api-orders.spec.ts --reporter=list
```

---

## Acceptance Criteria

- [ ] All 13 tests pass against the OLD service
- [ ] Zero 500 errors
- [ ] All error codes match: `ORDER_NOT_FOUND`, `ORDER_EMPTY`, `ORDER_INVALID_QUANTITY`, `ORDER_CANNOT_CANCEL_SHIPPED`
- [ ] POST creates with 201 and Location header
- [ ] Status transitions work: Pending → Confirmed → Shipped
- [ ] Cannot cancel shipped orders (422)
- [ ] Total calculation verified (subtotal + tax + shipping)
- [ ] Admin authorization working (403 for non-admin)
- [ ] Order line items preserved with all properties
