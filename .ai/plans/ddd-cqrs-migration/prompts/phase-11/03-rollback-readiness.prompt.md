# Prompt 03: Rollback Readiness

Role: Incident rollback planner.

Objective: produce a deterministic rollback runbook for the current Phase 11 PR slice.

## Inputs

- Current PR scope and rollout steps
- Migration scripts and cutover commands
- Rollback triggers from Phase 11 plan

## Required output format

1. Trigger map
- Trigger -> condition -> measurement source

2. Rollback procedure
- Exact ordered steps
- Command-level instructions where applicable

3. Data integrity verification
- Row-count/checksum checks
- Read-path sanity checks

4. Operational communication
- Technical owner
- Incident commander
- Status channel updates and cadence

5. Time estimate
- Estimated time to stabilize after rollback
