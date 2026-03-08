# Backend Overview

Updated: 2026-03-08
Owner: @ivans

## Purpose
Quick orientation for backend work in this repository.

## Architecture
- Clean Architecture flow:
  - API -> Application -> Core
  - Infrastructure -> Core/Application
- Core contains domain model and contracts.
- Application contains DTOs, validators, services, mapping.
- Infrastructure contains EF Core data access, repositories, migrations, unit of work.
- API contains controllers, middleware, action filters, DI/bootstrap.

## Non-Negotiable Rules
- Services inject `IUnitOfWork` (not repositories directly).
- Services return `Result<T>` for expected business outcomes.
- Repositories do not call `SaveChangesAsync`; UnitOfWork commits.
- Controllers stay thin and return `ApiResponse<T>` envelopes.
- Async methods include `CancellationToken cancellationToken = default`.

## Key Reference Paths
- Unit of Work: `src/backend/ECommerce.Infrastructure/UnitOfWork.cs`
- Result model: `src/backend/ECommerce.Core/Results/Result.cs`
- Error codes: `src/backend/ECommerce.Core/Constants/ErrorCodes.cs`
- API response DTO: `src/backend/ECommerce.Application/DTOs/Common/ApiResponse.cs`
- Controllers: `src/backend/ECommerce.API/Controllers/`
- Services: `src/backend/ECommerce.Application/Services/`
- Repositories: `src/backend/ECommerce.Infrastructure/Repositories/`

## Read Next
- `.ai/backend/controllers.md`
- `.ai/backend/validation.md`
- `.ai/backend/error-handling.md`
- `.ai/workflows/adding-feature.md`
