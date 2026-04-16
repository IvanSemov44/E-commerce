# Phase 10 Prompt Run Checklist

Purpose: fast, repeatable execution flow for Phase 10 prompt templates inside VS Code.

## What this gives you

- One operational sequence from architecture review to merge gate.
- Exact prompt file to use at each step.
- Exact terminal commands to run for evidence and validation.

## Preconditions

1. Open repository root in VS Code.
2. Ensure you are on the Phase 10 branch.
3. Ensure these files exist and are up to date:
- `.ai/plans/ddd-cqrs-migration/phases/phase-10-persistence-and-integration-boundaries.md`
- `.ai/plans/ddd-cqrs-migration/phases/phase-10-appdbcontext-playbook.md`
- `.ai/plans/ddd-cqrs-migration/phases/phase-10-ownership-matrix.md`
- `.ai/plans/ddd-cqrs-migration/phases/phase-10-appdbcontext-allowlist.md`
- `.ai/plans/ddd-cqrs-migration/phases/phase-10-temporary-bridge-register.md`

## Step 0: Fast evidence snapshot (terminal)

Run from repo root:

```powershell
git status --short
git ls-files | rg "^src/backend/src/"
```

If `rg` is not installed in your shell, use:

```powershell
$matches = git ls-files | Select-String '^src/backend/src/'; if ($null -eq $matches) { '0' } else { $matches.Count }
```

Run from `src/backend`:

```powershell
dotnet build ECommerce.sln -v minimal
```

Expected:
- duplicate-path command returns no lines
- build succeeds before planning next slice

## Step 1: Architecture review prompt

Prompt file:
- `.ai/plans/ddd-cqrs-migration/prompts/phase-10/01-architect-review.prompt.md`

Execution in VS Code Chat:
1. Open the prompt file.
2. Copy full prompt text.
3. Paste into chat and run.
4. Save output into current PR notes or working notes.

Mandatory result from AI:
- findings by severity
- evidence references
- required corrections
- residual risks

## Step 2: Ownership mapper prompt

Prompt file:
- `.ai/plans/ddd-cqrs-migration/prompts/phase-10/02-ownership-mapper.prompt.md`

Execution:
1. Run prompt.
2. Apply updates to:
- `.ai/plans/ddd-cqrs-migration/phases/phase-10-ownership-matrix.md`
- `.ai/plans/ddd-cqrs-migration/phases/phase-10-appdbcontext-allowlist.md`
- `.ai/plans/ddd-cqrs-migration/phases/phase-10-temporary-bridge-register.md`

Mandatory rule:
- unknown ownership is allowed only if explicitly marked unknown + follow-up scan task.

## Step 3: Select one bridge and plan one slice

Prompt file:
- `.ai/plans/ddd-cqrs-migration/prompts/phase-10/03-implementation-slice-planner.prompt.md`

Execution:
1. Pick one open bridge from bridge register.
2. Run planner prompt.
3. Confirm scope is exactly one reversible slice.

Mandatory outputs:
- file-level scope
- test gates
- rollback steps
- done criteria

## Step 4: Implement slice and validate

After implementation, run from repo root:

```powershell
git status --short
git ls-files | rg "^src/backend/src/"
```

Run from `src/backend`:

```powershell
dotnet build ECommerce.sln -v minimal
dotnet test
```

If migration/persistence changed, include migration verification notes in PR.

## Step 5: Pre-merge gatekeeper prompt

Prompt file:
- `.ai/plans/ddd-cqrs-migration/prompts/phase-10/04-pre-merge-gatekeeper.prompt.md`

Execution:
1. Run gatekeeper prompt on final diff and artifact files.
2. Treat FAIL as a hard stop.

Hard fail conditions:
- new multi-DbContext coordinator
- AppDbContext allowlist violation
- bridge without removal phase/test gate
- ownership change without matrix update
- migration change without rollback notes

## Step 6: Post-change retrospective prompt

Prompt file:
- `.ai/plans/ddd-cqrs-migration/prompts/phase-10/05-post-change-retrospective.prompt.md`

Execution:
1. Run after merge or failed attempt.
2. Add guardrail updates to docs/checklists for repeated issues.

## Merge checklist (copy/paste)

- [ ] Architect review findings resolved or explicitly accepted.
- [ ] Ownership matrix updated.
- [ ] AppDbContext allowlist updated.
- [ ] Bridge register updated.
- [ ] Build and tests passed.
- [ ] Rollback notes included.
- [ ] Gatekeeper prompt result is PASS.

## Optional: one-session command block

Use this only after implementation is complete:

```powershell
git status --short
git ls-files | rg "^src/backend/src/"
dotnet build src/backend/ECommerce.sln -v minimal
dotnet test src/backend/ECommerce.sln
```
