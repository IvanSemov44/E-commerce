# CQRS and MediatR

**Read this after `01-ddd-fundamentals.md`.**

---

## What is CQRS?

CQRS stands for **Command Query Responsibility Segregation**. It's one idea:

> **Separate the code that reads data from the code that writes data.**

That's it. Everything else is implementation detail.

### Why separate reads from writes?

Think about our current `ProductService`:

```csharp
public class ProductService
{
    // WRITE: validates, loads aggregate, mutates, saves
    public async Task<Result<ProductDetailDto>> CreateProductAsync(CreateProductDto dto, ...) { ... }
    public async Task<Result<ProductDetailDto>> UpdateProductAsync(Guid id, UpdateProductDto dto, ...) { ... }

    // READ: just fetches and maps to DTO
    public async Task<Result<PaginatedResult<ProductDto>>> GetProductsAsync(ProductQueryParameters query, ...) { ... }
    public async Task<Result<ProductDetailDto>> GetProductBySlugAsync(string slug, ...) { ... }
}
```

The reads and writes have completely different needs:

| Concern | Write (Command) | Read (Query) |
|---------|-----------------|--------------|
| Needs domain model? | Yes — must enforce invariants | No — just maps to DTOs |
| Needs validation? | Yes — business rules | No — just parameters |
| Needs transactions? | Often yes | No |
| Performance focus | Correctness first | Speed first |
| Complexity | High (business logic) | Low (just SQL/LINQ) |

Mixing them in one service means:
- Read methods load full aggregates just to map to DTOs (wasteful)
- Write methods get polluted with read-specific query optimization
- You can't optimize one without affecting the other

CQRS says: **split them into separate objects**.

---

## Commands and Queries

### Command
A **command** is a request to change state. It represents an intention: "I want to create a product."

```csharp
public record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    string Sku,
    Guid CategoryId
) : IRequest<Result<ProductDetailDto>>;
```

**Rules for commands:**
- Named as imperative verb: `Create`, `Update`, `Delete`, `Cancel`, `Place`
- Always changes state (or fails trying)
- Handled by exactly ONE handler
- Returns `Result<T>` (success/failure), not the full read model

### Query
A **query** is a request to read data. It asks a question: "What products match this filter?"

```csharp
public record GetProductsQuery(
    int Page,
    int PageSize,
    string? Search,
    Guid? CategoryId
) : IRequest<Result<PaginatedResult<ProductDto>>>;
```

**Rules for queries:**
- Named as question: `Get`, `Find`, `List`, `Search`
- NEVER changes state (no side effects)
- Returns data (DTOs, not entities)
- Can bypass the domain model entirely (query the DB directly)

### Handler
Each command or query has exactly one handler:

```csharp
public class CreateProductHandler : IRequestHandler<CreateProductCommand, Result<ProductDetailDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<ProductDetailDto>> Handle(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Create domain object (aggregate enforces its invariants)
        var product = Product.Create(
            ProductName.Create(command.Name),
            command.Description,
            Money.Create(command.Price, "USD"),
            Sku.Create(command.Sku),
            command.CategoryId
        );

        // 2. Persist
        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 3. Return result
        return Result<ProductDetailDto>.Ok(product.ToDetailDto());
    }
}

public class GetProductsHandler : IRequestHandler<GetProductsQuery, Result<PaginatedResult<ProductDto>>>
{
    private readonly IProductReadRepository _readRepo; // Optimized for reads

    public async Task<Result<PaginatedResult<ProductDto>>> Handle(
        GetProductsQuery query,
        CancellationToken cancellationToken)
    {
        // No aggregate loading — query DTOs directly
        return Result<PaginatedResult<ProductDto>>.Ok(
            await _readRepo.GetProductsAsync(query, cancellationToken)
        );
    }
}
```

---

## MediatR

MediatR is the .NET library that implements the **mediator pattern**. Instead of a controller calling a service directly, it sends a request to MediatR, which routes it to the right handler.

### Before (current code)
```csharp
// Controller depends directly on service
public class ProductsController
{
    private readonly IProductService _productService;

    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        var result = await _productService.CreateProductAsync(dto, ...);
        // ...
    }
}
```

### After (with MediatR)
```csharp
// Controller depends only on MediatR
public class ProductsController
{
    private readonly IMediator _mediator;

    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        var result = await _mediator.Send(new CreateProductCommand(
            dto.Name, dto.Description, dto.Price, dto.Sku, dto.CategoryId
        ));
        // ...
    }
}
```

**Why is this better?**
1. Controller doesn't know which handler processes the request
2. You can add cross-cutting concerns (logging, validation, transactions) via **pipeline behaviors** without changing any handler
3. Each handler is a single class with a single responsibility — easy to test

### Pipeline Behaviors

MediatR lets you insert middleware into the request pipeline. Every request passes through these behaviors before reaching its handler.

```
Request → [Logging] → [Validation] → [Transaction] → Handler → Response
```

```csharp
// Runs BEFORE every command handler
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // 1. Validate the request
        var failures = _validators
            .Select(v => v.Validate(request))
            .SelectMany(result => result.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
            return /* validation failure result */;

        // 2. Pass to next behavior or handler
        return await next();
    }
}
```

**Pipeline behaviors we'll create:**
1. **LoggingBehavior** — logs every request/response with timing
2. **ValidationBehavior** — runs FluentValidation on commands before handler executes
3. **TransactionBehavior** — wraps command handlers in a database transaction
4. **PerformanceBehavior** — warns on slow handlers (> 500ms)

### MediatR Notifications (for Domain Events)

MediatR also supports **notifications** — messages that can have multiple handlers. This is perfect for domain events.

```csharp
// Domain event (published after aggregate is saved)
public record OrderPlacedEvent(Guid OrderId, decimal TotalAmount) : INotification;

// Multiple handlers react independently
public class SendOrderConfirmationHandler : INotificationHandler<OrderPlacedEvent>
{
    public async Task Handle(OrderPlacedEvent notification, CancellationToken ct)
    {
        await _emailService.SendOrderConfirmationAsync(notification.OrderId, ct);
    }
}

public class ReduceInventoryHandler : INotificationHandler<OrderPlacedEvent>
{
    public async Task Handle(OrderPlacedEvent notification, CancellationToken ct)
    {
        // Load order, reduce stock for each item
    }
}
```

---

## What About Event Sourcing?

You asked about this. Here's the explanation:

### Standard approach (what we're doing)
Store the **current state** of entities in the database.

```
Products table:
| Id  | Name        | Price | Stock |
| abc | Widget      | 19.99 | 42    |  ← current state
```

When you update: `UPDATE Products SET Price = 24.99 WHERE Id = 'abc'`
The old price (19.99) is gone forever.

### Event Sourcing (what we're NOT doing)
Store the **events** that happened, not the current state.

```
ProductEvents table:
| Id  | ProductId | EventType      | Data                    |
| 1   | abc       | ProductCreated | { Name: "Widget", ... } |
| 2   | abc       | PriceChanged   | { From: 19.99, To: 24.99 } |
| 3   | abc       | StockReduced   | { Quantity: 3 }         |
```

To get current state: replay all events from the beginning.

**Pros of Event Sourcing:**
- Complete audit trail (you know everything that ever happened)
- Time-travel (reconstruct state at any past point)
- Can build new read models from old events

**Cons of Event Sourcing:**
- Massive complexity increase
- Eventual consistency everywhere
- Hard to query (need separate read models)
- Steep learning curve
- Hard to change event schemas

**Why we're NOT using it:** It's overkill for our e-commerce app. We get 90% of the benefits from DDD + CQRS + Domain Events without the complexity. We can always add it later for specific contexts (like Ordering) if we need a full audit trail.

---

## CQRS Depth Levels

There are levels of CQRS. We're doing **Level 2**:

### Level 1: Separate handlers (basic)
- Commands and Queries are separate objects with separate handlers
- Both use the same database and same models
- **This is the minimum CQRS**

### Level 2: Separate models (what we're doing)
- Commands go through rich domain aggregates
- Queries use optimized read models (DTOs directly from DB)
- Same database, different access patterns
- **Best balance of benefit vs complexity**

### Level 3: Separate databases
- Write side has a normalized database
- Read side has denormalized read-optimized database
- Events sync write DB → read DB
- **Only for extreme scale — NOT for us**

---

## How CQRS + DDD Work Together

```
                    ┌─────────────┐
                    │  Controller  │
                    └──────┬──────┘
                           │ Send via MediatR
                ┌──────────┴──────────┐
                │                     │
         ┌──────┴──────┐    ┌────────┴────────┐
         │   Command    │    │     Query        │
         │   Handler    │    │     Handler      │
         └──────┬──────┘    └────────┬────────┘
                │                     │
         Load Aggregate          Query DB directly
                │                     │
         ┌──────┴──────┐    ┌────────┴────────┐
         │   Domain     │    │   Read Model     │
         │   Model      │    │   (DTO)          │
         │ (Aggregate)  │    │                  │
         └──────┬──────┘    └─────────────────┘
                │
         Call domain methods
         Raise domain events
                │
         ┌──────┴──────┐
         │   Save via   │
         │  Repository  │
         └──────┬──────┘
                │
         ┌──────┴──────┐
         │  Dispatch    │
         │  Domain      │
         │  Events      │
         └─────────────┘
```

**Command flow:**
1. Controller sends `CreateProductCommand` via MediatR
2. Pipeline: Validation → Transaction → Handler
3. Handler creates `Product` aggregate (domain rules enforced)
4. Handler saves via repository
5. Domain events dispatched (e.g., `ProductCreatedEvent`)
6. Event handlers react (update search index, etc.)

**Query flow:**
1. Controller sends `GetProductsQuery` via MediatR
2. Pipeline: Logging → Handler
3. Handler queries database directly (no aggregate loading)
4. Returns DTOs optimized for the UI
5. No domain events, no transactions needed

---

## Packages We'll Need

```xml
<!-- In API project -->
<PackageReference Include="MediatR" Version="12.*" />
<PackageReference Include="MediatR.Extensions.Microsoft.DependencyInjection" Version="11.*" />

<!-- Or the newer combined package -->
<PackageReference Include="MediatR" Version="12.*" />
```

MediatR 12+ includes DI registration. We register it in `Program.cs`:

```csharp
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(CreateProductCommand).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
});
```
