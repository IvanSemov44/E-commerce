# Coverage Targets

Targets are per-layer minimums. They are enforced by coverlet thresholds (Phase T-5). Until then, they are enforced by code review.

**On coverage numbers:** A coverage percentage is a detection tool, not a goal. 100% line coverage with useless assertions catches nothing. The numbers below represent the minimum surface needed to catch regressions that actually happen — not an arbitrary metric to game.

---

## Backend

### Layer 1 — Domain Tests

| What | Target | Why this number |
|---|---|---|
| Every `public` aggregate method | 100% | Domain rules are the highest-value code in the system. An uncovered branch is a business invariant with no safety net. |
| Every `Result.Fail` branch | 100% | Each distinct error code is a contract. If a migration renames an error code, a test must fail immediately. |
| Every value object | 100% | Value objects validate their own construction. Every invalid input variant is a different user mistake that must be caught. |
| Domain events raised | 100% | An untested event means the subscriber (handler, projection) will never be exercised either — cascading blind spot. |

If a method has 3 failure branches, it needs 4 tests: 1 success + 3 failures.

---

### Layer 2 — Application Tests

| What | Target | Why this number |
|---|---|---|
| Every command handler `Handle` | 80% | The 80% targets: happy path + not-found + the primary business error. Edge cases that are pure domain invariants already belong in Layer 1 — testing them again in the handler is duplication. |
| Every query handler `Handle` | 80% | Happy path (returns data) + not-found (returns error). Projection handlers may also need the "returns empty list" case. |
| `SaveChangesAsync` call count | Must verify | This proves the unit of work boundary is respected. A command that never calls `SaveChangesAsync` silently discards data. |
| Fake repository state | Must verify | Assert what ended up in the fake, not that a method was called. State verification catches missed persistence. |

---

### Layer 3 — Integration Tests

| What | Target | Why this number |
|---|---|---|
| Every controller endpoint | 1 happy path | Proves routing, middleware, and handler wiring work together. Domain rules are already covered in Layer 1. |
| Every write endpoint | + 1 validation failure (400) | Proves the `[ValidationFilter]` is wired up and the validator rejects bad input before reaching the handler. |
| Every protected endpoint | + 1 unauthenticated (401) | Proves the `[Authorize]` attribute is present. Missing it exposes the endpoint publicly. |
| Every role-restricted endpoint | + 1 wrong-role (403) | Proves the policy is correct. A customer must not be able to hit admin endpoints. |

One test per scenario is enough here. Multiple 400 tests for different invalid fields belong in the validator's unit tests (Layer 6), not here.

---

### Layer 4 — Characterization Tests

Not measured by percentage. Measured by completeness:
- Written before every refactor or migration step. **Never after.**
- Every endpoint in scope gets the full matrix: 200, 400, 401, 403, 404, 422.
- Every 422 test must assert the exact `ErrorCode` string — that is the migration safety net.

---

### Layer 5 — Projection Sync Tests

| What | Target | Why this number |
|---|---|---|
| Every `INotificationHandler<TEvent>` | 3 tests: insert, update, delete | Insert and delete are obvious. The **update path** is the one that silently breaks — if the handler doesn't find the existing row and just creates a duplicate, an insert-only test never catches it. All three paths are required. |

---

### Layer 6 — Unit Tests (Infrastructure)

| What | Target | Why |
|---|---|---|
| Every middleware | Happy path + each error case | Middleware has no framework to fall back on — it must be tested directly |
| Every validator | Each rule: valid + invalid | FluentValidation rules have their own branching logic |
| Every action filter | All branches | Filters run on every request in scope |

---

## Frontend

### Layer F1 — Component Tests

| What | Target | Why this number |
|---|---|---|
| Every component in `features/` | 1 render test minimum | Catches broken imports, missing props, and obvious render crashes immediately |
| Loading state | If component has loading state | Users experience loading states; they must not render broken |
| Empty state | If component has empty state | Empty state is easy to forget and often shows a blank screen instead of a message |
| Error state | If component can error | Error boundaries and error UI must be visible, not silently swallowed |
| User interaction | Every button/form that dispatches | If an interaction does nothing, it is invisible to the user but a regression to the system |

---

### Layer F2 — Hook Tests

| What | Target | Why |
|---|---|---|
| Every custom hook | Initial state + at least one state transition | The initial state verifies the hook is wired correctly. The transition verifies the logic. |
| Error handling | If hook can fail, test the error path | Error paths in hooks are almost never exercised manually during development |

---

### Layer F3 — Slice / Selector Tests

| What | Target | Why |
|---|---|---|
| Every reducer action | 1 test per action | Reducers are pure functions — they are trivial to test and the cost of a missed regression is high (broken cart state, lost auth, etc.) |
| Every non-trivial selector | 1 test | Selectors with derived logic (totals, filters, formatted values) need boundary checks |

---

### Layer F4 — E2E Tests

| What | Target | Why |
|---|---|---|
| Every critical user journey | 1 happy-path E2E | End-to-end tests catch integration failures no unit test can — routing, cookies, real API responses |
| Auth-gated pages | 1 redirect test (unauthenticated) | Must prove the route guard works in the actual browser, not just in unit tests |
| API contract | 1 `api-*.spec.ts` per feature area | Contract tests run without a browser — fast, stable, and prove the API shape before UI tests even run |

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
| Reviews.Infrastructure | Projection sync missing update path | High |
| All projection handlers | Only insert + delete tested; no update path | High |
| Frontend slices | No slice tests exist yet | Medium |
| Frontend components | No MSW server set up yet — all tests use vi.mock | High |
