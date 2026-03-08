# Backend Idempotency Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Prevent duplicate side effects for unsafe operations (order creation, payment processing, refunds).

## Core Rules
1. Require `Idempotency-Key` header for duplicate-sensitive write endpoints.
2. Validate idempotency key format at controller boundary.
3. Use `IIdempotencyStore.StartAsync` before business operation.
4. Return replayed successful response when status is `Replay`.
5. Call `CompleteAsync` only for successful cacheable outcomes.
6. Call `AbandonAsync` for failed/non-cacheable outcomes.

## Real Code References
- Orders controller idempotency flow:
  - `src/backend/ECommerce.API/Controllers/OrdersController.cs`
- Payments controller idempotency flow:
  - `src/backend/ECommerce.API/Controllers/PaymentsController.cs`
- Idempotency contract:
  - `src/backend/ECommerce.Application/Interfaces/IIdempotencyStore.cs`
- Distributed implementation:
  - `src/backend/ECommerce.Application/Services/DistributedIdempotencyStore.cs`

## Status Handling
- `Acquired`: proceed with operation.
- `Replay`: return cached successful response.
- `InProgress`: return conflict/processing response and avoid duplicate execution.

## Common Mistakes
- Not validating missing/invalid `Idempotency-Key`.
- Caching failed responses that should be retriable.
- Forgetting to abandon key for failure paths.

## Checklist
- [ ] Endpoint validates `Idempotency-Key`.
- [ ] Controller handles `Acquired/Replay/InProgress` explicitly.
- [ ] Success path calls `CompleteAsync`.
- [ ] Failure path calls `AbandonAsync`.
