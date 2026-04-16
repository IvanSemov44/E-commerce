# Test Targets & Roadmap

This document maps current test coverage to target coverage, identifies gaps, and provides a roadmap to close them.

---

## Current State Summary

| Area | Test Files | Tests | Status |
|---|---|---|---|
| **Backend BC Tests** | 8 projects | 397 | Domain + Application (Catalog, Identity, Inventory, Ordering, Promotions, Reviews, Shopping) |
| **Backend Integration** | 1 project | 698 | ECommerce.Tests (API, Unit, Integration) |
| **Frontend Storefront** | 108 | 891 | All passing |
| **Total** | 109+ | 1986 | |

### Backend BC Test Breakdown

| BC | Test Files | Test Methods |
|---|---|---|
| Catalog | 5 | 118 |
| Identity | 4 | 94 |
| Inventory | 5 | 55 |
| Ordering | 3 | 30 |
| Promotions | 3 | 39 |
| Reviews | 2 | 38 |
| Shopping | 3 | 23 |
| **Subtotal** | **25** | **397** |

---

## Target State by Layer

### Backend Targets

| Layer | Target | Current | Gap |
|---|---|---|---|
| **L1: Domain** | 100% aggregate methods covered | ~80% | Some domain methods lack tests |
| **L2: Application** | 80% handler coverage | ~65% | Shopping (23 tests), Ordering (30 tests) low |
| **L3: Integration** | Every endpoint: 200 + 400 + 401 | ~70% | Some endpoints lack full matrix |
| **L5: Projection Sync** | insert + update + delete per handler | Partial | Update path often missing |

### Frontend Targets

| Layer | Target | Current | Gap |
|---|---|---|---|
| **F1: Components** | Every component: render + states + interactions | ~85% | Order pages, some shared UI |
| **F2: Hooks** | Every custom hook: initial + transition | ~80% | useCartOperations, useDebounce |
| **F3: Slices** | Every action + non-trivial selector | Complete | — |
| **F4: E2E** | Critical journeys + auth-gated | Partial | Limited coverage |

---

## Detailed Gap Analysis

### High Priority

| Gap | Layer | BC/Feature | Current → Target | Action |
|---|---|---|---|---|
| Shopping handler coverage | L2 | Shopping | 23 → 50+ | Add handler tests |
| Ordering handler coverage | L2 | Ordering | 30 → 50+ | Add handler tests |
| Projection sync update path | L5 | Reviews | Partial → Complete | Add update tests |
| MSW migration | F1 | Storefront | Partial → Complete | Convert vi.mock to MSW |

### Medium Priority

| Gap | Layer | BC/Feature | Action |
|---|---|---|---|
| Inventory handler tests | L2 | Inventory | Add more handler tests |
| Promotions handler tests | L2 | Promotions | Add more handler tests |
| useCartOperations | F2 | Cart | Add hook tests |
| OrderDetailPage | F1 | Orders | Add page tests |

### Low Priority

| Gap | Layer | Feature | Action |
|---|---|---|---|
| ProfilePage | F1 | Profile | Add page tests |
| useDebounce | F2 | Shared | Add hook tests |
| EmptyState | F1 | Shared UI | Add component tests |

---

## Roadmap to Targets

### Phase 1: Backend Domain & Application

**Goal:** Close L1/L2 gaps

- [ ] Shopping.Application — add handler tests (23 → 50+)
- [ ] Ordering.Application — add handler tests (30 → 50+)
- [ ] Inventory.Application — add handler tests (55 → 60+)
- [ ] Promotions.Application — add handler tests (39 → 50+)
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

- [ ] MSW migration: convert vi.mock files to MSW handlers
- [ ] Add OrderDetailPage, OrderHistoryPage tests
- [ ] Add useCartOperations hook tests
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
- **Frontend coverage** via Vitest coverage
- **No PR merge** without test file for new handler/component

---

## Related Docs

- [coverage-targets.md](coverage-targets.md) — Detailed target definitions
- [structure.md](structure.md) — Current test locations
- [taxonomy.md](taxonomy.md) — What test type goes where