# Phase 4, Step 7: Re-Run E2E Tests After Cutover

**Prerequisite**: Step 4 (Cutover) complete. Backend running on `http://localhost:5000` with PostgreSQL. No EF migration was needed for Phase 4 — the existing `Carts`, `CartItems`, and `Wishlists` tables are unchanged.

The `api-cart.spec.ts` and `api-wishlist.spec.ts` files were written in **step 0b** and passed against the OLD services. Now re-run the same files against the NEW MediatR handlers to confirm nothing regressed against a real PostgreSQL database.

---

## Run

```bash
# Backend must be running with PostgreSQL (not InMemory)
# No migration to apply — tables already exist
cd src/frontend/storefront
npx playwright test api-cart.spec.ts api-wishlist.spec.ts --reporter=list
```

---

## What to check

All tests from step 0b must still pass. Pay special attention to the intentional **breaking changes** introduced in Phase 4:

| Test | What changed | What to verify |
|------|-------------|----------------|
| `POST /cart/add-item` without token → **401** | Was `AllowAnonymous`, now `[Authorize]` | Confirm characterization tests were updated in step 0 to expect 401 |
| `PUT /cart/update-item/{id}` without token → **401** | Was `AllowAnonymous`, now `[Authorize]` | Same — update chars if not already done |
| `DELETE /cart/remove-item/{id}` → 401 without token | Auth requirement preserved | AllowAnonymous was never set here |
| `PUT /cart/items/{id}` route alias → 200 | Alias preserved | Both `update-item/{id}` AND `items/{id}` must work |
| `DELETE /cart/items/{id}` route alias → success | Alias preserved | Both `remove-item/{id}` AND `items/{id}` must work |
| `POST /cart/clear` AND `DELETE /cart` → 200 (anonymous) | Still `[AllowAnonymous]` | Anonymous clear must not hit DB, return 200 |
| `GET /wishlist/contains/{productId}` → `data` is `bool` | `ApiResponse<bool>` not `ApiResponse<object>` | `typeof body.data === 'boolean'` |
| All wishlist endpoints → 401 without token | Entire controller is `[Authorize]` | No anonymous access at all |
| `POST /cart/add-item` → `CART_FULL` → **422** | Error code mapping | Not 400 or 500 |
| `QUANTITY_INVALID` → **422** | Error code mapping | Not 400 |
| `CART_ITEM_NOT_FOUND` → **404** | Error code mapping | Not 422 |
| `WISHLIST_FULL` → **422** | Error code mapping | Not 400 |

---

## Verify table integrity

Since no EF migration ran, confirm the existing tables are intact and the new handlers write correctly:

```sql
-- Cart written by AddToCartCommand must exist
SELECT c."Id", c."UserId", COUNT(ci."Id") AS item_count
FROM "Carts" c
LEFT JOIN "CartItems" ci ON ci."CartId" = c."Id"
GROUP BY c."Id", c."UserId"
LIMIT 10;

-- CartItems must have UnitPrice snapshotted (not zero)
SELECT "Id", "ProductId", "Quantity", "UnitPrice", "Currency"
FROM "CartItems"
LIMIT 10;

-- Wishlists table still has one row per (UserId, ProductId)
SELECT "UserId", COUNT("ProductId") AS product_count
FROM "Wishlists"
GROUP BY "UserId"
LIMIT 10;
```

If `CartItems.UnitPrice` is 0 or null, the `IShoppingDbReader.GetProductPriceAsync` lookup failed silently. Check the handler and Product data.

---

## Auth regression check

Two endpoints changed auth requirements in Phase 4. The characterization tests (step 0) should already reflect this. Verify:

```bash
# These must return 401 in the new implementation
curl -s -o /dev/null -w "%{http_code}" -X POST http://localhost:5000/api/cart/add-item \
  -H "Content-Type: application/json" \
  -d '{"productId":"00000000-0000-0000-0000-000000000001","quantity":1}'
# Expected: 401

curl -s -o /dev/null -w "%{http_code}" -X PUT http://localhost:5000/api/cart/items/00000000-0000-0000-0000-000000000001 \
  -H "Content-Type: application/json" \
  -d '{"quantity":2}'
# Expected: 401
```

If e2e tests fail here because they still expect 200 anonymous — the Playwright spec must be updated to send a valid auth token (or expect 401). The characterization tests from step 0 already pin this new contract.

---

## If a test fails after cutover

1. **401 unexpected on previously-anonymous endpoint**: Check that `[AllowAnonymous]` is on `POST /cart/get-or-create`, `POST /cart/clear`, `DELETE /cart`, and `POST /cart/validate/{cartId}`. These must NOT require auth.

2. **401 expected but test sends no token** (add-item, update-item): Update the Playwright spec to authenticate before calling these endpoints — this is an intentional Phase 4 scope change.

3. **`GET /wishlist/contains/{id}` — `data` is object not bool**: Check `WishlistController.IsProductInWishlist` returns `ApiResponse<bool>` (not `ApiResponse<object>`). The `IsProductInWishlistQuery` handler must return `Result<bool>`.

4. **Route alias 404** (`/cart/items/{id}` returns 404): Confirm both `[HttpPut("update-item/{cartItemId:guid}")]` and `[HttpPut("items/{cartItemId:guid}")]` are on the same action in `CartController`.

5. **`CART_FULL` or `QUANTITY_INVALID` returns 400 instead of 422**: Check `MapResult` in `CartController` — these codes must map to `UnprocessableEntity()`.

6. **`CART_ITEM_NOT_FOUND` returns 422 instead of 404**: Check `MapResult` — `CART_ITEM_NOT_FOUND` must map to `NotFound()`.

7. **Anonymous `POST /cart/clear` fails**: Check `ClearCartCommand(null)` path — when `UserId` is null the handler must return `Result.Ok(empty stub)` without touching the DB.

8. **Price is 0 on added cart item**: `IShoppingDbReader.GetProductPriceAsync` returned null. Confirm the product exists in the database and `ShoppingDbReader` queries the correct table/property path.

Narrow down the layer:
- If **characterization tests pass** but e2e fails → issue is PostgreSQL-specific (data, concurrency, raw SQL quoting)
- If **both fail** → issue is in the handler or controller logic — fix at that layer first

---

## Acceptance Criteria

- [ ] All tests from `api-cart.spec.ts` pass against the NEW handlers
- [ ] All tests from `api-wishlist.spec.ts` pass against the NEW handlers
- [ ] Zero regressions on endpoints that did NOT change auth
- [ ] `POST /cart/add-item` and `PUT /cart/update-item/{id}` now return 401 without token — Playwright spec updated to reflect this
- [ ] Both route aliases work: `/cart/update-item/{id}` and `/cart/items/{id}`, `/cart/remove-item/{id}` and `/cart/items/{id}`
- [ ] `GET /wishlist/contains/{id}` returns `data: true/false` (a plain bool, not an object)
- [ ] `POST /cart/clear` and `DELETE /cart` return 200 for anonymous users
- [ ] `CART_FULL` and `QUANTITY_INVALID` → 422; `CART_ITEM_NOT_FOUND` → 404
- [ ] `CartItems.UnitPrice` is snapshotted correctly (matches the product price at add time)
- [ ] Old `CartService` and `WishlistService` no longer referenced anywhere — `dotnet build` clean
