# DDD & CQRS Migration Plan

**Created:** 2026-03-24
**Status:** Planning
**Owner:** @ivans

---

## What We're Doing

Migrating the entire backend from the current service-oriented architecture to **Domain-Driven Design (DDD)** with **CQRS (Command Query Responsibility Segregation)** using MediatR.

This is an **incremental migration**: old and new patterns coexist. Each bounded context is migrated one at a time. The app stays functional throughout.

## Why

1. **Current state**: Entities are mostly anemic data bags. All business logic lives in Application services. This works, but as complexity grows, services become god-classes that are hard to test and reason about.
2. **DDD** moves business logic into the domain model where it belongs. Aggregates enforce their own invariants. The code reads like the business speaks.
3. **CQRS** separates read and write concerns. Commands validate and mutate state through aggregates. Queries bypass domain complexity and read efficiently. This makes each side independently optimizable.
4. **Learning**: This migration is a structured learning exercise. Understanding DDD/CQRS deeply is a principal-level skill.

## Current State Assessment

| Aspect | Current | Target |
|--------|---------|--------|
| Entities | Anemic data bags (properties only) | Rich domain models (behavior + invariants) |
| Business logic | In Application services | In Aggregate Roots and Domain Services |
| Service methods | Mix of reads and writes | Separated into Commands and Queries |
| Side effects | Direct service-to-service calls | Domain Events + Handlers |
| Project structure | 4 projects (API, Application, Core, Infrastructure) | SharedKernel + bounded context projects |
| Communication | Service injection | MediatR + Domain Events |
| Validation | FluentValidation on DTOs only | DTO validation + Domain invariants |

## Read Order

Before starting any implementation, read in this order:

1. `theory/01-ddd-fundamentals.md` — Core DDD concepts (aggregates, value objects, domain events)
2. `theory/02-cqrs-and-mediatr.md` — CQRS pattern and MediatR library
3. `theory/03-query-side.md` — Thin read stack, projections, read vs write DTOs
4. `theory/04-value-types-and-dtos.md` — Value objects (record vs class), enums, structs, DTOs, AutoMapper
5. `theory/05-api-layer.md` — Controllers, Result pattern, ApiResponse, GlobalExceptionHandler, ICurrentUserService
6. `theory/06-ef-core-persistence.md` — How EF Core persists value objects (value converters vs owned entities)
7. `theory/07-testing-ddd.md` — Domain unit tests, handler tests, integration tests, characterization tests
8. `context-map.md` — Our bounded contexts and how they relate
9. `target-structure.md` — Final project/folder structure
10. `rules.md` — DDD/CQRS rules for this project
11. `phases/phase-0-foundation.md` — First implementation phase

## Bounded Contexts (Migration Order)

| # | Context | Key Aggregates | Why This Order |
|---|---------|----------------|----------------|
| 0 | Foundation ✅ | SharedKernel, MediatR | Infrastructure needed by everything |
| 1 | **Catalog** ✅ | Product, Category | Simplest, most referenced, best to learn on |
| 2 | **Identity** ✅ | User | Deep dive into Value Objects |
| 3 | **Inventory** | InventoryItem | Introduces Domain Events |
| 4 | **Shopping** | Cart, Wishlist | Aggregate Root with child entities |
| 5 | **Promotions** | PromoCode | Domain Services pattern |
| 6 | **Reviews** | Review | Cross-context references |
| 7 | **Ordering** | Order | Most complex, orchestration, sagas |
| 8 | Extraction | — | Separate assemblies per context |

See `context-map.md` for full analysis.

## Phase Overview

### Phase 0: Foundation (no business logic changes)
- Create `ECommerce.SharedKernel` project with DDD base classes
- Install and configure MediatR in API
- Create pipeline behaviors (validation, logging, transaction)
- Everything still works exactly as before

### Phase 1: Catalog Bounded Context
- **Learn**: Aggregate Roots, Entities, Value Objects, Commands, Queries, Handlers
- Create `Catalog.Domain`: Rich Product aggregate (with ProductImage children), Category aggregate
- Create `Catalog.Application`: Commands (CreateProduct, UpdateProduct, ...) and Queries (GetProducts, GetProductBySlug, ...)
- Create `Catalog.Infrastructure`: Repositories, EF configurations
- Update controllers to dispatch via MediatR instead of calling services
- Delete old ProductService, CategoryService

### Phase 2: Identity Bounded Context
- **Learn**: Value Objects deeply (Email, PersonName, PhoneNumber), Password as domain concept
- Rich User aggregate with domain methods
- Auth as an Application concern (not domain)

### Phase 3: Inventory Bounded Context
- **Learn**: Domain Events, Event Handlers, cross-context communication
- Separate InventoryItem aggregate (references ProductId by ID, not navigation)
- Domain Events: StockReduced, LowStockDetected
- Event Handlers replace direct service calls

### Phase 4: Shopping Bounded Context
- **Learn**: Aggregate boundaries, transaction boundaries, consistency rules
- Cart aggregate with CartItem child entities
- Invariant enforcement (no duplicates, quantity limits, stock validation)

### Phase 5: Promotions Bounded Context
- **Learn**: Domain Services, Specification pattern
- PromoCode aggregate with rich validation
- DiscountCalculation as Domain Service

### Phase 6: Reviews Bounded Context
- **Learn**: Anti-corruption layers, referencing external aggregates by ID
- Review aggregate with edit-window invariant
- References Product and User by ID only

### Phase 7: Ordering Bounded Context
- **Learn**: Complex aggregates, state machines, process managers
- Order aggregate with status transition state machine
- OrderPlaced event triggers inventory reduction, email, cart clearing
- Process manager coordinates multi-step flows

### Phase 8: Assembly Extraction & Integration Events
- **Learn**: Physical bounded context separation, integration events vs domain events
- Extract each context into separate solution folders
- Replace in-process events with integration events where needed

## Technical Debt

During the migration, old and new code coexist. This creates deliberate, tracked debt.

See `debt/README.md` for the full debt register — every naming conflict, every dual registration, and the exact cleanup step for each.

Key items:
- **Naming conflicts** — `IProductRepository`, `Product`, `Category`, etc. exist in both Core and the new context projects during transition. Resolved at each phase's cutover step.
- **Dual `IUnitOfWork`** — old `Core.IUnitOfWork → UnitOfWork` and new `SharedKernel.IUnitOfWork → MediatRUnitOfWork` coexist until Phase 7 complete.
- **AutoMapper vulnerability** — cannot remove until all old services are gone (Phase 7).

---

## Multi-AI Workflow

This migration uses 4 AI roles. See `prompts/roles.md` for full definitions.

| Role | Tool | Purpose |
|------|------|---------|
| **Orchestrator** | Claude Code (this plan) | Creates the plan, theory, rules, analyzes order |
| **Descriptor** | Kilo Code | Reads the plan, generates step-by-step prompts |
| **Programmer** | Copilot / GPT | Implements code from prompts |
| **Tester** | AI of choice | Verifies implementation matches plan |

## Migration Strategy

### Incremental Coexistence Rules
1. **Never break the build**: After every step, the app must compile and run
2. **One context at a time**: Complete one bounded context before starting the next
3. **Old code stays until replaced**: Don't delete a service until its MediatR replacement is working
4. **Shared database**: All contexts share one DbContext during migration (extract in Phase 8)
5. **Controllers adapt last**: Update controllers to use MediatR only after handlers are tested

### Definition of Done (per bounded context)
- [ ] **Characterization tests written BEFORE deleting the old service** — integration tests that capture every endpoint's current behavior (happy path + key error paths). These run against the new handlers and must pass before the old service is removed.
- [ ] Domain project created with rich aggregates and value objects
- [ ] Application project created with Commands, Queries, and Handlers
- [ ] Infrastructure project created with repositories and EF configs
- [ ] Old service deleted (only after characterization tests pass on new handlers)
- [ ] Controllers updated to use MediatR
- [ ] All existing functionality preserved (no regressions — proven by characterization tests)
- [ ] Domain invariants enforced (things that were bugs before are now impossible)
- [ ] Authorization verified: handlers respect the same permission rules as the old service (see rules.md §Authorization)

## Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| CQRS library | MediatR | Industry standard for .NET, great pipeline support |
| Event Sourcing | **No** | Adds massive complexity, not needed for this domain |
| Domain Events | **Yes** (in-process via MediatR) | Decouples aggregates, enables side-effect handling |
| Separate DbContexts | **Later** (Phase 8) | Keep migration simple, extract when patterns are solid |
| Bounded context projects | **Yes** (one per context) | Clean separation, enforced by compiler |
| Shared Kernel | **Separate project** | Base classes reused across all contexts |
| **Notifications** | **Infrastructure side effect** | No Notifications bounded context. Email is sent by event handlers in each context calling `IEmailService`. Interface in Application, implementation (SendGrid/SMTP) in Infrastructure. Notification preferences/templates/history = Phase 9 if ever needed. |
| **Payment** | **External system — result recorded** | The domain does not process payments. The API layer calls Stripe before dispatching `PlaceOrderCommand`. `PaymentInfo` value object records the result (reference ID, method, amount). No payment SDK in the domain. |
| **ICurrentUserService** | **Interface in SharedKernel, implementation in API** | Handlers in Application projects need current user. If the interface lived in API, Application → API dependency would violate Clean Architecture. SharedKernel is the correct home. |
