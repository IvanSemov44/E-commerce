# Prompt Template: Bounded Context Migration (Phases 1–7)

**Use this template to create the prompt files for each new bounded context.**

Copy it, replace `{Context}` with the context name (Catalog, Identity, Inventory, etc.),
and fill in the context-specific sections marked with `← FILL IN`.

---

## How to Use This Template

For each phase (1–7), create a folder:
```
prompts/phase-{N}-{context}/
    step-1-domain.md          ← Create the Domain project (aggregates, value objects, events)
    step-2-application.md     ← Create the Application project (commands, queries, handlers)
    step-3-infrastructure.md  ← Create the Infrastructure project (repos, EF configs)
    step-4-cutover.md         ← Update controllers, delete old service, verify
```

Each file follows the same structure as the Phase 0 prompt files (Descriptor prompt + Programmer prompt + Tester checklist).

---

## Template: Step 1 — Domain Project

```markdown
# Phase {N}, Step 1: {Context} Domain Project

**Prerequisite**: Phase {N-1} must be complete.

---

## Prompt for Descriptor (Kilo Code)

\`\`\`
# Role: Descriptor — Phase {N}: {Context} Bounded Context, Step 1

You are the Descriptor. Phase {N-1} is done.
Now guide me through creating the {Context}.Domain project.

## Read These Files First

1. `.ai/plans/ddd-cqrs-migration/context-map.md` — {Context} section (aggregates, value objects, invariants, events)
2. `.ai/plans/ddd-cqrs-migration/rules.md` — aggregate rules 1–7, value object rules 8–12
3. `.ai/plans/ddd-cqrs-migration/target-structure.md` — target folder layout for {Context}
4. `.ai/plans/ddd-cqrs-migration/theory/01-ddd-fundamentals.md` — refresh on aggregates/value objects
5. `.ai/plans/ddd-cqrs-migration/phases/phase-{N}-{context}.md` — this phase's detailed plan

## Your Task

Guide me through creating the Domain project ONLY. No Application or Infrastructure yet.

For each aggregate:
1. Explain the aggregate boundary and why (what invariants it enforces)
2. Walk through each value object and why it's not a primitive
3. Show domain events the aggregate raises and when
4. Reference applicable rules from rules.md
5. Provide the Programmer prompt

## Key Concepts for This Phase  ← FILL IN per context

Phase 1 Catalog: Aggregate Roots, Entities, Value Objects, Commands, Queries
Phase 2 Identity: Value Objects deeply (Email, PersonName), Password as domain concept
Phase 3 Inventory: Domain Events, cross-context communication by ID
Phase 4 Shopping: Aggregate boundaries with children, Cart consistency rules
Phase 5 Promotions: Domain Services, Specification pattern
Phase 6 Reviews: Anti-corruption layer, cross-context ID references
Phase 7 Ordering: State machines, process managers, complex orchestration

## Constraints
- Domain project references ONLY ECommerce.SharedKernel — nothing else
- No EF Core references in Domain
- Aggregates enforce all invariants via DomainException
- External aggregates referenced by ID only (Rule 2)
\`\`\`

---

## Prompt for Programmer

\`\`\`
# Task: Create {Context}.Domain Project — Phase {N} Step 1

## Project Setup

\`\`\`bash
cd src/backend
dotnet new classlib -n ECommerce.{Context}.Domain -f net10.0
dotnet sln ../../ECommerce.sln add ECommerce.{Context}.Domain/ECommerce.{Context}.Domain.csproj

# Only dependency: SharedKernel
dotnet add ECommerce.{Context}.Domain/ECommerce.{Context}.Domain.csproj \
    reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj

# Delete auto-generated Class1.cs
\`\`\`

## Aggregates to Create  ← FILL IN from context-map.md

### Aggregate: {AggregateRoot}

Location: `ECommerce.{Context}.Domain/Aggregates/{AggregateRoot}/`

Files:
- `{AggregateRoot}.cs` — the root entity
  - Inherits from `AggregateRoot` (SharedKernel)
  - Private constructor (no public new — use static factory method)
  - Static factory: `{AggregateRoot}.Create(...)` — validates, creates, raises Created event
  - Domain methods: one method per business operation (not setters)
  - Raises domain events for every state change worth reacting to
  - Collections exposed as `IReadOnlyCollection<T>`

- `{ChildEntity}.cs` — child entities if any
  - Inherits from `Entity` (SharedKernel)
  - Created only through the aggregate root method

- `Events/{AggregateRoot}CreatedEvent.cs`
- `Events/{AggregateRoot}UpdatedEvent.cs`
  ... ← FILL IN from context-map.md

## Value Objects to Create  ← FILL IN from context-map.md

Location: `ECommerce.{Context}.Domain/ValueObjects/`

Each value object:
- Inherits from `ValueObject` (SharedKernel)
- Immutable (no setters)
- Private constructor + static `Create(...)` factory that validates
- Throws `{Context}DomainException` if invalid
- Implements `GetEqualityComponents()`

## Repository Interfaces  ← FILL IN per aggregate

Location: `ECommerce.{Context}.Domain/Interfaces/`

```csharp
public interface I{AggregateRoot}Repository
{
    Task<{AggregateRoot}?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync({AggregateRoot} entity, CancellationToken ct = default);
    Task UpdateAsync({AggregateRoot} entity, CancellationToken ct = default);
    // add GetByXxx methods as needed by the context
}
```

## Domain Exception

Location: `ECommerce.{Context}.Domain/Exceptions/{Context}DomainException.cs`

```csharp
public class {Context}DomainException : DomainException
{
    public {Context}DomainException(string message) : base(message) { }
    public {Context}DomainException(string code, string message) : base(code, message) { }
}
```

## Build Verification

\`\`\`bash
dotnet build ECommerce.{Context}.Domain/ECommerce.{Context}.Domain.csproj
dotnet build  # Entire solution still builds
\`\`\`

## Rules to Follow
- Rule 1: One repository interface per aggregate root
- Rule 2: External refs by ID only — no navigation properties to other aggregates
- Rule 4: Aggregate root controls children — no public Add/Remove on children
- Rule 5: Collections are IReadOnlyCollection
- Rule 6: Only root calls AddDomainEvent
- Rule 7: No service injection in aggregates
- Rule 8–11: Value object rules (immutable, validated at creation, equality by value)
- Rule 13: Domain events in past tense
- Rule 25: Repository interface in Domain, implementation in Infrastructure (Step 3)

## Acceptance Criteria
- [ ] ECommerce.{Context}.Domain project created and added to solution
- [ ] Only dependency: ECommerce.SharedKernel
- [ ] No EF Core using statements anywhere in Domain project
- [ ] All aggregates created with factory methods and domain methods
- [ ] All value objects created (immutable, validated)
- [ ] Domain events defined as records
- [ ] Repository interfaces defined
- [ ] {Context}DomainException created
- [ ] `dotnet build` succeeds for Domain project and entire solution
\`\`\`

---

## Tester Checklist — Step 1

1. No `using Microsoft.EntityFrameworkCore` in any file in the Domain project
2. No public setters on aggregate properties (only `private set` or `init`)
3. Aggregates have static factory methods, not public constructors
4. Value objects validate in constructor — test by trying to create an invalid one
5. Repository interface is in Domain, not Application or Infrastructure
6. Domain events are `record` types inheriting `DomainEventBase`
7. `dotnet build` succeeds
```

---

## Template: Step 2 — Application Project

```markdown
# Phase {N}, Step 2: {Context} Application Project

**Prerequisite**: Step 1 ({Context}.Domain) must be complete.

---

## Prompt for Programmer

\`\`\`
# Task: Create {Context}.Application Project — Phase {N} Step 2

## Project Setup

\`\`\`bash
cd src/backend
dotnet new classlib -n ECommerce.{Context}.Application -f net10.0
dotnet sln ../../ECommerce.sln add ECommerce.{Context}.Application/ECommerce.{Context}.Application.csproj

dotnet add ECommerce.{Context}.Application/ECommerce.{Context}.Application.csproj \
    reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
dotnet add ECommerce.{Context}.Application/ECommerce.{Context}.Application.csproj \
    reference ECommerce.{Context}.Domain/ECommerce.{Context}.Domain.csproj

dotnet add ECommerce.{Context}.Application/ECommerce.{Context}.Application.csproj package MediatR
dotnet add ECommerce.{Context}.Application/ECommerce.{Context}.Application.csproj package FluentValidation
\`\`\`

## Commands to Create  ← FILL IN from context-map.md / phase detail doc

For EACH command, create a folder `Commands/{CommandName}/` containing:

**{CommandName}Command.cs**
```csharp
// Implements ITransactionalCommand if it needs a DB transaction
public record {CommandName}Command(...) : IRequest<Result<{ReturnType}>>, ITransactionalCommand;
```

**{CommandName}CommandHandler.cs**
```csharp
public class {CommandName}CommandHandler : IRequestHandler<{CommandName}Command, Result<{ReturnType}>>
{
    // Inject: I{AggregateRoot}Repository, IUnitOfWork, ICurrentUserService (if auth needed)

    public async Task<Result<{ReturnType}>> Handle(
        {CommandName}Command command, CancellationToken cancellationToken)
    {
        // 1. Authorization check (Rule 38: fail fast before loading aggregate)
        // 2. Load aggregate via repository
        // 3. Call domain method (let domain enforce invariants)
        // 4. SaveChangesAsync (UnitOfWork stamps UpdatedAt + dispatches events)
        // 5. Return Result.Ok(...)
    }
}
```

**{CommandName}CommandValidator.cs**
```csharp
public class {CommandName}CommandValidator : AbstractValidator<{CommandName}Command>
{
    public {CommandName}CommandValidator()
    {
        // Input validation (not business rules — those are in the aggregate)
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        // ...
    }
}
```

## Queries to Create  ← FILL IN

For EACH query, create a folder `Queries/{QueryName}/` containing:

**{QueryName}Query.cs**
```csharp
// No ITransactionalCommand — queries never need transactions
public record {QueryName}Query(...) : IRequest<Result<{DtoType}>>;
```

**{QueryName}QueryHandler.cs**
```csharp
public class {QueryName}QueryHandler : IRequestHandler<{QueryName}Query, Result<{DtoType}>>
{
    private readonly AppDbContext _db; // or IProductReadRepository

    public async Task<Result<{DtoType}>> Handle(
        {QueryName}Query query, CancellationToken cancellationToken)
    {
        // Project directly to DTO via .Select()
        // Use .AsNoTracking()
        // No aggregate loading
    }
}
```

## DTOs to Create  ← FILL IN

Location: `ECommerce.{Context}.Application/DTOs/`

One DTO file per read view. Keep DTOs flat and UI-friendly.

## Event Handlers (if context raises events consumed by others)

Location: `ECommerce.{Context}.Application/EventHandlers/`

```csharp
public class {WhatItDoes}On{EventName}Handler : INotificationHandler<{EventName}>
{
    public async Task Handle({EventName} notification, CancellationToken ct)
    {
        // One responsibility only (Rule 16)
        // Log and swallow exceptions — don't crash the caller (Rule 17)
    }
}
```

## Register Application assembly in API Program.cs

```csharp
// Add to AddMediatR in Program.cs:
cfg.RegisterServicesFromAssembly(typeof({SomeCommand}Command).Assembly);
```

## Acceptance Criteria
- [ ] Application project created and added to solution
- [ ] Only dependencies: SharedKernel, {Context}.Domain, MediatR, FluentValidation
- [ ] All commands created with handler + validator
- [ ] All queries created with handler
- [ ] All DTOs created
- [ ] Application assembly registered in Program.cs AddMediatR
- [ ] `dotnet build` succeeds
\`\`\`
```

---

## Template: Step 3 — Infrastructure Project

```markdown
# Phase {N}, Step 3: {Context} Infrastructure Project

\`\`\`
# Task: Create {Context}.Infrastructure Project — Phase {N} Step 3

## Project Setup

\`\`\`bash
dotnet new classlib -n ECommerce.{Context}.Infrastructure -f net10.0
dotnet sln ../../ECommerce.sln add ECommerce.{Context}.Infrastructure/ECommerce.{Context}.Infrastructure.csproj

dotnet add ECommerce.{Context}.Infrastructure/ECommerce.{Context}.Infrastructure.csproj \
    reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
dotnet add ECommerce.{Context}.Infrastructure/ECommerce.{Context}.Infrastructure.csproj \
    reference ECommerce.{Context}.Domain/ECommerce.{Context}.Domain.csproj
dotnet add ECommerce.{Context}.Infrastructure/ECommerce.{Context}.Infrastructure.csproj \
    package Microsoft.EntityFrameworkCore
\`\`\`

## Repository Implementations

Location: `Repositories/{AggregateRoot}Repository.cs`

Implements `I{AggregateRoot}Repository` from the Domain project.
Uses the shared AppDbContext (Phase 8 will extract context per bounded context).

## EF Core Configurations

Location: `Configurations/{Entity}Configuration.cs`

Implements `IEntityTypeConfiguration<{Entity}>`.
Configures value objects as owned entities or value conversions.
Adds to AppDbContext.OnModelCreating via `modelBuilder.ApplyConfiguration(...)`.

## Acceptance Criteria
- [ ] Infrastructure project created
- [ ] Repository implementations match the domain interfaces
- [ ] EF configurations created for all aggregates and value objects
- [ ] Repository interfaces registered in DI
- [ ] `dotnet build` succeeds
\`\`\`
```

---

## Template: Step 4 — Cutover (Delete Old Service)

```markdown
# Phase {N}, Step 4: Cutover — Delete Old Service

**This is the "point of no return" for this bounded context.**
**Do NOT start this step until Steps 1–3 are complete AND characterization tests pass.**

\`\`\`
# Task: Cutover {Context} Bounded Context — Phase {N} Step 4

## Pre-cutover checklist (MUST all pass)
- [ ] Characterization tests written for all {Context} endpoints
- [ ] Characterization tests pass against new MediatR handlers
- [ ] Authorization behavior verified (new handlers respect same rules as old service)
- [ ] `dotnet build` is clean

## Cutover steps

### 1. Update controllers

For each {Context} controller:
- Replace `I{Context}Service` injection with `IMediator`
- Replace service calls with `_mediator.Send(new {CommandName}Command(...))`
- Keep controller thin — no business logic

### 2. Remove old service

Delete:
- `ECommerce.Application/Services/{Context}Service.cs`
- `ECommerce.Application/Interfaces/I{Context}Service.cs`
- Remove DI registration from Program.cs

### 3. Run full test suite

\`\`\`bash
dotnet test
dotnet run --project ECommerce.API
# Manually test the endpoints that were migrated
\`\`\`

## Acceptance Criteria
- [ ] Old service file deleted
- [ ] Old service interface deleted
- [ ] Controllers dispatch via MediatR
- [ ] All characterization tests pass
- [ ] `dotnet build` clean
- [ ] No references to old service remain (grep to confirm)
\`\`\`
```

---

## Naming Reference for Each Phase

| Phase | Context | Key Learn | Aggregates |
|-------|---------|-----------|------------|
| 1 | Catalog | Aggregates, Value Objects | Product, Category |
| 2 | Identity | Deep Value Objects, auth as app concern | User |
| 3 | Inventory | Domain Events, cross-context by ID | InventoryItem |
| 4 | Shopping | Aggregate boundaries, children | Cart, Wishlist |
| 5 | Promotions | Domain Services, Specification | PromoCode |
| 6 | Reviews | Anti-corruption layer | Review |
| 7 | Ordering | State machines, process managers | Order |
