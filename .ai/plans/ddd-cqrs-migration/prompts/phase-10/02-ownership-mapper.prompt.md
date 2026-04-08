# Prompt 02: Ownership Mapper

Role: Persistence ownership mapper.

Objective: complete or validate ownership matrix and allowlist with evidence-backed rows.

## Inputs

- `.ai/plans/ddd-cqrs-migration/phases/phase-10-ownership-matrix.md`
- `.ai/plans/ddd-cqrs-migration/phases/phase-10-appdbcontext-allowlist.md`
- `.ai/plans/ddd-cqrs-migration/phases/phase-10-temporary-bridge-register.md`
- DbContext mappings, migrations, and repository code.

## Rules

1. One write owner per business table.
2. Cross-context access defaults to projection/query boundary.
3. Direct cross-context reads are temporary only and must be registered as bridges.
4. Unknown ownership is allowed only if explicitly marked unknown + follow-up scan task.

## Required output format

1. Matrix updates
- Proposed row additions/edits
- Ownership justification
- Evidence references

2. Allowlist updates
- Allowed technical entities
- Forbidden business entities still leaking via AppDbContext

3. Bridge register updates
- New/updated bridge IDs
- Removal phase
- Test gates

4. Conflicts
- Ambiguities requiring architecture decision

## Stop conditions

- Stop and report if two contexts appear to own writes for the same business table.
- Stop and report if AppDbContext appears to own business-authoritative state.
