# Workflow: Testing Strategy

Updated: 2026-03-08
Owner: @ivans

## Purpose
Run the right test level for each change and keep feedback fast without skipping critical coverage.

## Test Pyramid for This Repo
- Unit tests: default for service/repository/validator/component logic.
- Integration tests: backend API contract and end-to-end backend flows.
- E2E tests: storefront/admin critical user journeys.

## What to Run by Change Type
1. Backend service/repository/validator change:
- Run targeted backend tests first.
- Then run full backend suite before merge.

2. API controller/contract change:
- Run affected integration tests.
- Run full backend suite before merge.

3. Storefront/admin component or slice change:
- Run targeted Vitest suite in the affected app.
- Run E2E if user flow is impacted.

4. Cross-cutting auth/cart/order/payment changes:
- Run backend full tests + relevant frontend unit tests + relevant E2E.

## Commands

### Backend (`src/backend`)
```powershell
dotnet test
```

### Storefront (`src/frontend/storefront`)
```powershell
npm run test:run
npm run test:coverage
npm run test:e2e
```

### Admin (`src/frontend/admin`)
```powershell
npm run test:run
npm run test:coverage
npm run test:e2e
```

## E2E Quick Path
- Storefront Playwright reference: `src/frontend/storefront/e2e/README.md`
- Optional API flow helper script: `scripts/e2e.ps1`

## Coverage Guidance
- Prioritize behavior and failure paths over chasing synthetic 100% line coverage.
- Every bug fix should include at least one regression test.
- Prefer deterministic tests over flaky UI/network timing dependencies.

## Real Code References
- Backend unit tests: `src/backend/ECommerce.Tests/Unit/`
- Backend integration tests: `src/backend/ECommerce.Tests/Integration/`
- Storefront tests: `src/frontend/storefront/src/**/__tests__/`
- Admin tests: `src/frontend/admin/src/**/__tests__/`
- Storefront E2E: `src/frontend/storefront/e2e/`

## Common Failure Modes
- Running full suite too early instead of targeted tests first.
- Merging behavior changes without regression tests.
- Treating flaky tests as acceptable instead of fixing root cause.
- Forgetting E2E when auth/cart/checkout flow changed.
