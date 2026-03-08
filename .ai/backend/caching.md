# Backend Caching Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Define safe caching patterns with graceful degradation and clear ownership boundaries.

## Core Rules
1. Keep caching concerns in infrastructure/application services, not controllers.
2. Treat cache as an optimization; business flow must still work when cache is unavailable.
3. Use explicit TTLs and operation-specific expiration windows.
4. Log cache failures as warnings and continue where business-safe.
5. Preserve correctness first, then performance.

## Real Code References
- Cache registration: `src/backend/ECommerce.API/Program.cs`
- Distributed idempotency implementation: `src/backend/ECommerce.Application/Services/DistributedIdempotencyStore.cs`
- Idempotency contract: `src/backend/ECommerce.Application/Interfaces/IIdempotencyStore.cs`

## Pattern Notes
- Redis is used when available, with fallback behavior.
- `IDistributedCache` is the abstraction boundary.
- Idempotency uses state envelopes (`in_progress`, `completed`) and replay behavior.

## Common Mistakes
- Making cache availability a hard dependency for core requests.
- Omitting TTL decisions and leaving entries effectively unbounded.
- Storing payloads without clear serialization/versioning strategy.

## Checklist
- [ ] Cache integration is behind an abstraction.
- [ ] Cache miss/failure path still returns correct behavior.
- [ ] TTL is explicit and justified.
- [ ] Logs distinguish normal cache misses from cache failures.
