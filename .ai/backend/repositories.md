# Backend Repositories Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Define repository responsibilities and the boundary between repositories and UnitOfWork.

## Core Rules
1. Repositories handle data access only.
2. Repositories never call `SaveChangesAsync`.
3. Services commit through `IUnitOfWork.SaveChangesAsync`.
4. Use specialized repositories for feature-specific queries.
5. Use generic repository for simple CRUD-only entities.
6. For read-only queries, prefer non-tracking (`AsNoTracking`) where appropriate.

## Pattern in This Codebase
- Generic repository base:
  - `src/backend/ECommerce.Infrastructure/Repositories/Repository.cs`
- Specialized repositories:
  - `src/backend/ECommerce.Infrastructure/Repositories/ProductRepository.cs`
  - `src/backend/ECommerce.Infrastructure/Repositories/OrderRepository.cs`
- Unit of Work entry point:
  - `src/backend/ECommerce.Infrastructure/UnitOfWork.cs`

## Query Rules
- Include required navigation properties explicitly when needed.
- Keep filtering/sorting/pagination in repositories for reusable query behavior.
- Pass `CancellationToken` through async methods.

## Common Mistakes
- Calling `SaveChangesAsync` inside repository methods.
- Injecting repositories directly into services instead of `IUnitOfWork`.
- Returning tracked entities for read-only screens by default.

## Checklist
- [ ] Repository method is data access only.
- [ ] No commit operation inside repository.
- [ ] Service uses UnitOfWork to commit.
- [ ] Cancellation token is supported for async methods.
