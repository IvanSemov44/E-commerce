Phase 3 — Kickoff Plan

Goal
- Continue the DTO/mapping improvements and broaden quality coverage: consolidate DTOs, add integration tests, improve API docs, and add performance/CI improvements.

Scope (initial)
- DTO consolidation: audit and centralize shared DTOs in `ECommerce.Application/DTOs` and update `MappingProfile`.
- Integration tests: add basic end-to-end tests for cart → order and wishlist flows (uses in-memory DB or test host).
- API docs: improve Swagger metadata and examples for key endpoints.
- Performance: add caching for read-heavy endpoints (Products, Categories) using in-memory or Redis (configurable).
- CI: ensure GitHub Actions run build, unit tests, and integration tests on PR.

First actions (this session)
1. Create branch `phase3/plan` (ask before pushing).
2. Create this Phase 3 plan file (done).
3. Ask you which Phase 3 task to begin first (recommended: `Commit & push` Phase 2 changes, then `DTO consolidation`).

Deliverables
- `docs/PHASE3_PLAN.md` (this file).
- PR with Phase 2 commits (if you approve push).
- Follow-up PR(s) for DTO consolidation, integration tests, API docs, caching, and CI.

Next step: confirm whether you want me to push Phase 2 commits now and create branch `phase3/plan`, or start DTO consolidation locally first.