# Service and Command Handler Rules

## Scope

Applies to: MediatR command handlers, domain services, application services, UnitOfWork commit boundaries.

---

## Hard Rules

1. **Handlers orchestrate — they do not contain business logic.** The handler loads aggregates, calls domain methods, saves, and dispatches events. Business decisions (state transitions, invariant checks) live in the aggregate.

   ```csharp
   // Correct — handler orchestrates, aggregate decides
   var order = await _orders.GetByIdAsync(command.OrderId, ct);
   var result = order.Cancel(command.Reason);
   if (result.IsFailure) return result;
   await _uow.SaveChangesAsync(ct);

   // Wrong — business logic in handler
   if (order.Status == OrderStatus.Shipped)
       return Result.Fail(OrderErrors.CannotCancelShipped);
   order.Status = OrderStatus.Cancelled;
   ```

2. **Commands return `Result<T>`.** All command handlers return `Result<T>`. Use `Result.Fail(ErrorCodes.X)` for expected business failures. Exceptions are reserved for infrastructure failures.

3. **Services inject `IUnitOfWork`, not individual repositories.** The handler accesses repositories through the UoW or through injected repository interfaces. It does not construct repository instances.

4. **No domain logic in handlers.** Condition checks that belong in an aggregate method must not be duplicated in the handler.

5. **Domain services contain cross-aggregate logic only.** If logic fits in one aggregate, it belongs there. A domain service (e.g. `DiscountCalculator`) is justified only when logic spans multiple aggregates and cannot belong to either root.

6. **Aggregates do not inject services.** Aggregates take all needed data as method parameters. If the aggregate needs external data to make a decision, the handler fetches it and passes it in.

7. **Authorization is checked before any domain work.** Return `Result.Fail(ErrorCodes.Unauthorized)` at the top of the handler before loading aggregates or calling domain methods.

8. **`CancellationToken` is threaded through all async methods.** Every async method signature includes `CancellationToken cancellationToken = default`. All async calls inside pass it.

---

## Allowed Patterns

- Handler fetches data from multiple repositories to pass as parameters into an aggregate method.
- Domain service injected into a handler to perform cross-aggregate coordination.
- `ICurrentUserService` injected into handlers for ownership checks.

---

## Forbidden Patterns

- Business invariant checks duplicated in both handler and aggregate.
- Direct EF context access from a handler (handlers go through repositories).
- `throw` for expected business failures — use `Result.Fail`.
- Loading an aggregate after the authorization check fails.
- Calling `_repository.UpdateAsync` on an EF-tracked aggregate before `SaveChangesAsync`.

---

## Required Tests

- Unit test per handler: confirms the correct aggregate method is called, the correct repository is called, and the result maps correctly.
- Integration test: command through MediatR → handler → real DB → verify aggregate was saved correctly.
- Authorization test: confirms unauthorized caller gets `Result.Fail` before any domain work runs.

---

## Required Evidence (before merge)

- No business logic present in handler that belongs in the aggregate.
- Authorization check present and tested.
- `CancellationToken` threaded through all async calls.

---

## Definition of Done

All hard rules pass. Required tests are green. Required evidence is attached in the PR.
