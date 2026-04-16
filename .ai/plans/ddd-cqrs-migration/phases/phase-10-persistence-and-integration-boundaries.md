# Phase 10: Persistence Ownership and Integration Boundary Hardening

Status: Completed
Owner: @ivans
Created: 2026-04-08
Last updated: 2026-04-08

## Completion summary

Phase 10 is complete.

Key outcomes delivered:

- Temporary bridges BR-001 through BR-005 are closed in the bridge register.
- Shared AppDbContext business/runtime bridge usage was removed from active migration targets.
- Cross-context transaction choreography was removed from business command flow.
- Integration reliability persistence was isolated into dedicated integration context wiring.
- Startup and data-protection composition were hardened to reduce API-level shared-context coupling.

Validation evidence (across slices):

- `dotnet build src/backend/ECommerce.API/ECommerce.API.csproj` succeeded on final slices.
- Targeted payment/integration characterization gates used during cutovers remained green.
- Bridge register contains per-bridge closure notes and test gates.

## Kickoff baseline completed

The following Phase 10 prerequisites are already complete:

- Phase 9 is merged into `main` and marked completed.
- Phase 10 branch is created and pushed: `feature/phase-10-persistence-and-integration-boundaries`.
- Accidental duplicate nested backend test files under `src/backend/src/backend/...` were removed.
- Repository hygiene/build validation passed after cleanup (`dotnet build src/backend/ECommerce.sln`).

This document now tracks active Phase 10 execution from that clean baseline.

## AppDbContext decision playbook

Use this as the canonical execution guide for `AppDbContext` scope, boundaries, and cutover rules:

- `.ai/plans/ddd-cqrs-migration/phases/phase-10-appdbcontext-playbook.md`

PR 1 working artifacts:
- `.ai/plans/ddd-cqrs-migration/phases/phase-10-ownership-matrix.md`
- `.ai/plans/ddd-cqrs-migration/phases/phase-10-appdbcontext-allowlist.md`
- `.ai/plans/ddd-cqrs-migration/phases/phase-10-temporary-bridge-register.md`

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

Current state:
- This gate is satisfied. Phase 9 closure is complete and Phase 10 has started.

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

## Mandatory architecture decisions (missing before, now required)

Before PR 2, lock these decisions in writing (ADR-style note in this phase folder):

1. Technical context strategy:
   - keep and re-scope `ECommerce.Infrastructure` as technical context, or
   - introduce explicit `Integration`/`Platform`/`Operations` context and retire current shared role.
2. AppDbContext end-state:
   - infra-only with explicit entity allowlist, or
   - fully removed.
3. Dashboard persistence contract:
   - projection/read-model only (never business write source of truth).

No implementation PR proceeds without these decisions.

## Workstream A: Ownership Baseline

Deliverables:
- table ownership matrix
- dependency scan report
- approved target schema map

Required ownership matrix columns (minimum):
- table/view name
- write owner context
- allowed readers
- reader mechanism (projection/query API)
- direct cross-context access allowed? (yes/no)
- migration bridge? (yes/no + removal phase)
- rollback impact

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

Hard guardrail:
- maintain an explicit AppDbContext entity allowlist (integration/ops entities only in final state).

## Workstream C: Transaction Boundary Hardening

Deliverables:
- revised unit-of-work behavior aligned to local context transaction
- removal of fragile multi-DbContext begin/commit choreography for business commands

Checks:
- no partial-commit risk from sequential commit across unrelated context transactions
- failure semantics tested

Hard guardrail:
- forbid any new multi-DbContext UoW coordinator implementation.

## Workstream D: Event and Projection Reliability

Deliverables:
- idempotent projection handlers
- replay-safe upsert/delete semantics
- dead-letter and retry policy review

Checks:
- replay tests pass
- duplicate event handling tests pass
- projection lag and failure visibility in logs/metrics

Operational SLOs (define and track):
- projection freshness target (for example P95 lag threshold per projection)
- dead-letter growth threshold and alerting rule
- replay completion time target

## Workstream E: Schema and Data Migration

Deliverables:
- context-by-context migration scripts
- data backfill/verification scripts
- rollback plan per cutover step

Checks:
- row-count and checksum verification for moved data
- uniqueness and key constraints validated post-cutover
- zero-downtime or controlled downtime procedure documented

Cutover runbook must include:
- pre-cutover checks
- dual-read/verification window steps
- explicit rollback trigger conditions
- post-cutover validation checklist

## Dependency rules for DDD compliance (missing before, now explicit)

Allowed high-level dependencies:
1. API -> Application + Infrastructure + SharedKernel + Contracts
2. Application -> Domain + SharedKernel + Contracts
3. Infrastructure -> Application + Domain + SharedKernel + Contracts
4. Domain -> SharedKernel only

Disallowed dependencies:
1. Domain -> Infrastructure
2. Business context Domain/Application -> another context Infrastructure
3. Shared technical context -> business write-model ownership

Temporary migration exceptions must be:
- documented in PR,
- time-boxed to a target phase,
- removed before Phase 10 done.

## Known DDD violation patterns to eliminate in this phase

1. Cross-context transaction choreography for a single business command.
2. Shared AppDbContext used as business data backdoor across contexts.
3. Dashboard/ops store becoming business-authoritative state.
4. Direct cross-context reads that bypass projections/integration contracts.

These patterns are considered blockers for Phase 10 completion.

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
- dependency impact note (which allowed/disallowed dependency rules are touched)
- temporary bridge declaration (if any) with removal target

## Definition of done for Phase 10

1. Shared-context ambiguity is eliminated (clear final role or fully removed).
2. Context write ownership is explicit and enforced.
3. Cross-context consistency relies on events/outbox, not direct relational coupling.
4. Migration and schema validator behavior matches final ownership model.
5. Runbooks exist for replay, dead-letter recovery, and drift checks.
6. Technical context boundary is explicit (project + ownership + allowlist documented).
7. No open temporary bridge exceptions remain without approved follow-up issue and due phase.

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

1. Complete and approve the ownership matrix artifact.
2. Complete and approve the AppDbContext allowlist artifact.
3. Register all temporary bridges with removal phases and test gates.
4. Lock the `AppDbContext` decision using the Phase 10 AppDbContext playbook.
5. Select one pilot context for first persistence cutover (recommended: Catalog or Shopping).
6. Deliver PR 1 (decision + ownership matrix + allowlist + bridge register) with validation notes and rollback notes.
7. Deliver PR 2 for the first bounded-context shared-context dependency chain removal.

## Repository hygiene guardrails for Phase 10

To avoid accidental duplicate trees or stale code paths during persistence migration:

1. Do not create nested repo mirrors (for example `src/backend/src/backend`).
2. Before each PR, run a tracked-file sanity check for nested duplicates and out-of-scope roots.
3. Keep migration/code changes in scoped PR slices; avoid mixed cleanup + schema cutover in one commit unless cleanup is required to unblock the slice.

Recommended checks before opening each PR:

```powershell
git status --short
git ls-files | rg "^src/backend/src/"
dotnet build src/backend/ECommerce.sln -v minimal
```

Expected result for the duplicate-path check: no output.

## Notes for maintainers

If this phase changes established architecture conventions, update these docs in the same PR:
- .ai/README.md
- .ai/workflows/database-migrations.md
- .ai/plans/ddd-cqrs-migration/README.md
- .ai/plans/ddd-cqrs-migration/target-structure.md
