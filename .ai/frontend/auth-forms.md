# Auth Forms — RegisterPage Pattern

Updated: 2026-03-21
Owner: @ivans

## Purpose
Document the RegisterPage as the canonical template for auth forms. Apply this pattern when
refactoring LoginPage, ForgotPasswordPage, and ResetPasswordPage.

---

## Folder Structure

Every auth page is self-contained:

```
RegisterPage/
  RegisterPage.tsx          # Component — JSX only, no logic
  RegisterPage.module.css   # Styles — design tokens only
  registerSchema.ts         # Page-specific Zod schema + exported type
  useRegisterForm.ts        # Page-specific form hook
  index.ts                  # Barrel: export { RegisterPage }
  __tests__/
    registerSchema.test.ts  # Pure Zod unit tests
    RegisterPage.test.tsx   # Component integration tests
```

**Colocation rule:** schema and hook live in the page folder. Promote to shared only if two or
more pages need identical logic.

---

## Layer 1 — Schema (`registerSchema.ts`)

- Imports shared field builders (`emailField`, `passwordField`) from `authSchemas.ts`.
- Defines page-specific fields inline (firstName, lastName, termsAccepted).
- Cross-field refinements (password match, terms acceptance) at the object level — they run only after all field checks pass.
- `abort: true` on required checks stops further checks for that field when it is empty.
- Exports a named type: `RegisterFormValues = z.infer<ReturnType<typeof createRegisterSchema>>`.

**Shared field builders** in `authSchemas.ts` — reused across multiple page schemas:
```typescript
export const emailField = (t) =>
  z.string()
    .min(1, { error: t('auth.emailRequired'), abort: true })
    .check(z.email({ error: t('auth.emailInvalid') }));

export const passwordField = (t) =>
  z.string()
    .min(1, { error: t('auth.passwordRequired'), abort: true })
    .min(8, t('auth.passwordMinLength'))
    .regex(/[A-Z]/, t('auth.passwordUppercase'))
    .regex(/[a-z]/, t('auth.passwordLowercase'))
    .regex(/[0-9]/, t('auth.passwordDigit'));
```

---

## Layer 2 — Hook (`useRegisterForm.ts`)

Owns all state and side effects. The component destructures and renders only.

| Concern | Implementation |
|---|---|
| Submit lifecycle | `useActionState(async () => {...}, null)` |
| Form state | `useState<RegisterFormValues>(INITIAL_VALUES)` |
| Validation | `schema.safeParse(values)` on submit; per-field on blur |
| Password visibility | `usePasswordVisibility()` — one instance per password field |
| Focus on error | Not implemented — `react-hooks/refs` and `react-hooks/immutability` rules prohibit passing refs through custom hooks. Errors are shown inline; browser scrolls naturally. |
| Backend field errors | `parseBackendFieldErrors(err, CODE_TO_FIELD)` |
| Generic backend error | `handleError(err, fallbackMessage)` → toast |

**`CODE_TO_FIELD`** maps business error codes to field names:
```typescript
const CODE_TO_FIELD: Partial<Record<string, keyof RegisterFormValues>> = {
  DUPLICATE_EMAIL: 'email',
};
```

**Cross-field validation in `handleChange`:** when `password` changes, immediately re-validate
`confirmPassword` if it already has a value or an existing error — prevents stale mismatch
errors persisting while the user re-types the password.

**`FIELD_ORDER`** constant was removed along with `focusFirstError` — see Focus on error row above.

---

## Layer 3 — Component (`RegisterPage.tsx`)

Pure JSX. Destructures the hook, renders nothing else.

**Required on every auth form element:**

```tsx
<h1 id="register-title">...</h1>
<form action={action} noValidate aria-labelledby="register-title">
```

**Required on every `<Input>`:**
```tsx
<Input
  disabled={isPending}
  required
  error={fieldErrors.fieldName}
  ref={(el) => { fieldRefs.current.fieldName = el ?? undefined; }}
/>
```

**Password inputs additionally need:**
```tsx
<Input
  type={password.inputType}
  autoComplete="new-password"
  autoCapitalize="none"
  spellCheck={false}
  trailingElement={
    <PasswordToggleButton
      show={password.show}
      ariaLabel={password.ariaLabel}
      onClick={password.toggle}
    />
  }
/>
```

**Email input additionally needs:**
```tsx
<Input type="email" autoCapitalize="none" autoCorrect="off" autoComplete="email" />
```

---

## Shared Auth Components

| Component / Hook | Location | Purpose |
|---|---|---|
| `PasswordToggleButton` | `features/auth/components/PasswordToggleButton/` | Eye icon button — owns its own styles and icons. Props: `{ show, ariaLabel, onClick }`. |
| `PasswordStrengthIndicator` | `features/auth/components/PasswordStrengthIndicator/` | Segmented strength bar + rule checklist. Prop: `{ password: string }`. Returns `null` on empty. |
| `usePasswordVisibility` | `features/auth/hooks/usePasswordVisibility.ts` | Returns `{ show, toggle, inputType, ariaLabel }`. One instance per password field. |

---

## Backend Error Handling

`parseBackendFieldErrors` handles two ASP.NET error shapes and returns a field error map, or
`null` if neither shape matches (caller falls through to generic toast).

| Shape | HTTP | Structure | Handling |
|---|---|---|---|
| ASP.NET validation | 400 | `{ errors: { Email: ["msg"] } }` | PascalCase keys → camelCase fields |
| Business error | 409 | `{ errorDetails: { code: "DUPLICATE_EMAIL", message: "..." } }` | Mapped via `CODE_TO_FIELD` |
| Everything else | any | — | `handleError` → generic toast |

---

## Raw Checkbox Accessibility

The `Input` component handles `aria-invalid` and `aria-describedby` automatically.
For raw `<input type="checkbox">` with validation errors (e.g., terms), wire it manually:

```tsx
<input
  type="checkbox"
  aria-required="true"
  aria-invalid={!!fieldErrors.termsAccepted || undefined}
  aria-describedby={fieldErrors.termsAccepted ? 'terms-error' : undefined}
/>
{fieldErrors.termsAccepted && (
  <p id="terms-error" role="alert">{fieldErrors.termsAccepted}</p>
)}
```

`aria-invalid={value || undefined}` — removes the attribute entirely when valid; `false` is
invalid HTML for `aria-invalid`.

---

## CSS Conventions

Use design tokens exclusively. Old aliases (`--color-text-*`, `--color-bg-*`, `--color-border`,
`--color-primary`) do not exist — they silently produce no style in dark mode.

| Category | Token pattern | Example |
|---|---|---|
| Surfaces | `--surface-{primary\|secondary\|tertiary}` | backgrounds |
| Text | `--text-{primary\|secondary\|muted}` | copy |
| Borders | `--border-{default\|hover\|focus}` | dividers, inputs |
| Brand color | `--color-brand`, `--color-brand-hover` | links, accents |
| Spacing | `--space-{N}` | padding, gap, margin |
| Font size | `--text-{xs\|sm\|base\|lg\|...}` | all font sizes |
| Font weight | `--font-{normal\|medium\|semibold\|bold}` | all weights |
| Shadows | `--shadow-{sm\|md\|lg\|xl}` | card elevation |
| Transitions | `--transition-colors`, `--transition-shadow` | hover effects |

**Two accepted raw values** (no token exists): `max-width: 500px` (card width), `min-height: calc(100vh - 200px)`.

---

## Test Strategy

**Schema tests** — pure unit, no React, no DOM:
- Import `i18n.t.bind(i18n)` from the real i18n instance (initialized in test setup).
- One test per rule: required, max length, email format, each password regex, cross-field match, terms.
- Helper: `firstErrors(data)` collects the first issue per field path for clean assertions.

**Component tests** — integration via `renderWithProviders`:
```typescript
vi.mock('@/features/auth/api/authApi', () => ({
  useRegisterMutation: () => [mockRegister, {}],
}));
vi.mock('react-router', async (importOriginal) => ({
  ...await importOriginal(),
  useNavigate: () => mockNavigate,
}));
```

Required test cases:
1. Renders all fields and submit button.
2. Shows client-side required errors on empty submit — mutation never called.
3. Shows terms error when checkbox unchecked — mutation never called.
4. Success: mutation called with correct payload, Redux `isAuthenticated` true, navigate called.
5. `DUPLICATE_EMAIL` backend error shown inline on email field with `aria-invalid`.
6. ASP.NET validation error shown inline on the matching field.
7. Field error clears immediately when user starts typing.

---

## Migration Checklist — Applying to Another Auth Page

- [ ] Create page folder with: component, CSS, schema, hook, `index.ts`, `__tests__/`.
- [ ] Schema: shared field builders + page-specific fields + `abort: true` on required checks.
- [ ] Hook: `useActionState`, `FIELD_ORDER`, `focusFirstError`, `CODE_TO_FIELD`, `parseBackendFieldErrors`.
- [ ] Component: `noValidate`, `aria-labelledby`, all inputs `disabled={isPending}` and `required`.
- [ ] Password inputs: `spellCheck={false}`, `autoCapitalize="none"`, `PasswordToggleButton`.
- [ ] Required checkboxes: `aria-required`, `aria-invalid`, `aria-describedby` + error `id`.
- [ ] CSS: tokens only — verify with grep for `--color-text`, `--color-bg`, `--color-border`.
- [ ] Tests: schema unit tests + component integration tests covering all submit paths.
