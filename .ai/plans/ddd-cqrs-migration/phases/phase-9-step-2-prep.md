# Phase 9 - Step 2 Prep (Dashboard CQRS)

Date: 2026-04-07
Branch: feature/phase-9-step-2-dashboard-cqrs
Status: Ready to implement

## Goal
Replace legacy `IDashboardService`/`DashboardService` with CQRS queries owned by bounded contexts:
- Ordering: order/revenue metrics + trends
- Identity: customer count
- Catalog: active product count

Controller should compose these query results and stop depending on `ECommerce.Application.DTOs.Dashboard`.

## Current Baseline (confirmed)
Legacy dashboard references still exist in:
- `src/backend/ECommerce.API/Controllers/DashboardController.cs`
- `src/backend/ECommerce.API/Extensions/ServiceCollectionExtensions.cs` (registers `IDashboardService`)
- `src/backend/ECommerce.Application/Interfaces/IDashboardService.cs`
- `src/backend/ECommerce.Application/Services/DashboardService.cs`
- `src/backend/ECommerce.Application/DTOs/Dashboard/*`
- Tests:
  - `src/backend/ECommerce.Tests/Integration/DashboardControllerTests.cs`
  - `src/backend/ECommerce.Tests/Unit/Services/DashboardServiceTests.cs`

## Important Design Notes
1. Existing BC repository interfaces do not yet expose dashboard metrics.
2. New metrics methods must be added to BC repository interfaces and implemented in BC infrastructure repos:
   - Ordering:
     - `GetTotalOrdersCountAsync`
     - `GetTotalRevenueAsync`
     - `GetOrdersTrendAsync(int days)`
     - `GetRevenueTrendAsync(int days)`
   - Identity:
     - `GetCustomersCountAsync`
   - Catalog:
     - `GetActiveProductsCountAsync`
3. Keep service-layer rules:
   - Handlers return `Result<T>`.
   - No `SaveChangesAsync` in repository methods.

## Step 2 Implementation Plan (tiny commits)
1. Add Ordering query + repo contract/implementation
   - `GetOrderStatsQuery` + handler + DTO in Ordering Application
   - Extend `Ordering.Domain.Interfaces.IOrderRepository`
   - Implement methods in `Ordering.Infrastructure.Persistence.Repositories.OrderRepository`

2. Add Identity query + repo contract/implementation
   - `GetUserStatsQuery` + handler + DTO in Identity Application
   - Extend `Identity.Domain.Interfaces.IUserRepository`
   - Implement in `Identity.Infrastructure.Repositories.UserRepository`

3. Add Catalog query + repo contract/implementation
   - `GetProductStatsQuery` + handler + DTO in Catalog Application
   - Extend `Catalog.Domain.Interfaces.IProductRepository`
   - Implement in `Catalog.Infrastructure.Repositories.ProductRepository`

4. Migrate API controller
   - Move to `src/backend/ECommerce.API/Features/Dashboard/Controllers/DashboardController.cs`
   - Use `IMediator` only
   - Compose endpoint response from BC queries
   - Add API-layer composition DTO(s) under `Features/Dashboard`

5. Remove legacy dashboard stack
   - Delete:
     - `src/backend/ECommerce.Application/Interfaces/IDashboardService.cs`
     - `src/backend/ECommerce.Application/Services/DashboardService.cs`
     - `src/backend/ECommerce.Application/DTOs/Dashboard/*`
   - Remove DI registration from `ServiceCollectionExtensions.cs`

6. Tests migration
   - Update integration tests for new controller location/behavior
   - Delete `DashboardServiceTests.cs` only when replacement handler tests exist and pass

## Verification Gates
Run after each mini-step:

Backend build:
- `dotnet build src/backend/ECommerce.sln`

Dashboard focused tests:
- `dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj --filter "Dashboard"`

Legacy reference checks (workspace grep):
- `IDashboardService|DashboardService|ECommerce.Application.DTOs.Dashboard`

Expected at end of Step 2:
- no matches in production code for those three patterns.

## Not In Scope
- Step 3 controller reorganization beyond Dashboard.
- Step 5 repository deletions from old Core/Infrastructure monolith.
- Any migration of `ECommerce.Application` project deletion (Step 6).
