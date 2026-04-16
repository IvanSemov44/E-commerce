# Frontend Accessibility Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Ensure interactive UI remains keyboard-usable, screen-reader friendly, and regression-resistant.

## Core Rules
1. Every interactive control must have an accessible name.
2. Prefer semantic HTML elements and correct form associations (`label` + `htmlFor`).
3. Use ARIA only to enhance semantics, not replace them.
4. Keep focus-visible states clear and consistent.
5. Add tests for accessibility-critical interactions on reusable components.

## Real Code References
- Global focus styles: `src/frontend/storefront/src/index.css`
- Accessible input labeling: `src/frontend/storefront/src/shared/components/ui/Input/Input.tsx`
- Raw checkbox with validation ARIA: `src/frontend/storefront/src/features/auth/pages/RegisterPage/RegisterPage.tsx`
- Payment method radiogroup semantics: `src/frontend/storefront/src/features/checkout/components/PaymentMethodSelector/PaymentMethodSelector.tsx`
- Accessibility component tests:
  - `src/frontend/storefront/src/shared/components/ui/Input/__tests__/Input.test.tsx`
  - `src/frontend/storefront/src/features/products/components/ProductImageGallery/ProductImageGallery.test.tsx`

## Practical Guidance
- Use `aria-live` for async status updates where users need announcement feedback.
- Mark decorative icons as `aria-hidden="true"`.
- Keep keyboard focus order aligned with visual flow.
- The `Input` component wires `aria-invalid` and `aria-describedby` automatically. For raw
  `<input type="checkbox">` with validation, add these manually — see `.ai/frontend/storefront/auth-forms.md`
  (Raw Checkbox Accessibility section).
- Use `aria-invalid={value || undefined}` not `aria-invalid={false}` — the attribute must be
  absent when valid, not set to `false`.

## Common Mistakes
- Clickable elements without labels.
- Using generic `div` containers where native controls are appropriate.
- Styling away focus outlines without an accessible replacement.

## Checklist
- [ ] Inputs and controls have accessible names.
- [ ] Form controls use proper label associations.
- [ ] Focus-visible styles are present.
- [ ] Key reusable components include accessibility assertions in tests.
