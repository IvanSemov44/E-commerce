# Frontend Styling Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Keep styling consistent, maintainable, and accessible.

## Core Rules
1. Prefer CSS Modules for component-level styles.
2. Reuse design tokens and shared variables from global styles.
3. Keep focus-visible and interaction states explicit.
4. Avoid hardcoded theme values when tokenized alternatives exist.

## Real Code References
- Global tokens/base styles: `src/frontend/storefront/src/index.css`
- Component-scoped styles:
  - `src/frontend/storefront/src/shared/components/ui/Input/Input.module.css`
  - `src/frontend/storefront/src/shared/components/ui/Button/Button.module.css`

## Common Mistakes
- Global class leakage into feature modules.
- Inconsistent spacing/color values across similar components.
- Removing focus outlines without accessible replacement.
