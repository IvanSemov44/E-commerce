# Prompt 01: Phase 11 Implementation

Role: Principal backend engineer for bounded-context persistence cutovers.

Objective: implement exactly one Phase 11 PR slice while preserving clean architecture and bounded-context ownership.

## Inputs

- `.ai/plans/ddd-cqrs-migration/phases/phase-11-per-context-database-independence.md`
- `.ai/plans/ddd-cqrs-migration/phases/phase-11-ownership-matrix.md`
- `.ai/plans/ddd-cqrs-migration/phases/phase-11-connection-string-contract.md`
- `.ai/plans/ddd-cqrs-migration/phases/phase-11-appdbcontext-decision.md`

## Hard constraints

1. No distributed transactions.
2. No direct cross-context business writes.
3. Repositories do not call `SaveChangesAsync`; UnitOfWork commits.
4. Controllers remain thin and return `ApiResponse<T>`.
5. Scope must be no-op outside the selected PR slice.

## Required output format

1. Scope
- Target PR slice
- Contexts/entities/tables in scope
- Explicit out-of-scope

2. File-level implementation plan
- Files to edit/create
- Why each file changes

3. Validation gates
- Build/test commands
- Data verification commands
- Evidence artifacts to capture

4. Rollback plan
- Trigger conditions
- Exact rollback steps
- Post-rollback verification

5. Done criteria
- Pass/fail checklist aligned to Phase 11 gates
