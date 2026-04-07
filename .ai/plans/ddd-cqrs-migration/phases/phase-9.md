# Phase 9: API Layer Reorganization & Old Service/Repository Cleanup

**Status**: In Progress — Phase 8 characterization tests ongoing in parallel.

**Learn**: Bounded context ownership at the API layer, vertical slice organization, incremental deletion of legacy monolith code.

---

## AI Handoff Guide

> Read this entire section before touching any code.
> This is a refactoring plan — not a feature plan. The goal is to move and delete code, not improve it.

### Stack Context

- **.NET 10**, C#, ASP.NET Core Web API
- **MediatR** for CQRS dispatch (`IMediator`, `IRequest<T>`, `IRequestHandler<TRequest, TResponse>`)
- **FluentValidation** for validators
- **EF Core** for persistence — one shared `AppDbContext` (still, during Phase 9)
- **SharedKernel** result type: `Result<T>` from `ECommerce.SharedKernel.Results` — not `ECommerce.Core.Results`
- **`DomainError`** for failure payloads — `Result<T>.Fail(DomainError)`, `Result<T>.Ok(data)`
- Primary constructor syntax for new code: `public class Foo(IBar bar)` not `public Foo(IBar bar) { _bar = bar; }`
- Test framework: **MSTest** (`[TestClass]`, `[TestMethod]`) + **FluentAssertions** + **Moq**

### Gold Standard Examples

When creating or moving anything, match these files exactly — do not invent your own patterns:

| What | Gold Standard File |
|---|---|
| Controller style | `src/backend/ECommerce.API/Controllers/PromoCodesController.cs` |
| Command handler | `src/backend/Promotions/ECommerce.Promotions.Application/Commands/CreatePromoCode/CreatePromoCodeCommandHandler.cs` |
| BC Application layer structure | `src/backend/Promotions/ECommerce.Promotions.Application/` |
| BC test project structure | `src/backend/Promotions/ECommerce.Promotions.Tests/` |

**Controller pattern (from PromoCodesController):**
- Primary constructor: `public class XxxController(IMediator mediator) : ControllerBase`
- `FrozenSet<string>` for error code categorization (notFound, conflict, unprocessable)
- `private IActionResult MapError(DomainError error)` method
- `.ToActionResult(data => Ok(...), MapError)` extension for Result mapping
- `[ValidationFilter]` on write endpoints
- `CancellationToken ct = default` on every async action

**Handler pattern (from CreatePromoCodeCommandHandler):**
- `sealed class`, primary constructor
- Returns `Result<TDto>` — never throws, never returns null
- Validates via domain value objects, not FluentValidation inside the handler
- Uses BC repository directly — never `IUnitOfWork` from `ECommerce.Core`
- Never calls `SaveChangesAsync` — UoW pipeline commits
- Manual DTO mapping via private static `MapToDto` method — no AutoMapper

### Session Scope Rule

**Execute exactly one step per session.** Do not continue to the next step in the same session even if the current step finishes quickly. The plan requires a `dotnet build` + test run between steps — that is the step boundary.

### Minimal Footprint Rule

When moving a file:
- Change **only** the namespace declaration and `using` statements
- Do not rename methods, restructure logic, fix style, or add comments
- Do not improve what you move — that is a separate PR

When deleting a file:
- Confirm the deletion gate is satisfied first (see Testing Strategy section)
- Remove its DI registration from `ServiceCollectionExtensions.cs` in the same commit
- `dotnet build` must be green before the PR is opened

### Do Not List

- Do not add new features, abstractions, or helper methods not required by the step
- Do not use `ECommerce.Core.Results` — use `ECommerce.SharedKernel.Results`
- Do not call `SaveChangesAsync` in any new handler
- Do not use AutoMapper in new BC code — use manual mapping
- Do not modify `AppDbContext` or migrations as part of any Phase 9 step
- Do not combine two steps in one PR
- Do not delete a test file before its replacement BC test is written and green

### Baseline Check (Run Before Starting Any Step)

```powershell
# 1. Build must be green — record any existing warnings, do not add new ones
dotnet build src/backend/ECommerce.sln

# 2. Record which tests pass right now
dotnet test src/backend/ECommerce.sln --no-build

# 3. Confirm your current step's pre-conditions (see step section)
```

If the build is red before you start — stop. Do not proceed. Report the error.

---

## Live Progress

> Update this table when a step is merged. Do not update mid-branch — only on merge.
> Last updated: 2026-04-07

| Step | Status | Notes |
|---|---|---|
| Early Wins | **Merged** | CartService and ReviewService are gone. Shared cart/review DTOs still have live references. |
| Step 0A | **Merged** | Native Shopping BC handlers introduced in `ECommerce.Shopping.Application/` (bridge was removed in Step 0B). |
| Step 0B | **Merged** | Native Shopping handlers own wishlist. No bridge or old WishlistService references remain. |
| Step 1 | **Merged** | Payments BC created under `src/backend/Payments/`; `PaymentsController` moved to Features and old `IPaymentService`/`PaymentService` removed. |
| Step 2 | Not Started | DashboardService still live. Prep checklist: `phase-9-step-2-prep.md` |
| Step 3 | Not Started | All controllers still in `Controllers/`. Prep checklist: `phase-9-step-3-prep.md` |
| Step 4 | In Progress (Branch Complete) | Shared folder reorganization implemented on `feature/phase-9-step-4-shared-folder`; pending merge. Prep checklist: `phase-9-step-4-prep.md` |
| Step 5 | **Merged** | Legacy repository implementations/interfaces removed; inventory compatibility bridge deleted. Prep checklist: `phase-9-step-5-prep.md` |
| Step 6 | **Merged** | `ECommerce.Application` deleted after DTO/validator/interface/service migration to Contracts/SharedKernel/Infrastructure. Prep checklist: `phase-9-step-6-prep.md` |
| Step 7 | Not Started | `ECommerce.Core` still live. Prep checklist: `phase-9-step-7-prep.md` |

### Step 0 Checklist

**Done**
- Step 0A: Wishlist bridge introduction is merged.
- Early Wins: orphaned `CartService` and `ReviewService` are deleted.
- Step 0B: native Shopping handlers fully own wishlist operations.

---

## What Phase 9 Is

By the end of Phase 8, all bounded contexts are logically separated in their own Domain/Application/Infrastructure projects. However, `ECommerce.API` still has a **monolith API structure**:

- All controllers live in one flat `Controllers/` folder
- Some controllers still delegate to old `IXxxService` classes (not CQRS handlers)
- Old repositories still exist in `ECommerce.Infrastructure/Repositories/`
- Old core interfaces still exist in `ECommerce.Core/Interfaces/Repositories/`
- `ECommerce.Application` and `ECommerce.Core` are still live projects

Phase 9 reorganizes the API layer to match the bounded context structure, migrates remaining old services to CQRS, and deletes orphaned legacy code.

---

## Step Dependency Graph

You cannot delete `ECommerce.Application` (Step 6) until all services are gone (Steps 0–2 + early wins).
You cannot delete `ECommerce.Core` (Step 7) until repositories are gone (Step 5).
Steps 3 and 4 are mechanical and independent — they can happen any time after Step 0.

```
[Step 0: Wishlist Bridge] ──┐
[Step 1: Payments BC]       ├──► [Step 6: Delete ECommerce.Application]
[Step 2: Dashboard CQRS]   ──┘         │
[Early Wins: Orphaned svc] ──┘         ▼
                                  [Step 7: Delete ECommerce.Core]
                                        ▲
[Step 5: Delete Repositories] ──────────┘

[Step 3: Controller Moves]  ── independent, low risk
[Step 4: Shared Folder]     ── independent, low risk
```

**Rule:** Always do a `dotnet build` and run characterization tests after each step before moving to the next.

---

## Branch Strategy

Each step should be an isolated branch and PR:

```
feature/phase-9-step-0-wishlist-bridge
feature/phase-9-step-1-payments-bc
feature/phase-9-step-2-dashboard-cqrs
feature/phase-9-step-3-controller-moves
feature/phase-9-step-4-shared-folder
feature/phase-9-step-5-delete-repositories
feature/phase-9-step-6-delete-application
feature/phase-9-step-7-delete-core
```

Never combine a deletion step with a new feature in the same PR.

---

## Current State (as of start of Phase 9)

### Controllers: Migration Status

| Controller | BC Folder | MediatR? | Old Service? |
|---|---|---|---|
| `CatalogProductsController` | `Catalog/` | ✅ | ✗ |
| `CatalogCategoriesController` | `Catalog/` | ✅ | ✗ |
| `InventoryController` | `Inventory/` | ✅ | ✗ |
| `PromoCodesController` | `Promotions/` | ✅ | ✗ |
| `CartController` | `Shopping/` | ✅ | ✗ |
| `WishlistController` | `Shopping/` | ✅ via Bridge | ⚠️ via Bridge |
| `ReviewsController` | `Reviews/` | ✅ | ✗ |
| `AuthController` | `Identity/` | ✅ | ✗ |
| `ProfileController` | `Identity/` | ✅ | ✗ |
| `OrdersController` | `Ordering/` | ✅ | ✗ |
| `DashboardController` | ❌ None | ✗ | ✅ IDashboardService |
| `PaymentsController` | ❌ None | ✗ | ✅ IPaymentService |
| `IntegrationDeadLettersController` | ❌ None | ✗ | ✅ IDeadLetterReplayService* |

*`IDeadLetterReplayService` is infrastructure tooling, not a business service — it stays but moves.

### Legacy Services Inventory

| Service | Status | Action |
|---|---|---|
| `CartService` | **Orphaned** — CartController already uses CQRS | Delete (Early Win) |
| `ReviewService` | **Orphaned** — ReviewsController already uses CQRS | Delete (Early Win) |
| `InventoryService` | Registered with `// kept for OrderService compatibility` comment | **Investigate first** (see below) |
| `WishlistService` | Used by `WishlistMediatorBridge.cs` | Delete in Step 0 |
| `DashboardService` | Used by `DashboardController` | Delete in Step 2 |
| `PaymentService` | Used by `PaymentsController` | Delete in Step 1 |
| `CurrentUserService` | Cross-cutting — used by many controllers | **Keep** |
| `DistributedIdempotencyStore` | Cross-cutting | **Keep** |
| `SendGridEmailService` / `SmtpEmailService` | Cross-cutting | **Keep** |
| `WebhookVerificationService` | Payment-specific | Move to Payments BC in Step 1 |

### Known Gap: IInventoryService

`ServiceCollectionExtensions.cs` line 345–346:
```csharp
// IInventoryService kept for OrderService compatibility
services.AddScoped<IInventoryService, InventoryService>();
```

**Before starting Phase 9:** Find what is still calling `IInventoryService`. This must be resolved before Step 5 (repository deletion). If `OrderService` is still alive in the old monolith, that is a Phase 8 migration gap.

```powershell
rg "IInventoryService" src/backend --include="*.cs"
```

### Known Gap: AutoMapper / MappingProfile

`DashboardService` and other old services use `IMapper` (AutoMapper). `MappingProfile.cs` lives in `ECommerce.Application`.

**Decision required before Step 2:** Do per-BC application layers use AutoMapper or manual mapping?

| Option | Trade-off |
|---|---|
| Keep AutoMapper per BC | Each BC registers its own profile. Familiar, less code. |
| Manual mapping in handlers | No dependency, explicit, easier to debug. Preferred in pure CQRS. |

**Recommendation:** New BCs (Payments) use manual mapping. Existing migrated BCs keep their current approach. Do not add AutoMapper to new BCs going forward.

Once `DashboardService` is deleted in Step 2, check if `MappingProfile.cs` has any remaining mappings. If empty, delete it and remove AutoMapper from `ECommerce.Application.csproj`.

---

## Target Structure

```
ECommerce.API/
├── Features/
│   ├── Catalog/
│   │   └── Controllers/
│   │       ├── CatalogProductsController.cs
│   │       └── CatalogCategoriesController.cs
│   ├── Identity/
│   │   └── Controllers/
│   │       ├── AuthController.cs
│   │       └── ProfileController.cs
│   ├── Inventory/
│   │   └── Controllers/
│   │       └── InventoryController.cs
│   ├── Shopping/
│   │   └── Controllers/
│   │       ├── CartController.cs
│   │       └── WishlistController.cs
│   ├── Promotions/
│   │   └── Controllers/
│   │       └── PromoCodesController.cs
│   ├── Reviews/
│   │   └── Controllers/
│   │       └── ReviewsController.cs
│   ├── Ordering/
│   │   └── Controllers/
│   │       └── OrdersController.cs
│   ├── Payments/                          ← NEW bounded context (Step 1)
│   │   └── Controllers/
│   │       └── PaymentsController.cs
│   ├── Dashboard/                         ← Cross-context read model (Step 2)
│   │   └── Controllers/
│   │       └── DashboardController.cs
│   └── IntegrationOps/                    ← Admin infrastructure tooling (Step 3)
│       └── Controllers/
│           └── IntegrationDeadLettersController.cs
│
└── Shared/                                ← Cross-cutting concerns only
    ├── ActionFilters/
    ├── Behaviors/
    ├── Middleware/
    ├── HealthChecks/
    ├── Configuration/
    ├── Extensions/
    └── Helpers/
```

### Folders to Delete (Orphaned After Reorganization):
```
ECommerce.API/Controllers/              ← Empty after Step 3
ECommerce.API/Models/                   ← Already empty now
ECommerce.API/Validators/              ← Already empty now
ECommerce.API/Configuration/           ← Empty after Step 4
ECommerce.API/Extensions/              ← Empty after Step 4
ECommerce.API/Helpers/                 ← Empty after Step 4
```

---

## Steps

---

### Early Wins: Delete Orphaned Services

**Effort:** Low — 30 minutes
**Risk:** Low — nothing depends on these

`CartService` and `ReviewService` are already orphaned. Their controllers use pure CQRS.
Delete them before touching anything else — it reduces noise and confirms nothing breaks.

**What to do:**
1. Delete `src/backend/ECommerce.Application/Services/CartService.cs`
2. Delete `src/backend/ECommerce.Application/Interfaces/ICartService.cs`
3. Delete `src/backend/ECommerce.Application/Services/ReviewService.cs`
4. Delete `src/backend/ECommerce.Application/Interfaces/IReviewService.cs`
5. Remove their registrations from `ServiceCollectionExtensions.cs`
6. Delete related DTOs that are no longer referenced anywhere:
   - `ECommerce.Application/DTOs/Cart/*` — verify not used by CartController (it uses BC DTOs)
   - `ECommerce.Application/DTOs/Reviews/*` — verify not used by ReviewsController
7. `dotnet build` → must be green

**Verify with:**
```powershell
# rg (ripgrep — available via VS Code / winget)
rg "ICartService|IReviewService" src/backend --include="*.cs"
# Expected: zero results
```

---

### Step 0A: Wishlist Bridge Introduction ✅ MERGED

**Goal:** Introduce native CQRS handlers in `ECommerce.Shopping.Application/` while keeping the bridge alive as a safety net. The controller dispatches via MediatR but the bridge still delegates to `WishlistService` underneath.

**State:** Complete. The following exist in `ECommerce.Shopping.Application/`:
- `Commands/AddToWishlist/`, `Commands/RemoveFromWishlist/`, `Commands/ClearWishlist/`
- `Queries/GetWishlist/`, `Queries/IsProductInWishlist/`

The bridge (`WishlistMediatorBridge.cs`) and `WishlistService` are still registered. Both sets of handlers exist simultaneously. `WishlistCharacterizationTests.cs` explicitly notes: *"Must pass against both old WishlistService AND new MediatR handlers."*

**Nothing to do here — already merged.**

---

### Step 0B: Wishlist Bridge Removal

**Effort:** Medium — half a day
**Risk:** Medium — native handlers must be fully exercised before bridge is deleted

**Pre-condition:** `WishlistCharacterizationTests` must be green against the native BC handlers before the bridge is deleted. Verify by temporarily removing the bridge registration and running tests. If green, proceed. If not, fix the native handlers first.

**Goal:** Delete the bridge and `WishlistService`. Native BC handlers take full ownership.

**What to do:**
1. Verify native handlers use Shopping BC repository directly — not `IWishlistService`
2. Temporarily comment out `WishlistMediatorBridge.cs` handler registrations and run:
   - `dotnet test --filter "Wishlist"` — must be green before proceeding
3. Delete `WishlistMediatorBridge.cs`
4. Delete `IWishlistService` + `WishlistService` from `ECommerce.Application`
5. Remove `services.AddScoped<IWishlistService, WishlistService>()` from `ServiceCollectionExtensions.cs`
6. Delete `ECommerce.Application/DTOs/Wishlist/*` if no longer referenced
7. Delete `ECommerce.Tests/Unit/Services/WishlistServiceTests.cs`

**Verify (PowerShell / rg):**
```powershell
# PowerShell
Select-String -Path "src/backend/**/*.cs" -Pattern "IWishlistService|WishlistMediatorBridge" -Recurse
# Expected: zero results

# rg (ripgrep — available via VS Code)
rg "IWishlistService|WishlistMediatorBridge" src/backend --include="*.cs"
# Expected: zero results
```

---

### Step 1: Payments Bounded Context Creation

**Effort:** High — 2–3 days (new BC, not just a move)
**Risk:** High — idempotency, webhooks, refund logic must not regress

**Goal:** Create `src/backend/Payments/` BC and migrate `PaymentsController` to CQRS.

**What to do:**
1. Create `ECommerce.Payments.Core/` — payment entities, `PaymentStatus` enum, `PaymentId` value object
2. Create `ECommerce.Payments.Application/` — commands and queries:
   - `ProcessPaymentCommand` + Handler (includes idempotency logic currently in controller)
   - `RefundPaymentCommand` + Handler
   - `GetPaymentDetailsQuery` + Handler
   - `GetPaymentIntentQuery` + Handler
3. Create `ECommerce.Payments.Infrastructure/`:
   - Move `IWebhookVerificationService` + `WebhookVerificationService` here
   - Move `IPaymentStore` + `InMemoryPaymentStore` here
4. Rewrite `PaymentsController` to use `IMediator` only — push idempotency down into the command handler
5. Move controller to `Features/Payments/Controllers/`
6. Delete `IPaymentService` + `PaymentService` from `ECommerce.Application`
7. Move validators to `ECommerce.Payments.Application/Validators/`

**Note on idempotency:** The controller currently manages idempotency directly. During this step, decide if idempotency belongs in the handler or a MediatR pipeline behavior. Either way — move it out of the controller.

**Program.cs / ServiceCollectionExtensions.cs cleanup:**
- Remove `services.AddScoped<IPaymentService, PaymentService>()`
- Remove `services.AddScoped<IWebhookVerificationService, WebhookVerificationService>()`
- Add Payments BC service registrations

**Delete after step:**
- `src/backend/ECommerce.Application/Interfaces/IPaymentService.cs`
- `src/backend/ECommerce.Application/Services/PaymentService.cs`
- `src/backend/ECommerce.Application/Validators/Payments/*`
- `src/backend/ECommerce.Application/DTOs/Payments/*` (after verifying nothing else references them)

**Verify:**
```powershell
rg "IPaymentService|PaymentService" src/backend --include="*.cs"
# Expected: zero results
dotnet test --filter "Payment"
```

---

### Step 2: Dashboard CQRS Migration

**Effort:** Medium — 1 day
**Risk:** Low — Dashboard is admin-only read model, no mutation logic

**Goal:** Replace `IDashboardService` with CQRS queries across the owning BCs.

**What `DashboardService` currently queries (already known):**
```csharp
_unitOfWork.Orders.GetTotalOrdersCountAsync()
_unitOfWork.Orders.GetTotalRevenueAsync()
_unitOfWork.Orders.GetOrdersTrendAsync(30)
_unitOfWork.Orders.GetRevenueTrendAsync(30)
_unitOfWork.Users.GetCustomersCountAsync()
_unitOfWork.Products.GetActiveProductsCountAsync()
```

**Cross-context read model design:** Dashboard aggregates data from 3 BCs. Two options:

| Option | Trade-off |
|---|---|
| One `GetDashboardStatsQuery` in a thin Dashboard read model | Queries each BC's DbContext directly. Simple but couples Dashboard to BC schemas. |
| Separate queries per BC, composed in controller | `GetOrderStatsQuery` (Ordering), `GetUserStatsQuery` (Identity), `GetProductStatsQuery` (Catalog). Controller calls 3 mediator sends. |

**Recommendation:** Use Option B. Each BC exposes its own stats query. `DashboardController` composes them. This makes each query independently testable.

**What to do:**
1. Add `GetOrderStatsQuery` + Handler to `ECommerce.Ordering.Application/`
2. Add `GetUserStatsQuery` + Handler to `ECommerce.Identity.Application/`
3. Add `GetProductStatsQuery` + Handler to `ECommerce.Catalog.Application/`
4. Rewrite `DashboardController` to call 3 queries via `IMediator` and compose the response
5. Create `DashboardStatsDto` in `ECommerce.API/Features/Dashboard/` (API-layer composition DTO)
6. Move controller to `Features/Dashboard/Controllers/`
7. Delete `IDashboardService` + `DashboardService`

**Note:** All 4 controller endpoints (`GetStats`, `GetOrderStats`, `GetUserStats`, `GetRevenueStats`) currently call the same method — they are not properly separated. During this migration, make each endpoint call only the query it needs.

**Program.cs / ServiceCollectionExtensions.cs cleanup:**
- Remove `services.AddScoped<IDashboardService, DashboardService>()`
- Remove AutoMapper registration if `MappingProfile.cs` is now empty

**Delete after step:**
- `src/backend/ECommerce.Application/Interfaces/IDashboardService.cs`
- `src/backend/ECommerce.Application/Services/DashboardService.cs`
- `src/backend/ECommerce.Application/DTOs/Dashboard/*`
- `src/backend/ECommerce.Application/MappingProfile.cs` (if empty)

**Verify:**
```powershell
rg "IDashboardService|DashboardService" src/backend --include="*.cs"
# Expected: zero results
```

---

### Step 3: Controller Folder Reorganization

**Effort:** Low — 1–2 hours mechanical work
**Risk:** Low — namespace changes only, no logic changes

**Goal:** Move all already-migrated controllers into `Features/{Context}/Controllers/`.

**For each move:**
1. Create the target folder
2. Move the file
3. Update the namespace declaration
4. `dotnet build` — if it fails, a `using` statement is referencing the old namespace

**Move order (easiest first):**
1. `PromoCodesController` → `Features/Promotions/Controllers/`
2. `ReviewsController` → `Features/Reviews/Controllers/`
3. `InventoryController` → `Features/Inventory/Controllers/`
4. `CatalogProductsController` + `CatalogCategoriesController` → `Features/Catalog/Controllers/`
5. `AuthController` + `ProfileController` → `Features/Identity/Controllers/`
6. `CartController` + `WishlistController` → `Features/Shopping/Controllers/`
7. `OrdersController` → `Features/Ordering/Controllers/`
8. `IntegrationDeadLettersController` → `Features/IntegrationOps/Controllers/`

**After all moves:**
- Delete `ECommerce.API/Controllers/` (now empty)
- `dotnet build` → green
- Run all characterization tests

---

### Step 4: Shared Folder Reorganization

**Effort:** Low — 30 minutes
**Risk:** Low — namespace changes, update `using` in Program.cs

**Goal:** Move cross-cutting concerns into `Shared/`.

**What to move:**
- `Configuration/*` → `Shared/Configuration/`
- `Extensions/*` → `Shared/Extensions/`
- `Helpers/PaginationRequestNormalizer.cs` → `Shared/Helpers/`

**Keep in place (already well-organized):**
- `ActionFilters/`
- `Behaviors/`
- `Middleware/`
- `HealthChecks/`

**Program.cs cleanup:**
- Update `using` statements for moved namespaces

**Delete after:**
- `ECommerce.API/Configuration/` (empty)
- `ECommerce.API/Extensions/` (empty)
- `ECommerce.API/Helpers/` (empty)

---

### Step 5: Old Repository & Interface Deletion

**Effort:** Medium — half a day (verification-heavy)
**Risk:** High — confirm every BC has its own repository before deleting anything

**Pre-conditions (verify all before deleting a single file):**

```powershell
# Confirm no reference to old IUnitOfWork from ECommerce.Core
rg "ECommerce.Core.Interfaces.Repositories" src/backend --include="*.cs"

# Confirm no reference to old repository interfaces
rg "ICartRepository|ICategoryRepository|IOrderRepository|IProductRepository|IReviewRepository|IUserRepository|IWishlistRepository" src/backend --include="*.cs"

# Resolve this before proceeding:
rg "IInventoryService" src/backend --include="*.cs"
```

**Delete `ECommerce.Infrastructure/Repositories/`:**
- `CartRepository.cs`
- `CategoryRepository.cs`
- `OrderRepository.cs`
- `ProductRepository.cs`
- `Repository.cs`
- `ReviewRepository.cs`
- `UserRepository.cs`
- `WishlistRepository.cs`

**Delete `ECommerce.Core/Interfaces/Repositories/`:**
- `IRepository.cs`
- `ICartRepository.cs`
- `ICategoryRepository.cs`
- `IOrderRepository.cs`
- `IProductRepository.cs`
- `IReviewRepository.cs`
- `IUnitOfWork.cs`
- `IUserRepository.cs`
- `IWishlistRepository.cs`

**Also clean from `ECommerce.Core`:**
- `ECommerce.Core/Entities/*` — verify each entity is superseded by a per-BC aggregate
- `ECommerce.Core/Exceptions/*` — verify SharedKernel equivalents exist
- `ECommerce.Core/Results/*` — verify `ECommerce.SharedKernel.Results` is used everywhere

**Program.cs / ServiceCollectionExtensions.cs cleanup:**
- Remove `MediatRUnitOfWork` registration from `ECommerce.Infrastructure` if it references old `IUnitOfWork`

---

### Step 6: ECommerce.Application Deletion

**Effort:** Low — 30 minutes (if all pre-conditions are met)
**Risk:** High — compilation will catch any missed references

**Pre-conditions — all must be green:**
- [ ] `CartService` + `ReviewService` deleted (Early Wins)
- [ ] `WishlistService` deleted (Step 0)
- [ ] `PaymentService` deleted (Step 1)
- [ ] `DashboardService` deleted (Step 2)
- [ ] `IInventoryService` gap resolved
- [ ] No DTOs in `ECommerce.Application` are still referenced
- [ ] No validators in `ECommerce.Application` are still referenced
- [ ] `MappingProfile.cs` deleted or empty
- [ ] `BusinessRulesOptions.cs` moved to `Shared/Configuration/`
- [ ] `CurrentUserService` moved to `ECommerce.SharedKernel` or `ECommerce.API/Shared/`
- [ ] `DistributedIdempotencyStore` moved to `ECommerce.SharedKernel` or Infrastructure
- [ ] `SendGridEmailService` / `SmtpEmailService` moved to Notifications BC or SharedKernel

**Verify no remaining references:**
```powershell
rg "using ECommerce.Application" src/backend --include="*.cs"
# Expected: zero results
```

**Delete:**
- `src/backend/ECommerce.Application/` (entire directory)
- Remove from `ECommerce.sln`
- Remove `<ProjectReference Include="...ECommerce.Application.csproj" />` from `ECommerce.API.csproj`

---

### Step 7: ECommerce.Core Deletion

**Effort:** Low — 30 minutes (if Step 5 is done)
**Risk:** Medium — `ECommerce.Core.Enums` may still be referenced in controllers

**Pre-conditions:**
- [ ] All entities superseded by per-BC aggregates (verify Step 5)
- [ ] `ErrorCodes` constants moved to SharedKernel or per-BC constants
- [ ] `PaginationConstants` moved to SharedKernel
- [ ] `StringMaskingExtensions` moved to SharedKernel
- [ ] `ECommerce.Core.Enums` (UserRole, OrderStatus, PaymentStatus, DiscountType) — check if any controller still references these

**Verify:**
```powershell
rg "using ECommerce.Core" src/backend --include="*.cs"
# Expected: zero results
```

**Delete:**
- `src/backend/ECommerce.Core/` (entire directory)
- Remove from `ECommerce.sln`
- Remove `<ProjectReference>` from all `.csproj` files

---

## Deletion Order Summary

```
Early Wins : CartService, ReviewService (orphaned — delete immediately)
Step 0     : WishlistService + WishlistMediatorBridge
Step 1     : PaymentService + IPaymentService
Step 2     : DashboardService + IDashboardService + MappingProfile (if empty)
Step 3     : Controllers/ folder (empty after moves)
Step 4     : Configuration/, Extensions/, Helpers/ folders (empty after moves)
Step 5     : ECommerce.Infrastructure/Repositories/ + ECommerce.Core/Interfaces/Repositories/
Step 6     : ECommerce.Application/ (entire project)
Step 7     : ECommerce.Core/ (entire project)
```

---

## Invariants to Preserve

### Write-Commit Policy (Non-Negotiable)

During Phase 9 you will have old code (repositories calling `SaveChangesAsync`) and new code (UoW committed via MediatR transaction behavior) coexisting. This is the most likely source of bugs during the migration.

**The rule:**
> All writes in new or migrated code commit **only** via the MediatR `TransactionBehavior` pipeline. Repositories **never** call `SaveChangesAsync` directly.

Specifically:
- Command handlers call repository methods (`Add`, `Update`, `Remove`) — no `SaveChangesAsync`
- `SaveChangesAsync` is called once per command by the transaction pipeline behavior
- Old services (`CartService`, `WishlistService`, etc.) may still call `SaveChangesAsync` — this is acceptable **only while they exist**. They must not be called by new code paths
- When a service is deleted (Steps 0B, 1, 2, Early Wins), verify the replacement handler follows the new policy before deleting

To audit any new handler:
```powershell
# rg — should return zero results in any new BC handler file
rg "SaveChangesAsync" src/backend/Shopping/ECommerce.Shopping.Application --include="*.cs"
rg "SaveChangesAsync" src/backend/Payments --include="*.cs"
```

### Services and Interfaces

- `ICurrentUserService` — cross-cutting, not a business service
- `IIdempotencyStore` — cross-cutting
- `IEmailService` — cross-cutting (until Notifications BC is created)
- `IDeadLetterReplayService` — infrastructure ops tooling, not a business service
- Integration outbox/saga code in `ECommerce.Infrastructure/Integration/` — separate concern from Phase 9
- `ECommerce.Infrastructure` project itself — still owns `AppDbContext`, migrations, and integration infrastructure

---

## Testing Strategy

### The Core Rule

**Old monolith tests die with the monolith. New BC tests live in BC test projects.**

### Deletion Gate Rule

> No test file is deleted until the replacement BC tests are written, passing, and listed in the PR checklist.

Every PR that deletes a test file must include in its description:
```
Deleted: ECommerce.Tests/Unit/Services/WishlistServiceTests.cs
Replaced by: ECommerce.Shopping.Tests/Commands/AddToWishlistCommandHandlerTests.cs (green ✅)
             ECommerce.Shopping.Tests/Commands/RemoveFromWishlistCommandHandlerTests.cs (green ✅)
             ECommerce.Shopping.Tests/Queries/GetWishlistQueryHandlerTests.cs (green ✅)
```

If the replacement tests do not exist yet, do not delete the old test — even if the production code is deleted. Leave a `// TODO Phase 9 StepX: delete after Shopping.Tests covers this` comment and track it.

`ECommerce.Tests/` is the monolith test project. It will eventually be deleted along with `ECommerce.Application` and `ECommerce.Core`. Do not add new tests to it during Phase 9.

---

### Tests to Delete (with the code they test)

Delete each test file in the same PR as the code it tests. Never leave a test file pointing at a deleted class.

**Unit/Services/** — delete with each service:
```
CartServiceTests.cs          ← delete with CartService      (Early Wins)
ReviewServiceTests.cs        ← delete with ReviewService    (Early Wins)
WishlistServiceTests.cs      ← delete with WishlistService  (Step 0)
PaymentServiceTests.cs       ← delete with PaymentService   (Step 1)
DashboardServiceTests.cs     ← delete with DashboardService (Step 2)
InventoryServiceTests.cs     ← delete when IInventoryService gap is resolved
```

**Unit/Repositories/** — delete with each repository (Step 5):
```
CartRepositoryTests.cs
CategoryRepositoryTests.cs
OrderRepositoryTests.cs
ProductRepositoryTests.cs
RepositoryTests.cs
ReviewRepositoryTests.cs
UnitOfWorkTests.cs
UserRepositoryTests.cs
WishlistRepositoryTests.cs
```

**Unit/Validators/** — delete with each validator (Steps 1–2 or Step 6):
```
CartValidatorsTests.cs
PaymentValidatorsTests.cs
OrderValidatorsTests.cs
(others as their source validators are deleted)
```

**Unit/Mappings/**:
```
AutoMapperConfigurationTests.cs   ← delete with MappingProfile.cs (Step 2)
```

**Why delete and not migrate?**
These tests mock `ICartRepository`, `IUnitOfWork`, `IMapper` — none of which exist after Phase 9. Rewriting them to point at new BC internals would mean replacing 80%+ of the test body. The behavior is already covered by characterization tests. Delete the old test; write a focused new one in the BC test project.

---

### Tests to Keep (Safety Net)

These test HTTP behavior through the full stack. Controller moves and namespace changes are invisible to them. Run them after every step — a green suite means the step succeeded.

```
Integration/*CharacterizationTests.cs    ← keep, run after every step
Integration/*ControllerTests.cs          ← keep, run after every step
```

**One exception — `DashboardControllerTests.cs`:** This tests the old `IDashboardService` behavior. After Step 2 the test structure will change significantly. Rewrite it in Step 2 alongside the controller migration.

---

### Tests to Update (Don't Delete, Don't Leave Broken)

**`Unit/Architecture/BackendGuideConventionsTests.cs`**

This test enforces conventions by scanning `ECommerce.API/Controllers/`. After Step 3 that path no longer exists — the test will fail or silently pass vacuously (no files to scan = no violations found).

Update it in Step 3 to scan `ECommerce.API/Features/**/Controllers/` instead:

```csharp
// Before (Step 3 breaks this):
var controllersPath = Path.Combine(repoRoot, "src", "backend", "ECommerce.API", "Controllers");
var controllerFiles = Directory.GetFiles(controllersPath, "*.cs", SearchOption.TopDirectoryOnly);

// After (Step 3 fix):
var featuresPath = Path.Combine(repoRoot, "src", "backend", "ECommerce.API", "Features");
var controllerFiles = Directory.GetFiles(featuresPath, "*.cs", SearchOption.AllDirectories)
    .Where(f => f.Contains(Path.DirectorySeparatorChar + "Controllers" + Path.DirectorySeparatorChar));
```

Do this in the same PR as Step 3 controller moves.

---

### New Tests to Write

Each new BC handler needs its own unit test in the BC's test project — not in `ECommerce.Tests/`.

| Step | New Tests | Location |
|---|---|---|
| Step 0 | `AddToWishlistCommandHandlerTests`, `RemoveFromWishlistCommandHandlerTests`, `ClearWishlistCommandHandlerTests`, `GetWishlistQueryHandlerTests` | `ECommerce.Shopping.Tests/` |
| Step 1 | `ProcessPaymentCommandHandlerTests`, `RefundPaymentCommandHandlerTests`, `GetPaymentDetailsQueryHandlerTests` | `ECommerce.Payments.Tests/` (new project) |
| Step 2 | `GetOrderStatsQueryHandlerTests`, `GetUserStatsQueryHandlerTests`, `GetProductStatsQueryHandlerTests` | Each BC's test project |

**What to test in each handler test:**
- Happy path returns correct result
- Domain rule violations return `Result.Failure` with correct error code
- Repository is called with the correct arguments
- No calls to `SaveChangesAsync` inside query handlers

**What not to test in handler tests:**
- HTTP status code mapping — that's the controller's job and covered by integration tests
- Validation — that's the validator's job and covered by validator tests

---

### Test Inventory: End State

When Phase 9 is complete, `ECommerce.Tests/` should contain only:

```
Integration/*CharacterizationTests.cs   ← keep
Integration/*ControllerTests.cs         ← keep
Integration/TestWebApplicationFactory.cs
Helpers/IntegrationTestBase.cs
Helpers/TestDataFactory.cs
Unit/Architecture/BackendGuideConventionsTests.cs  ← updated
Unit/ActionFilters/ValidationFilterAttributeTests.cs
Unit/Middleware/*
Unit/HealthChecks/*
Unit/Extensions/DatabaseSchemaValidatorTests.cs
Unit/Services/CurrentUserServiceTests.cs
Unit/Services/InMemoryPaymentStoreTests.cs  ← moves to Payments.Tests in Step 1
Unit/Services/SendGridEmailServiceTests.cs  ← keep until Notifications BC
Unit/Services/SmtpEmailServiceTests.cs      ← keep until Notifications BC
Unit/Services/WebhookVerificationServiceTests.cs ← moves to Payments.Tests in Step 1
```

Everything else in `ECommerce.Tests/` is deleted during Phase 9 steps.

---

## Definition of Done

Phase 9 is complete when:

- [ ] `dotnet build` — no **new** warnings introduced vs the Phase 8 baseline (existing package/analyzer warnings are pre-existing and do not block completion)
- [ ] All characterization tests green
- [ ] `rg "using ECommerce.Application|using ECommerce.Core" src/backend --include="*.cs"` → zero results
- [ ] `ECommerce.Application.csproj` and `ECommerce.Core.csproj` are removed from `ECommerce.sln`
- [ ] All controllers live under `ECommerce.API/Features/{BoundedContext}/Controllers/`
- [ ] All cross-cutting infrastructure lives under `ECommerce.API/Shared/`
- [ ] `ECommerce.API/Controllers/` folder does not exist
- [ ] Every deleted test file has a linked replacement in the BC test project (deletion gate satisfied)
