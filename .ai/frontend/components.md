# Frontend Components Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Keep components focused, composable, and easy to test.

## Core Rules
1. Components should be presentational by default.
2. Keep API/data-fetch logic in feature API files and hooks.
3. Move complex state transitions to hooks or slices.
4. Keep props explicit and strongly typed.
5. Prefer composition over large monolithic components.

## Real Code References
- Shared UI components: `src/frontend/storefront/src/shared/components/`
- Feature UI components: `src/frontend/storefront/src/features/`
- App entry composition: `src/frontend/storefront/src/App.tsx`

## Practical Guidance
- Co-locate feature-specific components under their feature folder.
- Keep shared components domain-agnostic in `shared/components`.
- Avoid passing raw API envelopes into UI components.

## Common Mistakes
- Embedding endpoint calls directly in component bodies.
- One component handling layout, data loading, mutation, and error parsing all at once.
- Reusing a component across features with incompatible prop contracts.

## Checklist
- [ ] Component API is typed and minimal.
- [ ] Data fetching delegated to RTK Query hooks or feature hooks.
- [ ] Shared component is domain-agnostic.
- [ ] Rendering logic stays clear and testable.
