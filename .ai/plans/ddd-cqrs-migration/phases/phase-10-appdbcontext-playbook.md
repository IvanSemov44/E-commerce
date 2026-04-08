# Phase 10 AppDbContext Playbook

Status: Active guidance for Phase 10
Owner: @ivans
Created: 2026-04-08

## Purpose

This document defines exactly how to handle `AppDbContext` during Phase 10 so persistence ownership becomes explicit and safe.

This is a decision + execution playbook:
- what role `AppDbContext` should have,
- what to remove,
- what is allowed temporarily,
- and how to verify each cutover.

## Why AppDbContext is the critical risk

`AppDbContext` still sits in the center of startup, migration, and transaction orchestration.
If left ambiguous, bounded context ownership remains theoretical instead of enforced.

Current hotspots in code:
- Global `IUnitOfWork` is registered as cross-context orchestration in API composition root.
- `CrossContextMediatRUnitOfWork` starts/commits transactions across multiple context DbContexts in one command path.
- API startup migration path still migrates through `AppDbContext`.
- Some bounded-context infrastructure paths still read shared business tables through `AppDbContext`.

## Final target for AppDbContext (recommended)

Principal-level recommendation for this repository:

1. Keep `AppDbContext` as infrastructure-only in final state.
2. Move all business write ownership to bounded-context DbContexts.
3. Keep only cross-cutting infrastructure tables in `AppDbContext`, for example:
   - integration outbox/inbox,
   - dead-letter and replay tracking,
   - optional saga state and operational metadata.
4. Do not allow bounded-context business repositories/services to query business entities through `AppDbContext`.

This gives a practical migration path without forcing a risky big-bang deletion.

## Decision options and trade-offs

### Option A: AppDbContext infra-only (recommended)

Pros:
- Lower migration risk.
- Keeps existing integration/ops persistence stable.
- Clean boundary rule is still enforceable for business data.

Cons:
- AppDbContext still exists, so guardrails must be strict.

### Option B: Remove AppDbContext entirely

Pros:
- Maximum boundary purity.

Cons:
- Higher complexity now (all integration/ops tables must move immediately).
- Larger rollback surface.

### Option C: Keep hybrid business + infrastructure usage (not acceptable)

Pros:
- Short-term convenience.

Cons:
- Preserves hidden coupling.
- Blocks true bounded context ownership.
- Keeps partial-commit risk in cross-context command flows.

## Non-negotiable boundary rules

1. One business command commits one context-owned write model transaction.
2. Cross-context effects use outbox + integration events + projection handlers.
3. No direct cross-context business table writes.
4. No shared transaction choreography across unrelated DbContexts.
5. If strict consistency is needed, use process state + compensation instead of multi-context ACID assumptions.

## What to do in Phase 10 (execution order)

## Step 1: Lock ownership matrix

Deliverable:
- table ownership matrix: table -> write owner context -> read consumers -> access mode.

Rule:
- each business table has exactly one write owner.

## Step 2: Declare and implement AppDbContext scope

Deliverable:
- ADR-style decision in this phase docs: `AppDbContext = infrastructure-only`.

Implementation rule:
- AppDbContext may keep integration/ops entities only.
- Any business entity access via AppDbContext is migration debt and must have a removal ticket.

## Step 3: Remove cross-context command transaction orchestration

Deliverable:
- replace global cross-context UoW behavior with context-local transaction behavior.

Rule:
- command handlers save only through their owning context/UoW.
- cross-context updates become event-driven.

## Step 4: Cut one dependency chain at a time

Recommended pilot:
- Reviews dependency on Catalog existence checks.

Pattern:
- replace direct shared-context business read with projection/read model contract.

## Step 5: Migration and rollout hardening

Per slice required artifacts:
- migration scripts,
- backfill/verification scripts,
- rollback notes,
- targeted tests and replay/idempotency tests.

## AppDbContext allowed and forbidden usage

Allowed:
- integration messaging persistence,
- operational metadata,
- startup migration/validation for infrastructure-owned tables.

Forbidden:
- querying products/orders/users/cart business entities for bounded-context logic,
- using AppDbContext as fallback repository for domain use cases,
- joining multiple bounded-context business tables in write flows.

## Your real question: should Infrastructure be separate per bounded context?

Short answer: yes, this is usually the right DDD architecture for medium and large systems.

For this repository, having separate projects such as:
- `Catalog.Domain`
- `Catalog.Application`
- `Catalog.Infrastructure`

is a strong and correct direction.

## How principal/senior/DDD authors usually decide

They optimize for business boundaries first, then enforce those boundaries in code and deployment over time.

Typical decision logic:

1. If a bounded context has distinct business rules and data ownership, give it its own Domain and Application layers.
2. If persistence/integration concerns differ, give it its own Infrastructure layer too.
3. Keep shared technical capabilities in common infrastructure only when they are truly cross-cutting (for example outbox plumbing, logging, retries).
4. Do not centralize business persistence in one shared Infrastructure project after bounded contexts are defined.

## What is good architecture vs bad architecture here

Good:
- Catalog has its own Infrastructure project for repositories, EF mappings, read-model adapters, and integration handlers related to Catalog ownership.
- Catalog writes are committed via Catalog-owned persistence boundary.

Bad:
- Catalog business persistence is implemented in a global shared infrastructure path because it is convenient.
- AppDbContext becomes a backdoor that bypasses context ownership.

## Recommended model for this migration

Use per-context Infrastructure projects as the default.

Keep AppDbContext only for cross-cutting infrastructure persistence (integration outbox/inbox, dead-letter, saga/ops metadata) unless there is a temporary migration bridge with explicit removal tracking.

This means your intuition was correct: separate Infrastructure under each bounded context is the principal-level direction.

## Your clarified model: own technical bounded context for shared persistence

Yes, this is a strong principal-level approach when done intentionally.

You can define a dedicated technical context (name examples):
- `Integration`
- `Platform`
- `Operations`

This context owns cross-cutting technical persistence only, such as:
- outbox/inbox,
- dead-letter and replay tracking,
- saga/process state,
- operational dashboard read models and metrics snapshots.

Important: this is a technical/supporting bounded context, not a business domain context.

## Boundary contract for the technical context

1. It must not own core business aggregates (Product, Order, User, Cart, etc.).
2. It can store workflow/process state and messaging reliability data.
3. Business contexts publish events; technical context consumes/coordinates operational concerns.
4. Dashboard data in this context should be read/projection-oriented, never the source of truth for business writes.

## Dashboard nuance (important)

`Dashboard` is safe in the technical context only if it is:
- read-only projection data,
- derived from business-context events,
- not used as a write model for business decisions.

If dashboard tables start holding business-authoritative state, move that state back to the owning business context.

## How projection sync works (concrete example)

Example flow: Reviews checks if a product exists before creating a review.

Request-time read path:

```text
User -> Reviews API endpoint
Reviews handler -> ICatalogService.ProductExistsAsync(productId)
ICatalogService -> Reviews local projection table (ReviewProductProjections)
Reviews handler -> continue or return PRODUCT_NOT_FOUND
```

Sync path that keeps the local projection fresh:

```text
Catalog write model changes product
Catalog publishes ProductProjectionUpdatedIntegrationEvent
Reviews integration-event handler consumes event
Reviews upserts/deletes row in ReviewProductProjections
Future review requests read updated local projection
```

Key point:
- Catalog owns product write truth.
- Reviews owns only its local read projection for Reviews use cases.
- Integration events are the sync mechanism between those boundaries.

## Suggested naming and structure

For this repository, a clear end-state could be:

- Keep business contexts with their own Infra projects (`Catalog.Infrastructure`, `Ordering.Infrastructure`, etc.).
- Move shared technical persistence currently in `AppDbContext` into one explicit technical context/project over time.
- Either:
   - rename/re-scope `AppDbContext` to this technical context role, or
   - replace it with a dedicated technical DbContext and retire `AppDbContext`.

## Honest verdict for current top-level backend projects

This section answers directly for:
- `src/backend/ECommerce.Tests`
- `src/backend/ECommerce.SharedKernel`
- `src/backend/ECommerce.Infrastructure`
- `src/backend/ECommerce.Contracts`
- `src/backend/ECommerce.API`

### 1) ECommerce.SharedKernel

Verdict: keep, but keep it small and strict.

Principal-level rule:
- SharedKernel should contain only truly shared abstractions and primitives (Result, base domain abstractions, tiny interfaces, common constants).
- Do not place business rules from a specific context here.

Status signal:
- Current role is mostly correct.

### 2) ECommerce.Contracts

Verdict: keep, and treat as integration contract boundary.

Principal-level rule:
- Contracts should hold integration events and transport contracts.
- It must not become a dumping ground for business logic or context internals.

Status signal:
- Current direction is good for event-driven boundaries.

### 3) ECommerce.API

Verdict: keep as composition root and transport layer only.

Principal-level rule:
- API wires modules, middleware, DI, auth, hosting, and endpoint transport concerns.
- API should not host business persistence orchestration logic that bypasses bounded contexts.

Status signal:
- Structure is acceptable, but some persistence orchestration still leaks through API-level shared paths during migration.

### 4) ECommerce.Infrastructure

Verdict: keep short-term, but reshape into explicit technical context (or split out one).

Principal-level rule:
- If this project remains, its scope must become technical/cross-cutting only.
- It should not depend on business Domain projects for bounded-context business persistence.

Important current reality (must be fixed in Phase 10):
- `ECommerce.Infrastructure.csproj` currently references business domains (`Promotions.Domain`, `Inventory.Domain`), which indicates hybrid coupling.

Target:
- Move/limit this project to integration and operational persistence (outbox/inbox/dead-letter/saga/ops),
- move business persistence to each context Infrastructure project.

### 5) ECommerce.Tests

Verdict: keep, but reduce monolithic coupling over time.

Principal-level rule:
- Keep a central integration test host project if useful for end-to-end API behavior.
- Prefer context-specific test projects for domain/application/handler tests.

Status signal:
- Current `ECommerce.Tests` references many infrastructure projects, which is acceptable during migration,
- but long-term should be minimized to avoid one giant test dependency graph.

## Recommended end-state map (clear ownership)

1. `ECommerce.SharedKernel`: shared primitives only.
2. `ECommerce.Contracts`: integration contracts/events only.
3. `ECommerce.API`: composition root + HTTP transport only.
4. `ECommerce.Infrastructure`: technical context only (or replaced by explicit `Integration/Platform/Operations` context).
5. Bounded-context `*.Infrastructure` projects: all business persistence ownership.
6. `ECommerce.Tests`: API/integration host tests; business behavior tests live mostly in context test projects.

## Practical rule of thumb for PR decisions

When touching Catalog persistence, ask:

1. Is this change about Catalog-owned business data?
   - If yes, it belongs in Catalog.Infrastructure.
2. Is this change purely cross-cutting technical messaging/operations?
   - If yes, it may belong in shared infrastructure/AppDbContext scope.
3. Is this a temporary bridge?
   - If yes, document removal criteria and target phase in the same PR.

## PR gate checklist (must pass each slice)

1. Build and targeted tests are green.
2. No new business usage of AppDbContext added.
3. Ownership matrix updated if table ownership changed.
4. Migration verification and rollback notes included.
5. Duplicate/replay handling tests included for integration-event paths.

Recommended commands:

```powershell
git status --short
git ls-files | rg "^src/backend/src/"
dotnet build src/backend/ECommerce.sln -v minimal
```

If persistence or migrations changed, also run:

```powershell
cd src/backend
dotnet test
```

## Anti-pattern alarms

Stop and redesign if you see any of these:
- adding another multi-DbContext transaction coordinator,
- calling `SaveChangesAsync` across multiple bounded-context DbContexts in one command flow,
- adding new business entity DbSets into AppDbContext to "speed up" migration,
- cross-context business reads without projection contract.

## Definition of done for AppDbContext in Phase 10

1. AppDbContext role is documented and enforced as infrastructure-only (or fully removed if chosen).
2. No bounded-context business command depends on cross-context transaction choreography.
3. Remaining business reads across contexts are projection/read-model based.
4. Ops runbook exists for replay, dead-letter recovery, and drift verification.

## AI execution layer (prompt-engineering upgrade)

This section makes AI output deterministic and reviewable.

Without this, even good architecture guidance can fail due to inconsistent prompts.

Phase 10 prompt pack files:
- `.ai/plans/ddd-cqrs-migration/prompts/phase-10/README.md`
- `.ai/plans/ddd-cqrs-migration/prompts/phase-10/01-architect-review.prompt.md`
- `.ai/plans/ddd-cqrs-migration/prompts/phase-10/02-ownership-mapper.prompt.md`
- `.ai/plans/ddd-cqrs-migration/prompts/phase-10/03-implementation-slice-planner.prompt.md`
- `.ai/plans/ddd-cqrs-migration/prompts/phase-10/04-pre-merge-gatekeeper.prompt.md`
- `.ai/plans/ddd-cqrs-migration/prompts/phase-10/05-post-change-retrospective.prompt.md`
- `.ai/plans/ddd-cqrs-migration/prompts/phase-10/prompt-run-checklist.md`

### Prompting objectives

1. Produce evidence-backed migration decisions.
2. Prevent hallucinated ownership/dependency claims.
3. Standardize output so PR reviewers can quickly validate correctness.
4. Keep every recommendation tied to a concrete artifact update.

### Required AI output contract

Any AI run for Phase 10 must return these sections in order:

1. `Findings` (ordered by severity)
2. `Evidence` (file paths + exact references searched)
3. `Decision` (what is approved/rejected and why)
4. `Plan Delta` (what changed in docs/artifacts)
5. `Risks` (new or unresolved)
6. `Validation` (what was checked and what remains)

If any section is missing, response is non-compliant.

### Evidence policy (anti-hallucination)

AI must not claim any of the following without evidence references:
- table ownership,
- dependency direction,
- transaction behavior,
- event idempotency behavior,
- migration safety status.

Approved evidence types:
- project references,
- DbContext/entity mapping files,
- migration files,
- DI registrations,
- tests proving behavior.

### Prompt templates (copy and reuse)

## Template A: Architecture reviewer prompt

Use when reviewing DDD correctness and boundary compliance.

```text
Role: Principal DDD reviewer.
Goal: Review Phase 10 artifacts for boundary violations and missing decisions.
Constraints:
- Prioritize critical risks first.
- No generic advice; every finding must include evidence path.
- Flag DDD violations explicitly.
Output format:
1) Findings by severity
2) Evidence
3) Required changes
4) Residual risks
Context files:
- .ai/plans/ddd-cqrs-migration/phases/phase-10-persistence-and-integration-boundaries.md
- .ai/plans/ddd-cqrs-migration/phases/phase-10-appdbcontext-playbook.md
- .ai/plans/ddd-cqrs-migration/phases/phase-10-ownership-matrix.md
- .ai/plans/ddd-cqrs-migration/phases/phase-10-appdbcontext-allowlist.md
- .ai/plans/ddd-cqrs-migration/phases/phase-10-temporary-bridge-register.md
```

## Template B: Ownership matrix completion prompt

Use when filling or validating matrix rows.

```text
Role: Persistence ownership mapper.
Goal: Complete/validate ownership matrix rows using only repository evidence.
Rules:
- One write owner per business table.
- Cross-context reads must be projection/query-api unless marked temporary bridge.
- Any uncertainty must be marked "unknown" with required follow-up scan.
Output:
1) Proposed row updates
2) Evidence per row
3) Conflicts requiring decision
4) Bridge entries to add/update
```

## Template C: Implementation planner prompt

Use before coding a migration slice.

```text
Role: Migration implementation planner.
Goal: Produce PR slice plan for one dependency-chain cutover.
Rules:
- Keep scope to one reversible slice.
- Include rollback and test gates.
- Include docs updates in same PR.
Output:
1) Scope
2) Files to touch
3) Test plan
4) Rollback steps
5) Done criteria
```

## Template D: Pre-merge gatekeeper prompt

Use before approving PR.

```text
Role: Final gatekeeper.
Goal: Check if PR satisfies Phase 10 non-negotiables.
Rules:
- Fail if any DDD blocker is present.
- Fail if bridge added without removal phase and test gate.
- Fail if AppDbContext allowlist is violated.
Output:
1) Pass/Fail
2) Blocking issues
3) Required follow-ups
```

### Quality rubric for AI responses

Score each AI response from 0 to 2 on each axis:

1. `Correctness`: technically accurate and evidence-backed.
2. `Boundary fidelity`: respects DDD context boundaries.
3. `Actionability`: concrete next actions, not abstract advice.
4. `Traceability`: maps recommendations to artifacts and tests.

Interpretation:
- `7-8`: usable for immediate execution.
- `5-6`: usable with reviewer edits.
- `<5`: re-run prompt with tighter constraints.

### Common prompt failure modes and fixes

1. Failure: generic recommendations with no evidence.
   - Fix: require evidence section and fail response if missing.
2. Failure: mixed multiple migration slices in one plan.
   - Fix: enforce single-slice scope in template.
3. Failure: ignores rollback strategy.
   - Fix: make rollback section mandatory.
4. Failure: hidden dependency assumptions.
   - Fix: require explicit dependency listing and rule check.

### Suggested workflow using templates

1. Run Template A to detect risks.
2. Run Template B to update matrix/allowlist/bridge register.
3. Run Template C for one concrete PR slice.
4. Implement.
5. Run Template D before merge.
