# Final Delivery Roadmap

Purpose: Turn the current MVP into a production-ready release with controlled risk, stable behavior, and consistent quality across backend, storefront, and admin.

Baseline date: 2026-05-27

---

## Success Criteria

- Backend refactor is complete for controllers, Program.cs settings, and related extension methods without architecture rule violations.
- Test suite is green (backend + frontend + E2E) and behavior parity is confirmed for storefront critical flows.
- Admin panel code quality is aligned with storefront standards (API integration, state, tests, and documentation).
- Deployment is reproducible with a release checklist, rollback plan, and post-deploy validation.

---

## Phase Plan (Execution Order)

## Phase 0 - Planning and Baseline (1 day)

Deliverables:
- Freeze scope for release (no unrelated feature churn).
- Capture current baseline: passing tests, endpoint map, known gaps.
- Create tracking board with owners, estimates, and Definition of Done per task.

Definition of Done:
- One prioritized board exists with labels: Blocker, Sprint, Backlog.
- Baseline test results and open issues are documented.

---

## Phase 1 - Backend Refactor and Hardening (1-2 weeks)

Scope:
- Refactor controllers for consistency:
  - Thin controllers
  - Consistent ApiResponse usage
  - Full ProducesResponseType coverage
  - ValidationFilter on write DTO endpoints
- Refactor Program.cs and service registration:
  - Remove ambiguous registrations
  - Keep environment-specific settings explicit
  - Ensure extension methods have clear composition boundaries
- Refactor related extension methods to avoid duplicated or scattered configuration logic.

Testing:
- Update/add controller integration tests for changed behavior and response contracts.
- Add regression tests for error handling and status code mapping.

Definition of Done:
- No controller contract regressions.
- Swagger/OpenAPI output is complete for changed endpoints.
- All backend tests pass.

---

## Phase 2 - Documentation Upgrade to Project Standard (2-3 days)

Scope:
- Update docs where patterns changed:
  - docs/api-reference.md
  - docs/error-codes-reference.md
  - docs/architecture.md
  - docs/testing.md
- Update .ai guidance for any changed implementation pattern.

Definition of Done:
- Docs reflect current code behavior.
- No stale instructions remain for changed patterns.

---

## Phase 3 - Storefront E2E Behavior Parity (3-5 days)

Goal: confirm behavior remains the same after backend refactor.

Critical E2E flows:
- Auth: login, register, reset password
- Catalog: browse, filter/search, product detail
- Cart/Checkout: add item, quantity change, promo, checkout submit
- Orders: list and order detail
- Profile/Wishlist: core user actions

Definition of Done:
- E2E suite passes in CI/local.
- Any behavior drift is fixed or accepted with explicit product sign-off.

---

## Phase 4 - Admin Panel Quality Alignment (1-2 weeks)

Scope:
- Align admin architecture with storefront quality level:
  - RTK Query-first data fetching (no ad-hoc fetch logic in components)
  - Clean feature boundaries
  - Typed API contracts
  - Consistent error/loading handling
- Raise test coverage for critical admin flows.
- Improve admin docs in docs and .ai/frontend/admin.

Definition of Done:
- Admin critical flows tested and stable.
- Quality checks match storefront baseline expectations.

---

## Phase 5 - Final Deployment and Validation (2-3 days)

Pre-deploy checklist:
- Full test suite green (backend, frontend unit/integration, E2E).
- Environment settings verified.
- DB migration strategy verified (if applicable).
- Rollback plan prepared and tested.

Deploy:
- Deploy backend and frontends in controlled order.
- Run smoke tests immediately after deployment.

Post-deploy:
- Monitor logs, health checks, and key business metrics.
- Create release report: shipped changes, known limitations, follow-up backlog.

Definition of Done:
- Production is stable.
- No Sev-1/Sev-2 incidents in initial observation window.

---

## Workstream Checklist

## Backend
- [ ] Controllers refactored and standardized
- [ ] Program.cs settings cleaned
- [ ] Extension method registrations cleaned
- [ ] Backend regression tests updated

## Documentation
- [ ] docs updated for changed behavior
- [ ] .ai docs updated for changed patterns

## Storefront
- [ ] E2E critical flows passing
- [ ] No behavior regressions after backend refactor

## Admin
- [ ] Data access and state management aligned with storefront standards
- [ ] Critical admin tests added/passing

## Release
- [ ] Deployment checklist completed
- [ ] Rollback verified
- [ ] Post-deploy validation complete

---

## Suggested Cadence

- Weekly planning: pick top priorities by impact and risk.
- Daily standup: blockers, ownership, ETA changes.
- End-of-week review: shipped vs planned, risks for next week.

Recommended capacity split:
- 60% delivery
- 25% fixes/tech debt
- 15% risk buffer

---

## Risks and Controls

- Risk: hidden API regressions after refactor
  - Control: contract tests + E2E parity pack before release
- Risk: admin panel quality lags behind storefront
  - Control: enforce same architecture/testing gates
- Risk: deployment surprises
  - Control: explicit release checklist + rollback rehearsal

---

## Immediate Next 7 Days

1. Finalize backend refactor scope and owner assignment.
2. Complete ProducesResponseType and response contract consistency.
3. Stabilize Program.cs and extension method composition.
4. Run backend regression tests and fix failures.
5. Start storefront E2E parity pack for top 5 user journeys.