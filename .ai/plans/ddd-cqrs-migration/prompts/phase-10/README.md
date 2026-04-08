# Phase 10 Prompt Pack

Purpose: reusable, evidence-first prompts for Phase 10 persistence and boundary hardening.

## Files

- `01-architect-review.prompt.md`
- `02-ownership-mapper.prompt.md`
- `03-implementation-slice-planner.prompt.md`
- `04-pre-merge-gatekeeper.prompt.md`
- `05-post-change-retrospective.prompt.md`
- `prompt-run-checklist.md`

## Usage order

1. Run `01-architect-review.prompt.md`.
2. Run `02-ownership-mapper.prompt.md`.
3. Run `03-implementation-slice-planner.prompt.md`.
4. Implement the slice.
5. Run `04-pre-merge-gatekeeper.prompt.md`.
6. After merge or failed attempt, run `05-post-change-retrospective.prompt.md`.

## Required context files for all prompts

- `.ai/plans/ddd-cqrs-migration/phases/phase-10-persistence-and-integration-boundaries.md`
- `.ai/plans/ddd-cqrs-migration/phases/phase-10-appdbcontext-playbook.md`
- `.ai/plans/ddd-cqrs-migration/phases/phase-10-ownership-matrix.md`
- `.ai/plans/ddd-cqrs-migration/phases/phase-10-appdbcontext-allowlist.md`
- `.ai/plans/ddd-cqrs-migration/phases/phase-10-temporary-bridge-register.md`

## Global response rules

- No claims without evidence references.
- Findings ordered by severity first.
- One migration slice per execution.
- Include rollback and test gate details.
- If uncertain, mark as unknown and request exact scan.
