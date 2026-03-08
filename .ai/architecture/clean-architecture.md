# Clean Architecture Rules

Updated: 2026-03-08
Owner: @ivans

## Purpose
Codify dependency direction and responsibilities per layer.

## Dependency Rules
1. Core: domain entities, enums, constants, results, interfaces.
2. Application: use cases and orchestration; depends on Core.
3. Infrastructure: data access/external adapters; depends on Core/Application.
4. API: transport layer and composition root; depends on Application.

## Enforcement Rules
- Services depend on `IUnitOfWork`, not concrete repositories.
- Repositories do not call `SaveChangesAsync`.
- Controllers remain thin and return API response envelopes.

## Real Code References
- Unit of Work contract usage: `src/backend/ECommerce.Application/Services/ProductService.cs`
- Unit of Work implementation: `src/backend/ECommerce.Infrastructure/UnitOfWork.cs`
- Generic repository boundary: `src/backend/ECommerce.Infrastructure/Repositories/Repository.cs`
- API controllers: `src/backend/ECommerce.API/Controllers/`

## Violations to Avoid
- API calling Infrastructure directly.
- Business rules inside controllers.
- Persistence-specific details leaking into Core.
