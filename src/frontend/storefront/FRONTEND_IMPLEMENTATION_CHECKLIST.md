# Frontend Implementation Checklist

Source plan: `COMPREHENSIVE_FRONTEND_IMPLEMENTATION_PLAN.md`

## Phase Status

- [x] Phase 1: Foundation and enforcement
- [ ] Phase 2: Type safety cleanup
- [ ] Phase 3: Import path standardization
- [ ] Phase 4: Icon and SVG compliance
- [ ] Phase 5: Co-location completion
- [ ] Phase 6: Data flow and error UX consistency
- [ ] Phase 7: i18n, accessibility, performance

## P0 Blocking

- [x] RTK Query is used for all feature API calls
- [ ] `transformResponse` unwraps `ApiResponse<T>`
- [x] No untyped `any` in production app code
- [ ] Mutations use `useApiErrorHandler`
- [ ] Production inline SVG replaced by shared icon components

## P1 Expected

- [ ] `@` alias used for all non-sibling imports
- [ ] Co-location pattern applied on touched components
- [ ] Feature component barrel exports are minimal (component-only)
- [ ] Shared UI barrel exports include required public types
- [ ] Loading/error/empty UX is consistent (prefer `QueryRenderer`)

## P2 Recommended

- [ ] URL query persistence for filters/search
- [ ] Route-level lazy loading for non-critical routes
- [ ] Optimistic updates for cart/wishlist where useful
- [ ] Duplicate mutation submission guards in checkout/payment paths

## Tests and Gates

- [x] `npm run lint` passes in storefront
- [x] `npm run build` passes in storefront
- [x] Unit tests pass for touched components/hooks
- [ ] Integration tests cover cart/checkout core paths
- [ ] E2E smoke tests remain green

## i18n / A11y / Performance

- [ ] New UI strings added in both `en.json` and `bg.json`
- [ ] Accessibility checks added for key pages/forms
- [ ] Performance budgets enforced in CI

## Governance

- [ ] Resolve form validation standard conflict (Zod vs custom-only)
- [ ] Keep `COLOCATION_ADOPTION_TRACKER.md` updated weekly
- [ ] Keep `COLOCATION_MIGRATION.md` aligned with implementation decisions

## Run Snapshot (2026-03-07)

- [x] Lint clean: zero errors and zero warnings
- [x] Build passes (`tsc -b && vite build`)
- [x] Vitest suite passes: `80` test files, `704` tests
- [x] Addressed warning cleanup targets (function length/complexity and react-refresh false-positive)
