# Standard: Documentation Maintenance

Updated: 2026-03-08
Owner: @ivans

## Purpose
Keep AI documentation accurate and prevent drift as code evolves.

## Canonical Rules
1. If a PR changes an established pattern, update related `.ai` docs in the same PR.
2. If docs and code conflict, code is source of truth and docs must be fixed before merge.
3. Do not leave "update docs later" for pattern changes.
4. Prefer linking to canonical docs instead of duplicating rules.

## Where to Start
- Canonical hub: `.ai/README.md`
- Tool adapters:
  - `CLAUDE.md`
  - `.github/copilot-instructions.md`

## Required Review Checks
- [ ] Pattern changed? `.ai` docs updated in this PR.
- [ ] New anti-pattern discovered? Added to `.ai/reference/common-mistakes.md`.
- [ ] New workflow introduced? Added under `.ai/workflows/` and linked from `.ai/README.md`.
- [ ] Links and commands in changed docs were sanity-checked.

## Cadence
- Weekly (15 minutes): check recent merged PRs for undocumented pattern changes.
- Monthly (30 minutes): deduplicate, archive stale notes, and refresh common mistakes.

## Common Failure Modes
- Docs updated in separate PRs and forgotten.
- Multiple files define same rule differently.
- Legacy docs remain primary source instead of `.ai` docs.

## Related Files
- `CONTRIBUTING.md`
- `.ai/reference/common-mistakes.md`
- `.ai/workflows/troubleshooting.md`
