# DDD & CQRS Rules for This Project

**These rules apply to all code written during and after the migration.**

---

## Aggregate Rules

1. **One repository per aggregate root.** No `IProductImageRepository`. Images are accessed through `IProductRepository` which loads the full aggregate.

2. **External references by ID only.** An Order references a Product by `Guid ProductId`, NOT by a navigation property `Product Product`. Cross-aggregate navigation properties are forbidden.

3. **One transaction per aggregate.** A single `SaveChangesAsync()` call saves one aggregate. If you need to modify two aggregates, use domain events (eventually consistent) or a domain service with explicit transaction.

4. **Aggregate root controls children.** To add an image to a product: `product.AddImage(url, alt)`, NOT `context.ProductImages.Add(new ProductImage(...))`. Children have no public setters accessible from outside the aggregate.

5. **Collections are read-only externally.** Expose `IReadOnlyCollection<T>`, not `List<T>`. Mutation only through aggregate root methods.

   **Child entities are `internal`.** Child entities inside an aggregate are `internal sealed class`, not `public`. This enforces the aggregate boundary at compile time — code outside the `{Context}.Domain` assembly cannot reference child entities directly at all. The aggregate root stays `public`.

   ```csharp
   public sealed class Product : AggregateRoot { }      // ✅ public — repositories return this
   internal sealed class ProductImage : Entity { }       // ✅ internal — unreachable from outside Domain

   public class ProductImage : Entity { }                // ❌ — aggregate boundary is just a convention, not enforced
   ```

   The Infrastructure project needs access for EF Core entity configuration. Grant it via `[assembly: InternalsVisibleTo("ECommerce.{Context}.Infrastructure")]` in the Domain project's `Properties/AssemblyInfo.cs`.

6. **Aggregate root raises events.** Only the root calls `AddDomainEvent(...)`. Child entities signal the root, and the root decides whether to raise an event.

7. **No dependencies in aggregates.** Aggregates don't inject services. They take all needed data as method parameters. If an aggregate needs external data to make a decision, the handler fetches it and passes it in.

## Value Object Rules

8. **Immutable.** No setters. Once created, a value object never changes. To "change" it, create a new one.

   **Sealed.** All value objects and aggregate roots must be `sealed`. Value objects: subclassing breaks value-equality semantics (`record` equality and `GetEqualityComponents()` both assume a fixed set of properties). Aggregate roots: subclassing could bypass the invariants the root enforces.

   ```csharp
   public sealed record ProductName { ... }          // ✅
   public sealed class Money : ValueObject { ... }   // ✅
   public sealed class Product : AggregateRoot { }   // ✅

   public record ProductName { ... }                 // ❌ — subclassable, equality unsafe
   public class Product : AggregateRoot { }          // ❌ — invariants can be bypassed
   ```

   Base classes (`AggregateRoot`, `Entity`, `ValueObject`) in SharedKernel must NOT be sealed — they exist to be inherited.

9. **Validated at creation. Return `Result<T>`, never throw.** A `Money` value object with amount -50 should be impossible to create. Validate in the factory method and return `Result<T>.Fail(ContextErrors.X)` for invalid input. Domain validation is expected flow — not exceptional. Exceptions are reserved for infrastructure failures (DB down, null ref bugs).

   ```csharp
   // ✅ correct
   public static Result<Money> Create(decimal amount, string currency)
   {
       if (amount < 0) return Result<Money>.Fail(CatalogErrors.MoneyNegative);
       return Result<Money>.Ok(new Money(amount, currency));
   }

   // ❌ wrong — throws for expected business conditions
   public static Money Create(decimal amount, string currency)
   {
       if (amount < 0) throw new CatalogDomainException("MONEY_NEGATIVE", "...");
       return new Money(amount, currency);
   }
   ```

   All error definitions for a context live in a single `{Context}Errors.cs` static class with `readonly DomainError` fields. No magic strings at throw sites.

10. **Equality by value.** Two `Money(100, "USD")` are equal. Override `Equals` and `GetHashCode` (or use records).

11. **Self-contained logic.** Value objects can have behavior: `Money.Add(Money other)`, `Email.Normalize()`, `DateRange.Contains(DateTime date)`.

12. **Replace primitives.** Wherever a primitive carries business meaning, use a value object: `decimal price` → `Money price`, `string email` → `Email email`, `int quantity` → `Quantity quantity`.

## Domain Event Rules

13. **Past tense naming.** Events are facts: `OrderPlaced`, `StockReduced`, `ProductCreated`. Not `PlaceOrder`, `ReduceStock`.

14. **Immutable data.** Events carry data, not behavior. Use records: `public record OrderPlacedEvent(Guid OrderId, decimal TotalAmount) : IDomainEvent;`

15. **Dispatch after save.** Events are collected during aggregate mutation and dispatched AFTER `SaveChangesAsync()` succeeds. Never dispatch before persistence — if the save fails, the event was a lie.

16. **Handlers are independent.** Each event handler does one thing. If `OrderPlacedEvent` needs to reduce stock AND send email, that's two separate handlers, not one handler doing both.

17. **Handlers don't throw to callers.** If an event handler fails, it logs and retries or compensates. It doesn't bubble up to crash the command handler that raised the event.

## CQRS Rules

18. **Commands change state.** Named as imperative verbs: `CreateProduct`, `UpdateOrderStatus`, `CancelOrder`.

19. **Queries read state.** Named as questions: `GetProductById`, `GetProducts`, `SearchProducts`. Queries NEVER modify data.

20. **One handler per request.** Each `IRequest<T>` has exactly one `IRequestHandler<TRequest, TResponse>`.

21. **Commands go through aggregates.** Command handlers load aggregates, call domain methods, and save. They don't write raw SQL or manipulate EF entities directly.

22. **Queries bypass aggregates.** Query handlers can use optimized read paths: projections, raw SQL, read-only DbContext. They return DTOs, never entities.

23. **Commands return Result<T>.** Use the existing `Result<T>` pattern for all command results. Queries also return `Result<T>` for consistency.

24. **Commands carry data, not services.** A command is a data record. It doesn't inject dependencies. The handler has dependencies.

## Repository Rules (DDD-style)

25. **Interface in Domain, implementation in Infrastructure.** `IProductRepository` lives in `Catalog.Domain`. `ProductRepository` lives in `Catalog.Infrastructure`.

26. **Repository returns aggregates.** `GetByIdAsync` returns the full aggregate (root + children). Not a partial entity.

27. **No IQueryable leaking.** Repositories don't expose `IQueryable<T>`. They expose specific methods: `GetByIdAsync`, `GetBySlugAsync`. The query shape is defined by the repository interface, not composed by callers.

28. **Read repositories can be separate.** For queries, you can have a separate `IProductReadRepository` that returns DTOs directly. This keeps the write repository focused on aggregates.

## Service Rules

29. **Application services (handlers) orchestrate.** They load aggregates, call domain methods, save, and dispatch events. They don't contain business logic.

30. **Domain services contain cross-aggregate logic.** `DiscountCalculator` that needs PromoCode rules + Order subtotal = domain service. If logic fits in one aggregate, it belongs there.

31. **No domain logic in handlers.** If you see `if (order.Status == Shipped) return Fail(...)` in a handler, that belongs in `Order.Cancel()` instead.

## Authorization Rules

36. **Authorization lives in handlers, not the domain.** The domain aggregate doesn't know who is calling it. Authorization is an application concern. Command and query handlers are responsible for checking if the caller has permission before delegating to the domain.

37. **Use ICurrentUserService.** The interface lives in **SharedKernel** (so handlers in any context can reference it without depending on the API project). The implementation lives in **API** (reads JWT claims from `IHttpContextAccessor`):
    ```csharp
    // SharedKernel/Interfaces/ICurrentUserService.cs
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
        bool IsAuthenticated { get; }
        bool IsInRole(string role);
    }
    // ECommerce.API/Services/CurrentUserService.cs  ← implementation only
    ```
    Handlers inject `ICurrentUserService` and check permissions early — before loading aggregates.

38. **Unauthorized = fail fast.** Return `Result.Fail(ErrorCodes.Unauthorized)` (or throw `UnauthorizedException`) at the top of the handler before any domain work. Do not load the aggregate first and then check.

39. **Permission rules must match the old service.** When migrating a context, verify the new handler enforces the same authorization rules as the old service. This is part of the Definition of Done (see README.md).

## Caching Rules

40. **Cache at the query handler level.** Query handlers are the correct place to check and populate a cache. Commands must never cache their results.

41. **Invalidate cache via domain event handlers.** When an aggregate changes state, a domain event handler evicts the relevant cache keys. This keeps the cache consistent without coupling commands to cache logic.
    ```
    ProductUpdatedEvent → InvalidateCacheOnProductUpdatedHandler → remove product cache keys
    ```

42. **Cache read models, not aggregates.** Only DTOs (read models) go into cache. Never serialize and cache a domain aggregate — aggregates carry domain logic and mutable state, not data snapshots.

## Testing Rules

32. **Domain layer: unit tests.** Test aggregate methods, value object validation, domain events raised. No mocking needed — pure C#.

33. **Handlers: unit tests with mocked repos.** Test that the handler calls the right aggregate methods and saves.

34. **Integration tests: full flow.** Command through MediatR → handler → real DB. Verify the aggregate was saved correctly.

35. **No testing infrastructure directly.** Don't test EF configurations or repository LINQ. That's covered by integration tests.

## EF Core Persistence Rules

43. **Record value objects use value converters, not owned entities.** A single-property record (Email, Slug, Sku, ProductName) maps to one column via `HasConversion(to, from)`. Never use `OwnsOne()` for single-property records — it creates ugly shadow columns and fights with private constructors.

   **Critical corollary — unique indexes on value objects:** With `HasConversion`, the property is a first-class column and `HasIndex` works normally:
   ```csharp
   builder.Property(p => p.Slug)
       .HasConversion(s => s.Value, v => Slug.Create(v).GetDataOrThrow())
       .HasColumnName("Slug");
   builder.HasIndex(p => p.Slug).IsUnique();  // ✅ works
   ```
   With `OwnsOne`, EF's shadow property name is `Slug_Value` (not `Slug`), so `EF.Property<string>(p, "Slug")` in `HasIndex` silently references a non-existent property — the unique constraint is never applied. This is a silent data integrity bug.

44. **Multi-property value objects use `OwnsOne()` with explicit column names.** For `class : ValueObject` with multiple properties (Money, PersonName, DateRange), use `builder.OwnsOne(...)` and always call `.HasColumnName(...)` on each property. Never rely on EF's auto-generated `PropertyName_FieldName` format.

45. **Enums are always stored as strings.** Every enum property must have `.HasConversion<string>()` in its EF configuration. Never store enums as integers (data corruption risk on reorder).

   **Common mistake — shadow property with wrong type:**
   ```csharp
   builder.Property<int>("Status").HasColumnName("Status");   // ❌ int, silent data corruption
   builder.Property(p => p.Status).HasConversion<string>();  // ✅ correct
   ```
   Using `Property<int>("Status")` creates a shadow property that ignores the actual enum type and stores integers.

46. **Aggregate private parameterless constructors must be empty.** EF Core calls the private `()` constructor to materialize from the database. It must do nothing — no validation, no events, no defaults. All of that lives in the factory method (`Create`/`Register`/`Place`).

47. **Back collection properties with private fields.** Aggregate collections must use a `private readonly List<T> _field` backing field and expose `IReadOnlyCollection<T>` via property. EF Core accesses the backing field during materialization.

   **`UpdateAsync` is not needed for EF-tracked aggregates.** When a handler calls `GetByIdAsync`, EF Core tracks the returned aggregate. Domain method calls mutate the tracked entity. `_uow.SaveChangesAsync()` persists the diff automatically. Calling `_repository.UpdateAsync(aggregate)` before `SaveChangesAsync` is redundant — it causes an extra DB round-trip and risks missing fields in a manual mapping. Repository `UpdateAsync` should only exist if using a disconnected pattern (e.g. loading from cache). In the standard handler pattern:
   ```csharp
   // ✅ correct — load, mutate via domain method, save
   var product = await _products.GetByIdAsync(id, ct);
   product.UpdatePrice(newPrice);          // EF tracks the change
   await _uow.SaveChangesAsync(ct);        // persists the diff

   // ❌ wrong — extra round-trip, manual mapping, misses fields
   var product = await _products.GetByIdAsync(id, ct);
   product.UpdatePrice(newPrice);
   await _products.UpdateAsync(product, ct);  // loads from DB again and copies fields
   await _uow.SaveChangesAsync(ct);
   ```

## Validation Rules

Three distinct layers. Each has one job. Nothing bleeds into the next layer.

```
HTTP request
    ↓
[FromBody] binding           ← null / wrong types → 400 (ModelState, framework)
    ↓
ValidationBehavior           ← IValidator<TCommand> → empty strings, ranges, required fields → 400
    ↓
Handler existence checks     ← does the referenced entity exist? → 404
    ↓
Aggregate domain methods     ← business invariants, state machine rules → Result.Fail → 422 / 409
```

48. **Every write command has a FluentValidation validator.** Create `{CommandName}CommandValidator : AbstractValidator<{CommandName}Command>` in the same folder as the command. Validate input shape: not-empty strings, positive numbers, valid GUIDs. If there is nothing to validate (e.g. a delete command with only an `Id`), skip the validator — but every command that accepts user-typed strings or numbers needs one.

49. **`ValidationBehavior` returns `Result.Fail`, it does not throw.** Throwing `ValidationException` from the pipeline requires the global exception middleware to handle that specific exception type. If the middleware does not list `ValidationException` explicitly, it falls through to 500. The safe approach that works with the Result pattern used everywhere else:
   ```csharp
   if (failures.Any())
   {
       var first = failures.First();
       return CreateFailureResult<TResponse>(new DomainError("VALIDATION_FAILED", first.ErrorMessage));
   }
   ```
   This keeps the entire request pipeline exception-free for expected failures. Exceptions are reserved for infrastructure failures (DB down, null ref bugs).

50. **Domain aggregates enforce business invariants, not input shape.**

51. **Two error files per context — know which layer owns which error.**
   - `{Context}Errors.cs` in the **Domain** project — value object validation failures, invariant violations. These fire when the data itself is structurally invalid: `NameEmpty`, `PriceMustBePositive`, `SlugInvalid`.
   - `{Context}ApplicationErrors.cs` in the **Application** project — cross-entity checks in handlers: `ProductNotFound`, `SkuAlreadyExists`, `CategoryHasProducts`. These fire when the operation cannot proceed because of how entities relate to each other.
   - Rule of thumb: if the error comes from inside `ValueObject.Create()` or `Aggregate.DomainMethod()` → Domain errors. If the error comes from a repository check or existence check in the handler → Application errors.

52. **Authorization: controller guards authentication, handler guards ownership.**
   - `[Authorize(Roles = "Admin")]` on the controller action → handles 401 (not logged in) and 403 (wrong role) at the HTTP layer. Use this for all-or-nothing role access (only admins can create products).
   - `ICurrentUserService` in the handler → use for ownership checks where a user can only act on their OWN data (a user can only update their OWN profile, not another user's). Check at the top of the handler before loading anything.
   - Do NOT duplicate: if the controller already guards a role, the handler does NOT also check that role. The controller is the outer guard for HTTP; the handler only adds checks for cross-user scenarios.

53. **`record` vs `class : ValueObject` — when to use which.**
   - `sealed record` → single-property value objects (`Email`, `Slug`, `Sku`, `ProductName`). .NET record equality compares all properties by value automatically. One property = no ambiguity.
   - `sealed class : ValueObject` → multi-property value objects (`Money`, `PersonName`, `DateRange`). Override `GetEqualityComponents()` to define what makes two instances equal.
   - Never use `class : ValueObject` for single-property VOs — the `record` is simpler and has less ceremony.

54. **Domain event handlers run inside the save transaction — keep them fast and non-throwing.**
   Domain events are dispatched by `IDomainEventDispatcher` inside `SaveChangesAsync`, after the DB write but within the same transaction scope. If an event handler throws, the transaction rolls back and the aggregate save is undone. Therefore:
   - Event handlers that do infrastructure work (email, external HTTP, cache) must swallow exceptions and log — never re-throw (Rule 17 applies here for this exact reason).
   - Event handlers that do secondary DB writes (e.g. create an audit log entry) are fine to throw — the rollback is correct behavior if the secondary write fails.
   - Heavy work (sending emails, calling Stripe) belongs in a background job triggered by the event, not executed synchronously inside the handler.

55. **InMemory database does not enforce unique constraints.** `HasIndex(...).IsUnique()` is silently ignored by the EF Core InMemory provider. Characterization tests that use InMemory will not catch duplicate slug/sku/email violations at the DB level. The handler's existence checks (e.g. `SlugExistsAsync`) must be present and tested explicitly. For full constraint validation, use Testcontainers with a real Postgres instance in integration tests. If `Product.Create("")` returns `Result.Fail(CatalogErrors.NameEmpty)`, that is correct — the domain enforces its own invariants. But the FluentValidation validator should have already caught the empty string before the handler ever called `Product.Create`. An empty string reaching the aggregate is a gap in the validation layer, not proof that the domain is doing the right job. Both layers must exist and each must do its own job.

## Naming Conventions

| Thing | Convention | Example |
|-------|-----------|---------|
| Command | `{Verb}{Noun}Command` | `CreateProductCommand` |
| Query | `Get{Noun}Query` / `Get{Noun}ByXQuery` | `GetProductBySlugQuery` |
| Command Handler | `{Command}Handler` | `CreateProductCommandHandler` |
| Query Handler | `{Query}Handler` | `GetProductBySlugQueryHandler` |
| Domain Event | `{Noun}{PastVerb}Event` | `ProductCreatedEvent` |
| Event Handler | `{WhatItDoes}On{Event}Handler` | `SendEmailOnOrderPlacedHandler` |
| Value Object | Business name | `Money`, `Email`, `Slug` |
| Domain Errors (values) | `{Context}Errors` | `CatalogErrors` |
| Aggregate Root | Business name | `Product`, `Order`, `Cart` |

## File Organization (per bounded context)

```
ECommerce.{Context}.Domain/
├── Aggregates/
│   └── {AggregateRoot}/
│       ├── {AggregateRoot}.cs        (the root entity)
│       ├── {ChildEntity}.cs          (child entities)
│       └── Events/
│           └── {Event}.cs
├── ValueObjects/
│   └── {ValueObject}.cs
├── Errors/
│   └── {Context}Errors.cs            (static class with DomainError fields — one per validation rule)
├── Interfaces/
│   └── I{AggregateRoot}Repository.cs
└── Services/
    └── {DomainService}.cs

ECommerce.{Context}.Application/
├── Commands/
│   └── {CommandName}/
│       ├── {CommandName}Command.cs
│       ├── {CommandName}CommandHandler.cs
│       └── {CommandName}CommandValidator.cs
├── Queries/
│   └── {QueryName}/
│       ├── {QueryName}Query.cs
│       └── {QueryName}QueryHandler.cs
├── DTOs/
│   └── {Dto}.cs
└── EventHandlers/
    └── {HandlerName}.cs

ECommerce.{Context}.Infrastructure/
├── Repositories/
│   └── {AggregateRoot}Repository.cs
├── Configurations/
│   └── {Entity}Configuration.cs
└── ReadModels/
    └── {ReadRepository}.cs
```
