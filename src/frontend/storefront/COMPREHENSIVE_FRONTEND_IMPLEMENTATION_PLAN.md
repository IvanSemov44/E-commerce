# Comprehensive Frontend Implementation Plan and Checklist

**Project**: E-Commerce Storefront  
**Location**: `src/frontend/storefront`  
**Based on**: `FRONTEND_CODING_GUIDE.md`  
**Prepared**: 2026-03-07

---

## Purpose

This document translates the frontend coding guide into an execution-ready implementation plan with a full checklist. It is intended to be used as the source of truth for migration and compliance work.

## Progress Log

### 2026-03-07 (Phase 1 Baseline Implementation)

- Fixed storefront lint blockers in test suites:
- `src/shared/components/ProtectedRoute/ProtectedRoute.test.tsx`
- `src/shared/components/SearchBar/SearchBar.test.tsx`
- `src/shared/components/SearchBar/hooks/__tests__/useKeyboardNavigation.test.ts`
- `src/shared/components/SearchBar/hooks/__tests__/useSearch.test.ts`
- `src/shared/components/ThemeToggle/ThemeToggle.test.tsx`
- `src/features/products/components/ProductInfo/ProductInfo.test.tsx`
- `src/shared/components/ErrorAlert/ErrorAlert.test.tsx`
- `src/shared/components/OptimizedImage/OptimizedImage.test.tsx`
- Validation results:
- `npm run lint`: passes with warnings only (0 errors)
- `npm run build`: passes
- `npm run test:run`: passes (`80` files, `704` tests)
- Remaining technical debt from this phase:
- None for lint baseline. Warning cleanup completed in follow-up hardening pass.

### 2026-03-07 (Phase 1 Hardening - Warning Cleanup)

- Refactored warning-heavy files to comply with complexity and function length constraints:
- `src/pages/HomePage/HomePage.tsx` (extracted reusable product grid/section renderers)
- `src/shared/components/Pagination/Pagination.tsx` (moved page-range logic to helper)
- `src/shared/components/SearchBar/SearchBar.tsx` (split dropdown + shortcut logic)
- `src/shared/components/ThemeToggle/ThemeToggle.tsx` (split cycle/dropdown components and stabilized mount behavior)
- `src/shared/hooks/useErrorHandler.ts` (reduced branch complexity via normalization helpers)
- `src/shared/lib/test/test-utils.tsx` (suppressed known react-refresh false-positive on test utility re-export)
- Validation results after hardening:
- `npm run lint`: clean (0 errors, 0 warnings)
- `npm run build`: passes
- `npm run test:run`: passes (`80` files, `704` tests)

---

## Current Baseline Snapshot

- API slices are already feature-organized and injected through shared `baseApi`.
- Co-location migration is partially complete (`COLOCATION_ADOPTION_TRACKER.md` is active).
- Shared icon library exists in `src/shared/components/icons/`.
- Lint currently fails in storefront (`npm run lint` returned non-zero in latest run).
- Known compliance debt from recent scan:
- `any` usage occurrences: ~30 (mostly tests + test helpers)
- Relative import occurrences (`../` style beyond same-directory): ~101
- Inline SVG usage still present outside icon library (notably `StarRating.tsx` + test mocks)

---

## Critical Decision Before Full Rollout

There is a guide-level inconsistency that should be resolved once and enforced consistently:

- One section recommends Zod-based form validation.
- Another section says forms use custom `useForm` and no Zod dependency.
- Actual code currently uses Zod schemas in auth/profile/checkout.

### Decision Required

Choose one standard and keep it consistent:

- Option A: Keep Zod for schema validation and align the guide text.
- Option B: Remove Zod and migrate all schema validation to `useForm` + custom validators.

Until this decision is made, use existing feature patterns to avoid churn.

---

## Delivery Model

- Use incremental PRs with clear scope (1 major area at a time).
- Prefer low-risk refactors first: typing, imports, structural alignment.
- Keep behavior changes separate from architecture cleanup whenever possible.
- Validate each phase with lint/build/tests before moving on.

---

## Phase Plan

## Phase 1: Foundation and Enforcement

### Goals

- Stabilize quality gates.
- Convert the plan into enforceable checks.

### Actions

- Ensure storefront CI gate sequence covers:
- `npm run lint`
- `npm run build`
- `npm run test` (or equivalent stable subset)
- Enforce import conventions (`@` alias) in lint rules.
- Enforce no-`any` rule with documented exceptions (if any).
- Confirm `baseApi` remains single source of API integration truth.

### Exit Criteria

- Lint/build pass in storefront.
- Rule violations reported consistently in PR checks.

---

## Phase 2: Type Safety Cleanup

### Goals

- Eliminate avoidable `any` usage.
- Improve typed test utilities and mocks.

### Actions

- Refactor `src/shared/lib/test/test-utils.tsx` to typed preloaded state and middleware signatures.
- Replace `any` in component tests with proper mock types (`SVGProps`, typed mock stores, typed hook responses).
- Remove `as any` casts where safe alternatives exist.

### Exit Criteria

- All `any` occurrences either removed or explicitly justified with comments.
- No regression in test reliability.

---

## Phase 3: Import Path Standardization

### Goals

- Adopt `@` alias consistently for non-sibling imports.

### Actions

- Replace deep relative imports across `src/**/*.{ts,tsx}`.
- Keep `./` sibling imports where appropriate.
- Avoid mixed style (`@` + deep relative) in same module.

### Exit Criteria

- Import path style is consistent and enforced by lint.

---

## Phase 4: Icon and SVG Compliance

### Goals

- Centralize icon usage to shared icon components.

### Actions

- Replace non-library inline SVG usage in app components with icons from `src/shared/components/icons/`.
- Keep test-only inline SVG mocks minimal and typed.
- Add missing icon components if needed.

### Exit Criteria

- No inline SVG in production feature/shared UI components except justified edge cases.

---

## Phase 5: Co-location Completion

### Goals

- Finish component co-location rollout with tracker-driven execution.

### Actions

- Prioritize high-traffic components still marked `Not Started` / `In Progress`.
- For each migrated component:
- Ensure folder structure follows template.
- Ensure `index.ts` export policy is correct.
- Ensure tests are colocated and passing.

### Exit Criteria

- Priority components migrated and tracker statuses updated.

---

## Phase 6: Data Flow and Error UX Consistency

### Goals

- Keep RTK Query as the only server state transport.
- Standardize user-facing async/error behavior.

### Actions

- Ensure all API responses are unwrapped via `transformResponse` from `ApiResponse<T>`.
- Ensure mutation flows use `useApiErrorHandler` and avoid ad-hoc casting.
- Standardize loading/error/empty handling via `QueryRenderer` where appropriate.

### Exit Criteria

- Common query/mutation UX is uniform across key pages.

---

## Phase 7: Internationalization, Accessibility, and Performance

### Goals

- Raise quality on user-facing experience and production behavior.

### Actions

- Complete i18n extraction and parity in `en.json` and `bg.json`.
- Add accessibility checks for key flows (auth, cart, checkout).
- Enforce performance budget checks:
- main chunk <= 200KB gzip
- lazy chunks <= 150KB gzip
- Monitor LCP/INP/CLS targets.

### Exit Criteria

- No new hardcoded user strings in changed files.
- Accessibility baseline tests exist for critical pages.
- Budget checks are visible and enforced in CI.

---

## Execution Order (Recommended PR Sequence)

1. PR-1: Type safety debt (`any` cleanup in test utils and tests).
2. PR-2: Import alias normalization (`@` paths).
3. PR-3: Inline SVG to icon-library cleanup.
4. PR-4 to PR-n: Co-location backlog (high-priority components first).
5. Follow-up: QueryRenderer + error handling consistency pass.
6. Final: i18n/a11y/perf budget hardening.

---

## Master Checklist

## P0 Blocking

- [ ] All API calls use RTK Query (`baseApi.injectEndpoints`), no direct `fetch`/`axios` usage in features.
- [ ] All API envelopes are unwrapped with `transformResponse`.
- [ ] All components/hooks are typed without `any` in production code.
- [ ] Mutation errors handled through `useApiErrorHandler`.
- [ ] SVG icon usage centralized in `src/shared/components/icons/`.

## P1 Expected

- [ ] All non-sibling imports use `@` alias.
- [ ] Co-location pattern applied for touched components.
- [ ] Barrel export policy respected:
- [ ] feature component `index.ts` exports component only
- [ ] shared/ui component `index.ts` exports component + public types
- [ ] Query loading/error/empty states standardized via `QueryRenderer` where practical.

## P2 Recommended

- [ ] URL persistence implemented for filter/search state.
- [ ] Route-level lazy loading applied to non-critical routes.
- [ ] Optimistic UX used for cart/wishlist where beneficial.
- [ ] Duplicate mutation submission protection added on sensitive forms.

## Testing and Quality

- [ ] `npm run lint` passes in storefront.
- [ ] `npm run build` passes in storefront.
- [ ] Unit tests pass for touched scopes.
- [ ] Integration tests cover cart/checkout core flows.
- [ ] E2E smoke path remains green.

## i18n / A11y / Perf

- [ ] All new user-visible strings are translated in `en` and `bg`.
- [ ] Key forms and icon-only controls pass accessibility checks.
- [ ] Performance budgets enforced in CI.

## Governance

- [ ] Resolve and document form-validation standard (Zod vs custom-only) as a single project rule.
- [ ] Keep this document updated as phases complete.

---

## Suggested Tracking Updates in Existing Docs

- Reflect per-component migration status in `COLOCATION_ADOPTION_TRACKER.md`.
- Keep implementation notes and rationale in `COLOCATION_MIGRATION.md`.
- Use this file for cross-cutting, guide-wide compliance tracking.
