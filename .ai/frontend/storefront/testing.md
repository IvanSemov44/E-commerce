# Frontend Testing Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Standardize frontend testing across unit, integration, and E2E layers.

## Core Rules
1. Use Vitest + Testing Library for component and hook behavior.
2. Test user-observable behavior, not implementation details.
3. Cover loading, error, empty, and success states for data-driven UI.
4. Keep E2E tests focused on critical purchase/auth journeys.

## Real Code References
- Frontend tests root: `src/frontend/storefront/src/**/__tests__/`
- Hook test example: `src/frontend/storefront/src/shared/hooks/__tests__/useForm.test.ts`
- Component a11y test example: `src/frontend/storefront/src/shared/components/ui/Input/__tests__/Input.test.tsx`
- E2E script entry: `scripts/e2e.ps1`

## Common Mistakes
- Snapshot-only testing for dynamic components.
- Ignoring error states from RTK Query hooks.
- Making tests brittle with implementation-coupled selectors.
