# Phase 0, Step 1: Create SharedKernel Project

**This is the prompt to give to the Descriptor (Kilo Code), who will then guide the Programmer.**

---

## Prompt for Descriptor (Kilo Code)

Copy this into a new Kilo Code chat:

```
# Role: Descriptor for DDD/CQRS Migration

You are the Descriptor in a DDD/CQRS migration of an ASP.NET Core 10 e-commerce backend. Your job is to read the migration plan and guide the Programmer step by step.

## Context

We are migrating the backend from a service-oriented architecture to DDD + CQRS with MediatR. This is Phase 0: Foundation — setting up infrastructure with no business logic changes.

## Read These Files First

Before generating any prompts, read these files carefully:

1. `.ai/plans/ddd-cqrs-migration/README.md` — master migration plan
2. `.ai/plans/ddd-cqrs-migration/rules.md` — DDD/CQRS rules for this project
3. `.ai/plans/ddd-cqrs-migration/theory/01-ddd-fundamentals.md` — DDD concepts
4. `.ai/plans/ddd-cqrs-migration/theory/02-cqrs-and-mediatr.md` — CQRS concepts
5. `.ai/plans/ddd-cqrs-migration/target-structure.md` — target folder structure
6. `.ai/plans/ddd-cqrs-migration/phases/phase-0-foundation.md` — Phase 0 detailed plan
7. `.ai/plans/ddd-cqrs-migration/prompts/roles.md` — your role definition

## Your Task

Guide me through Phase 0 step by step. For each step:

1. **Explain** what we're building and why (2-3 sentences, teach me the concept)
2. **List** the exact files to create with full paths
3. **Describe** the code structure expected
4. **Reference** which rules from rules.md apply
5. **Give me** the implementation prompt I should send to the Programmer AI
6. **Define** verification criteria (how to know the step is done correctly)

Start with Step 1: Create the ECommerce.SharedKernel project.

After I confirm Step 1 is done, move to Step 2 (MediatR setup), then Step 3 (Pipeline behaviors), then Step 4 (Domain event dispatcher).

## Important Constraints

- The app must compile and run after EVERY step
- No existing code is modified in Phase 0 (we're only ADDING new infrastructure)
- The SharedKernel project depends on NOTHING except MediatR (for INotification)
- Domain classes (Entity, AggregateRoot, ValueObject) are pure C# — no EF Core references
- Follow the code examples in phase-0-foundation.md but adapt them if the current codebase needs slight adjustments
- Check the existing BaseEntity class in ECommerce.Core/Entities/BaseEntity.cs to ensure compatibility

## Current Codebase State

- Backend: ASP.NET Core 10, C# 13
- Database: PostgreSQL via EF Core
- Existing structure: ECommerce.API, ECommerce.Application, ECommerce.Core, ECommerce.Infrastructure
- Existing patterns: Result<T>, IUnitOfWork, BaseEntity, FluentValidation
- Branch: main (clean, no uncommitted changes)
```

---

## Prompt for Programmer (to be sent by Descriptor for Step 1)

If you're going directly to the Programmer AI, use this prompt:

```
# Task: Create ECommerce.SharedKernel Project

## What and Why

We are beginning a DDD/CQRS migration. The first step is creating a SharedKernel project — a class library that contains the base building blocks (Entity, AggregateRoot, ValueObject, etc.) that ALL bounded contexts will share. Think of it as the foundation layer.

This project must have ZERO business logic. It only has abstract base classes and interfaces.

## Instructions

### 1. Create the project

```bash
cd src/backend
dotnet new classlib -n ECommerce.SharedKernel -f net10.0
dotnet sln ../../ECommerce.sln add ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
```

Delete the auto-generated `Class1.cs`.

### 2. Add MediatR package (needed for INotification)

```bash
dotnet add ECommerce.SharedKernel/ECommerce.SharedKernel.csproj package MediatR
```

### 3. Create the following files

**File: `ECommerce.SharedKernel/Domain/Entity.cs`**
- Abstract class `Entity`
- Properties: `Guid Id` (protected set), `DateTime CreatedAt` (protected set), `DateTime UpdatedAt` (protected set)
- Parameterless constructor: auto-generates Id with `Guid.NewGuid()`, sets CreatedAt/UpdatedAt to `DateTime.UtcNow`
- Constructor with `Guid id` parameter for reconstitution from DB
- Override `Equals`: two entities are equal if same type and same Id
- Override `GetHashCode`: based on Id
- Override `==` and `!=` operators

**File: `ECommerce.SharedKernel/Domain/AggregateRoot.cs`**
- Abstract class inheriting from `Entity`
- Private `List<IDomainEvent> _domainEvents`
- Public `IReadOnlyCollection<IDomainEvent> DomainEvents` (read-only view)
- Protected method `AddDomainEvent(IDomainEvent domainEvent)`
- Public method `ClearDomainEvents()`
- Both parameterless and `Guid id` constructors calling base

**File: `ECommerce.SharedKernel/Domain/ValueObject.cs`**
- Abstract class `ValueObject`
- Abstract method `IEnumerable<object?> GetEqualityComponents()`
- Override `Equals`: compares all equality components with `SequenceEqual`
- Override `GetHashCode`: combines all components with `HashCode.Combine`
- Override `==` and `!=` operators

**File: `ECommerce.SharedKernel/Domain/IDomainEvent.cs`**
- Interface `IDomainEvent` extending `MediatR.INotification`
- Property: `DateTime OccurredAt { get; }`

**File: `ECommerce.SharedKernel/Domain/DomainEventBase.cs`**
- Abstract record `DomainEventBase` implementing `IDomainEvent`
- Property: `DateTime OccurredAt` initialized to `DateTime.UtcNow`

**File: `ECommerce.SharedKernel/Domain/DomainException.cs`**
- Class `DomainException` extending `Exception`
- Property: `string Code { get; }`
- Constructor: `(string message)` with Code = "DOMAIN_ERROR"
- Constructor: `(string code, string message)` with custom code

**File: `ECommerce.SharedKernel/Domain/IDomainEventDispatcher.cs`**
- Interface `IDomainEventDispatcher`
- Method: `Task DispatchEventsAsync(CancellationToken cancellationToken = default)`

**File: `ECommerce.SharedKernel/Interfaces/IUnitOfWork.cs`**
- Interface `IUnitOfWork` extending `IDisposable`
- Methods:
  - `Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)`
  - `Task BeginTransactionAsync(CancellationToken cancellationToken = default)`
  - `Task CommitTransactionAsync(CancellationToken cancellationToken = default)`
  - `Task RollbackTransactionAsync(CancellationToken cancellationToken = default)`
  - `bool HasActiveTransaction { get; }`

### 4. Verify

```bash
cd src/backend
dotnet build ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
dotnet build  # Verify entire solution still builds
```

## Rules to Follow (from rules.md)
- Rule 8: Value objects are immutable, no setters
- Rule 9: Validated at creation
- Rule 10: Equality by value
- SharedKernel has NO dependencies except MediatR
- All classes use proper namespace: `ECommerce.SharedKernel.Domain` or `ECommerce.SharedKernel.Interfaces`

## Acceptance Criteria
- [ ] ECommerce.SharedKernel project exists at `src/backend/ECommerce.SharedKernel/`
- [ ] Project is added to the solution
- [ ] Project has ONLY MediatR as a NuGet dependency
- [ ] All 8 files created with correct content
- [ ] `dotnet build` succeeds for SharedKernel and entire solution
- [ ] No existing code was modified
- [ ] Class1.cs is deleted
```

---

## After Step 1 Completes

Come back to the Descriptor to get the prompt for Step 2 (MediatR setup in API project), or use the Phase 0 plan directly.

The Tester should verify:
1. SharedKernel project references only MediatR
2. No `using Microsoft.EntityFrameworkCore` in any SharedKernel file
3. Entity has proper equality by Id
4. ValueObject has proper equality by components
5. AggregateRoot has the domain events collection
6. `dotnet build` succeeds
7. Existing tests still pass
