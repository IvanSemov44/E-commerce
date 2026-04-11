# Test Targets & Roadmap

This document maps current test coverage to target coverage, identifies gaps, and provides a roadmap to close them.

---

## Current State Summary

| Area | Test Files | Tests | Status |
|---|---|---|---|
| **Backend** | 81 | ~500+ | 8 BC test projects + ECommerce.Tests |
| **Frontend** | 108 | 891 | All passing |
| **Total** | 189 | ~1400+ | Healthy baseline |

---

## Target State by Layer

### Backend Targets

| Layer | Target | Current | Gap |
|---|---|---|---|
| **L1: Domain** | 100% aggregate methods covered | ~70% | Missing handler tests in some BCs |
| **L2: Application** | 80% handler coverage | ~60% | Shopping, Ordering gaps |
| **L3: Integration** | Every endpoint: 200 + 400 + 401 | Partial | Some endpoints lack full matrix |
| **L5: Projection Sync** | insert + update + delete per handler | Partial | Update path often missing |

### Frontend Targets

| Layer | Target | Current | Gap |
|---|---|---|---|
| **F1: Components** | Every component: render + states + interactions | ~80% | Missing for some shared UI |
| **F2: Hooks** | Every custom hook: initial + transition | ~75% | useCartOperations, useDebounce |
| **F3: Slices** | Every action + non-trivial selector | Complete | — |
| **F4: E2E** | Critical journeys + auth-gated | Partial | Limited coverage |

---

## Detailed Gap Analysis

### High Priority

| Gap | Layer | BC/Feature | Action |
|---|---|---|---|
| Projection sync update path | L5 | Reviews | Add update tests for handlers |
| MSW migration | F1 | Storefront | 52 files still use vi.mock |
| Shopping handler coverage | L2 | Shopping.Application | Add handler tests |
| OrderDetailPage tests | F1 | Orders | Add page-level tests |

### Medium Priority

| Gap | Layer | BC/Feature | Action |
|---|---|---|---|
| Ordering handler stats | L2 | Ordering.Application | Add GetOrderStats tests |
| useCartOperations | F2 | Cart | Add hook tests |
| useDebounce | F2 | Shared | Add hook tests |

### Low Priority

| Gap | Layer | BC/Feature | Action |
|---|---|---|---|
| ProfilePage | F1 | Profile | Add page tests |
| EmptyState, QuantityControl | F1 | Shared UI | Add component tests |

---

## Roadmap to Targets

### Phase 1: Backend Domain & Application (In Progress)

**Goal:** Close L1/L2 gaps

- [ ] Shopping.Application — add handler tests (4 → 19+)
- [ ] Ordering.Application — add GetOrderStats handler tests
- [ ] Reviews.Application — add remaining handler tests
- [ ] Verify 80% handler coverage across all BCs

### Phase 2: Backend Projection Sync

**Goal:** Complete L5 coverage

- [ ] Every projection handler: add update path tests
- [ ] Reviews infrastructure: add update tests
- [ ] Verify 3-test coverage (insert + update + delete)

### Phase 3: Backend Integration

**Goal:** Complete L3 full matrix

- [ ] Every endpoint: ensure 200 + 400 + 401 tests exist
- [ ] Add 403 tests for role-restricted endpoints
- [ ] Add 404 tests for not-found scenarios

### Phase 4: Frontend Component & Hook Coverage

**Goal:** Close F1/F2 gaps

- [ ] MSW migration: convert remaining 52 vi.mock files to MSW handlers
- [ ] Add OrderDetailPage, OrderHistoryPage tests
- [ ] Add useCartOperations, useDebounce hook tests
- [ ] Add missing shared UI component tests

### Phase 5: E2E Expansion

**Goal:** Expand F4 coverage

- [ ] Add critical journey E2E tests
- [ ] Add auth-gated redirect tests
- [ ] Add API contract tests per feature

---

## Enforcement

Once all phases complete, enforce via CI:

- **coverlet thresholds** in each .csproj
- **Frontend coverage** via Vitest coverage (TBD)
- **No PR merge** without test file for new handler/component

---

## Related Docs

- [coverage-targets.md](coverage-targets.md) — Detailed target definitions
- [structure.md](structure.md) — Current test locations
- [taxonomy.md](taxonomy.md) — What test type goes where