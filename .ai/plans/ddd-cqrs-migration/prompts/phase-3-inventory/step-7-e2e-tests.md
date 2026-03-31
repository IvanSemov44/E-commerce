# Phase 3, Step 7: Re-Run E2E Tests After Cutover

**Prerequisite**: Step 4 (Cutover) complete. Backend running on `http://localhost:5000` with the migrated PostgreSQL database (EF migration `Phase3_ExtractInventoryItem` applied).

The `api-inventory.spec.ts` was written in **step 0b** and passed against the OLD service. Now re-run the same file against the NEW MediatR handlers to confirm nothing regressed against a real PostgreSQL database.

---

## Run

```bash
# Backend must be running with migrated PostgreSQL (not InMemory)
# Ensure EF migration Phase3_ExtractInventoryItem has been applied
cd src/frontend/storefront
npx playwright test api-inventory.spec.ts --reporter=list
```

---

## What to check

All tests from step 0b must still pass. Pay special attention to:

| Test | Why it matters |
|------|----------------|
| `GET /inventory` → 401 without token | Auth requirement unchanged after migration |
| `GET /inventory/{productId}` → anonymous accessible | AllowAnonymous preserved in new controller |
| `POST /inventory/check-availability` → 200 with `isAvailable` | Response shape must include `isAvailable` field |
| `POST /inventory/{productId}/adjust` → 404 for unknown product | `INVENTORY_ITEM_NOT_FOUND` → 404 confirmed at database level |
| `GET /inventory/{productId}/available` → `isAvailable` field | DTO shape confirmed with real data |
| Admin endpoints → 401 without token | Role-based auth wired correctly in new controller |

---

## Verify data migration

Before running e2e tests, confirm the migration ran correctly:

```sql
-- InventoryItems must exist for every seeded product
SELECT COUNT(*) FROM "InventoryItems";
-- Must be > 0 and equal to product count

-- Products table must NOT have stock columns
SELECT column_name FROM information_schema.columns
WHERE table_name = 'Products'
AND column_name IN ('StockQuantity', 'LowStockThreshold');
-- Must return 0 rows
```

If `InventoryItems` is empty, the data seed in the migration failed. Do not proceed — fix the migration first.

---

## If a test fails after cutover

1. **Auth failures (401/403 unexpected)**: Check that `[AllowAnonymous]` is on the correct endpoints in the new controller (`GetProductStock`, `CheckAvailableQuantity`, `CheckStockAvailability`)
2. **404 for seeded product**: Check the data migration seeded `InventoryItems` correctly — `ProductId` must match the seeded product's `Id`
3. **Response shape change** (e.g. `isAvailable` field missing): Align the DTO returned by `GetInventoryByProductIdQuery` or `CheckStockAvailability` handler
4. **`INVENTORY_ITEM_NOT_FOUND` → wrong HTTP status**: Check `MapInventoryResult` in the controller — must map to 404
5. **Domain events not firing**: Confirm `AddInventoryInfrastructure()` is called in `Program.cs` and MediatR is registered from the Application assembly

Narrow down the layer by running characterization tests (step 0) first — if they pass and e2e fails, the issue is database-level (EF mapping or migration).

---

## Acceptance Criteria

- [ ] All tests from `api-inventory.spec.ts` pass against the NEW handlers
- [ ] Zero regressions compared to the step 0b baseline
- [ ] Data migration verified: `InventoryItems` table populated, `Products` stock columns dropped
- [ ] Anonymous endpoints remain accessible without token (no auth regression)
- [ ] `isAvailable` field present in `check-availability` response
- [ ] `INVENTORY_ITEM_NOT_FOUND` returns 404 (not 422 or 500) against real database
