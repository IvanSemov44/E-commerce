# Frontend Hooks Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Use custom hooks to centralize reusable behavior and reduce component complexity.

## Core Rules
1. Extract reusable UI/behavior logic into hooks.
2. Keep hooks pure in contract: input -> derived state/actions.
3. Use hooks for cross-feature concerns like error parsing or form handling.
4. Keep side effects explicit and scoped.
5. Type hook inputs/outputs strictly.

## Real Code References
- API error handling hook: `src/frontend/storefront/src/shared/hooks/useApiErrorHandler.ts`
- Form helper hook: `src/frontend/storefront/src/shared/hooks/useForm.ts`
- Typed Redux hooks: `src/frontend/storefront/src/shared/hooks/redux.ts`

## Practical Guidance
- Prefer one clear responsibility per hook.
- Return stable handlers when practical to reduce re-renders.
- Keep toast/notification integration centralized in shared hooks/utilities.

## Common Mistakes
- Hiding broad side effects in generic hooks.
- Returning loosely typed bags of values.
- Mixing unrelated concerns in one hook.

## Checklist
- [ ] Hook has one clear responsibility.
- [ ] Input/output contracts are typed.
- [ ] Side effects are explicit and predictable.
- [ ] Hook reduces component complexity meaningfully.
