# Backend Transactions Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Define when and how to use transactions safely in application services.

## Core Rules
1. Keep transaction scope inside service orchestration, not repositories.
2. Use UnitOfWork transaction APIs for multi-step write flows.
3. Commit once per successful business operation.
4. Roll back on failure and return consistent `Result<T>`.
5. Avoid long-running transaction scopes across external calls.

## Real Code References
- Transaction entry points: `src/backend/ECommerce.Infrastructure/UnitOfWork.cs`
- Example multi-step service flows:
  - `src/backend/ECommerce.Application/Services/OrderService.cs`
  - `src/backend/ECommerce.Application/Services/PaymentService.cs`

## Practical Guidance
- Use explicit transaction for operations that must be atomic across multiple repository writes.
- For single-write operations, a plain UnitOfWork commit is usually enough.
- Keep transaction block narrow and avoid network calls inside it.

## Common Mistakes
- Starting transactions in repository classes.
- Calling `SaveChangesAsync` multiple times in one business operation without clear reason.
- Holding open transactions while waiting on external services.

## Checklist
- [ ] Transaction used only when atomic multi-step write is needed.
- [ ] UnitOfWork controls begin/commit/rollback.
- [ ] Failure path returns clear `Result<T>` error.
- [ ] External I/O kept outside transaction window.
