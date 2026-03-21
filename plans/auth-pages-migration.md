# Auth Pages Migration Plan

**Goal:** Migrate LoginPage, ForgotPasswordPage, and ResetPasswordPage to the RegisterPage
pattern. Each page must match RegisterPage structurally, stylistically, and in test coverage.

**Template:** `src/frontend/storefront/src/features/auth/pages/RegisterPage/`
**Doc reference:** `.ai/frontend/auth-forms.md` — read this first, it documents every decision.

---

## What "Done" Means Per Page

Every page must have this exact folder shape when finished:

```
PageName/
  PageName.tsx              ← JSX only, destructures hook
  PageName.module.css       ← design tokens only, no raw color/weight/spacing
  pageNameSchema.ts         ← Zod schema + exported type (page-specific)
  usePageNameForm.ts        ← useActionState hook, all logic here
  index.ts                  ← export { PageName } from './PageName'  (already exists)
  __tests__/
    pageNameSchema.test.ts  ← pure Zod unit tests, no React
    PageName.test.tsx       ← component integration tests
```

---

## Critical Rules (Violations = Wrong)

### useActionState — not useForm
All three pages currently use `useForm` + `zodValidate`. Replace entirely with `useActionState`.
The `useForm` hook and `zodValidate` imports must be removed. Do not keep them.

### CSS tokens — no raw aliases
The current CSS files use variables that do not exist: `--color-text-primary`, `--color-bg-secondary`,
`--color-border`, `--color-primary`, `--color-primary-dark`. These silently produce no style in dark
mode. Replace every one. Full mapping:

| Wrong (current) | Correct token |
|---|---|
| `--color-bg-secondary` | `--surface-secondary` |
| `--color-bg-primary` | `--surface-primary` |
| `--color-text-primary` | `--text-primary` |
| `--color-text-secondary` | `--text-secondary` |
| `--color-border` | `--border-default` |
| `--color-primary` | `--color-brand` |
| `--color-primary-dark` | `--color-brand-hover` |
| `--color-success-light` | `--color-success-50` |
| `--color-success-border` | `--color-success-500` |
| `--color-success` (text) | `--color-success-600` |
| `box-shadow: 0 10px 30px rgba(0,0,0,0.1)` | `var(--shadow-lg)` |
| `font-weight: 600` | `var(--font-semibold)` |
| `font-weight: 500` | `var(--font-medium)` |
| `transition: color 0.2s ease` | `var(--transition-colors)` |
| `border-radius: 8px` | `var(--radius-lg)` |
| `padding: 2rem 1rem` | `var(--space-8) var(--space-4)` |
| `padding: 1rem` | `var(--space-4)` |
| `padding: 1.5rem` | `var(--space-6)` |
| `gap: 1.25rem` | `var(--space-5)` |
| `gap: 1.5rem` | `var(--space-6)` |
| `gap: 1rem` | `var(--space-4)` |
| `margin-bottom: 1.5rem` | `var(--space-6)` |
| `margin-bottom: 1rem` | `var(--space-4)` |
| `padding-top: 1rem` | `var(--space-4)` |
| `font-size: 0.875rem` | `var(--text-sm)` |
| `font-size: 1.5rem` (breakpoint override) | `var(--text-2xl)` |
| `font-size: 1.125rem` | `var(--text-lg)` |

**Accepted raw values** (no token exists): `max-width: 400px`, `min-height: calc(100vh - 200px)`,
`font-size: 1.75rem` (title), `font-size: 0.95rem` (description — between sm and base).

### Form element requirements
Every form must have:
```tsx
<h1 id="page-title">...</h1>
<form action={action} noValidate aria-labelledby="page-title">
```
Every `<Input>` must have `disabled={isPending}`.

### Schema colocation
`createLoginSchema`, `createForgotPasswordSchema`, `createResetPasswordSchema` are currently in
`authSchemas.ts` but are page-specific (each used by exactly one page). Move each to its page
folder during migration. After all three pages are done, remove the three `create*Schema`
exports from `authSchemas.ts` and their `*FormValues` types. Keep `emailField` and `passwordField`
— they are the shared builders used by the page schemas.

---

## Page 1 — LoginPage

**Current state:** Single file, uses `useForm`, no password toggle, no `noValidate`, no ARIA.

### New files to create

#### `LoginPage/loginSchema.ts`
Move `createLoginSchema` and `LoginFormValues` from `authSchemas.ts` here.
Import `emailField` from `@/features/auth/schemas/authSchemas`.
Password field on login uses only `.min(1, t('auth.passwordRequired'))` — no strength rules
(strength rules are for registration only).

```typescript
import { z } from 'zod';
import type { TFunction } from 'i18next';
import { emailField } from '@/features/auth/schemas/authSchemas';

export const createLoginSchema = (t: TFunction) =>
  z.object({
    email: emailField(t),
    password: z.string().min(1, { error: t('auth.passwordRequired'), abort: true }),
  });

export type LoginFormValues = z.infer<ReturnType<typeof createLoginSchema>>;
```

#### `LoginPage/useLoginForm.ts`
```typescript
import { useState, useMemo, useActionState, useRef } from 'react';
import { useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useLoginMutation } from '@/features/auth/api/authApi';
import { useAppDispatch } from '@/shared/lib/store';
import { loginSuccess } from '@/features/auth/slices/authSlice';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { useToast, useApiErrorHandler } from '@/shared/hooks';
import { parseBackendFieldErrors } from '@/shared/lib/utils';
import { usePasswordVisibility } from '@/features/auth/hooks/usePasswordVisibility';
import { createLoginSchema } from './loginSchema';
import type { LoginFormValues } from './loginSchema';

type FieldErrors = Partial<Record<keyof LoginFormValues, string>>;

const FIELD_ORDER: ReadonlyArray<keyof LoginFormValues> = ['email', 'password'];

const INITIAL_VALUES: LoginFormValues = { email: '', password: '' };

// Login errors do not map to individual fields (security: don't reveal which field is wrong)
const CODE_TO_FIELD: Partial<Record<string, keyof LoginFormValues>> = {};

export function useLoginForm() {
  const { t } = useTranslation();
  const [login] = useLoginMutation();
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { toast } = useToast();
  const { handleError } = useApiErrorHandler();

  const schema = useMemo(() => createLoginSchema(t), [t]);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [values, setValues] = useState<LoginFormValues>(INITIAL_VALUES);
  const password = usePasswordVisibility();
  const fieldRefs = useRef<Partial<Record<keyof LoginFormValues, HTMLInputElement>>>({});

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setValues((prev) => ({ ...prev, [name]: value }));
    setFieldErrors((prev) => ({ ...prev, [name as keyof LoginFormValues]: undefined }));
  };

  const handleBlur = (e: React.FocusEvent<HTMLInputElement>) => {
    const name = e.target.name as keyof LoginFormValues;
    const result = schema.safeParse(values);
    const fieldIssue = result.success
      ? undefined
      : result.error.issues.find((issue) => issue.path[0] === name);
    setFieldErrors((prev) => ({ ...prev, [name]: fieldIssue?.message }));
  };

  const focusFirstError = (errors: FieldErrors) => {
    const first = FIELD_ORDER.find((f) => errors[f]);
    if (first) fieldRefs.current[first]?.focus();
  };

  const [, action, isPending] = useActionState(async () => {
    const result = schema.safeParse(values);

    if (!result.success) {
      const errors: FieldErrors = {};
      for (const issue of result.error.issues) {
        const field = issue.path[0] as keyof LoginFormValues;
        if (field !== undefined && !errors[field]) errors[field] = issue.message;
      }
      setFieldErrors(errors);
      focusFirstError(errors);
      return null;
    }

    try {
      const response = await login(result.data).unwrap();
      if (response.success && response.user) {
        dispatch(loginSuccess(response.user));
        toast.success(t('auth.loginSuccess'));
        navigate(ROUTE_PATHS.home);
      } else {
        toast.error(response.message || t('auth.loginError'));
      }
    } catch (err) {
      const backendErrors = parseBackendFieldErrors(err, CODE_TO_FIELD);
      if (backendErrors) {
        setFieldErrors(backendErrors);
        focusFirstError(backendErrors);
      } else {
        handleError(err, t('auth.loginError'));
      }
    }

    return null;
  }, null);

  return { values, fieldErrors, password, fieldRefs, handleChange, handleBlur, action, isPending };
}
```

### Modify `LoginPage/LoginPage.tsx`
Replace the entire component. Key changes from current:
- Remove `useForm`, `useLoginMutation`, all logic — move to hook
- Import `useLoginForm` from `./useLoginForm`
- Add `id="login-title"` to h1, `noValidate aria-labelledby="login-title"` to form
- Change `onSubmit={form.handleSubmit}` → `action={action}`
- Add `disabled={isPending}` to all inputs and button
- Add `autoComplete="email"`, `autoCapitalize="none"`, `autoCorrect="off"` to email input
- Add password visibility toggle via `PasswordToggleButton` and `type={password.inputType}`
- Add `autoComplete="current-password"`, `autoCapitalize="none"`, `spellCheck={false}` to password
- Add `ref` callbacks to both inputs via `fieldRefs`
- Button label: `isPending ? t('auth.loggingIn') : t('auth.login')`

### Modify `LoginPage/LoginPage.module.css`
Apply full CSS token mapping from the table above. `.forgotPassword` class only needs layout
tokens (it has no color). The `.card` max-width stays `400px` (no token).

### Create `LoginPage/__tests__/loginSchema.test.ts`
Pure Zod tests. Use `i18n.t.bind(i18n)`. Test:
- email: required (`abort: true` stops further checks), invalid format
- password: required only (no strength rules on login)

### Create `LoginPage/__tests__/LoginPage.test.tsx`
Mock `useLoginMutation` and `useNavigate`. Required test cases:
1. Renders email field, password field, submit button, forgot password link.
2. Shows client-side required errors on empty submit — mutation not called.
3. Success: dispatches `loginSuccess`, Redux `isAuthenticated` true, navigates to `/`.
4. Backend error (e.g. 401 with message) → toast shown via `handleError`, no navigation.
5. Error clears when user starts typing in that field.
6. Password visibility toggle changes input type between `password` and `text`.

---

## Page 2 — ForgotPasswordPage

**Current state:** Single file, uses `useForm`, manages success state locally in component.
No `noValidate`, no ARIA. No password fields — simplest of the three.

### New files to create

#### `ForgotPasswordPage/forgotPasswordSchema.ts`
Move `createForgotPasswordSchema` and `ForgotPasswordFormValues` from `authSchemas.ts`.
Import `emailField` from `@/features/auth/schemas/authSchemas`.

```typescript
import { z } from 'zod';
import type { TFunction } from 'i18next';
import { emailField } from '@/features/auth/schemas/authSchemas';

export const createForgotPasswordSchema = (t: TFunction) =>
  z.object({ email: emailField(t) });

export type ForgotPasswordFormValues = z.infer<ReturnType<typeof createForgotPasswordSchema>>;
```

#### `ForgotPasswordPage/useForgotPasswordForm.ts`
Note: `success` state moves from component into the hook (component should be pure JSX).

```typescript
import { useState, useMemo, useActionState, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import type { ChangeEvent, FocusEvent } from 'react';
import { useForgotPasswordMutation } from '@/features/auth/api/authApi';
import { useApiErrorHandler } from '@/shared/hooks';
import { createForgotPasswordSchema } from './forgotPasswordSchema';
import type { ForgotPasswordFormValues } from './forgotPasswordSchema';

type FieldErrors = Partial<Record<keyof ForgotPasswordFormValues, string>>;

const INITIAL_VALUES: ForgotPasswordFormValues = { email: '' };

export function useForgotPasswordForm() {
  const { t } = useTranslation();
  const [forgotPassword] = useForgotPasswordMutation();
  const { handleError } = useApiErrorHandler();

  const schema = useMemo(() => createForgotPasswordSchema(t), [t]);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [values, setValues] = useState<ForgotPasswordFormValues>(INITIAL_VALUES);
  const [submitted, setSubmitted] = useState(false);
  const emailRef = useRef<HTMLInputElement | undefined>(undefined);

  const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
    setValues((prev) => ({ ...prev, email: e.target.value }));
    setFieldErrors({});
  };

  const handleBlur = (e: FocusEvent<HTMLInputElement>) => {
    const result = schema.safeParse(values);
    const fieldIssue = result.success
      ? undefined
      : result.error.issues.find((issue) => issue.path[0] === 'email');
    setFieldErrors({ email: fieldIssue?.message });
  };

  const [, action, isPending] = useActionState(async () => {
    const result = schema.safeParse(values);

    if (!result.success) {
      const message = result.error.issues[0]?.message;
      setFieldErrors({ email: message });
      emailRef.current?.focus();
      return null;
    }

    try {
      await forgotPassword({ email: result.data.email }).unwrap();
      setSubmitted(true);
    } catch (err) {
      // Security: always show success to avoid email enumeration — but surface network errors
      handleError(err, t('common.error'));
    }

    return null;
  }, null);

  return { values, fieldErrors, submitted, emailRef, handleChange, handleBlur, action, isPending };
}
```

### Modify `ForgotPasswordPage/ForgotPasswordPage.tsx`
- Remove `useForm`, `useForgotPasswordMutation`, local `success` state
- Import `useForgotPasswordForm` from `./useForgotPasswordForm`
- Rename `success` → `submitted` (from hook)
- Add `id="forgot-password-title"` to h1, `noValidate aria-labelledby="forgot-password-title"` to form
- Change `onSubmit` → `action={action}`
- Add `disabled={isPending}` to input and button
- Add `autoComplete="email"`, `autoCapitalize="none"`, `autoCorrect="off"` to email
- Add `ref` callback for `emailRef`

### Modify `ForgotPasswordPage/ForgotPasswordPage.module.css`
Apply full CSS token mapping. No success box tokens (`--color-success-*`) exist. Use:
- Success box background: `--color-success-50`
- Success box border: `1px solid var(--color-success-500)`
- Success text: `var(--color-success-600)`
- `border-radius: 8px` → `var(--radius-lg)`
- `margin: 1rem 0` → `var(--space-4) 0`
- `gap: 1.5rem` (in `.centered`) → `var(--space-6)`
- `margin-bottom: 0.5rem` → `var(--space-2)`

### Create `ForgotPasswordPage/__tests__/forgotPasswordSchema.test.ts`
Pure Zod tests:
- email: required, invalid format, valid email passes.

### Create `ForgotPasswordPage/__tests__/ForgotPasswordPage.test.tsx`
Mock `useForgotPasswordMutation`. Required test cases:
1. Renders email field and submit button.
2. Shows email required error on empty submit.
3. On success response: shows success state (check email message visible, form not visible).
4. On API error: shows error toast, form stays visible.
5. Error clears when user types.

---

## Page 3 — ResetPasswordPage

**Current state:** Single file, uses `useForm`, no password toggles, manages success and
invalid-link states in component. Most complex of the three due to URL params and two password
fields.

### New files to create

#### `ResetPasswordPage/resetPasswordSchema.ts`
Move `createResetPasswordSchema` and `ResetPasswordFormValues` from `authSchemas.ts`.
Import `passwordField` from `@/features/auth/schemas/authSchemas`.
Fix: current schema has `confirmPassword: z.string().min(1, t('auth.confirmPasswordRequired'))`
but the key `auth.confirmPasswordRequired` does NOT exist in en.json. Use the same constructed
string pattern as RegisterPage: `` `${t('auth.confirmPassword')} ${t('common.required').toLowerCase()}` ``.

```typescript
import { z } from 'zod';
import type { TFunction } from 'i18next';
import { passwordField } from '@/features/auth/schemas/authSchemas';

export const createResetPasswordSchema = (t: TFunction) =>
  z
    .object({
      password: passwordField(t),
      confirmPassword: z
        .string()
        .min(1, {
          error: `${t('auth.confirmPassword')} ${t('common.required').toLowerCase()}`,
          abort: true,
        }),
    })
    .refine((data) => data.password === data.confirmPassword, {
      message: t('auth.passwordsDoNotMatch'),
      path: ['confirmPassword'],
    });

export type ResetPasswordFormValues = z.infer<ReturnType<typeof createResetPasswordSchema>>;
```

#### `ResetPasswordPage/useResetPasswordForm.ts`
Note: `success` state and URL param reading move into the hook.

```typescript
import { useState, useMemo, useActionState, useRef } from 'react';
import { useNavigate, useSearchParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import type { ChangeEvent, FocusEvent } from 'react';
import { useResetPasswordMutation } from '@/features/auth/api/authApi';
import { useToast, useApiErrorHandler } from '@/shared/hooks';
import { parseBackendFieldErrors } from '@/shared/lib/utils';
import { usePasswordVisibility } from '@/features/auth/hooks/usePasswordVisibility';
import { createResetPasswordSchema } from './resetPasswordSchema';
import type { ResetPasswordFormValues } from './resetPasswordSchema';

type FieldErrors = Partial<Record<keyof ResetPasswordFormValues, string>>;

const FIELD_ORDER: ReadonlyArray<keyof ResetPasswordFormValues> = ['password', 'confirmPassword'];
const INITIAL_VALUES: ResetPasswordFormValues = { password: '', confirmPassword: '' };

// Map backend token error codes to fields if the API returns them
const CODE_TO_FIELD: Partial<Record<string, keyof ResetPasswordFormValues>> = {};

export function useResetPasswordForm() {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [resetPassword] = useResetPasswordMutation();
  const { toast } = useToast();
  const { handleError } = useApiErrorHandler();

  const email = searchParams.get('email') ?? '';
  const token = searchParams.get('token') ?? '';

  const schema = useMemo(() => createResetPasswordSchema(t), [t]);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [values, setValues] = useState<ResetPasswordFormValues>(INITIAL_VALUES);
  const [submitted, setSubmitted] = useState(false);
  const password = usePasswordVisibility();
  const confirmPassword = usePasswordVisibility();
  const fieldRefs = useRef<Partial<Record<keyof ResetPasswordFormValues, HTMLInputElement>>>({});

  const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    const newValues = { ...values, [name]: value } as ResetPasswordFormValues;
    setValues(newValues);

    const updates: FieldErrors = { [name as keyof ResetPasswordFormValues]: undefined };
    if (name === 'password' && (newValues.confirmPassword || fieldErrors.confirmPassword)) {
      updates.confirmPassword =
        newValues.confirmPassword !== value ? t('auth.passwordsDoNotMatch') : undefined;
    }
    setFieldErrors((prev) => ({ ...prev, ...updates }));
  };

  const handleBlur = (e: FocusEvent<HTMLInputElement>) => {
    const name = e.target.name as keyof ResetPasswordFormValues;
    const result = schema.safeParse(values);
    const fieldIssue = result.success
      ? undefined
      : result.error.issues.find((issue) => issue.path[0] === name);
    setFieldErrors((prev) => ({ ...prev, [name]: fieldIssue?.message }));
  };

  const focusFirstError = (errors: FieldErrors) => {
    const first = FIELD_ORDER.find((f) => errors[f]);
    if (first) fieldRefs.current[first]?.focus();
  };

  const [, action, isPending] = useActionState(async () => {
    const result = schema.safeParse(values);

    if (!result.success) {
      const errors: FieldErrors = {};
      for (const issue of result.error.issues) {
        const field = issue.path[0] as keyof ResetPasswordFormValues;
        if (field !== undefined && !errors[field]) errors[field] = issue.message;
      }
      setFieldErrors(errors);
      focusFirstError(errors);
      return null;
    }

    try {
      await resetPassword({ email, token, newPassword: result.data.password }).unwrap();
      setSubmitted(true);
      toast.success(t('resetPassword.passwordResetSuccess'));
      navigate(ROUTE_PATHS.login);
    } catch (err) {
      const backendErrors = parseBackendFieldErrors(err, CODE_TO_FIELD);
      if (backendErrors) {
        setFieldErrors(backendErrors);
        focusFirstError(backendErrors);
      } else {
        handleError(err, t('resetPassword.failed'));
      }
    }

    return null;
  }, null);

  return {
    values,
    fieldErrors,
    password,
    confirmPassword,
    fieldRefs,
    submitted,
    hasValidParams: Boolean(email && token),
    handleChange,
    handleBlur,
    action,
    isPending,
  };
}
```

### Modify `ResetPasswordPage/ResetPasswordPage.tsx`
- Remove all logic from component, import `useResetPasswordForm`
- Replace `success` local state with `submitted` from hook
- Replace `email && token` check with `hasValidParams` from hook
- Add `id="reset-password-title"` to h1, `noValidate aria-labelledby="reset-password-title"` to form
- Change `onSubmit` → `action={action}`
- Add `disabled={isPending}` to all inputs and button
- Add `PasswordToggleButton` to both password inputs (same pattern as RegisterPage)
- Add `autoComplete="new-password"`, `autoCapitalize="none"`, `spellCheck={false}` to both password inputs
- Add `ref` callbacks for both inputs via `fieldRefs`
- Import `PasswordStrengthIndicator` and render after password field

### Modify `ResetPasswordPage/ResetPasswordPage.module.css`
Apply full CSS token mapping. Same success box treatment as ForgotPasswordPage.

### Create `ResetPasswordPage/__tests__/resetPasswordSchema.test.ts`
Pure Zod tests. Use `i18n.t.bind(i18n)`. Test:
- password: required (abort stops further), min 8, uppercase, lowercase, digit
- confirmPassword: required
- confirmPassword: mismatch rejected
- valid data passes

### Create `ResetPasswordPage/__tests__/ResetPasswordPage.test.tsx`
Mock `useResetPasswordMutation`, `useNavigate`, `useSearchParams`. Required test cases:
1. With valid URL params: renders both password fields and submit button.
2. Without URL params (no email/token): renders invalid-link message, no form.
3. Client-side required errors on empty submit.
4. Success: navigates to `/login`, success state shown.
5. Backend error: inline field error shown.
6. Password strength indicator appears after typing in password field.
7. Password/confirm mismatch shows error.
8. Error clears when user starts typing.

---

## After All Three Pages Are Done — Cleanup

Remove from `authSchemas.ts`:
- `createLoginSchema` and `LoginFormValues`
- `createForgotPasswordSchema` and `ForgotPasswordFormValues`
- `createResetPasswordSchema` and `ResetPasswordFormValues`

Keep in `authSchemas.ts`:
- `emailField`
- `passwordField`

Run a project-wide grep for any remaining imports of the removed exports to confirm nothing is broken:
```
grep -r "createLoginSchema\|createForgotPasswordSchema\|createResetPasswordSchema" src/frontend/storefront/src
```
Expected: only the three page schema files themselves should appear (not authSchemas.ts).

---

## Test Mocking Patterns

### Mutation mock (same for all pages)
```typescript
const mockMutationFn = vi.fn();
vi.mock('@/features/auth/api/authApi', () => ({
  useLoginMutation: () => [mockMutationFn, {}],
  // or useForgotPasswordMutation / useResetPasswordMutation
}));
```

Success response:
```typescript
mockMutationFn.mockReturnValue({
  unwrap: vi.fn().mockResolvedValue({ success: true, user: { ... } }),
});
```

Backend error (field-level):
```typescript
mockMutationFn.mockReturnValue({
  unwrap: vi.fn().mockRejectedValue({
    data: { errors: { Email: ['Email is already in use'] } },
  }),
});
```

Backend error (business code):
```typescript
mockMutationFn.mockReturnValue({
  unwrap: vi.fn().mockRejectedValue({
    data: { errorDetails: { code: 'INVALID_TOKEN', message: 'Token has expired' } },
  }),
});
```

### Navigate mock
```typescript
const mockNavigate = vi.fn();
vi.mock('react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router')>();
  return { ...actual, useNavigate: () => mockNavigate };
});
```

### SearchParams mock (ResetPasswordPage only)
```typescript
vi.mock('react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router')>();
  return {
    ...actual,
    useNavigate: () => mockNavigate,
    useSearchParams: () => [new URLSearchParams('email=test@example.com&token=abc123'), vi.fn()],
  };
});
```

For the invalid-link test, override with empty params:
```typescript
// Inside the specific test:
vi.mocked(useSearchParams).mockReturnValue([new URLSearchParams(), vi.fn()]);
// Or mock at module level with empty and override per-test
```

### Render helper
```typescript
import { renderWithProviders } from '@/shared/lib/test/test-utils';
// Use for all component tests — provides Redux store + BrowserRouter
renderWithProviders(<LoginPage />);
```

---

## Verification

After implementing all three pages, run the full auth test suite:
```bash
cd src/frontend/storefront
npx vitest run src/features/auth
```

Expected output: all tests in all auth page `__tests__/` folders pass.

Also verify the existing RegisterPage tests still pass (they should be untouched):
```bash
npx vitest run src/features/auth/pages/RegisterPage/__tests__
```

Check for stale imports after authSchemas.ts cleanup:
```bash
npx tsc --noEmit
```
No TypeScript errors = migration complete.
