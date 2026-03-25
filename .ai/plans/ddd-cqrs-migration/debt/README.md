# DDD Migration: Technical Debt Register

This folder tracks debt **deliberately accumulated** during the migration.
Each item is expected and planned — not a mistake. Every item has a clear cleanup step.

> Rule: never leave a debt item open after the phase that created it is done.
> The cleanup column says exactly when and what to delete.

---

## How to Use This

- Before starting a phase: read the debt items that phase introduces
- After completing a phase: execute the cleanup for that phase's debt
- The goal: zero open debt items after Phase 7

---

## Debt Items

### D-01 — Naming Conflicts: Repository Interfaces

**Introduced:** Phase 1
**Status:** Open

Old `ECommerce.Core.Interfaces.Repositories` has interfaces that share names with the new context-specific interfaces:

| Old (Core) | New (Catalog.Domain) | Conflict |
|---|---|---|
| `IProductRepository` | `IProductRepository` | Same name, different assemblies, different contracts |
| `ICategoryRepository` | `ICategoryRepository` | Same name, different assemblies, different contracts |

Same conflict will appear in later phases:

| Phase | Old | New |
|---|---|---|
| 2 | `Core.IUserRepository` | `Identity.Domain.IUserRepository` |
| 4 | `Core.ICartRepository`, `Core.IWishlistRepository` | `Shopping.Domain.I*` |
| 6 | `Core.IReviewRepository` | `Reviews.Domain.IReviewRepository` |
| 7 | `Core.IOrderRepository` | `Ordering.Domain.IOrderRepository` |

**How to avoid compile errors during migration:**
Use fully qualified names in any file that references both, or add a `using` alias:
```csharp
using OldProductRepo = ECommerce.Core.Interfaces.Repositories.IProductRepository;
using NewProductRepo = ECommerce.Catalog.Domain.Interfaces.IProductRepository;
```
In practice, the old interface is only used by `UnitOfWork` and old services.
The new interface is only used by new handlers and repositories.
They rarely appear in the same file — conflict is mostly at the assembly level.

**Cleanup:** After Phase 1 cutover — delete `Core.Interfaces.Repositories.IProductRepository` and `ICategoryRepository` (old services are gone, nothing references them).
Repeat per phase for each context's old repository interface.

---

### D-02 — Naming Conflicts: Entity vs Aggregate Classes

**Introduced:** Phase 1
**Status:** Open

Old `ECommerce.Core.Entities` has entity classes that share names with new aggregates:

| Old (Core.Entities) | New (Catalog.Domain) |
|---|---|
| `Product` | `Product` |
| `Category` | `Category` |

Same pattern repeats in later phases (`User`, `Order`, `Review`, etc.).

**How to avoid compile errors during migration:**
Again, the old entity is only used by the old service, old repositories, and `AppDbContext`.
The new aggregate is only used by new handlers and new repositories.
Use fully qualified names only in the `AppDbContext` and EF configuration files where both temporarily exist:
```csharp
// In AppDbContext during Phase 1 transition
public DbSet<ECommerce.Core.Entities.Product> Products { get; set; }       // old — still active
public DbSet<ECommerce.Catalog.Domain.Aggregates.Product> CatalogProducts { get; set; } // new — temporary name
```

**Cleanup:** After Phase 1 cutover — delete `Core.Entities.Product`, `Core.Entities.Category`, rename `CatalogProducts` back to `Products` in `AppDbContext`.

---

### D-03 — AppDbContext: Mixed Entity Types

**Introduced:** Phase 1
**Status:** Open

`AppDbContext` currently has `DbSet<ECommerce.Core.Entities.Product>`.
The new `ProductRepository` (Phase 1) needs `DbSet<ECommerce.Catalog.Domain.Aggregates.Product>`.

These are different types — `AppDbContext` cannot have both pointing to the same table.

**Transition approach:**
1. During Phase 1 development: new `ProductRepository` is built but not yet wired — old `AppDbContext.Products` still active
2. At cutover (Step 5): update `AppDbContext.Products` to use the new aggregate type, add new EF configuration, remove old `ProductConfiguration`
3. Run EF migration if the schema changed (value objects may add/rename columns)

**Cleanup:** Completed at Phase 1 cutover (Step 5). Repeated per phase.

---

### D-04 — DTO Naming Conflicts

**Introduced:** Phase 1
**Status:** Open

Old `ECommerce.Application.DTOs.Products` has:
- `ProductDetailDto`
- `ProductDto`

New `ECommerce.Catalog.Application.DTOs` will also have:
- `ProductDetailDto`
- `ProductDto`

**How to avoid compile errors:**
The old DTOs are used by old controllers via `IProductService`.
After cutover, controllers use `IMediator` and receive new DTOs.
The transition is file-by-file — the controller imports one or the other, never both.

**Cleanup:** After Phase 1 cutover — delete `Application.DTOs.Products.*` and `Application.DTOs.Categories.*`. Old DTO namespaces are gone.

---

### D-05 — Old Services Coexist with New Handlers

**Introduced:** Phase 1 (and each subsequent phase)
**Status:** Open

During Phase 1 development, both exist simultaneously:
- `ProductService` / `CategoryService` (old, still registered in DI)
- `CreateProductCommandHandler`, `GetProductsQueryHandler`, etc. (new, registered via MediatR)

Until cutover, controllers still call the old services. New handlers exist but are not called.

**Risk:** Forgetting to delete the old service after cutover — both are registered in DI, causing confusion.

**Cleanup:** Step 5 of each phase — explicitly delete old service class, interface, and DI registration. Characterization tests confirm nothing broke.

---

### D-06 — Dual IUnitOfWork Registrations

**Introduced:** Phase 0
**Status:** Permanent until Phase 7 complete

Two `IUnitOfWork` registrations exist in DI:
- `Core.Interfaces.Repositories.IUnitOfWork → UnitOfWork` (old services)
- `SharedKernel.Interfaces.IUnitOfWork → MediatRUnitOfWork` (new handlers)

This is intentional. Old services cannot be changed to use the new interface.

**Cleanup:** After Phase 7 — once all old services are deleted, remove `Core.IUnitOfWork`, `UnitOfWork`, and the old DI registration. Only `SharedKernel.IUnitOfWork → MediatRUnitOfWork` remains.

---

### D-07 — AutoMapper Vulnerability

**Introduced:** Before migration (pre-existing)
**Status:** Open

`AutoMapper 12.0.1` has a known high severity vulnerability (GHSA-rvv3-g6hj-g44x).
New DDD code does not use AutoMapper — it uses extension methods (`product.ToDetailDto()`).
Old services still depend on AutoMapper and cannot be changed without risk during migration.

**Cleanup:** After each phase's old service is deleted, AutoMapper usage decreases.
After Phase 7 — if all old services are gone, remove AutoMapper entirely. Confirm with `dotnet build --no-restore` that nothing still imports it.

---

## Debt Summary by Phase

| Phase | Debt Introduced | Debt Cleaned Up |
|---|---|---|
| Phase 0 | D-06 (dual IUnitOfWork) | — |
| Phase 1 | D-01, D-02, D-03, D-04, D-05 | D-01 (catalog repos), D-02 (catalog entities), D-03, D-04, D-05 (catalog services) |
| Phase 2 | D-01, D-02, D-05 (identity) | Same — cleaned at phase 2 cutover |
| Phase 3 | D-01, D-02, D-05 (inventory) | Same |
| Phase 4 | D-01, D-02, D-05 (shopping) | Same |
| Phase 5 | D-01, D-02, D-05 (promotions) | Same |
| Phase 6 | D-01, D-02, D-05 (reviews) | Same |
| Phase 7 | D-01, D-02, D-05 (ordering) | D-01–D-07 all closed — migration complete |
