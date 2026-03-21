# AI Assistant Docs - E-Commerce

Updated: 2026-03-08
Owner: @ivans

## Purpose
This is the canonical AI documentation for this repository. All assistant-specific entry files should point here first.

## Read Order
1. Read `.ai/README.md` (this file).
2. Read `.ai/workflows/adding-feature.md` for implementation flow.
3. Read `.ai/workflows/database-migrations.md` before any schema or persistence change.
4. Read `.ai/workflows/testing-strategy.md` before deciding test scope.
5. Read `.ai/workflows/deployment.md` for environment/deploy changes.
6. Read `.ai/workflows/troubleshooting.md` when investigating failures.
7. Read `.ai/backend/error-handling.md` before touching service/controller logic.
8. Read `.ai/reference/common-mistakes.md` before writing code.
9. Read `.ai/workflows/post-modification-checks.md` — run after every code change.

## By Task
- Understand architecture decisions and boundaries:
  - `.ai/architecture/overview.md`
  - `.ai/architecture/clean-architecture.md`
  - `.ai/architecture/patterns.md`
  - `.ai/architecture/decisions/`
- Add a new backend feature:
  - `.ai/workflows/adding-feature.md`
  - `.ai/backend/error-handling.md`
- Create or apply a migration:
  - `.ai/workflows/database-migrations.md`
- Plan tests for a change:
  - `.ai/workflows/testing-strategy.md`
- Deploy or update infrastructure:
  - `.ai/workflows/deployment.md`
- Debug failures quickly:
  - `.ai/workflows/troubleshooting.md`
- **Verify code after any modification:**
  - `.ai/workflows/post-modification-checks.md`
- Update/maintain AI docs:
  - `.ai/standards/documentation.md`
- Apply coding/security/review standards:
  - `.ai/standards/code-style.md`
  - `.ai/standards/git-workflow.md`
  - `.ai/standards/security.md`
- Fix API errors or status code behavior:
  - `.ai/backend/error-handling.md`
- Implement/modify backend transport layer:
  - `.ai/backend/overview.md`
  - `.ai/backend/controllers.md`
  - `.ai/backend/validation.md`
- Implement/modify backend data/business layers:
  - `.ai/backend/repositories.md`
  - `.ai/backend/services.md`
  - `.ai/backend/transactions.md`
  - `.ai/backend/performance.md`
  - `.ai/backend/caching.md`
  - `.ai/backend/idempotency.md`
  - `.ai/backend/concurrency.md`
  - `.ai/backend/query-patterns.md`
- Implement/modify frontend state or API integration:
  - `.ai/frontend/overview.md`
  - `.ai/frontend/redux.md`
  - `.ai/frontend/api-integration.md`
  - `.ai/frontend/type-safety.md`
- Build or refactor frontend UI architecture:
  - `.ai/frontend/components.md`
  - `.ai/frontend/hooks.md`
  - `.ai/frontend/routing.md`
  - `.ai/frontend/forms.md`
  - `.ai/frontend/auth-forms.md`
  - `.ai/frontend/accessibility.md`
- Avoid known pitfalls:
  - `.ai/reference/common-mistakes.md`
- Look up structure/examples/terms quickly:
  - `.ai/reference/file-structure.md`
  - `.ai/reference/technologies.md`
  - `.ai/reference/code-examples.md`
  - `.ai/reference/glossary.md`

## Tracking
- Active debt: `.ai/tracking/technical-debt.md`
- Planned improvements: `.ai/tracking/improvements.md`
- Improvement orchestration: `.ai/tracking/future-improvements-orchestration.md`

## Tool Adapters (Must Exist)
- Claude Code: `CLAUDE.md`
- GitHub Copilot: `.github/copilot-instructions.md`
- Cursor (if used): `.cursorrules` or `.cursor/rules/*`
- Cline (if used): `.clinerules`

## Scope Rules
- Keep one topic per file.
- Keep docs concise, but complete.
- Prefer links to canonical docs instead of duplicating rules.

## Maintenance Contract
- If a PR changes an established pattern, update docs in the same PR.
- If code and docs conflict, code is source of truth; fix docs immediately.
- No "update docs later" for pattern changes.
