# Frontend Forms Standard

Updated: 2026-03-21
Owner: @ivans

## Purpose
Standardize form state, validation, submission, and error rendering across the frontend.

## Two Patterns in Use

### Pattern A — `useActionState` (canonical, use for new forms)
React 19 native. RegisterPage is the reference implementation.

- `useActionState(async () => {...}, null)` owns the submit lifecycle
- Controlled inputs with `useState`; schema colocated in the page folder
- `noValidate` + `aria-labelledby` on every form
- `disabled={isPending}` on every input during submission
- `focusFirstError` moves keyboard focus to the first failed field
- `parseBackendFieldErrors` maps backend errors to fields before falling back to toast

### Pattern B — `useForm` hook (legacy)
Used by LoginPage, ForgotPasswordPage, ResetPasswordPage. Will migrate to Pattern A.

- Shared `useForm` hook + `zodValidate` bridge utility
- Schema passed as a validator function, not colocated

## Core Rules
1. Never mix RTK Query server state with local form state.
2. Colocate page-specific schemas and hooks inside the page folder — not in shared.
3. Promote to shared only when two or more pages need the identical logic.
4. Parse backend errors before generic toast: `parseBackendFieldErrors(err, CODE_TO_FIELD)`.
5. Add `noValidate` whenever Zod handles validation — browser and Zod must not both fire.
6. Disable all inputs with `disabled={isPending}` to prevent double-submit.
7. Focus the first errored field after failed submit for keyboard accessibility.

## Real Code References
- **Pattern A full reference:** `src/frontend/storefront/src/features/auth/pages/RegisterPage/`
- **Pattern A guide:** `.ai/frontend/storefront/auth-forms.md`
- **Pattern B hook:** `src/frontend/storefront/src/shared/hooks/useForm.ts`
- **Shared field builders:** `src/frontend/storefront/src/features/auth/schemas/authSchemas.ts`
- **Backend error parser:** `src/frontend/storefront/src/shared/lib/utils/parseBackendFieldErrors.ts`
- **Input component:** `src/frontend/storefront/src/shared/components/ui/Input/Input.tsx`

## Common Mistakes
- Putting page-specific schema in `authSchemas.ts` — only shared field builders live there.
- Missing `noValidate` — browser validation fires alongside Zod, producing duplicate errors.
- Not disabling inputs during `isPending` — users can double-submit.
- Using a generic toast for backend field errors instead of showing them inline.
- Missing `abort: true` on required checks — further field checks still fire on empty input.

## Checklist
- [ ] Form has `noValidate` and `aria-labelledby`.
- [ ] All inputs `disabled={isPending}`.
- [ ] `parseBackendFieldErrors` called before falling back to generic toast.
- [ ] First errored field focused programmatically on submit failure.
- [ ] Schema and hook colocated in the page folder.
