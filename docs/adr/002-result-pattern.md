# ADR 002 — Result<T> Pattern instead of Exceptions

**Status:** Accepted
**Date:** 2025
**Deciders:** Core team

---

## Context

Services need to communicate two categories of failure back to controllers:

1. **Business failures** — "email already exists", "promo code expired", "item out of stock". These are *expected* outcomes, not bugs.
2. **Unexpected failures** — null ref, DB timeout, unhandled edge case. These are bugs or infrastructure problems.

Using exceptions for both creates noise: business failures fill up error logs, and controllers end up with try/catch blocks containing business logic.

## Decision

Services return `Result<T>` for all expected outcomes. Exceptions are reserved for truly unexpected failures only.

```csharp
// Service returns Result
public async Task<Result<OrderDto>> CreateOrder(CreateOrderDto dto, ...)
{
    if (stockInsufficient)
        return Result<OrderDto>.Failure(ErrorCodes.InsufficientStock);

    // ...
    return Result<OrderDto>.Success(orderDto);
}

// Controller maps Result to HTTP
var result = await _orderService.CreateOrder(dto);
return result.IsSuccess
    ? CreatedAtAction(..., result.Value)
    : result.ToActionResult();   // maps error codes to 4xx
```

`Result<T>` has three states:
- `Success(T value)` — happy path
- `Failure(string errorCode)` — known business failure
- `ValidationFailure(Dictionary<string,string[]> errors)` — field-level validation errors

All error codes are constants in `ECommerce.Core/Constants/ErrorCodes.cs`.

## Alternatives considered

| Option | Why rejected |
|--------|-------------|
| Throw custom exceptions (e.g. `OutOfStockException`) | Exceptions are for exceptional cases; out-of-stock is a normal business outcome. Also slower (stack unwinding) and adds try/catch clutter to controllers |
| Return `null` or `bool` | Loses the error reason; caller must guess what went wrong |
| MediatR + FluentResults | Adds MediatR dependency for no benefit in a service-injection architecture |

## Consequences

**Good:**
- Business failures don't pollute error logs
- Controllers stay thin — no try/catch for business logic
- Error reasons are always explicit via `ErrorCodes` constants
- Easy to unit test: assert `result.IsSuccess`, `result.ErrorCode`

**Watch out for:**
- Don't return `Result.Failure` for infrastructure errors (DB down, null ref) — let those throw and be caught by `GlobalExceptionMiddleware`
- Don't invent new error codes inline — always add to `ErrorCodes.cs` so they're discoverable
- `Result<Unit>` for void operations (e.g. `DeleteProduct`)
