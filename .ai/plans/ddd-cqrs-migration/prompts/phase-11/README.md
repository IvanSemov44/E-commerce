# Phase 11 Prompt Pack

Purpose: reusable, evidence-first prompts for per-context database independence execution.

## Files

- `01-implementation.prompt.md`
- `02-validation.prompt.md`
- `03-rollback-readiness.prompt.md`
- `04-post-cutover-evidence.prompt.md`
- `prompt-run-checklist.md`

## Usage order

1. Run `01-implementation.prompt.md`.
2. Implement one PR slice only.
3. Run `02-validation.prompt.md`.
4. Run `03-rollback-readiness.prompt.md` before cutover.
5. Run `04-post-cutover-evidence.prompt.md` after validation.

## Required context files for all prompts

- `.ai/plans/ddd-cqrs-migration/phases/phase-11-per-context-database-independence.md`
- `.ai/plans/ddd-cqrs-migration/phases/phase-11-ownership-matrix.md`
- `.ai/plans/ddd-cqrs-migration/phases/phase-11-connection-string-contract.md`
- `.ai/plans/ddd-cqrs-migration/phases/phase-11-appdbcontext-decision.md`

## Global response rules

- No claims without evidence references.
- Findings ordered by severity first.
- One migration slice per execution.
- Include rollback and test gate details.
- If uncertain, mark as unknown and require an explicit scan task.
