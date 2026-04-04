# Phase 5, Step 7: Re-Run E2E Tests After Cutover

**Prerequisite**: Step 4 (Cutover) complete. Backend running on `http://localhost:5000` with the PostgreSQL database. No EF migration was applied in Phase 5 — the `PromoCodes` table is unchanged.

The `api-promo-codes.spec.ts` was written in **step 0b** and passed against the OLD service. Now re-run the same file against the NEW MediatR handlers to confirm nothing regressed against a real PostgreSQL database.

---

## Run

```bash
# Backend must be running with PostgreSQL (not InMemory)
# No migration to apply
cd src/frontend/storefront
npx playwright test api-promo-codes.spec.ts --reporter=list
```

---

## What to check

All tests from step 0b must still pass. Pay special attention to:

| Test | Why it matters |
|------|----------------|
| `POST /validate` unknown code → 200 with `isValid=false` | Handler must not throw or return 404 for missing codes |
| `POST /validate` SAVE20 + 100 → `discountAmount=20` | DiscountValue.Calculate and DiscountCalculator work end-to-end with real data |
| `POST /api/promo-codes` → 201 with Location header | `CreatedAtAction` wiring in new controller preserved |
| `DELETE /api/promo-codes/{id}` → 404 for unknown | `PROMO_CODE_NOT_FOUND` still maps to 404 |
| `GET /api/promo-codes/{id}` → 404 for unknown | Same error code, same HTTP status |
| Anonymous access to `active` and `validate` | `[AllowAnonymous]` preserved in new controller |
| Admin required for all write operations | `[Authorize(Roles = "Admin,SuperAdmin")]` preserved |
| `DUPLICATE_PROMO_CODE` → 409 | Conflict mapping preserved |
| Code stored as uppercase | `PromoCodeString.Create` normalization persisted in DB |

---

## Verify data integrity

Since no migration ran, confirm the existing data is still intact and new handlers write correctly:

```sql
-- Seeded SAVE20 must still exist
SELECT "Id", "Code", "DiscountType", "DiscountValue", "IsActive"
FROM "PromoCodes"
WHERE "Code" = 'SAVE20';
-- Expected: one row with DiscountType='Percentage', DiscountValue=20, IsActive=true

-- RowVersion column must still be present (concurrency token)
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_name = 'PromoCodes' AND column_name = 'RowVersion';
-- Must return one row

-- After creating a new code via the API, confirm it appears in DB
SELECT "Id", "Code", "DiscountType", "DiscountValue"
FROM "PromoCodes"
ORDER BY "CreatedAt" DESC
LIMIT 5;
```

---

## If a test fails after cutover

1. **`POST /validate` returns 404 instead of 200 for unknown codes**: The `ValidatePromoCodeQuery` handler must return `Result.Ok(...)` when the code is not found — not a failure result. Check `ValidatePromoCodeQueryHandler.Handle` — the not-found branch must return `Result<ValidatePromoCodeDto>.Ok(new ... { IsValid = false })`.

2. **`discountAmount` is 0 for SAVE20**: `DiscountCalculator.Calculate` is not being called, or the EF `DiscountValue` owned entity is not mapping `DiscountValue` column to `Amount` property. Check `PromoCodeConfiguration` — `Amount` must map to `"DiscountValue"` column name.

3. **`Code` field is null on returned DTOs**: `PromoCodeString` value converter is not wired in EF configuration. Verify `HasConversion(v => v.Value, v => PromoCodeString.Reconstitute(v))` in `PromoCodeConfiguration`.

4. **`ValidPeriod` causes EF error**: If any existing row has `StartDate=NULL` but `EndDate` not null (or vice versa), EF may try to instantiate a half-populated `DateRange`. Confirm all existing rows have either both null or both non-null for StartDate/EndDate, and that `Navigation(...).IsRequired(false)` is set.

5. **`POST /api/promo-codes` returns 400 instead of 201**: The new `CreatePromoCodeRequestDto` may be missing a FluentValidation validator, causing `[ValidationFilter]` to reject it with 400. Add a validator for `CreatePromoCodeRequestDto` if needed, or remove `[ValidationFilter]` from Create temporarily and re-add after writing the validator.

6. **Old `PromoCodesControllerTests` fail**: These tests still reference old DTOs or `IPromoCodeService`. Confirm step 4 cleanup removed the old service registration and that tests that relied on `IPromoCodeService` DI are now resolved through MediatR.

7. **Concurrency conflict on concurrent updates**: If two requests update the same PromoCode simultaneously, EF should throw `DbUpdateConcurrencyException`. The `UpsertAsync` in `PromoCodeRepository` does not catch this — the UoW transaction pipeline (via `ITransactionalCommand`) should roll back and return `CONCURRENCY_CONFLICT`. Confirm the transaction behavior pipeline is wired for the Promotions Application assembly.

Narrow down the layer:
- If **characterization tests pass** but e2e fails → issue is PostgreSQL-specific (EF mapping, owned entity, value converter with real SQL)
- If **both fail** → issue is in handler or controller logic — fix that layer first

---

## Acceptance Criteria

- [ ] All tests from `api-promo-codes.spec.ts` pass against the NEW handlers
- [ ] Zero regressions vs. step 0b baseline
- [ ] `POST /validate` always returns 200 for valid and invalid codes
- [ ] `discountAmount=20` for SAVE20 with `orderAmount=100` confirmed against real PostgreSQL
- [ ] `POST /api/promo-codes` returns 201 with Location header
- [ ] `DUPLICATE_PROMO_CODE` → 409, `PROMO_CODE_NOT_FOUND` → 404 confirmed against real DB
- [ ] Code is stored as uppercase in PostgreSQL (confirmed via SQL query)
- [ ] `RowVersion` column intact (no migration dropped it)
- [ ] `IPromoCodeService` and old `Core.Entities.PromoCode` not referenced anywhere — `dotnet build` clean
