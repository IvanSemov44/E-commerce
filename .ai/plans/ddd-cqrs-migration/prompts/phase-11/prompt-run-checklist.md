# Phase 11 Prompt Run Checklist

Purpose: repeatable execution flow for Phase 11 prompts in VS Code Chat.

## Preconditions

1. Open repo root.
2. Confirm branch and working tree state.
3. Ensure these files exist and are current:
- `.ai/plans/ddd-cqrs-migration/phases/phase-11-per-context-database-independence.md`
- `.ai/plans/ddd-cqrs-migration/phases/phase-11-ownership-matrix.md`
- `.ai/plans/ddd-cqrs-migration/phases/phase-11-connection-string-contract.md`
- `.ai/plans/ddd-cqrs-migration/phases/phase-11-appdbcontext-decision.md`

## Execution order

1. Run `01-implementation.prompt.md`.
2. Implement exactly one PR slice.
3. Run `02-validation.prompt.md`.
4. Run `03-rollback-readiness.prompt.md` before cutover.
5. Run `04-post-cutover-evidence.prompt.md`.

## Mandatory merge checks

- [ ] Ownership matrix updated for changed scope.
- [ ] Connection contract updated for changed scope.
- [ ] AppDbContext decision constraints still satisfied.
- [ ] Build/test gates pass.
- [ ] Rollback runbook attached.
- [ ] Validation prompt result is PASS or PASS_WITH_ACCEPTED_RISK.
