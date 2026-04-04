# Phase 6, Step 7: Re-Run E2E Tests After Cutover

**Prerequisite**: Step 4 (Cutover) complete. Backend running on `http://localhost:5000` with the PostgreSQL database.

The `api-reviews.spec.ts` was written in **step 0b** and passed against the OLD service. Now re-run the same file against the NEW MediatR handlers to confirm nothing regressed against a real PostgreSQL database.

---

## Run

```bash
# Backend must be running with PostgreSQL
cd src/backend
dotnet run --project ECommerce.API &
sleep 3

# Run Playwright tests
cd ../../src/frontend/storefront
npx playwright test api-reviews.spec.ts --reporter=list
```

---

## What to check

All tests from step 0b must still pass. Pay special attention to:

| Test | Why it matters |
|------|----------------|
| `GET /api/products/{productId}/reviews` → 200 with approved only | Only approved reviews shown publicly |
| `GET /api/products/{id}/reviews` (unknown product) → 200 empty | No 404 for missing product |
| `POST /api/reviews` → 201 with Location header | CreatedAtAction wiring preserved |
| `POST /api/reviews` duplicate → 409 Conflict | User can't review same product twice |
| `POST /api/reviews` invalid rating → 400 | Validation enforced |
| `POST /api/reviews` empty text → 400 | Required field |
| `PUT /api/reviews/{id}` → 200 | Update returns modified review |
| `DELETE /api/reviews/{id}` → 200 | Delete succeeds silently |
| `POST /api/reviews/{id}/mark-helpful` → count increments | Counter works end-to-end |
| `POST /api/reviews/{id}/flag` → FlagCount > 0 | Moderation flagging works |
| `POST /api/reviews/{id}/approve` (non-admin) → 403 | Authorization enforced |
| `GET /api/reviews/admin/pending` (non-admin) → 403 | Admin-only query protected |

---

## Verify data integrity

Since a migration was run in step 3, confirm the table structure is correct and new handlers write correctly:

```sql
-- Table exists with all columns
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_name = 'Reviews'
ORDER BY ordinal_position;

-- Expected columns: Id, ProductId, AuthorId, AuthorName, Rating, Text, Status, HelpfulCount, FlagCount, RowVersion, CreatedAt, UpdatedAt

-- RowVersion column must be present (concurrency token)
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_name = 'Reviews' AND column_name = 'RowVersion';
-- Must return one row

-- Unique constraint on (ProductId, AuthorId)
SELECT *
FROM information_schema.table_constraints
WHERE table_name = 'Reviews' AND constraint_type = 'UNIQUE';

-- After creating a review via the API, confirm it appears in DB
SELECT "Id", "ProductId", "Rating", "Status", "CreatedAt"
FROM "Reviews"
ORDER BY "CreatedAt" DESC
LIMIT 5;
```

---

## If a test fails after cutover

1. **`POST /api/reviews` returns 400 instead of 201**: The new `CreateReviewRequestDto` may be missing a FluentValidation validator. Either add the validator or remove `[ValidationFilter]` temporarily to debug, then re-add after writing the validator.

2. **`GET /api/products/{id}/reviews` returns pending/rejected reviews**: The `GetByProductAsync` query must filter `Status == "Approved"`. Check `ReviewRepository.GetByProductAsync` logic.

3. **Duplicate review returns 200 instead of 409**: The `CreateReviewCommandHandler` check for duplicate must query by `(ProductId, AuthorId)` pair. Verify `GetByProductAndAuthorAsync` is being called correctly.

4. **Invalid rating returns 200 instead of 400**: The `Rating.Create` validation is not being called. Check `CreateReviewCommandHandler` validates both rating and text before creating aggregate.

5. **Mark helpful returns different count**: EF change tracking for the aggregate must be working. Verify `UpsertAsync` in the repository handles updates (found entities are tracked automatically).

6. **Flag threshold not changing status to Flagged**: The `Review.Flag()` method must check if `FlagCount >= 3` and set `Status = Flagged`. After 3 flags, verify status in DB is "Flagged".

7. **Admin approve (non-admin) returns 200 instead of 403**: The controller action must have `[Authorize(Roles = "Admin,SuperAdmin")]` attribute on `ApproveReview`. Verify attribute is present and working.

8. **RowVersion causes EF error**: If concurrent updates fail to return 409 Conflict, the transaction pipeline for `ITransactionalCommand` may not be wired. Verify the UoW behavior pipeline is active for Reviews assembly (step 3 registers in `OnModelCreating`).

9. **`Text` or `Rating` field is null on returned DTOs**: The EF value converters for `ReviewText` and `Rating` may not be wired. Verify `ReviewConfiguration` has `HasConversion` for both properties.

10. **Old `ReviewsControllerTests` fail**: These tests may still reference old DTOs or `IReviewService`. Confirm step 4 cleanup removed the old service registration and that tests are updated to use MediatR or skipped.

Narrow down the layer:
- If **characterization tests pass** but e2e fails → issue is PostgreSQL-specific (EF mapping, value converter, owned entity if any)
- If **both fail** → issue is in handler, controller, or domain logic — fix that layer first

---

## Acceptance Criteria

- [ ] All tests from `api-reviews.spec.ts` pass against the NEW handlers
- [ ] Zero regressions vs. step 0b baseline
- [ ] `GET /api/products/{id}/reviews` returns only approved reviews
- [ ] `POST /api/reviews` returns 201 with Location header
- [ ] Duplicate review returns 409 Conflict with `DUPLICATE_REVIEW`
- [ ] Invalid rating returns 400 with `INVALID_RATING`
- [ ] Empty text returns 400 with `REVIEW_TEXT_EMPTY`
- [ ] `PUT /api/reviews/{id}` returns 200 with updated data
- [ ] `DELETE /api/reviews/{id}` returns 200
- [ ] Mark helpful increments count correctly against real PostgreSQL
- [ ] Flag increments FlagCount and status changes to Flagged after 3 flags
- [ ] Admin approve requires authorization (403 for non-admin)
- [ ] `GET /api/reviews/admin/pending` requires admin role (403 for non-admin)
- [ ] `REVIEW_NOT_FOUND` → 404 confirmed against real DB
- [ ] `Reviews` table created with correct schema
- [ ] `RowVersion` column intact (no migration dropped it)
- [ ] `IReviewService` and old service not referenced anywhere — `dotnet build` clean
