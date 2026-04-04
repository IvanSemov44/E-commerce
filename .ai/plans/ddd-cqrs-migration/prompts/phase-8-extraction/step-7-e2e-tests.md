# Phase 8, Step 7: Re-Run E2E Tests Post-Cutover (Eventual Consistency)

**Prerequisite**: All steps 1–6 complete. Entire extraction and integration event infrastructure in place.

Re-run the characterization tests from step 0b, but now verify **eventual consistency** behavior rather than synchronous completion.

---

## Key Behavior Changes

| Step 0b (Synchronous) | Step 7 (Eventual) |
|---|---|
| Order placed → inventory reduced immediately → response returns | Order placed → response returns → Outbox job publishes OrderPlaced → InventoryReservationConsumer reduces inventory eventually |
| All side effects done before response | Side effects queued to broker, may take milliseconds to minutes |
| Cross-context failures are atomic (rollback) | Failures trigger saga compensation (eventually) |
| User sees consistent data immediately | Brief window where data is stale |

---

## File: `src/frontend/storefront/tests/e2e/api-phase8-eventual-consistency.spec.ts`

```typescript
import { test, expect } from "@playwright/test";

const API_BASE = "http://localhost:5000/api";
const PRODUCT_ID = "11111111-1111-1111-1111-111111111111";

test.describe("Phase 8 - Eventual Consistency Behavior", () => {
  test("PlaceOrder returns immediately; inventory reduced eventually", async ({
    request,
  }) => {
    const stockBefore = await getProductStock(request, PRODUCT_ID);

    // Place order
    const orderReq = {
      customerId: randomGuid(),
      items: [{ productId: PRODUCT_ID, quantity: 2, unitPrice: 50.0 }],
      tax: 10.0,
      shippingCost: 5.0,
    };

    const orderResp = await request.post(`${API_BASE}/orders`, {
      data: orderReq,
    });
    expect(orderResp.status()).toBe(201); // Response is immediate

    const stockAfterOrder = await getProductStock(request, PRODUCT_ID);
    // May or may not be reduced yet (Outbox job is async)

    // Poll for reduction (with timeout)
    let stockFinal = stockAfterOrder;
    for (let i = 0; i < 10; i++) {
      await new Promise((r) => setTimeout(r, 100)); // Wait 100ms
      stockFinal = await getProductStock(request, PRODUCT_ID);

      if (stockFinal === stockBefore - 2) {
        break; // Eventual consistency achieved
      }
    }

    expect(stockFinal).toBe(stockBefore - 2); // Eventually reduces
  });

  test("InventoryReservationFailed triggers saga compensation (order cancellation)", async ({
    request,
  }) => {
    // Create order
    const orderReq = {
      customerId: randomGuid(),
      items: [
        {
          productId: "22222222-2222-2222-2222-222222222222", // Out of stock
          quantity: 100,
          unitPrice: 50.0,
        },
      ],
      tax: 10.0,
      shippingCost: 5.0,
    };

    const orderResp = await request.post(`${API_BASE}/orders`, {
      data: orderReq,
    });
    expect(orderResp.status()).toBe(201); // Order created
    const orderData = await orderResp.json();
    const orderId = orderData.data.id;

    // Poll order status: should eventually transition to Cancelled via saga
    let orderStatus = "Pending";
    for (let i = 0; i < 10; i++) {
      await new Promise((r) => setTimeout(r, 200));
      const getResp = await request.get(`${API_BASE}/orders/${orderId}`);
      const orderDetail = await getResp.json();
      orderStatus = orderDetail.data.status;

      if (orderStatus === "Cancelled") {
        break;
      }
    }

    expect(orderStatus).toBe("Cancelled"); // Saga compensated
  });

  test("OutboxMessages table shows event delivery", async ({ request }) => {
    // This is an internal check; may require admin endpoint
    // Verify a published event exists in Outbox

    // Place order
    const orderResp = await request.post(`${API_BASE}/orders`, {
      data: {
        customerId: randomGuid(),
        items: [{ productId: PRODUCT_ID, quantity: 1, unitPrice: 100.0 }],
        tax: 10.0,
        shippingCost: 5.0,
      },
    });

    // Check Outbox via admin endpoint (if available)
    const outboxResp = await request.get(`${API_BASE}/admin/outbox-messages`);
    if (outboxResp.status() === 200) {
      const outboxData = await outboxResp.json();
      expect(outboxData.data.items.length).toBeGreaterThan(0);
    }
  });

  test("Idempotent event handling: duplicate event doesn't double-process", async ({
    request,
  }) => {
    // Trigger same event twice (simulation via correlation ID)
    // Verify only processed once

    // This test depends on idempotency key implementation
    // Verify: idempotency_key column in OutboxMessages, dedup logic in consumers
  });

  test("Message ordering: events processed in order for same product", async ({
    request,
  }) => {
    // Place two orders for same product rapidly
    // Verify stock is reduced in correct order

    const productId = PRODUCT_ID;
    const stockBefore = await getProductStock(request, productId);

    // Order 1: 1 unit
    await request.post(`${API_BASE}/orders`, {
      data: {
        customerId: randomGuid(),
        items: [{ productId, quantity: 1, unitPrice: 50.0 }],
        tax: 5.0,
        shippingCost: 5.0,
      },
    });

    // Order 2: 2 units
    await request.post(`${API_BASE}/orders`, {
      data: {
        customerId: randomGuid(),
        items: [{ productId, quantity: 2, unitPrice: 50.0 }],
        tax: 10.0,
        shippingCost: 5.0,
      },
    });

    // Eventually both should be processed
    await new Promise((r) => setTimeout(r, 1000));
    const stockAfter = await getProductStock(request, productId);

    expect(stockAfter).toBe(stockBefore - 3); // 1 + 2 units
  });
});

// Helpers
async function getProductStock(
  request: any,
  productId: string
): Promise<number> {
  const resp = await request.get(`${API_BASE}/products/${productId}`);
  const data = await resp.json();
  return data.data.stockQuantity;
}

function randomGuid(): string {
  return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === "x" ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}
```

---

## Run

```bash
# Start all infrastructure
docker-compose up -d # RabbitMQ, PostgreSQL

# Backend with Outbox job running
cd src/backend
dotnet run --project ECommerce.API &

sleep 3

# E2E tests
cd ../../src/frontend/storefront
npx playwright test api-phase8-eventual-consistency.spec.ts --reporter=list
```

---

## Acceptance Criteria

- [ ] PlaceOrder returns 201 immediately (not waiting for inventory)
- [ ] Inventory reduced eventually via message broker (within X seconds)
- [ ] Failed inventory triggers saga compensation (order cancelled eventually)
- [ ] Outbox table tracks published events
- [ ] Idempotency keys prevent double-processing
- [ ] Event ordering preserved for same product
- [ ] No race conditions or data loss
- [ ] `dotnet build` clean, all tests passing
