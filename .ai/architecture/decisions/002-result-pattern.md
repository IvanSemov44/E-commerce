# ADR-002: Use Result<T> for Expected Business Outcomes

Date: 2026-03-08
Status: accepted
Deciders: @ivans

## Context
Service methods frequently return expected failures (not found, invalid state, duplicates). Throwing exceptions for these paths creates noisy control flow.

## Decision
Use `Result<T>` for expected business outcomes and reserve exceptions for unexpected failures.

## Consequences
- Positive: explicit success/failure handling with stable error codes.
- Positive: controllers can map outcomes consistently.
- Negative: service method signatures become more explicit/verbose.

## References
- `src/backend/ECommerce.Core/Results/Result.cs`
- `src/backend/ECommerce.Core/Constants/ErrorCodes.cs`
- `src/backend/ECommerce.Application/Services/ProductService.cs`
