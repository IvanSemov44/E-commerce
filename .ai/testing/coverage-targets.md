# Coverage Targets

Targets are per-layer minimums. They are enforced by coverlet thresholds (Phase T-5). Until then, they are enforced by code review.

---

## Backend

### Layer 1 — Domain Tests

| What | Target | Notes |
|---|---|---|
| Every `public` aggregate method | 100% | Factory + all domain methods |
| Every `Result.Fail` branch | 100% | Each distinct error code = one test |
| Every value object | 100% | Valid inputs + each invalid input variant |
| Domain events raised | 100% | Assert event type and key properties |

If a method has 3 failure branches, it needs 4 tests: 1 success + 3 failures.

### Layer 2 — Application Tests

| What | Target | Notes |
|---|---|---|
| Every command handler `Handle` | 80% | Happy path + not-found + key business error |
| Every query handler `Handle` | 80% | Happy path + not-found |
| `SaveChangesAsync` call count | Must verify | Use `FakeUnitOfWork.SaveChangesCount` |
| Repository method called | Must verify | Check fake repo state, not mock verification |

### Layer 3 — Integration Tests

| What | Target | Notes |
|---|---|---|
| Every controller endpoint | 1 happy path test | Minimum — verify status + response shape |
| Every write endpoint | + validation failure (400) | Missing required field or invalid value |
| Every protected endpoint | + 401 unauthenticated | No token |
| Every role-restricted endpoint | + 403 wrong role | Customer hitting admin endpoint |

### Layer 4 — Characterization Tests

Not measured by percentage. Measured by completeness:
- Written before every refactor or migration step.
- Every endpoint in scope gets the full matrix: 200, 400, 401, 403, 404, 422.

### Layer 5 — Projection Sync Tests

| What | Target |
|---|---|
| Every `INotificationHandler<TEvent>` | 3 tests: insert path, update path, delete path |

### Layer 6 — Unit Tests (Infrastructure)

| What | Target |
|---|---|
| Every middleware | Happy path + each error case |
| Every validator | Each rule: valid + invalid |
| Every action filter | All branches |

---

## Frontend

### Layer F1 — Component Tests

| What | Target | Notes |
|---|---|---|
| Every component in `features/` | 1 render test minimum | Default props render correctly |
| Loading state | If component has loading state | Shows skeleton/spinner |
| Empty state | If component has empty state | Shows empty message |
| Error state | If component can error | Shows error message |
| User interaction | Every button/form that dispatches | Interaction triggers correct action |

### Layer F2 — Hook Tests

| What | Target |
|---|---|
| Every custom hook | Initial state + at least one state transition |
| Error handling | If hook can fail, test the error path |

### Layer F3 — Slice / Selector Tests

| What | Target |
|---|---|
| Every reducer action | 1 test per action |
| Every selector | 1 test with non-trivial logic |

### Layer F4 — E2E Tests

| What | Target |
|---|---|
| Every critical user journey | 1 happy-path E2E |
| Auth-gated pages | 1 redirect test (unauthenticated) |
| API contract | 1 `api-*.spec.ts` per feature area |

Critical journeys (must always have E2E coverage):
- Register + email verify
- Login / logout
- Browse catalog + search
- Add to cart + update quantity + remove
- Checkout (guest + authenticated)
- View orders

---

## Current Gaps (as of 2026-04-08)

| Area | Gap | Priority |
|---|---|---|
| Shopping.Application | Only 4 handler tests; domain has 19 | High |
| Ordering.Application | GetOrderStats has 1 test only | Medium |
| Reviews.Infrastructure | Projection sync only has insert + delete; missing update path | High |
| All projection handlers | Only insert + delete tested; no update path | High |
| Frontend slices | No slice tests exist yet | Medium |
