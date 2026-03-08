# Future Improvements Orchestration

Updated: 2026-03-08
Owner: @ivans
Status: Active planning source of truth

## Purpose
Single, style-consistent orchestration file for future improvements extracted from legacy markdown.
Only items that are relevant to the current codebase and architecture are included.

## How This File Is Used
- `Now`: actionable in upcoming sprints with clear code anchors.
- `Next`: valuable, but dependent on `Now` outcomes.
- `Later`: strategic options, not immediate standards.
- `Discarded for now`: ideas from legacy docs that do not match current structure or maturity.

## Selection Rules
1. Must align with current Clean Architecture and `.ai` rules.
2. Must have concrete code anchors in this repo.
3. Must be testable with explicit done criteria.
4. Avoid speculative platform work without present need.

## Now (High ROI, low ambiguity)

### 1) Idempotency and duplicate-request hardening
- Why: Orders/Payments already use idempotency headers and store abstractions; standards should formalize behavior and verification.
- Code anchors:
  - `src/backend/ECommerce.API/Controllers/OrdersController.cs`
  - `src/backend/ECommerce.API/Controllers/PaymentsController.cs`
  - `src/backend/ECommerce.Application/Interfaces/IIdempotencyStore.cs`
  - `src/backend/ECommerce.Application/Services/DistributedIdempotencyStore.cs`
- Effort: 0.5-1 day (docs + targeted tests/verification).
- Done when:
  - `.ai/backend/idempotency.md` exists and is linked.
  - Integration tests explicitly cover replay/in-progress/failure abandon paths.

### 2) Concurrency policy normalization
- Why: `[Timestamp]` and `DbUpdateConcurrencyException` handling exist, but guidance is spread.
- Code anchors:
  - `src/backend/ECommerce.Core/Entities/Order.cs`
  - `src/backend/ECommerce.Core/Entities/Cart.cs`
  - `src/backend/ECommerce.Core/Entities/Product.cs`
  - `src/backend/ECommerce.API/Middleware/GlobalExceptionMiddleware.cs`
- Effort: 0.5 day.
- Done when:
  - `.ai/backend/concurrency.md` exists and is linked.
  - Entity coverage matrix for concurrency tokens is documented.

### 3) API contract completeness for controllers
- Why: response shape/status-code standards are core and still a frequent review issue.
- Code anchors:
  - `src/backend/ECommerce.API/Controllers/`
  - `.ai/backend/api-contracts.md`
- Effort: 1-2 days incremental across features.
- Done when:
  - Controller checklist is explicit and used in PR review.
  - Missing `ProducesResponseType` debt trends toward zero.

### 4) Query performance guardrails (N+1 and projection discipline)
- Why: high risk regression area; rules should be explicit for service/repository authors.
- Code anchors:
  - `src/backend/ECommerce.Application/Services/CategoryService.cs`
  - `src/backend/ECommerce.Application/Services/ProductService.cs`
  - `src/backend/ECommerce.Infrastructure/Repositories/Repository.cs`
- Effort: 0.5 day docs + ongoing audits.
- Done when:
  - `.ai/backend/query-patterns.md` includes explicit N+1 checks and projection rules.

## Next (Valuable, medium complexity)

### 5) Query-result caching policy expansion
- Why: Redis/distributed cache is present; add a consistent cacheability matrix by endpoint type.
- Code anchors:
  - `src/backend/ECommerce.API/Program.cs`
  - `src/backend/ECommerce.Application/Services/DistributedIdempotencyStore.cs`
  - `.ai/backend/caching.md`
- Effort: 1-2 days with benchmarks.
- Done when:
  - Candidate endpoints and TTLs documented.
  - Invalidation rules mapped to write paths.

### 6) External-call resilience verification (retry/circuit-breaker)
- Why: high impact on reliability for payment-like integrations.
- Code anchors:
  - `src/backend/ECommerce.API/Extensions/`
  - `src/backend/ECommerce.Infrastructure/Resilience/`
- Effort: 1 day verify + harden.
- Done when:
  - Policy matrix documented per external dependency.
  - Failure-mode tests added for transient faults.

### 7) Frontend cross-app consolidation (storefront/admin)
- Why: reduces duplication and drift in API and state patterns.
- Code anchors:
  - `src/frontend/storefront/src/`
  - `src/frontend/admin/src/`
- Effort: 2-4 days staged.
- Done when:
  - Shared conventions list is explicit and adopted in both apps.

## Later (Strategic, optional until triggered)

### 8) Background jobs platform decision
- Legacy suggestions referenced Hangfire; keep as optional until workload justifies.
- Trigger: repeated fragile async tasks or operational visibility gaps.
- Output: ADR + implementation plan (not active default today).

### 9) API versioning program
- Legacy suggestions referenced Asp.Versioning.
- Trigger: real contract-breaking evolution pressure.
- Output: migration strategy doc + deprecation policy.

### 10) Saga/distributed transaction orchestration
- Trigger: true cross-service transaction requirements.
- Output: bounded-context workflow ADR + compensating-action playbook.

## Discarded For Now (from legacy docs)
- Large copy-paste templates as standards (too noisy and drifts fast).
- Broad "enterprise framework" additions without immediate pain signal.
- Prescriptive rules for components not currently in production architecture.

## Execution Rhythm
1. Pick one `Now` item per sprint.
2. Ship docs + code/test delta together.
3. Record completion date and move to changelog/PR notes.
4. Re-score `Next` monthly based on incident/review data.

## Ownership and Review
- Primary owner: `@ivans`
- Review cadence: every 2 weeks during active refactor window.
- Success metric: fewer architecture/style violations and less PR review churn on repeated issues.
