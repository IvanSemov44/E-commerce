# Cross-Cutting Persistence and Reliability Checklist

Use this checklist after the DbContext readiness checklist to catch silent correctness risks.

## 1. Global query filters are preserved and scoped correctly
- Verify soft-delete, tenancy, and other global filters are applied exactly once.
- Must match:
1. Filters present where expected.
2. No duplicate filtering in both context and query layer.
3. Filter behavior verified with focused tests.

## 2. Interceptors and model conventions are correctly scoped
- Verify EF interceptors/conventions (audit fields, timestamps, concurrency, domain event hooks) are neither missing nor duplicated.
- Must match:
1. Context-required interceptors are registered.
2. No interceptor executes twice for same operation.
3. Audit/concurrency behavior validated in tests.

## 3. Domain event publishing is bound to this context commit boundary
- Verify domain/integration event dispatch occurs from this context's UnitOfWork commit, not another context's pipeline.
- Must match:
1. Event dispatch trigger is in correct commit path.
2. Transactional boundary between state change and event enqueue is consistent.
3. Integration tests confirm commit-to-event behavior.

## 4. Outbox table ownership and write contract are explicit
- Verify where outbox physically lives and who is allowed to write to it.
- Must match:
1. Outbox ownership is documented as context-owned or platform-owned allowlist.
2. Writes from this context are explicitly allowed by contract.
3. No hidden cross-context business-table writes are introduced.

## 5. Health checks cover this context connection
- Verify per-context connection has explicit health check registration.
- Must match:
1. Context DB health appears in readiness checks.
2. Broken context connection fails readiness as expected.
3. Alerting path exists for sustained failures.

## 6. Schema isolation strategy is explicit
- Verify dedicated database or dedicated schema prefix strategy is enforced.
- Must match:
1. Naming collisions are prevented by design.
2. Shared-database deployments use explicit schema boundaries.
3. Migration output respects chosen isolation strategy.

## Minimum Definition of Ready
Cross-cutting reliability is ready only when all 6 checks pass with concrete evidence.
