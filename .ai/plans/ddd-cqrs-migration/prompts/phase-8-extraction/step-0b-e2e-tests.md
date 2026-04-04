# Phase 8, Step 0b: E2E Tests (Current Synchronous Behavior)

**Prerequisite**: Step 0 characterization tests pass.

Write Playwright E2E tests that verify synchronous cross-context behavior using the **current** system (single `AppDbContext`, MediatR domain events).

---

## File: `src/frontend/storefront/tests/e2e/api-phase8-current-state.spec.ts`

```typescript
import { test, expect } from "@playwright/test";

const API_BASE = "http://localhost:5000/api";

const PRODUCT_ID = "11111111-1111-1111-1111-111111111111";
const PROMO_CODE_ID = "55555555-5555-5555-5555-555555555555"; // SAVE20

test.describe("Phase 8 - Current Synchronous Behavior", () => {
  test("PlaceOrder reduces inventory synchronously", async ({ request }) => {
    // Get current stock
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
    expect(orderResp.status()).toBe(201);

    // Stock should be reduced IMMEDIATELY
    const stockAfter = await getProductStock(request, PRODUCT_ID);
    expect(stockAfter).toBe(stockBefore - 2);
  });

  test("AddToCart updates product LastViewedAt synchronously", async ({
    request,
  }) => {
    const productBefore = await getProductDetail(request, PRODUCT_ID);
    const lastViewedBefore = new Date(productBefore.lastViewedAt).getTime();

    // Add to cart
    await request.post(`${API_BASE}/cart/add-item`, {
      data: { productId: PRODUCT_ID, quantity: 1 },
    });

    // LastViewedAt should be updated IMMEDIATELY
    const productAfter = await getProductDetail(request, PRODUCT_ID);
    const lastViewedAfter = new Date(productAfter.lastViewedAt).getTime();

    expect(lastViewedAfter).toBeGreaterThanOrEqual(lastViewedBefore);
  });

  test("PlaceOrder with PromoCode increments usage synchronously", async ({
    request,
  }) => {
    const promoBefore = await getPromoCodeDetail(request, PROMO_CODE_ID);

    const orderReq = {
      customerId: randomGuid(),
      items: [{ productId: randomGuid(), quantity: 1, unitPrice: 100.0 }],
      promoCodeId: PROMO_CODE_ID,
      tax: 10.0,
      shippingCost: 5.0,
    };

    await request.post(`${API_BASE}/orders`, { data: orderReq });

    const promoAfter = await getPromoCodeDetail(request, PROMO_CODE_ID);
    expect(promoAfter.usedCount).toBe(promoBefore.usedCount + 1);
  });

  test("Cross-context failure rolls back atomically", async ({ request }) => {
    const outOfStockProduct = "22222222-2222-2222-2222-222222222222";

    const orderReq = {
      customerId: randomGuid(),
      items: [{ productId: outOfStockProduct, quantity: 100, unitPrice: 50.0 }],
      tax: 10.0,
      shippingCost: 5.0,
    };

    const resp = await request.post(`${API_BASE}/orders`, { data: orderReq });
    expect(resp.status()).toBe(400);

    const data = await resp.json();
    expect(data.code).toBe("INSUFFICIENT_INVENTORY");
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

async function getProductDetail(request: any, productId: string) {
  const resp = await request.get(`${API_BASE}/products/${productId}`);
  const data = await resp.json();
  return data.data;
}

async function getPromoCodeDetail(request: any, promoCodeId: string) {
  const resp = await request.get(`${API_BASE}/promo-codes/${promoCodeId}`);
  const data = await resp.json();
  return data.data;
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
cd src/backend
dotnet run --project ECommerce.API &
sleep 3

cd ../../src/frontend/storefront
npx playwright test api-phase8-current-state.spec.ts --reporter=list
```

All tests must pass. This is the baseline for Phase 8 refactoring.

---

## Acceptance Criteria

- [ ] PlaceOrder inventory reduction is synchronous
- [ ] AddToCart product metadata updates are synchronous
- [ ] PromoCode usage increments are synchronous
- [ ] Cross-context failures are atomic
- [ ] All side effects happen before response returns
