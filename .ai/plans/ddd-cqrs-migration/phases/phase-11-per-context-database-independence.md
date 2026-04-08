# Phase 11: Per-Context Database Independence

Status: In progress
Owner: @ivans
Created: 2026-04-08
Last updated: 2026-04-08 (runtime evidence added)

## PR5 narrow-slice progress (2026-04-08)

Commit:
1. `fa9502f` - start PR5 platform-only AppDb ownership

Changes in this slice:
1. Shared AppDb startup seeding disabled (ownership moved to context-owned seed paths).
2. AppDbContext runtime model switched to platform-only (legacy business mappings retained only for InMemory test provider compatibility during transition).
3. Infrastructure seed registration path changed to no-op for shared AppDb seeders.

Verification:
1. `dotnet build src/backend/ECommerce.API/ECommerce.API.csproj` passed.
2. Focused tests passed:
	- `Phase8MessageBrokerIntegrationTests`
	- `ReviewsProductProjectionBackfillCharacterizationTests`
3. Docker runtime checks passed:
	- `GET /health/ready` returned `200`
	- Startup logs include:
	  - `Shared AppDb seeding is disabled. Context-owned seed paths must be used.`
	  - `Reviews projection backfill completed...`

## Latest execution evidence (2026-04-08)

Merged backend commits in this phase slice:
1. `2878340` - strict context connection keys enforced (payments/integration/data-protection)
2. `f0cfc4b` - integration schema bootstrap + reviews projection bootstrap/backfill hardening

Verified outcomes:
1. Context connection keys are present and required for Catalog, Shopping, Identity, Ordering, Inventory, Promotions, Reviews, Payments.
2. Technical connection keys are present and required for Integration and DataProtection.
3. Event infrastructure tables exist under `integration` schema in PostgreSQL:
	- `integration.outbox_messages`
	- `integration.inbox_messages`
	- `integration.dead_letter_messages`
	- `integration.order_fulfillment_saga_states`
4. Reviews projection startup backfill now succeeds and logs:
	- `Reviews projection backfill completed. CatalogProducts=28, Inserted=28, Updated=0`

Validation evidence captured:
1. `dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj --no-build` passed in workspace context.
2. Focused event + projection tests passed:
	- `Phase8MessageBrokerIntegrationTests`
	- `ReviewsProductProjectionBackfillCharacterizationTests`
3. Docker runtime checks passed:
	- `GET /health/ready` returned `200`
	- API container healthy

Known non-phase blocker:
1. Frontend test files remain modified and intentionally outside backend phase commit scope.

## Purpose

Move from modular-monolith shared persistence patterns to true bounded-context database ownership.

Phase 10 removed temporary bridges and hardened boundaries. Phase 11 finishes physical persistence independence.

## Non-goals

1. No distributed transaction coordinator introduction.
2. No cross-context direct SQL reads for business decisions.
3. No big-bang cutover of all contexts in one PR.
4. No deploy-time coupling where one context migration blocks all others.

## Target end-state

1. Each bounded context has its own database and migration pipeline.
2. No bounded-context business entity is mapped in shared AppDbContext.
3. Cross-context consistency is event-driven (outbox/inbox/saga), not direct shared writes.
4. Shared technical persistence (if any) is platform-only and explicitly allowlisted.

## Program principles

1. One bounded context owns one write model and one migration stream.
2. Cross-context effects are asynchronous and idempotent (outbox/inbox/saga).
3. Every cutover is reversible within one deployment window.
4. Evidence-driven rollout: no phase advancement without passing gates.
5. AI-assisted execution must be deterministic: each PR has explicit prompt inputs, expected outputs, and verification artifacts.

## Readiness gates (must pass before PR 2)

1. Ownership matrix approved by backend maintainers.
2. Connection-string strategy approved for all environments (dev/stage/prod).
3. Migration ownership policy approved (who can alter which schema/db).
4. Observability baseline in place for projection lag, dead-letter growth, replay throughput.
5. Rollback playbook reviewed and tested once in staging for one pilot context.
6. Prompt pack baseline approved (implementation, validation, rollback, and evidence prompts).

## Scope

### In scope

- Per-context connection strings and context-specific migration ownership.
- Removal of bounded-context business aggregates from shared AppDbContext.
- Seeder strategy split per context.
- Operational runbooks for cutover, verification, rollback.

### Out of scope

- Full service decomposition into separate deployables (unless explicitly planned).
- New product features.

## Remaining shared points from Phase 10 baseline

1. Shared context definition still exists in AppDbContext.
2. Startup migration/seed abstraction still targets shared AppDbContext via infrastructure service.
3. Shared seeders and design-time factory remain AppDbContext-based.

## Dependency map

1. PR 1 is a hard prerequisite for PR 2-PR 5.
2. PR 2 pilot must be stable in staging before PR 3 begins.
3. PR 3 and PR 4 can overlap only if they touch disjoint contexts and migration streams.
4. PR 5 starts only after all context split PRs are merged and verified.

## PR slicing plan

### PR 1: Decision and ownership freeze

Deliverables:
1. Final ownership matrix with one write-owner per table.
2. Explicit keep/remove decision for AppDbContext (platform-only or deprecate).
3. Connection-string contract per bounded context.

Required artifact files:
1. `.ai/plans/ddd-cqrs-migration/phases/phase-11-ownership-matrix.md`
2. `.ai/plans/ddd-cqrs-migration/phases/phase-11-appdbcontext-decision.md`
3. `.ai/plans/ddd-cqrs-migration/phases/phase-11-connection-string-contract.md`

Validation:
1. Architecture review sign-off.
2. No table with multiple write owners.
3. Every table tagged as: business owner context OR platform-owned.

Rollback:
1. Revert docs-only PR.

Checklist for this phase (copy/paste into PR):

```markdown
## Phase 11 PR Checklist (PR 1)

### Scope
- [ ] Contexts in scope: Cross-context ownership and governance decisions
- [ ] Entities/tables in scope: Ownership matrix tables
- [ ] Explicit out-of-scope: Runtime data cutover

### Ownership and architecture
- [ ] Single write-owner confirmed for changed tables
- [ ] No direct cross-context business writes introduced
- [ ] Clean architecture dependency direction preserved

### Migrations and config
- [ ] Context-specific migration(s) added/updated (if applicable)
- [ ] Connection string/config keys documented
- [ ] Migration rollback procedure included

### Reliability and consistency
- [ ] Outbox/inbox/saga paths validated for impacted flows (if applicable)
- [ ] Duplicate delivery/idempotency behavior verified (if applicable)
- [ ] Dead-letter handling and replay path verified (if applicable)

### Test and verification gates
- [ ] Build passed
- [ ] Focused integration tests passed (if code touched)
- [ ] Row-count/checksum verification attached (if data move/cutover)
- [ ] Migration verification output attached (if applicable)

### SLO and operations
- [ ] Projection lag within threshold after cutover (if applicable)
- [ ] Dead-letter growth within threshold after cutover (if applicable)
- [ ] Rollback triggers reviewed before deployment

### Accountability
- [ ] Technical owner:
- [ ] Reviewer:
- [ ] Rollback approver:
- [ ] On-call owner for cutover:
- [ ] Incident commander:

### Evidence links
- [ ] Build/test logs:
- [ ] Verification scripts/results:
- [ ] Dashboard/metrics snapshot:
- [ ] Rollback runbook:

### AI prompt pack references
- [ ] Implementation prompt link:
- [ ] Validation prompt link:
- [ ] Rollback readiness prompt link:
- [ ] Post-cutover evidence prompt link:
```

### PR 2: Catalog + Shopping DB split pilot

Deliverables:
1. Catalog and Shopping use dedicated databases (not only dedicated DbContexts).
2. Context-specific migrations for both.
3. Dual-read verification scripts.

Validation:
1. Build green.
2. Targeted integration tests for catalog/shopping flows.
3. Row-count/checksum verification against source data.
4. Production-like load replay on staging for pilot read paths.

Rollback:
1. Switch connection routing back to shared DB.
2. Re-run verification script to confirm no data loss.

Checklist for this phase (copy/paste into PR):

```markdown
## Phase 11 PR Checklist (PR 2)

### Scope
- [ ] Contexts in scope: Catalog, Shopping
- [ ] Entities/tables in scope: Catalog + Shopping bounded-context tables
- [ ] Explicit out-of-scope: Other bounded contexts

### Ownership and architecture
- [ ] Single write-owner confirmed for changed tables
- [ ] No direct cross-context business writes introduced
- [ ] Clean architecture dependency direction preserved

### Migrations and config
- [ ] Context-specific migration(s) added/updated
- [ ] Connection string/config keys documented
- [ ] Migration rollback procedure included

### Reliability and consistency
- [ ] Outbox/inbox/saga paths validated for impacted flows
- [ ] Duplicate delivery/idempotency behavior verified
- [ ] Dead-letter handling and replay path verified

### Test and verification gates
- [ ] Build passed
- [ ] Focused integration tests passed
- [ ] Row-count/checksum verification attached (if data move/cutover)
- [ ] Migration verification output attached

### SLO and operations
- [ ] Projection lag within threshold after cutover
- [ ] Dead-letter growth within threshold after cutover
- [ ] Rollback triggers reviewed before deployment

### Accountability
- [ ] Technical owner:
- [ ] Reviewer:
- [ ] Rollback approver:
- [ ] On-call owner for cutover:
- [ ] Incident commander:

### Evidence links
- [ ] Build/test logs:
- [ ] Verification scripts/results:
- [ ] Dashboard/metrics snapshot:
- [ ] Rollback runbook:

### AI prompt pack references
- [ ] Implementation prompt link:
- [ ] Validation prompt link:
- [ ] Rollback readiness prompt link:
- [ ] Post-cutover evidence prompt link:
```

### PR 3: Identity + Ordering DB split

Deliverables:
1. Identity and Ordering moved to dedicated databases.
2. Shared AppDbContext mappings for those business entities removed.

Validation:
1. Auth and ordering integration tests green.
2. Migration drift checks pass.
3. No cross-context FK dependency remains between Identity and Ordering databases.

Rollback:
1. Repoint to prior connection strings.
2. Re-apply previous migration baseline.

Checklist for this phase (copy/paste into PR):

```markdown
## Phase 11 PR Checklist (PR 3)

### Scope
- [ ] Contexts in scope: Identity, Ordering
- [ ] Entities/tables in scope: Identity + Ordering bounded-context tables
- [ ] Explicit out-of-scope: Other bounded contexts

### Ownership and architecture
- [ ] Single write-owner confirmed for changed tables
- [ ] No direct cross-context business writes introduced
- [ ] Clean architecture dependency direction preserved

### Migrations and config
- [ ] Context-specific migration(s) added/updated
- [ ] Connection string/config keys documented
- [ ] Migration rollback procedure included

### Reliability and consistency
- [ ] Outbox/inbox/saga paths validated for impacted flows
- [ ] Duplicate delivery/idempotency behavior verified
- [ ] Dead-letter handling and replay path verified

### Test and verification gates
- [ ] Build passed
- [ ] Focused integration tests passed
- [ ] Row-count/checksum verification attached (if data move/cutover)
- [ ] Migration verification output attached

### SLO and operations
- [ ] Projection lag within threshold after cutover
- [ ] Dead-letter growth within threshold after cutover
- [ ] Rollback triggers reviewed before deployment

### Accountability
- [ ] Technical owner:
- [ ] Reviewer:
- [ ] Rollback approver:
- [ ] On-call owner for cutover:
- [ ] Incident commander:

### Evidence links
- [ ] Build/test logs:
- [ ] Verification scripts/results:
- [ ] Dashboard/metrics snapshot:
- [ ] Rollback runbook:

### AI prompt pack references
- [ ] Implementation prompt link:
- [ ] Validation prompt link:
- [ ] Rollback readiness prompt link:
- [ ] Post-cutover evidence prompt link:
```

### PR 4: Inventory + Promotions + Reviews + Payments DB split

Deliverables:
1. Remaining contexts moved to dedicated databases.
2. Shared business tables removed from AppDbContext mapping.

Validation:
1. Context-specific integration tests green.
2. Event/projection consistency tests green.
3. Replay and duplicate-delivery tests green for all touched projections.

Rollback:
1. Context-by-context routing rollback.
2. Replay integration events as needed.

Checklist for this phase (copy/paste into PR):

```markdown
## Phase 11 PR Checklist (PR 4)

### Scope
- [ ] Contexts in scope: Inventory, Promotions, Reviews, Payments
- [ ] Entities/tables in scope: Remaining bounded-context business tables
- [ ] Explicit out-of-scope: Already split contexts

### Ownership and architecture
- [ ] Single write-owner confirmed for changed tables
- [ ] No direct cross-context business writes introduced
- [ ] Clean architecture dependency direction preserved

### Migrations and config
- [ ] Context-specific migration(s) added/updated
- [ ] Connection string/config keys documented
- [ ] Migration rollback procedure included

### Reliability and consistency
- [ ] Outbox/inbox/saga paths validated for impacted flows
- [ ] Duplicate delivery/idempotency behavior verified
- [ ] Dead-letter handling and replay path verified

### Test and verification gates
- [ ] Build passed
- [ ] Focused integration tests passed
- [ ] Row-count/checksum verification attached (if data move/cutover)
- [ ] Migration verification output attached

### SLO and operations
- [ ] Projection lag within threshold after cutover
- [ ] Dead-letter growth within threshold after cutover
- [ ] Rollback triggers reviewed before deployment

### Accountability
- [ ] Technical owner:
- [ ] Reviewer:
- [ ] Rollback approver:
- [ ] On-call owner for cutover:
- [ ] Incident commander:

### Evidence links
- [ ] Build/test logs:
- [ ] Verification scripts/results:
- [ ] Dashboard/metrics snapshot:
- [ ] Rollback runbook:

### AI prompt pack references
- [ ] Implementation prompt link:
- [ ] Validation prompt link:
- [ ] Rollback readiness prompt link:
- [ ] Post-cutover evidence prompt link:
```

### PR 5: Shared context finalization

Deliverables:
1. AppDbContext reduced to platform-only entities, or removed entirely.
2. Shared seeders replaced by per-context seed strategy.
3. AppDbContextFactory updated/retired accordingly.

Validation:
1. No bounded-context business entity references in shared context.
2. Build and smoke tests green.
3. Design-time tooling updated and migration command docs verified.

Rollback:
1. Restore AppDbContext mappings from previous tag.
2. Re-enable previous seeding path.

Checklist for this phase (copy/paste into PR):

```markdown
## Phase 11 PR Checklist (PR 5)

### Scope
- [ ] Contexts in scope: Shared persistence finalization
- [ ] Entities/tables in scope: AppDbContext platform-only entities
- [ ] Explicit out-of-scope: New feature development

### Ownership and architecture
- [ ] Single write-owner confirmed for changed tables
- [ ] No direct cross-context business writes introduced
- [ ] Clean architecture dependency direction preserved

### Migrations and config
- [ ] Context-specific migration(s) added/updated
- [ ] Connection string/config keys documented
- [ ] Migration rollback procedure included

### Reliability and consistency
- [ ] Outbox/inbox/saga paths validated for impacted flows
- [ ] Duplicate delivery/idempotency behavior verified
- [ ] Dead-letter handling and replay path verified

### Test and verification gates
- [ ] Build passed
- [ ] Focused integration tests passed
- [ ] Row-count/checksum verification attached (if data move/cutover)
- [ ] Migration verification output attached

### SLO and operations
- [ ] Projection lag within threshold after cutover
- [ ] Dead-letter growth within threshold after cutover
- [ ] Rollback triggers reviewed before deployment

### Accountability
- [ ] Technical owner:
- [ ] Reviewer:
- [ ] Rollback approver:
- [ ] On-call owner for cutover:
- [ ] Incident commander:

### Evidence links
- [ ] Build/test logs:
- [ ] Verification scripts/results:
- [ ] Dashboard/metrics snapshot:
- [ ] Rollback runbook:

### AI prompt pack references
- [ ] Implementation prompt link:
- [ ] Validation prompt link:
- [ ] Rollback readiness prompt link:
- [ ] Post-cutover evidence prompt link:
```

## Required test gates per PR

1. dotnet build src/backend/ECommerce.API/ECommerce.API.csproj
2. Focused integration tests for touched contexts.
3. Outbox/inbox/saga reliability checks for cross-context flows.
4. Migration verification script output archived in PR notes.

Minimum gate example (adapt per PR):

```powershell
dotnet build src/backend/ECommerce.API/ECommerce.API.csproj
dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj --filter "FullyQualifiedName~Integration|FullyQualifiedName~Outbox|FullyQualifiedName~Ordering|FullyQualifiedName~Payment"
```

## AI prompt pack (required per PR)

Each PR must include a concise prompt pack in PR notes or linked docs.

Prompt file set in repo:
1. `.ai/plans/ddd-cqrs-migration/prompts/phase-11/README.md`
2. `.ai/plans/ddd-cqrs-migration/prompts/phase-11/01-implementation.prompt.md`
3. `.ai/plans/ddd-cqrs-migration/prompts/phase-11/02-validation.prompt.md`
4. `.ai/plans/ddd-cqrs-migration/prompts/phase-11/03-rollback-readiness.prompt.md`
5. `.ai/plans/ddd-cqrs-migration/prompts/phase-11/04-post-cutover-evidence.prompt.md`
6. `.ai/plans/ddd-cqrs-migration/prompts/phase-11/prompt-run-checklist.md`

### Prompt 1: Implementation prompt

Template:

```text
Role: Principal backend engineer in this repository.
Goal: Implement <PR scope> without violating clean architecture boundaries.
Inputs:
- Target contexts: <list>
- Tables/entities in scope: <list>
- Connection strings/config keys: <list>
- Existing bridges/constraints: <list>
Hard constraints:
- No distributed transactions.
- No direct cross-context business writes.
- Repositories do not commit; UnitOfWork commits.
- Controllers remain thin and return ApiResponse<T>.
Deliverables:
1) Code changes
2) Migration scripts
3) Rollback procedure
4) Evidence checklist updates
Definition of done:
- Build/test gates pass
- Verification artifacts attached
- Rollback validated in staging (or explicitly waived with owner approval)
```

### Prompt 2: Validation prompt

Template:

```text
Validate PR <id> against Phase 11 gates.
Check:
1) Ownership boundaries preserved
2) Migration drift absent
3) Outbox/inbox/saga behavior healthy
4) SLO thresholds not breached
Output:
- PASS/FAIL per gate
- Exact failing evidence
- Minimal fix plan in priority order
```

### Prompt 3: Rollback readiness prompt

Template:

```text
Assume cutover failure for PR <id>.
Produce a rollback runbook with:
1) Trigger condition mapping
2) Exact rollback commands/steps
3) Data integrity verification after rollback
4) Communication checklist (owner, incident commander, status channel)
5) Time-to-stabilize estimate
```

### Prompt 4: Post-cutover evidence prompt

Template:

```text
Summarize post-cutover evidence for PR <id>.
Include:
1) Build/test outputs
2) Row-count/checksum comparisons
3) Projection lag and dead-letter metrics
4) Replay throughput observed vs target
5) Residual risks and follow-up actions
```

## Prompt quality rubric (must score green)

1. Specificity: scope, entities, and environment explicitly listed.
2. Constraint fidelity: repository architecture rules encoded in prompt.
3. Verifiability: expected artifacts and gate outputs explicitly requested.
4. Reversibility: rollback path is first-class, not an afterthought.
5. Minimal ambiguity: no vague verbs like "optimize" or "improve" without measurable targets.

## Prompt anti-drift rules

1. Prompts must name exact contexts and entities in scope; avoid generic "all contexts" language.
2. Prompts must require no-op behavior outside declared scope.
3. Prompts must request a change log with file-by-file rationale.
4. Prompts must require explicit assumptions; unstated assumptions are treated as failures.
5. Prompt revisions must be versioned in PR notes when scope changes mid-implementation.

## Operational safeguards

1. Every cutover has dual-read verification window.
2. Every cutover has explicit rollback trigger thresholds.
3. Dead-letter and replay runbook exercised at least once in staging.
4. Projection freshness SLO monitored during each cutover.

## SLOs and alert thresholds

1. Projection lag P95: <= 2 minutes during steady state.
2. Dead-letter growth: alert on sustained > 20 messages/hour for one context.
3. Replay throughput: minimum sustained target defined per context before cutover.
4. Migration duration budget: per-context threshold defined and monitored in staging.

## Rollback triggers (hard stop)

1. Error rate increase > agreed threshold for touched endpoints.
2. Projection lag breaches SLO for > 15 minutes after cutover.
3. Dead-letter queue grows without draining for one processing window.
4. Data verification mismatch on checksum/row-count gates.

## Completion criteria

1. All bounded contexts use independent databases.
2. Shared AppDbContext no longer contains bounded-context business write model.
3. No direct cross-context business writes.
4. All migration pipelines and runbooks are owned by target contexts.
5. Validation evidence captured for every PR slice.

## Ownership and accountability

1. Each PR must list: technical owner, reviewer, rollback approver.
2. Each cutover window must list: on-call owner and incident commander.
3. Every migration script must have an explicit rollback script or rollback procedure.

## Suggested execution cadence

1. One context split PR every 3-5 working days, with staging soak between PRs.
2. Weekly architecture checkpoint to reassess risk and unblock dependencies.
3. Do not run two production cutovers in the same bounded context within one week.

## Definition of Ready and Definition of Done

Definition of Ready (before implementation starts):
1. Scope boundary is explicit (contexts, entities, migrations, configs).
2. Prompt pack drafted and reviewed by technical owner.
3. Validation gates and rollback triggers mapped to telemetry.

Definition of Done (before merge):
1. All required gates pass with archived evidence.
2. Rollback runbook is executable and reviewed.
3. Ownership and operational handoff notes are complete.
4. Post-cutover evidence summary is attached.

## PR description checklist template (copy/paste)

Use this block in every Phase 11 PR:

```markdown
## Phase 11 PR Checklist

### Scope
- [ ] Contexts in scope:
- [ ] Entities/tables in scope:
- [ ] Explicit out-of-scope:

### Ownership and architecture
- [ ] Single write-owner confirmed for changed tables
- [ ] No direct cross-context business writes introduced
- [ ] Clean architecture dependency direction preserved

### Migrations and config
- [ ] Context-specific migration(s) added/updated
- [ ] Connection string/config keys documented
- [ ] Migration rollback procedure included

### Reliability and consistency
- [ ] Outbox/inbox/saga paths validated for impacted flows
- [ ] Duplicate delivery/idempotency behavior verified
- [ ] Dead-letter handling and replay path verified

### Test and verification gates
- [ ] Build passed
- [ ] Focused integration tests passed
- [ ] Row-count/checksum verification attached (if data move/cutover)
- [ ] Migration verification output attached

### SLO and operations
- [ ] Projection lag within threshold after cutover
- [ ] Dead-letter growth within threshold after cutover
- [ ] Rollback triggers reviewed before deployment

### Accountability
- [ ] Technical owner:
- [ ] Reviewer:
- [ ] Rollback approver:
- [ ] On-call owner for cutover:
- [ ] Incident commander:

### Evidence links
- [ ] Build/test logs:
- [ ] Verification scripts/results:
- [ ] Dashboard/metrics snapshot:
- [ ] Rollback runbook:

### AI prompt pack references
- [ ] Implementation prompt link:
- [ ] Validation prompt link:
- [ ] Rollback readiness prompt link:
- [ ] Post-cutover evidence prompt link:
```
