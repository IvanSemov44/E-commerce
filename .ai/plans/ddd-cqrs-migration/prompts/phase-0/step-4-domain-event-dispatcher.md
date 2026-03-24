# Phase 0, Step 4: Domain Event Dispatcher

**Prerequisite**: Steps 1–3 must be complete. SharedKernel exists, MediatR is wired, behaviors are registered.

---

## Prompt for Descriptor (Kilo Code)

```
# Role: Descriptor for DDD/CQRS Migration — Phase 0, Step 4

You are the Descriptor. Steps 1–3 are done. Now guide me through the final Phase 0 step:
creating the domain event dispatcher and hooking it into SaveChangesAsync.

## Read These Files First

1. `.ai/plans/ddd-cqrs-migration/phases/phase-0-foundation.md` — Step 4 section
2. `.ai/plans/ddd-cqrs-migration/theory/01-ddd-fundamentals.md` — domain events section
3. `.ai/plans/ddd-cqrs-migration/rules.md` — domain event rules 13–17

## Your Task

Guide me through Step 4: the domain event dispatcher and the SaveChangesAsync override
that (a) stamps UpdatedAt and (b) dispatches events after persistence.

After this step, Phase 0 is complete. I should be able to confirm:
- The app runs exactly as before
- The infrastructure is ready for Phase 1 (Catalog bounded context)

## Constraints
- Events must be dispatched AFTER save succeeds, never before
- UpdatedAt must be stamped by UnitOfWork, not by domain aggregate methods
- IDomainEventDispatcher interface lives in SharedKernel, implementation in Infrastructure
```

---

## Prompt for Programmer

```
# Task: Domain Event Dispatcher — Phase 0 Step 4

## Context

Aggregates collect domain events internally (via AddDomainEvent in AggregateRoot).
We need infrastructure to:
1. Stamp UpdatedAt before saving
2. After a successful save, read those events and publish them via MediatR
3. Clear the events so they aren't dispatched again

This is the final piece of Phase 0 infrastructure.

## Instructions

### File 1: Check IDomainEventDispatcher in SharedKernel

This interface was created in Step 1. Confirm it exists at:
`src/backend/ECommerce.SharedKernel/Domain/IDomainEventDispatcher.cs`

Content should be:
```csharp
namespace ECommerce.SharedKernel.Domain;

public interface IDomainEventDispatcher
{
    Task DispatchEventsAsync(CancellationToken cancellationToken = default);
}
```

If it's missing, create it now.

### File 2: `DomainEventDispatcher.cs` — in Infrastructure project

Location: `src/backend/ECommerce.Infrastructure/DomainEventDispatcher.cs`

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Infrastructure;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly AppDbContext _context;
    private readonly IMediator _mediator;

    public DomainEventDispatcher(AppDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task DispatchEventsAsync(CancellationToken cancellationToken = default)
    {
        // Collect all aggregates that raised events during this unit of work
        var aggregatesWithEvents = _context.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        if (!aggregatesWithEvents.Any())
            return;

        var domainEvents = aggregatesWithEvents
            .SelectMany(a => a.DomainEvents)
            .ToList();

        // Clear BEFORE dispatching — prevents re-entrant dispatching
        // if an event handler triggers another save
        foreach (var aggregate in aggregatesWithEvents)
            aggregate.ClearDomainEvents();

        // Dispatch each event independently via MediatR notifications
        foreach (var domainEvent in domainEvents)
            await _mediator.Publish(domainEvent, cancellationToken);
    }
}
```

### File 3: Update UnitOfWork.SaveChangesAsync

Find the existing UnitOfWork class in Infrastructure. Update its SaveChangesAsync to:
1. Inject IDomainEventDispatcher via constructor
2. Stamp UpdatedAt before saving
3. Dispatch events after saving

```csharp
// Add to constructor
private readonly IDomainEventDispatcher _dispatcher;

// Constructor parameter addition:
public UnitOfWork(AppDbContext context, IDomainEventDispatcher dispatcher)
{
    _context = context;
    _dispatcher = dispatcher;
}

// Override SaveChangesAsync:
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    // ① Stamp timestamps — infrastructure concern, not domain concern
    foreach (var entry in _context.ChangeTracker.Entries<Entity>())
    {
        switch (entry.State)
        {
            case EntityState.Added:
                entry.Property(nameof(Entity.CreatedAt)).CurrentValue = DateTime.UtcNow;
                entry.Property(nameof(Entity.UpdatedAt)).CurrentValue = DateTime.UtcNow;
                break;
            case EntityState.Modified:
                entry.Property(nameof(Entity.UpdatedAt)).CurrentValue = DateTime.UtcNow;
                break;
        }
    }

    // ② Persist to database
    var result = await _context.SaveChangesAsync(cancellationToken);

    // ③ Dispatch domain events — ONLY after successful save
    //    If save threw, we never reach here — events that didn't persist are never dispatched
    await _dispatcher.DispatchEventsAsync(cancellationToken);

    return result;
}
```

Note: If UnitOfWork already has a SaveChangesAsync, modify it rather than adding a new override.

### Register DomainEventDispatcher in Program.cs

```csharp
builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
```

### Build and verify

```bash
dotnet build
dotnet run --project ECommerce.API/ECommerce.API.csproj
```

Test a few existing endpoints to confirm nothing broke.

## Rules to Follow (from rules.md)
- Rule 15: Dispatch after save — never before
- Rule 16: Each event handler does one thing
- Rule 17: Event handler failures don't crash the caller

## Acceptance Criteria
- [ ] `IDomainEventDispatcher` exists in SharedKernel (created in Step 1 or now)
- [ ] `DomainEventDispatcher.cs` exists in Infrastructure
- [ ] `DomainEventDispatcher` registered as `IDomainEventDispatcher` in DI
- [ ] `UnitOfWork.SaveChangesAsync` stamps UpdatedAt, then saves, then dispatches events
- [ ] Events are cleared BEFORE dispatching (prevents re-entry)
- [ ] `dotnet build` succeeds
- [ ] All existing endpoints return the same responses as before
- [ ] `dotnet test` passes (if tests exist)
```

---

## Phase 0 Complete

After this step, confirm the full Phase 0 checklist from `phases/phase-0-foundation.md`:

- [ ] SharedKernel exists with all 8 files
- [ ] MediatR installed and configured
- [ ] 4 pipeline behaviors registered in correct order
- [ ] ITransactionalCommand in SharedKernel
- [ ] UnitOfWork stamps UpdatedAt + dispatches events after save
- [ ] `dotnet build` clean
- [ ] `dotnet test` passes
- [ ] App runs, all existing functionality intact
- [ ] No entity or service was modified

**Next**: `phases/phase-1-catalog.md` (once it exists) — Catalog Bounded Context.
Use `prompts/phase-N-template.md` to understand the structure of Phase 1 prompts.

---

## Tester Verification Checklist

1. `IDomainEventDispatcher` is in `ECommerce.SharedKernel.Domain` namespace
2. `DomainEventDispatcher` is in `ECommerce.Infrastructure` namespace
3. `UnitOfWork.SaveChangesAsync` order: timestamps THEN save THEN dispatch
4. `ClearDomainEvents()` is called BEFORE `Publish()`, not after
5. `DomainEventDispatcher` registered as scoped (not singleton)
6. Solution builds with no warnings related to new files
7. All existing integration tests pass
