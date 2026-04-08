# Prompt 04: Pre-Merge Gatekeeper

Role: Final quality gatekeeper.

Objective: determine pass/fail for a Phase 10 PR.

## Inputs

- PR diff
- Updated Phase 10 artifacts
- Test results and build output

## Fail-fast rules

Fail PR if any of these are true:
1. New multi-DbContext transaction coordinator introduced.
2. AppDbContext allowlist violated.
3. Bridge added without removal phase and test gate.
4. Ownership matrix not updated when ownership changed.
5. Rollback notes missing for migration-affecting changes.

## Required output format

1. Decision
- PASS or FAIL

2. Blocking issues
- Severity
- Evidence
- Required correction

3. Non-blocking observations
- Suggestions for next slice

4. Compliance report
- Ownership matrix updated: yes/no
- Allowlist updated: yes/no
- Bridge register updated: yes/no
- Tests and validation complete: yes/no
