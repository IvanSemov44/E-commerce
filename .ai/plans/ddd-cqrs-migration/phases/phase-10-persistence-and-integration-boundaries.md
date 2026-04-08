# Phase 10: Persistence Ownership and Integration Boundary Hardening

Status: Proposed (not started)
Owner: @ivans
Created: 2026-04-08

## Why this phase exists

Phase 9 reorganized API/application code and removed legacy service/repository surface area.
However, persistence and integration boundaries are still hybrid:

- Shared AppDbContext is still active in startup, migrations, and some infrastructure paths.
- Several bounded-context DbContexts still map to legacy public tables.
- Cross-context transaction orchestration exists in one process (multi-DbContext unit of work).
- Event-driven projections exist, but boundary rules are not fully hardened.

This phase closes that gap.

## Decision: Phase 9 first, then Phase 10

Short answer:
- Finish Phase 9 first.
- Start Phase 10 immediately after Phase 9 exit criteria are green.

Reasoning:
- Phase 9 is currently in progress and is mostly code-ownership cleanup.
- Doing Phase 10 before finishing Phase 9 will create parallel moving targets and increase rollback risk.
- Phase 10 is persistence architecture work and should begin with stable controller/handler ownership from Phase 9.

## Senior recommendation on sequence

1. Complete all remaining Phase 9 steps and merge.
2. Run a Phase 9 closure checkpoint (build, tests, legacy usage scan).
3. Branch into Phase 10 with strict PR slicing (small, reversible moves).

Do not combine Phase 9 deletions and Phase 10 schema/data moves in one PR.

## Phase 9 exit criteria (must pass before Phase 10)

1. Build and tests are green on main.
2. Remaining legacy dependencies are intentionally documented (none accidental).
3. Controller ownership and feature folder structure are stable.
4. No open Step 2/3/4/7 blockers in phase-9 plan.
5. Characterization tests for migrated endpoints are passing.

## Phase 10 outcomes (target state)

1. Clear table ownership per bounded context.
2. Bounded-context DbContexts own writes for their context tables.
3. Cross-context data flow is event/projection based, not FK/navigation based.
4. Shared AppDbContext is either:
   - removed for business entities, or
   - narrowed to infrastructure-only concerns (explicitly documented).
5. Startup migration and schema validation reflect context ownership model.

## Scope in this phase

### In scope

- Persistence ownership matrix (table -> owner context -> consumers).
- Schema strategy finalization (public vs per-context schemas).
- Removal of accidental shared AppDbContext usage from bounded-context business code.
- Refactor of cross-context transaction behavior to avoid pseudo-distributed ACID assumptions.
- Outbox/inbox and projection hardening for cross-context synchronization.
- Migration scripts and backfill approach per context.
- Operational guardrails (idempotency, replay safety, drift detection).

### Out of scope

- New product features.
- UI redesign unrelated to migration.
- Full microservice decomposition into separate deployables.

## Key architectural rule for this phase

One business command should commit one context-owned write model transaction.
Cross-context effects must happen via integration events and projection handlers.

If a flow needs strict cross-context consistency, redesign with:
- local transaction + outbox
- compensating action
- explicit business process state

## Workstreams

## Workstream A: Ownership Baseline

Deliverables:
- table ownership matrix
- dependency scan report
- approved target schema map

Checks:
- each table has exactly one write owner
- each cross-context consumer is read-only via projection/read model

## Workstream B: AppDbContext Reduction

Deliverables:
- decision record: keep infra-only or remove fully
- migration of remaining bounded-context business usages off shared context

Checks:
- no bounded-context business repository depends on shared AppDbContext
- any remaining AppDbContext usage is explicitly infrastructure-scoped

## Workstream C: Transaction Boundary Hardening

Deliverables:
- revised unit-of-work behavior aligned to local context transaction
- removal of fragile multi-DbContext begin/commit choreography for business commands

Checks:
- no partial-commit risk from sequential commit across unrelated context transactions
- failure semantics tested

## Workstream D: Event and Projection Reliability

Deliverables:
- idempotent projection handlers
- replay-safe upsert/delete semantics
- dead-letter and retry policy review

Checks:
- replay tests pass
- duplicate event handling tests pass
- projection lag and failure visibility in logs/metrics

## Workstream E: Schema and Data Migration

Deliverables:
- context-by-context migration scripts
- data backfill/verification scripts
- rollback plan per cutover step

Checks:
- row-count and checksum verification for moved data
- uniqueness and key constraints validated post-cutover
- zero-downtime or controlled downtime procedure documented

## PR slicing strategy

PR 1: Architecture decision and ownership matrix only
PR 2: Remove first bounded-context shared-context dependency chain
PR 3: Transaction behavior hardening
PR 4: Projection idempotency and replay safety
PR 5+: Context-specific schema/data cutover in small batches

Each PR must include:
- tests
- migration verification notes
- rollback notes

## Definition of done for Phase 10

1. Shared-context ambiguity is eliminated (clear final role or fully removed).
2. Context write ownership is explicit and enforced.
3. Cross-context consistency relies on events/outbox, not direct relational coupling.
4. Migration and schema validator behavior matches final ownership model.
5. Runbooks exist for replay, dead-letter recovery, and drift checks.

## Risks and mitigations

Risk: Data drift during backfill/cutover
Mitigation: dual-read verification window and deterministic reconciliation script

Risk: Hidden dependencies on legacy tables
Mitigation: static code scan + SQL dependency scan before each cutover PR

Risk: Event duplication/out-of-order behavior
Mitigation: idempotency key enforcement and monotonic projection update rules

Risk: Partial commits from cross-context transaction assumptions
Mitigation: local transaction boundary only + outbox + compensation

## Immediate next actions

1. Finish remaining Phase 9 steps and mark closure status.
2. Create Phase 10 kickoff branch and publish ownership matrix.
3. Select one pilot context for first persistence cutover (recommended: Catalog or Shopping).
4. Implement and validate one full end-to-end migration slice before scaling to other contexts.

## Notes for maintainers

If this phase changes established architecture conventions, update these docs in the same PR:
- .ai/README.md
- .ai/workflows/database-migrations.md
- .ai/plans/ddd-cqrs-migration/README.md
- .ai/plans/ddd-cqrs-migration/target-structure.md
