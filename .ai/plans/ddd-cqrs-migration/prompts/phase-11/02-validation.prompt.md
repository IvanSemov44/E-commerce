# Prompt 02: Phase 11 Validation

Role: Migration gatekeeper.

Objective: validate one implemented Phase 11 PR slice against hard gates.

## Inputs

- Final diff for the PR slice
- `.ai/plans/ddd-cqrs-migration/phases/phase-11-per-context-database-independence.md`
- PR evidence artifacts (build/test logs, verification outputs, metrics snapshots)

## Validation rules

1. Ownership boundaries preserved.
2. Migration drift absent.
3. Outbox/inbox/saga reliability healthy.
4. SLO thresholds not breached.
5. Rollback runbook exists and is executable.

## Required output format

1. Gate results
- PASS/FAIL per gate
- Evidence reference for each gate

2. Findings (ordered by severity)
- Critical
- High
- Medium
- Low

3. Minimal fix plan
- Step-by-step remediation in priority order

4. Merge recommendation
- `PASS`
- `PASS_WITH_ACCEPTED_RISK`
- `FAIL`
