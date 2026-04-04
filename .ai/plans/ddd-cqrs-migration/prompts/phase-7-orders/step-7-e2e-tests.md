# Phase 7, Step 7: Re-Run E2E Tests After Cutover

**Prerequisite**: Step 4 (Cutover) complete. Backend running on `http://localhost:5000` with PostgreSQL database.

The `api-orders.spec.ts` written in **step 0b** passed against the OLD service. Now re-run against the NEW MediatR handlers to confirm no regressions against real PostgreSQL.

---

## Run

```bash
cd src/backend
dotnet run --project ECommerce.API &
sleep 3

cd ../../src/frontend/storefront
npx playwright test api-orders.spec.ts --reporter=list
```

---

## What to Check

All tests from step 0b must pass. Critical assertions:

| Test | Expected |
|------|----------|
| POST /api/orders → 201 with Location | Order creation |
| POST /api/orders (empty) → 400 | Validation |
| POST /api/orders (invalid qty) → 400 | Line item validation |
| GET /api/orders/{id} → 200 | Retrieve order |
| GET /api/orders/{id} (unknown) → 404 | Error mapping |
| GET /api/customers/{id}/orders → 200 paginated | Order history |
| POST /api/orders/{id}/confirm → 200 Confirmed | Status transition |
| POST /api/orders/{id}/ship → 200 Shipped | Status transition |
| POST /api/orders/{id}/cancel (pending) → 200 | Cancel allowed |
| POST /api/orders/{id}/cancel (shipped) → 422 | Cancel blocked |
| Total calculation | Subtotal + Tax + Shipping |
| Admin pending → 403 (non-admin) | Authorization |
| Line items | ProductId, Quantity, UnitPrice present |

---

## SQL Verification

```sql
-- Tables exist
SELECT name FROM sys.tables WHERE name IN ('Orders', 'OrderLineItems');
-- Expected: 2 rows

-- Indexes present
SELECT name FROM sys.indexes WHERE object_id = OBJECT_ID('Orders');

-- RowVersion column exists
SELECT column_name FROM information_schema.columns
WHERE table_name = 'Orders' AND column_name = 'RowVersion';

-- Sample order after creation
SELECT TOP 1 Id, OrderNumber, Status, Subtotal, Tax, ShippingCost, Total
FROM Orders
ORDER BY CreatedAt DESC;
```

---

## Failure Triage (10 scenarios)

1. **POST /api/orders returns 400 instead of 201** → Check PlaceOrderCommandHandler validates items before creating aggregate
2. **GET /api/customers/{id}/orders returns 404** → Should return 200 with empty list; GetByCustomerAsync must not filter missing customer
3. **Confirm returns "Order already confirmed" instead of 200** → Was order already confirmed in test? Clear seeded data or use fresh order
4. **Ship (not confirmed) returns 200 instead of error** → Order.Ship() must check `Status == Confirmed`; verify call order
5. **Cannot cancel shipped orders returns 200** → Order.Cancel() must check Status and return error before Shipped
6. **Total calculation wrong** → EF value converters for Money must work; verify HasConversion wiring in OrderConfiguration
7. **TrackingNumber null in response** → OrderDetailDto must include TrackingNumber; check mapping extension
8. **Line items null or empty** → Owned collection Items must be loaded by EF; verify OwnsMany wiring
9. **OrderNumber missing or wrong** → OrderNumber value converter must work; verify HasConversion and Reconstitute
10. **RowVersion causes concurrency errors on update** → Transaction pipeline for ITransactionalCommand must be wired

Narrow down:
- If **characterization tests pass** but E2E fails → EF mapping issue (value converter, owned entity, foreign key)
- If **both fail** → Handler or controller logic issue

---

## Acceptance Criteria

- [ ] All tests from `api-orders.spec.ts` pass against NEW handlers
- [ ] Zero regressions vs. step 0b baseline
- [ ] `POST /api/orders` returns 201 with Location header
- [ ] Empty cart validation (400) works
- [ ] `GET /api/customers/{id}/orders` returns paginated list
- [ ] Status transitions (Pending → Confirmed → Shipped) work
- [ ] Cannot cancel shipped orders (422)
- [ ] Total calculation correct
- [ ] `ORDER_NOT_FOUND` → 404 against real DB
- [ ] Admin authorization enforced (403)
- [ ] `Orders` and `OrderLineItems` tables created correctly
- [ ] `RowVersion` column present and functional
- [ ] `IOrderService` and old service not referenced — clean build
