# Phase 0: Foundation

**Goal**: Set up the DDD/CQRS infrastructure without changing any business logic. After this phase, the app works exactly as before, but we have the building blocks ready.

**Prerequisites**: Read `theory/01-ddd-fundamentals.md` and `theory/02-cqrs-and-mediatr.md` first.

---

## What We're Building

1. **ECommerce.SharedKernel** — A new class library project with DDD base classes
2. **MediatR setup** — Installed and configured in the API project
3. **Pipeline behaviors** — Cross-cutting concerns (logging, validation, transactions)
4. **Domain event dispatcher** — Infrastructure to dispatch events after saving

Nothing else changes. All existing services, controllers, and entities remain untouched.

---

## Step 1: Create SharedKernel Project

### Theory: Why SharedKernel?

In DDD, a Shared Kernel is code that multiple bounded contexts agree to share. It's the foundation that every context builds on. Without it, every context would need to define its own `Entity`, `AggregateRoot`, and `ValueObject` base classes — duplication that leads to divergence.

Our SharedKernel contains ONLY abstract base classes and interfaces. No business logic. Think of it like .NET's `System` namespace — infrastructure that everything uses.

### What to create

**Project**: `ECommerce.SharedKernel` (Class Library, .NET 10)
**Location**: `src/backend/ECommerce.SharedKernel/`
**References**: None (depends on nothing)

#### Files:

**1. `Domain/Entity.cs`** — Base for all entities
```csharp
namespace ECommerce.SharedKernel.Domain;

public abstract class Entity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime UpdatedAt { get; protected set; }

    protected Entity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    protected Entity(Guid id)
    {
        Id = id;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity? left, Entity? right)
        => Equals(left, right);

    public static bool operator !=(Entity? left, Entity? right)
        => !Equals(left, right);
}
```

**Why**: Entity equality is by identity (Id), not by value. Two `Product` objects with the same Id are the same product, even if Name differs. This is fundamental to DDD.

> **UpdatedAt is managed by infrastructure, not the domain.**
> Domain aggregate methods do NOT set `UpdatedAt` themselves — that is an infrastructure concern.
> In `UnitOfWork.SaveChangesAsync()` (Step 4), we iterate EF Change Tracker entries and write the timestamp:
> ```csharp
> foreach (var entry in ChangeTracker.Entries<Entity>())
> {
>     if (entry.State == EntityState.Modified)
>         entry.Property(nameof(Entity.UpdatedAt)).CurrentValue = DateTime.UtcNow;
> }
> ```
> EF Core can write to `protected set` properties via `entry.Property(...).CurrentValue`, so no change to the Entity class is needed.

**2. `Domain/AggregateRoot.cs`** — Base for aggregate roots
```csharp
namespace ECommerce.SharedKernel.Domain;

public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot() { }
    protected AggregateRoot(Guid id) : base(id) { }

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

**Why**: An AggregateRoot IS an Entity, plus it collects domain events. When the aggregate does something important (order placed, stock reduced), it calls `AddDomainEvent(...)`. After saving, the dispatcher reads `DomainEvents` and publishes them. Then `ClearDomainEvents()` resets for the next operation.

**3. `Domain/ValueObject.cs`** — Base for value objects
```csharp
namespace ECommerce.SharedKernel.Domain;

public abstract class ValueObject
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        var other = (ValueObject)obj;
        return GetEqualityComponents()
            .SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(0, (hash, component) =>
                HashCode.Combine(hash, component));
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right)
        => !(left == right);
}
```

**Why**: Value objects are equal when all their components are equal. `Money(100, "USD") == Money(100, "USD")` is true. Subclasses implement `GetEqualityComponents()` to define which properties matter for equality.

**Example** (preview — we'll build these in Phase 1):
```csharp
public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

**4. `Domain/IDomainEvent.cs`** — Marker interface
```csharp
using MediatR;

namespace ECommerce.SharedKernel.Domain;

public interface IDomainEvent : INotification
{
    DateTime OccurredAt { get; }
}
```

**Why**: Domain events implement `INotification` from MediatR so they can be dispatched through MediatR's notification pipeline. The `OccurredAt` timestamp records when the event happened.

**5. `Domain/DomainEventBase.cs`** — Base record for events
```csharp
namespace ECommerce.SharedKernel.Domain;

public abstract record DomainEventBase : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
```

**Why**: Saves boilerplate. Every event gets a timestamp automatically. Using `record` gives us immutability and value equality for free.

**6. `Domain/DomainException.cs`** — Base for domain violations
```csharp
namespace ECommerce.SharedKernel.Domain;

public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string message) : base(message)
    {
        Code = "DOMAIN_ERROR";
    }

    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}
```

**Why**: When an aggregate's invariant is violated (negative price, invalid status transition), it throws a `DomainException`. This is different from `Result<T>.Fail()` — exceptions are for programming errors or invariant violations that should NEVER happen if the system is used correctly. `Result<T>.Fail()` is for expected business outcomes (item not found, insufficient stock).

**7. `Interfaces/IUnitOfWork.cs`** — Base UoW interface
```csharp
namespace ECommerce.SharedKernel.Interfaces;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    bool HasActiveTransaction { get; }
}
```

**Why**: This is a simplified version of our current IUnitOfWork. Each bounded context can extend this interface if it needs context-specific repositories. The base interface just defines the transaction and save contract.

**8. `Interfaces/ITransactionalCommand.cs`** — Marker for commands that need a DB transaction
```csharp
namespace ECommerce.SharedKernel.Interfaces;

/// <summary>
/// Commands that implement this interface are automatically wrapped in a database
/// transaction by TransactionBehavior in the MediatR pipeline.
/// Queries must NEVER implement this.
/// </summary>
public interface ITransactionalCommand { }
```

**Why**: This marker lives in SharedKernel — NOT in the API project — because bounded context commands (e.g., `Catalog.Application.Commands.CreateProductCommand`) will implement it. If it lived in the API, Application projects would need to reference the API, which inverts the dependency direction and violates Clean Architecture.

### Verification
After this step:
- [ ] `dotnet build` succeeds for SharedKernel project
- [ ] SharedKernel has NO dependencies on other projects
- [ ] SharedKernel has NO NuGet packages except MediatR (for `INotification`)
- [ ] All 8 files created (Entity, AggregateRoot, ValueObject, IDomainEvent, DomainEventBase, DomainException, IUnitOfWork, ITransactionalCommand)
- [ ] All existing projects still compile and work

---

## Step 2: Install and Configure MediatR

### Theory: Why MediatR in the API?

MediatR is the mediator that routes commands/queries to their handlers. We install it in the API project because that's where the DI container lives (`Program.cs`). But the commands, queries, and handlers will live in each bounded context's Application project.

For now, we just install and configure it. No commands or queries yet.

### What to do

**1. Install NuGet package in API project:**
```bash
dotnet add src/backend/ECommerce.API/ECommerce.API.csproj package MediatR
```

**2. Install in SharedKernel (for INotification):**
```bash
dotnet add src/backend/ECommerce.SharedKernel/ECommerce.SharedKernel.csproj package MediatR
```

**3. Register MediatR in `Program.cs`:**
```csharp
// Add after existing service registrations
builder.Services.AddMediatR(cfg =>
{
    // For now, register from the API assembly
    // Later, we'll add each bounded context's Application assembly
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});
```

**4. Add project reference:**
```bash
# API references SharedKernel
dotnet add src/backend/ECommerce.API/ECommerce.API.csproj reference src/backend/ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
```

### Verification
- [ ] `dotnet build` succeeds for entire solution
- [ ] MediatR is resolvable from DI (add a health check or startup validation)
- [ ] No existing functionality is broken

---

## Step 3: Create Pipeline Behaviors

### Theory: What are Pipeline Behaviors?

Think of them as middleware for MediatR requests. Every command/query passes through these behaviors before reaching its handler. They're like ASP.NET middleware, but for the application layer.

```
Request → [Logging] → [Validation] → [Transaction] → Handler → Response
```

This is powerful because:
- You write logging ONCE, not in every handler
- You write validation ONCE, not in every handler
- You write transaction management ONCE, not in every handler

### What to create

**Location**: `src/backend/ECommerce.API/Behaviors/`

**1. `LoggingBehavior.cs`** — Logs every request with timing
```csharp
using MediatR;
using System.Diagnostics;

namespace ECommerce.API.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        _logger.LogInformation(
            "Handled {RequestName} in {ElapsedMs}ms",
            requestName, stopwatch.ElapsedMilliseconds);

        return response;
    }
}
```

**2. `ValidationBehavior.cs`** — Runs FluentValidation before handler
```csharp
using FluentValidation;
using MediatR;
using ECommerce.Core.Results;

namespace ECommerce.API.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
        {
            // Why throw here instead of returning Result.Fail(...)?
            //
            // Commands return Result<T>, but TResponse is a generic type parameter here.
            // We cannot call Result<T>.Fail(...) without knowing T at compile time.
            //
            // The solution: throw ValidationException and let the global exception handler
            // (see error-handling.md) convert it to a 400 response with the same shape
            // as Result.Fail. The contract to the client is identical either way.
            //
            // Two-path model for failures:
            //   Path A (validation)  → ValidationException → GlobalExceptionHandler → 400
            //   Path B (business)    → handler returns Result.Fail(...)  → controller → 422/400
            //
            // Rule of thumb: if the data was malformed before reaching the domain, throw.
            // If the domain rejected a valid request for business reasons, return Result.Fail.
            throw new ValidationException(failures);
        }

        return await next();
    }
}
```

**3. `TransactionBehavior.cs`** — Wraps commands in a transaction
```csharp
using MediatR;
using ECommerce.SharedKernel.Interfaces; // ITransactionalCommand lives here, NOT in API

namespace ECommerce.API.Behaviors;

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        IUnitOfWork unitOfWork,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only wrap transactional commands
        if (request is not ITransactionalCommand)
            return await next();

        // Don't nest transactions
        if (_unitOfWork.HasActiveTransaction)
            return await next();

        var requestName = typeof(TRequest).Name;

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            _logger.LogInformation("Begin transaction for {RequestName}", requestName);

            var response = await next();

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            _logger.LogInformation("Committed transaction for {RequestName}", requestName);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction failed for {RequestName}, rolling back", requestName);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
```

**4. `PerformanceBehavior.cs`** — Warns when a handler takes longer than 500ms
```csharp
using MediatR;
using System.Diagnostics;

namespace ECommerce.API.Behaviors;

public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const int WarningThresholdMs = 500;
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > WarningThresholdMs)
        {
            _logger.LogWarning(
                "Slow handler detected: {RequestName} took {ElapsedMs}ms (threshold: {ThresholdMs}ms). Request: {@Request}",
                typeof(TRequest).Name,
                stopwatch.ElapsedMilliseconds,
                WarningThresholdMs,
                request);
        }

        return response;
    }
}
```

**Why**: You set a 500ms budget. Any handler that exceeds it logs a warning — visible in logs before it becomes a user complaint. The `{@Request}` destructured log gives you the inputs that caused the slow path, which is critical for debugging. Note: LoggingBehavior already records timing per request; PerformanceBehavior is the *alert layer* that fires only when the threshold is crossed.

**5. Register behaviors in `Program.cs`:**
```csharp
builder.Services.AddMediatR(cfg =>
{
    // Phase 0: API assembly only (no bounded contexts yet)
    // As you add bounded contexts, append their Application assemblies here:
    //   cfg.RegisterServicesFromAssembly(typeof(CreateProductCommand).Assembly);  // Phase 1
    //   cfg.RegisterServicesFromAssembly(typeof(RegisterUserCommand).Assembly);   // Phase 2
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);

    // Order matters! Each behavior wraps the next like nested middleware.
    // Outer → Inner: Logging → Performance → Validation → Transaction → Handler
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
});
```

### Verification
- [ ] `dotnet build` succeeds
- [ ] All 4 behaviors exist: LoggingBehavior, PerformanceBehavior, ValidationBehavior, TransactionBehavior
- [ ] All 4 behaviors are registered in `Program.cs` in the correct order
- [ ] `ITransactionalCommand` referenced from `ECommerce.SharedKernel.Interfaces`, not defined inline
- [ ] No existing functionality is broken
- [ ] (Optional) Write a simple test command to verify the pipeline fires in correct order

---

## Step 4: Domain Event Dispatcher

### Theory: How Domain Events Flow

```
1. Handler calls aggregate method → aggregate adds event to internal list
2. Handler calls SaveChangesAsync() → changes persisted
3. AFTER save, dispatcher reads aggregate's DomainEvents
4. Dispatcher publishes each event via MediatR
5. Event handlers react
6. Aggregate's events are cleared
```

The dispatcher hooks into EF Core's `SaveChangesAsync()` override. This guarantees events are only dispatched after the save succeeds.

### What to create

**1. `IDomainEventDispatcher.cs`** in SharedKernel:
```csharp
namespace ECommerce.SharedKernel.Domain;

public interface IDomainEventDispatcher
{
    Task DispatchEventsAsync(CancellationToken cancellationToken = default);
}
```

**2. `DomainEventDispatcher.cs`** in Infrastructure:
```csharp
using MediatR;
using ECommerce.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;

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
        var aggregatesWithEvents = _context.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregatesWithEvents
            .SelectMany(a => a.DomainEvents)
            .ToList();

        // Clear events BEFORE dispatching to prevent re-entry
        foreach (var aggregate in aggregatesWithEvents)
            aggregate.ClearDomainEvents();

        // Dispatch each event
        foreach (var domainEvent in domainEvents)
            await _mediator.Publish(domainEvent, cancellationToken);
    }
}
```

**3. Hook into `SaveChangesAsync()`** — full implementation in `UnitOfWork.cs`:

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    // ① Stamp UpdatedAt on every modified entity (infrastructure concern, not domain)
    foreach (var entry in ChangeTracker.Entries<Entity>())
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

    // ② Persist
    var result = await base.SaveChangesAsync(cancellationToken);

    // ③ Dispatch domain events AFTER successful save
    //    If save failed, events are never dispatched — they were lies
    await _dispatcher.DispatchEventsAsync(cancellationToken);

    return result;
}
```

**Why this order matters**: If you dispatch events before saving, and the save then fails (e.g., DB constraint violation), the event handlers have already run and possibly sent emails or modified other aggregates. That is a consistency disaster. Events after save = events only for things that actually happened.

### Verification
- [ ] `dotnet build` succeeds
- [ ] DomainEventDispatcher is registered in DI
- [ ] UnitOfWork.SaveChangesAsync stamps `UpdatedAt` before persisting
- [ ] Domain events are dispatched after `base.SaveChangesAsync()` succeeds
- [ ] No existing functionality is broken
- [ ] (Optional) Write a test: create an aggregate with an event, save, verify the event was dispatched

---

## Cross-Cutting Note: Caching

CQRS's clean separation makes cache invalidation straightforward — a pattern the old service layer made messy.

**Where caching lives:**
- **Query handlers** are the right place to add a cache read (check cache, miss → hit DB, populate cache).
- **Domain event handlers** are the right place to invalidate cache entries (`ProductUpdatedEvent` → evict the product cache key).

**What this looks like (Phase 1+):**
```csharp
// Query handler — cache read
public async Task<Result<ProductDetailDto>> Handle(GetProductBySlugQuery query, ...)
{
    var cacheKey = $"product:slug:{query.Slug}";
    if (_cache.TryGetValue(cacheKey, out ProductDetailDto? cached))
        return Result<ProductDetailDto>.Ok(cached!);

    var dto = await _readRepo.GetBySlugAsync(query.Slug, cancellationToken);
    _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(5));
    return Result<ProductDetailDto>.Ok(dto);
}

// Domain event handler — cache invalidation
public class InvalidateCacheOnProductUpdatedHandler : INotificationHandler<ProductUpdatedEvent>
{
    public Task Handle(ProductUpdatedEvent notification, CancellationToken ct)
    {
        _cache.Remove($"product:slug:{notification.Slug}");
        _cache.Remove($"product:id:{notification.ProductId}");
        return Task.CompletedTask;
    }
}
```

**Rule**: Never add caching to command handlers. Commands mutate state; caching a mutation is a contradiction.

---

## Phase 0 Checklist

After completing all steps:

- [ ] `ECommerce.SharedKernel` project exists with all 8 files (Entity, AggregateRoot, ValueObject, IDomainEvent, DomainEventBase, DomainException, IUnitOfWork, ITransactionalCommand)
- [ ] MediatR is installed and configured in API
- [ ] Pipeline behaviors exist: Logging, PerformanceBehavior, Validation, Transaction (in that order)
- [ ] `ITransactionalCommand` imported from SharedKernel, not defined in API
- [ ] `UnitOfWork.SaveChangesAsync` stamps `UpdatedAt`, then saves, then dispatches domain events
- [ ] Domain event dispatcher is implemented and hooked into save
- [ ] `dotnet build` succeeds for entire solution
- [ ] `dotnet test` passes (if tests exist)
- [ ] App runs and all existing functionality works
- [ ] No entity or service was modified

**Duration estimate**: This is a setup phase. Focus on understanding each piece, not speed.

**Next**: Phase 1 — Catalog Bounded Context (where we apply everything we've built here)
