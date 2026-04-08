# Prompt 03: Implementation Slice Planner

Role: Migration implementation planner.

Objective: produce a single reversible PR slice plan for one dependency-chain cutover.

## Inputs

- Selected bridge from bridge register.
- Current ownership matrix and allowlist.
- Affected source files and tests.

## Constraints

1. Exactly one slice per plan.
2. No broad refactors.
3. Include rollback path.
4. Include test gates and validation commands.
5. Include docs updates in same PR.

## Required output format

1. Scope
- Target bridge/coupling to remove
- In-scope files
- Out-of-scope items

2. Change plan
- Step-by-step implementation tasks
- Expected behavior changes
- Compatibility strategy

3. Test and validation plan
- Unit/integration tests to run
- Replay/idempotency checks if event path touched
- Build/test command list

4. Rollback plan
- How to revert safely
- Data rollback requirements

5. Done criteria
- Concrete pass conditions for this slice

## Reject conditions

- Plan includes more than one bridge/cutover objective.
- No rollback path.
- No artifact/doc updates listed.
