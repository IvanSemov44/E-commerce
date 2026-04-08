# Testing System

Owner: @ivans
Updated: 2026-04-08

## Purpose

This folder is the single source of truth for how tests are written, organised, and generated in this repository. Every human and every AI session that touches tests must read this before writing a single line.

## Read Order (required)

1. **[taxonomy.md](taxonomy.md)** — what test type goes where. The law.
2. **[naming-conventions.md](naming-conventions.md)** — method names, file names, class names.
3. **[coverage-targets.md](coverage-targets.md)** — minimum expectations per layer and why.
4. **[anti-patterns.md](anti-patterns.md)** — what NOT to do, with examples.
5. Pattern doc for the layer you are working in:
   - Backend domain → [patterns/backend-domain-tests.md](patterns/backend-domain-tests.md)
   - Backend application → [patterns/backend-application-tests.md](patterns/backend-application-tests.md)
   - Backend integration → [patterns/backend-integration-tests.md](patterns/backend-integration-tests.md)
   - Backend characterization → [patterns/backend-characterization-tests.md](patterns/backend-characterization-tests.md)
   - Backend projection sync → [patterns/backend-projection-sync-tests.md](patterns/backend-projection-sync-tests.md)
   - Frontend unit/component/hook → [patterns/frontend-unit-tests.md](patterns/frontend-unit-tests.md)
   - Frontend E2E → [patterns/frontend-e2e-tests.md](patterns/frontend-e2e-tests.md)

---

## Library Stack

### Backend

| Library | Version | Role | Notes |
|---|---|---|---|
| **MSTest** | 4.0.1 | Test runner | Modern — source generators, nested classes, `[DataTestMethod]`. No reason to switch to xUnit. |
| **Shouldly** | 4.2.1 | Assertions | Migration complete. All 8 test projects migrated from FluentAssertions 7 (commercial license) to Shouldly (MIT). |
| **Moq** | 4.20.72 | Mocking | Acceptable in Layer 6 (infrastructure unit tests) only. Banned from BC domain and application tests — use hand-written fakes. |
| **Bogus** | 35.6.1 | Test data | Used in `ECommerce.Tests` for integration test data generation. |
| **EF SQLite in-memory** | 10.0.0 | Integration DB | Use for Layer 3 integration tests — enforces relational constraints unlike `UseInMemoryDatabase`. Already in `ECommerce.Tests.csproj`. |
| **EF InMemory** | 10.0.0 | Projection sync DB | Acceptable for Layer 5 only — projection handlers don't depend on relational constraints. |
| **coverlet** | 6.0.2 | Coverage | Thresholds enforcement planned for Phase T-5. |

**FluentAssertions migration — complete.** All 8 test projects now use Shouldly 4.2.1. coverlet 6.0.2 was also added to the 7 BC test projects that were missing it.

### Frontend

| Library | Version | Role | Notes |
|---|---|---|---|
| **Vitest** | 3.2.4 | Test runner | State of the art — fast, Vite-native, browser mode available. |
| **@testing-library/react** | 16.3.2 | Component rendering | Latest, React 19-ready. |
| **@testing-library/user-event** | 14.6.1 | User interaction simulation | Uses pointer events API. Always prefer over `fireEvent`. |
| **MSW v2** | 2.13.2 | HTTP interception | Installed. Server infrastructure exists in `src/shared/lib/test/msw-server.ts`. Lifecycle hooks wired in `setup.ts`. What remains: migrate existing `vi.mock` component tests to MSW handlers (52 test files). |
| **@playwright/test** | 1.58.2 | E2E browser automation | Very recent. Excellent. |

**MSW — infrastructure complete, migration pending.** `msw-server.ts` exists, lifecycle hooks are wired in `setup.ts`. The 52 test files that still use `vi.mock` for RTK Query hooks are technical debt to be migrated incrementally per feature (see Phase T-8).

**BrowserRouter — RESOLVED.** `test-utils.tsx` now uses `MemoryRouter`.

---

## AI Prompt Library

Pre-written, context-rich prompts for generating each test type. Use these instead of freehand prompting — they encode all conventions and prevent regressions.

| Prompt | Use when |
|---|---|
| [prompts/testing/backend-domain-test.md](../prompts/testing/backend-domain-test.md) | Adding/changing a domain aggregate or value object |
| [prompts/testing/backend-application-handler-test.md](../prompts/testing/backend-application-handler-test.md) | Adding/changing a command or query handler |
| [prompts/testing/backend-integration-endpoint-test.md](../prompts/testing/backend-integration-endpoint-test.md) | Adding/changing a controller endpoint |
| [prompts/testing/backend-characterization-test.md](../prompts/testing/backend-characterization-test.md) | Before refactoring any existing behavior |
| [prompts/testing/backend-projection-sync-test.md](../prompts/testing/backend-projection-sync-test.md) | Adding a new integration event handler that updates a read model |
| [prompts/testing/frontend-component-test.md](../prompts/testing/frontend-component-test.md) | Adding/changing a React component |
| [prompts/testing/frontend-hook-test.md](../prompts/testing/frontend-hook-test.md) | Adding/changing a custom hook |
| [prompts/testing/frontend-e2e-test.md](../prompts/testing/frontend-e2e-test.md) | Adding/changing a user-facing flow |

---

## Project Map

### Backend

| Project | Layer | Location |
|---|---|---|
| `ECommerce.Catalog.Tests` | Domain + Application | `src/backend/Catalog/ECommerce.Catalog.Tests/` |
| `ECommerce.Identity.Tests` | Domain + Application | `src/backend/Identity/ECommerce.Identity.Tests/` |
| `ECommerce.Inventory.Tests` | Domain + Application | `src/backend/Inventory/ECommerce.Inventory.Tests/` |
| `ECommerce.Ordering.Tests` | Domain + Application | `src/backend/Ordering/ECommerce.Ordering.Tests/` |
| `ECommerce.Promotions.Tests` | Domain + Application | `src/backend/Promotions/ECommerce.Promotions.Tests/` |
| `ECommerce.Reviews.Tests` | Domain + Application | `src/backend/Reviews/ECommerce.Reviews.Tests/` |
| `ECommerce.Shopping.Tests` | Domain + Application | `src/backend/Shopping/ECommerce.Shopping.Tests/` |
| `ECommerce.Tests` | Integration + Unit | `src/backend/ECommerce.Tests/` |

### Frontend

| App | Unit/Component/Hook | E2E |
|---|---|---|
| Storefront | `src/frontend/storefront/src/**/*.test.tsx` | `src/frontend/storefront/e2e/` |

---

## Phase Plan

| Phase | Goal | Status |
|---|---|---|
| T-1 | Foundation docs (this folder) | ✅ Done |
| T-2 | Standardise BC handler coverage (Shopping, Ordering gaps) | Pending |
| T-3 | Fill projection sync gaps (all event types: insert/update/delete) | Pending |
| T-4 | Integration test health (every endpoint: 200 + 400 + 401/403) | Pending |
| T-5 | Coverage enforcement in CI (coverlet thresholds per project) | Pending |
| T-6 | Migrate FluentAssertions → Shouldly across all projects | ✅ Done |
| T-7 | Add MSW + fix BrowserRouter in frontend test-utils | ✅ Done |
| T-8 | Migrate vi.mock RTK Query tests to MSW (52 files, do incrementally per feature) | Pending |
