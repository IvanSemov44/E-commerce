# Frontend Forms Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Standardize form state, validation, submission, and error rendering for consistency.

## Core Rules
1. Use shared `useForm` hook for local form state and submission flow.
2. Use schema-based validation through `zodValidate` where applicable.
3. Keep form UI components presentational; orchestration belongs in hooks/pages.
4. Bind field labels/errors consistently through shared input components.
5. Keep submit flows async-safe with loading state.

## Real Code References
- Shared form hook: `src/frontend/storefront/src/shared/hooks/useForm.ts`
- Zod bridge utility: `src/frontend/storefront/src/shared/lib/utils/zodValidate.ts`
- Shared input component: `src/frontend/storefront/src/shared/components/ui/Input/Input.tsx`
- Auth form usage:
  - `src/frontend/storefront/src/features/auth/pages/LoginPage.tsx`
  - `src/frontend/storefront/src/features/auth/pages/RegisterPage.tsx`
- Checkout orchestration hook: `src/frontend/storefront/src/features/checkout/hooks/useCheckout.ts`

## Practical Guidance
- Keep validation rules near schema definitions for each feature domain.
- Return first field error per input to reduce noise.
- Keep server error handling integrated with shared API error hooks.

## Common Mistakes
- Duplicating ad-hoc form state logic in each page.
- Mixing validation, API mutation, and rendering concerns in one large component.
- Showing inconsistent error shapes across forms.

## Checklist
- [ ] Form uses shared hook and typed values.
- [ ] Validation strategy is explicit and reusable.
- [ ] Submit flow manages `isSubmitting` correctly.
- [ ] Inputs expose labels and field-level errors consistently.
