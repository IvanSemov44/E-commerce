# Phase 6, Step 0b: E2E Tests (Frontend)

**Prerequisite**: Step 0 characterization tests pass. Backend running on `http://localhost:5000` with the **old** service.

Write Playwright tests that hit the Reviews endpoints against a real backend (with real PostgreSQL).

---

## File: `src/frontend/storefront/tests/e2e/api-reviews.spec.ts`

```typescript
import { test, expect } from "@playwright/test";

const API_BASE = "http://localhost:5000/api";

// Seeded IDs
const PRODUCT_ID = "11111111-1111-1111-1111-111111111111";
const APPROVED_REVIEW_ID = "22222222-2222-2222-2222-222222222222";
const PENDING_REVIEW_ID = "33333333-3333-3333-3333-333333333333";

// Helper: Generate unique code for testing
function uniqueCode(): string {
  return "REVIEW_" + Math.random().toString(36).substring(2, 11).toUpperCase();
}

// Helper: Generate random GUID
function randomGuid(): string {
  return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === "x" ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

test.describe("Reviews API (OLD Service)", () => {
  test("GET /api/products/{productId}/reviews - returns approved reviews only", async ({
    request,
  }) => {
    const response = await request.get(
      `${API_BASE}/products/${PRODUCT_ID}/reviews`
    );
    expect(response.status()).toBe(200);

    const data = await response.json();
    expect(data.success).toBe(true);
    expect(data.data).toBeDefined();
    expect(data.data.items).toBeInstanceOf(Array);

    // All returned reviews must have status "Approved"
    for (const review of data.data.items) {
      expect(review.status).toBe("Approved");
      expect(review).toHaveProperty("id");
      expect(review).toHaveProperty("rating");
      expect(review).toHaveProperty("text");
      expect(review).toHaveProperty("authorName");
      expect(review).toHaveProperty("helpfulCount");
    }
  });

  test("GET /api/products/{productId}/reviews - unknown product returns 200 with empty list", async ({
    request,
  }) => {
    const unknownId = randomGuid();
    const response = await request.get(
      `${API_BASE}/products/${unknownId}/reviews`
    );
    expect(response.status()).toBe(200);

    const data = await response.json();
    expect(data.data.items).toEqual([]);
  });

  test("POST /api/reviews - create returns 201 with Location header", async ({
    request,
  }) => {
    const createPayload = {
      productId: PRODUCT_ID,
      rating: 5,
      text: "Excellent product!",
    };

    const response = await request.post(`${API_BASE}/reviews`, {
      data: createPayload,
    });
    expect(response.status()).toBe(201);
    expect(response.headers()["location"]).toBeDefined();

    const data = await response.json();
    expect(data.success).toBe(true);
    expect(data.data).toHaveProperty("id");
    expect(data.data.rating).toBe(5);
    expect(data.data.text).toBe("Excellent product!");
    expect(data.data.status).toBe("Pending"); // New reviews start as Pending
  });

  test("POST /api/reviews - duplicate user+product returns 409", async ({
    request,
  }) => {
    // User 99999... already reviewed PRODUCT_ID (seeded)
    const createPayload = {
      productId: PRODUCT_ID,
      userId: "99999999-9999-9999-9999-999999999999",
      rating: 3,
      text: "Another review attempt",
    };

    const response = await request.post(`${API_BASE}/reviews`, {
      data: createPayload,
    });
    expect(response.status()).toBe(409);

    const data = await response.json();
    expect(data.code).toBe("DUPLICATE_REVIEW");
  });

  test("POST /api/reviews - invalid rating returns 400", async ({ request }) => {
    const createPayload = {
      productId: randomGuid(),
      rating: 6, // Invalid: out of range
      text: "Bad rating",
    };

    const response = await request.post(`${API_BASE}/reviews`, {
      data: createPayload,
    });
    expect(response.status()).toBe(400);

    const data = await response.json();
    expect(data.code).toBe("INVALID_RATING");
  });

  test("POST /api/reviews - empty text returns 400", async ({ request }) => {
    const createPayload = {
      productId: randomGuid(),
      rating: 4,
      text: "", // Empty
    };

    const response = await request.post(`${API_BASE}/reviews`, {
      data: createPayload,
    });
    expect(response.status()).toBe(400);

    const data = await response.json();
    expect(data.code).toBe("REVIEW_TEXT_EMPTY");
  });

  test("PUT /api/reviews/{id} - update returns 200", async ({ request }) => {
    const updatePayload = {
      rating: 4,
      text: "Updated review text",
    };

    const response = await request.put(`${API_BASE}/reviews/${APPROVED_REVIEW_ID}`, {
      data: updatePayload,
    });
    expect(response.status()).toBe(200);

    const data = await response.json();
    expect(data.data.rating).toBe(4);
    expect(data.data.text).toBe("Updated review text");
  });

  test("PUT /api/reviews/{id} - unknown ID returns 404", async ({ request }) => {
    const unknownId = randomGuid();
    const updatePayload = {
      rating: 3,
      text: "New text",
    };

    const response = await request.put(`${API_BASE}/reviews/${unknownId}`, {
      data: updatePayload,
    });
    expect(response.status()).toBe(404);

    const data = await response.json();
    expect(data.code).toBe("REVIEW_NOT_FOUND");
  });

  test("DELETE /api/reviews/{id} - delete returns 200", async ({ request }) => {
    // First create a review to delete
    const createPayload = {
      productId: PRODUCT_ID,
      rating: 2,
      text: "To be deleted",
    };
    const createResp = await request.post(`${API_BASE}/reviews`, {
      data: createPayload,
    });
    const createdId = (await createResp.json()).data.id;

    const response = await request.delete(`${API_BASE}/reviews/${createdId}`);
    expect(response.status()).toBe(200);
  });

  test("DELETE /api/reviews/{id} - unknown ID returns 404", async ({ request }) => {
    const unknownId = randomGuid();

    const response = await request.delete(`${API_BASE}/reviews/${unknownId}`);
    expect(response.status()).toBe(404);

    const data = await response.json();
    expect(data.code).toBe("REVIEW_NOT_FOUND");
  });

  test("POST /api/reviews/{id}/mark-helpful - increments helpful count", async ({
    request,
  }) => {
    // Get current helpful count
    const getBefore = await request.get(`${API_BASE}/reviews/${APPROVED_REVIEW_ID}`);
    const beforeData = await getBefore.json();
    const beforeCount = beforeData.data.helpfulCount;

    // Mark as helpful
    const response = await request.post(
      `${API_BASE}/reviews/${APPROVED_REVIEW_ID}/mark-helpful`,
      { data: {} }
    );
    expect(response.status()).toBe(200);

    // Verify increment
    const getAfter = await request.get(`${API_BASE}/reviews/${APPROVED_REVIEW_ID}`);
    const afterData = await getAfter.json();
    expect(afterData.data.helpfulCount).toBe(beforeCount + 1);
  });

  test("POST /api/reviews/{id}/flag - sets flagged status", async ({ request }) => {
    const flagPayload = {
      reason: "Inappropriate language",
    };

    const response = await request.post(
      `${API_BASE}/reviews/${APPROVED_REVIEW_ID}/flag`,
      { data: flagPayload }
    );
    expect(response.status()).toBe(200);

    const getResp = await request.get(`${API_BASE}/reviews/${APPROVED_REVIEW_ID}`);
    const data = await getResp.json();
    expect(data.data.flagCount).toBeGreaterThan(0);
  });

  test("POST /api/reviews/{id}/approve - non-admin returns 403", async ({
    request,
  }) => {
    const response = await request.post(
      `${API_BASE}/reviews/${PENDING_REVIEW_ID}/approve`,
      { data: {} }
    );
    expect(response.status()).toBe(403);
  });

  test("GET /api/reviews/admin/pending - non-admin returns 403", async ({
    request,
  }) => {
    const response = await request.get(`${API_BASE}/reviews/admin/pending`);
    expect(response.status()).toBe(403);
  });
});
```

---

## Run Against OLD Service

```bash
# 1. Start backend with old service
cd src/backend
dotnet run --project ECommerce.API &

# 2. Wait for startup
sleep 3

# 3. Run Playwright tests
cd ../../src/frontend/storefront
npx playwright test api-reviews.spec.ts --reporter=list

# 4. Verify all pass
```

---

## Critical Test Assertions

| Scenario | Expected | Why Critical |
|----------|----------|-------------|
| Unknown product ID | 200 OK with empty list | No 404 for missing products |
| Duplicate user+product | 409 Conflict with `DUPLICATE_REVIEW` | Prevents duplicate reviews |
| Invalid rating (6) | 400 Bad Request with `INVALID_RATING` | Validation enforced client-side and API |
| Empty text | 400 Bad Request with `REVIEW_TEXT_EMPTY` | Text is required |
| Create review | 201 Created with Location header | Standard REST create pattern |
| Update review | 200 OK with updated data | Update returns modified review |
| Delete review | 200 OK | Delete succeeds silently |
| Mark helpful | Increments count | Counter works correctly |
| Flag review | Sets flagCount > 0 | Moderation flagging works |
| Approve (non-admin) | 403 Forbidden | Authorization enforced |

---

## Acceptance Criteria

- [ ] All 13 tests pass against the OLD service
- [ ] Zero 500 errors
- [ ] All error codes match: `REVIEW_NOT_FOUND`, `DUPLICATE_REVIEW`, `INVALID_RATING`, `REVIEW_TEXT_EMPTY`
- [ ] Authorization (403 for non-admin) working
- [ ] POST creates with 201 and Location header
- [ ] Helpful count increments correctly
