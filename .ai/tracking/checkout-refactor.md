# Checkout Feature Refactor Plan

**Created:** 2026-03-16
**Owner:** @ivans
**Status:** 🔴 Not Started
**Scope:** `src/frontend/storefront/src/features/checkout/`

---

## Context for the AI Agent Reading This

You are about to refactor the checkout feature of a React + TypeScript + RTK Query e-commerce storefront. Read this entire document before touching any file. Each issue has a severity, a precise description of what is wrong, the exact files and lines affected, a step-by-step fix, a copy-paste AI prompt to implement it, and a verification checklist.

**Rules you must follow:**
- Follow the project architecture: RTK Query for server state, Redux slices for UI/client state, `useForm` + Zod for form validation.
- Never introduce manual `fetch` or `axios` calls. All API calls go through `baseApi.injectEndpoints`.
- `useCheckout.ts` is the single hook consumed by `CheckoutPage`. Keep that contract intact while splitting its internals.
- Run `npx tsc --noEmit` and `npx vitest run src/features/checkout` after every change before moving to the next issue.
- Do not add features. Fix only what is described.
- After all issues are resolved, run the full test suite: `npx vitest run`.

**Project stack:**
- React 18, TypeScript, Vite
- RTK Query (`baseApi.injectEndpoints`)
- Redux Toolkit (`useAppSelector`, `useAppDispatch`)
- `react-i18next` for translations
- Zod for schema validation
- CSS Modules for styling
- Vitest + Testing Library for tests

---

## Issue Index

| # | Title | Severity | Effort | Status |
|---|-------|----------|--------|--------|
| 1 | God hook — split `useCheckout.ts` | 🔴 Critical | High | ⬜ |
| 2 | `CheckoutPage.module.css` missing 9 class definitions | 🔴 Critical | Medium | ⬜ |
| 3 | `ShippingFormData` interface duplicated | 🟠 High | Low | ⬜ |
| 4 | `OrderSummary.hooks.ts` is dead code | 🟠 High | Low | ⬜ |
| 5 | `isGuestOrder` prop typed but never rendered | 🟡 Medium | Low | ⬜ |
| 6 | `setFormData` backward-compat adapter is pointless | 🟡 Medium | Low | ⬜ |
| 7 | Three `eslint-disable react-hooks/exhaustive-deps` | 🟡 Medium | Medium | ⬜ |
| 8 | `PaymentMethodSelector` missing `index.ts` | ⚪ Low | Trivial | ⬜ |
| 9 | Hardcoded 5-country list buried in JSX | 🟡 Medium | Low | ⬜ |
| 10 | Fake `maxStock: 99` shown as real stock data | 🟡 Medium | Medium | ⬜ |
| 11 | Stock check error conflates infra failure with business failure | 🟡 Medium | Low | ⬜ |
| 12 | Telemetry fires before knowing the outcome | ⚪ Low | Low | ⬜ |
| 13 | `useCheckout.test.tsx` — all mock paths are wrong | 🟠 High | Medium | ⬜ |

---

## Issue 1 — God Hook: Split `useCheckout.ts`

### What is wrong

`src/features/checkout/hooks/useCheckout.ts` is 351 lines and has the ESLint `max-lines-per-function` rule disabled. It owns seven unrelated responsibilities simultaneously:

1. Shipping form state + Zod validation
2. LocalStorage draft persistence and restoration
3. Authenticated user form pre-fill
4. Dual cart resolution (local Redux vs backend RTK Query)
5. Promo code state + validation API call
6. Inventory stock check mutation
7. Order submission, cart clearing, and success state
8. Telemetry tracking across all lifecycle events

This violates Single Responsibility Principle. It is impossible to unit test individual behaviors because every test must mock the entire dependency graph. When one concern changes (e.g., cart sync logic), the entire hook must be reread.

### Files affected

- `src/features/checkout/hooks/useCheckout.ts` (entire file)

### Target structure after fix

```
src/features/checkout/hooks/
├── useCheckout.ts          ← thin orchestrator (assembles sub-hooks, exposes UseCheckoutReturn)
├── useCheckoutForm.ts      ← form values, Zod validation, localStorage draft, user pre-fill
├── useCheckoutCart.ts      ← dual cart logic (local vs backend), subtotal calculation
├── useCheckoutOrder.ts     ← stock check, order submission, cart clearing, success state
├── useCheckoutPromo.ts     ← promo code state + validatePromoCode API call
└── __tests__/
    ├── useCheckout.test.tsx       (integration — the existing file, fixed)
    ├── useCheckoutForm.test.ts    (new)
    ├── useCheckoutCart.test.ts    (new)
    ├── useCheckoutOrder.test.ts   (new)
    └── useCheckoutPromo.test.ts   (new)
```

### Rules for the split

- `useCheckout.ts` must still export `useCheckout(): UseCheckoutReturn` — the `CheckoutPage` must not change.
- Each sub-hook is a pure, independently importable hook.
- `UseCheckoutReturn` interface stays in `useCheckout.ts` as the public contract.
- Shared types (`ShippingFormData`, `PromoCodeValidation`) move to `src/features/checkout/checkout.types.ts`.
- Remove `// eslint-disable-next-line max-lines-per-function` once the split is complete.

### Step-by-step fix

1. Create `src/features/checkout/checkout.types.ts` with all shared interfaces (see Issue 3).
2. Create `useCheckoutForm.ts` — extract: `useLocalStorage(CHECKOUT_DRAFT_KEY)`, `useForm(...)`, the draft persistence `useEffect`, the user pre-fill `useEffect`. Return `{ form, shippingDraft }`.
3. Create `useCheckoutCart.ts` — extract: `useGetCartQuery`, `useCartSync`, the `cartItems` and `subtotal` `useMemo`s. Return `{ cartItems, subtotal }`.
4. Create `useCheckoutPromo.ts` — extract: `promoCode`, `promoCodeValidation`, `validatingPromoCode` state, `handleApplyPromoCode`, `handleRemovePromoCode`. Return the full promo object.
5. Create `useCheckoutOrder.ts` — extract: `createOrder`, `clearCartApi`, `checkAvailabilityMutation`, `orderComplete`, `orderNumber`, `isGuestOrder`, `error`, `handleFormSubmit`. Return `{ orderComplete, orderNumber, error, isGuestOrder, handleFormSubmit }`.
6. Rewrite `useCheckout.ts` to compose the four sub-hooks and assemble the final return object. Should be under 60 lines.
7. Delete the `// eslint-disable-line max-lines-per-function` comment.

### AI Prompt

```
Refactor src/features/checkout/hooks/useCheckout.ts by splitting it into focused sub-hooks.

Steps:
1. Create src/features/checkout/checkout.types.ts with ShippingFormData, PromoCodeValidation, and UseCheckoutReturn interfaces (moved from useCheckout.ts). Export all of them.

2. Create src/features/checkout/hooks/useCheckoutForm.ts that extracts:
   - CHECKOUT_DRAFT_KEY constant
   - useLocalStorage for the draft
   - useForm initialization with zodValidate(checkoutSchema)
   - useEffect that persists form.values to localStorage
   - useEffect that pre-fills from authenticated user (with useRef guard instead of eslint-disable)
   - Returns: { form, setFormData }
   where setFormData is REMOVED (do not carry it forward — expose form.setFieldValue or form.setValues directly)

3. Create src/features/checkout/hooks/useCheckoutCart.ts that extracts:
   - useGetCartQuery (skip when !isAuthenticated)
   - useCartSync
   - cartItems useMemo (dual cart strategy)
   - subtotal useMemo
   - DEFAULT_CART_ITEM_MAX_STOCK constant
   - Returns: { cartItems, subtotal }

4. Create src/features/checkout/hooks/useCheckoutPromo.ts that extracts:
   - promoCode, promoCodeValidation, validatingPromoCode state
   - handleApplyPromoCode callback
   - handleRemovePromoCode callback
   - Returns: { promoCode, setPromoCode, promoCodeValidation, validatingPromoCode, handleApplyPromoCode, handleRemovePromoCode }

5. Create src/features/checkout/hooks/useCheckoutOrder.ts that extracts:
   - useCreateOrderMutation
   - useClearCartMutation
   - useCheckAvailabilityMutation
   - orderComplete, orderNumber, isGuestOrder, error state
   - handleFormSubmit (stock check → create order → clear cart → set success)
   - telemetry.track calls
   - Receives cartItems, subtotal, promoCode, promoCodeValidation, paymentMethod as arguments
   - Returns: { orderComplete, orderNumber, error, isGuestOrder, handleFormSubmit }

6. Rewrite useCheckout.ts to:
   - Import and compose all four sub-hooks
   - Import useAppSelector (for isAuthenticated, user), useAppDispatch (for clearCart)
   - Assemble and return UseCheckoutReturn
   - Be under 70 lines total
   - Remove all eslint-disable comments

7. Update src/features/checkout/checkout.types.ts to be the single source for all checkout types. Remove type declarations from useCheckout.ts, CheckoutForm.types.ts overlap.

The public API of useCheckout() — the UseCheckoutReturn shape — must not change. CheckoutPage.tsx must compile without any modifications.

After completing: run npx tsc --noEmit and confirm zero errors.
```

### Verification checklist

- [ ] `npx tsc --noEmit` passes with zero errors
- [ ] `useCheckout.ts` is under 70 lines
- [ ] No `eslint-disable max-lines-per-function` anywhere in the hooks folder
- [ ] No `eslint-disable react-hooks/exhaustive-deps` anywhere in the hooks folder
- [ ] `CheckoutPage.tsx` is unchanged
- [ ] Each sub-hook file is under 80 lines
- [ ] `checkout.types.ts` exists and exports `ShippingFormData`, `PromoCodeValidation`, `UseCheckoutReturn`
- [ ] `npx vitest run src/features/checkout` passes

---

## Issue 2 — `CheckoutPage.module.css` Missing 9 Class Definitions

### What is wrong

`CheckoutPage.tsx` references 10 CSS module classes. Only `.container` is defined in the CSS file. The remaining 9 return `undefined` at runtime, making them no-ops. The two-column checkout layout, header section, trust signals wrapper, form title, and success content have zero styling applied. This is a silent regression — there are no TypeScript errors because CSS Modules types are loose, and no tests cover page-level layout.

### Classes referenced in `CheckoutPage.tsx` vs defined in CSS

| Class name | Used in component | Defined in CSS |
|---|---|---|
| `.container` | ✅ | ✅ |
| `.content` | ✅ | ❌ |
| `.checkoutHeader` | ✅ | ❌ |
| `.checkoutTitle` | ✅ | ❌ |
| `.checkoutSubtitle` | ✅ | ❌ |
| `.trustSignalsWrapper` | ✅ | ❌ |
| `.grid` | ✅ | ❌ |
| `.summary` | ✅ | ❌ |
| `.successContent` | ✅ | ❌ |
| `.formTitle` | ✅ | ❌ |

### Files affected

- `src/features/checkout/pages/CheckoutPage/CheckoutPage.module.css`

### Step-by-step fix

Add all missing class definitions. Use the product pages and other feature pages in the codebase as style references for the two-column checkout layout pattern.

### AI Prompt

```
Fill in the missing CSS class definitions in:
src/features/checkout/pages/CheckoutPage/CheckoutPage.module.css

The component CheckoutPage.tsx uses these classes that are currently undefined:
.content, .checkoutHeader, .checkoutTitle, .checkoutSubtitle, .trustSignalsWrapper,
.grid, .summary, .successContent, .formTitle

Layout intent (infer from the JSX structure in CheckoutPage.tsx):
- .content: max-width wrapper inside .container, possibly with padding
- .checkoutHeader: centered text block at top of page
- .checkoutTitle: large heading (h1), should visually lead the page
- .checkoutSubtitle: muted subtitle paragraph beneath the title
- .trustSignalsWrapper: horizontal bar for trust badges, some vertical spacing
- .grid: two-column layout — left column (form) takes ~60% width, right column (summary) takes ~40%. Single column on mobile.
- .summary: the right column (sticky positioning on desktop is a good UX pattern for checkout)
- .successContent: centered flex container for the empty cart state and order success screen
- .formTitle: h2 style with icon and text inline (flex row, gap, align-items center)

Match the design language of ProductsPage.module.css for spacing scale (use rem units consistent with the rest of the app).
Add mobile-first responsive breakpoints at 768px.

Do not add vendor prefixes manually — Vite handles autoprefixer.
```

### Verification checklist

- [ ] All 10 classes exist in the CSS file
- [ ] Two-column `.grid` layout collapses to single column at ≤768px
- [ ] `.summary` has `position: sticky; top: 1rem` on desktop
- [ ] `.successContent` centers its child
- [ ] Visual inspection in browser shows a proper checkout layout
- [ ] No TypeScript or lint errors introduced

---

## Issue 3 — `ShippingFormData` Interface Duplicated

### What is wrong

`ShippingFormData` is declared twice with identical fields:

- `src/features/checkout/hooks/useCheckout.ts` lines 40–51
- `src/features/checkout/components/CheckoutForm/CheckoutForm.types.ts` lines 1–11

Additionally, `src/features/checkout/schemas/checkoutSchemas.ts` already exports `CheckoutFormValues` inferred from the Zod schema. Three sources of truth for the same shape will inevitably drift.

### Files affected

- `src/features/checkout/hooks/useCheckout.ts`
- `src/features/checkout/components/CheckoutForm/CheckoutForm.types.ts`
- `src/features/checkout/schemas/checkoutSchemas.ts`
- `src/features/checkout/checkout.types.ts` (to be created in Issue 1)

### Step-by-step fix

1. In `checkoutSchemas.ts`, the existing `export type CheckoutFormValues = z.infer<typeof checkoutSchema>` is already correct — this is the canonical source.
2. In `checkout.types.ts` (created in Issue 1), re-export it: `export type { CheckoutFormValues as ShippingFormData } from '../schemas/checkoutSchemas'`
3. Remove the duplicate declaration from `useCheckout.ts`
4. Update `CheckoutForm.types.ts` to import `ShippingFormData` from `checkout.types.ts` instead of declaring it

### AI Prompt

```
Eliminate the duplicate ShippingFormData interface in the checkout feature.

1. In src/features/checkout/schemas/checkoutSchemas.ts, verify that CheckoutFormValues is exported as:
   export type CheckoutFormValues = z.infer<typeof checkoutSchema>

2. In src/features/checkout/checkout.types.ts (created during the useCheckout split), add:
   export type { CheckoutFormValues as ShippingFormData } from '../schemas/checkoutSchemas';

3. In src/features/checkout/hooks/useCheckout.ts, remove the local ShippingFormData interface declaration. Import ShippingFormData from '../checkout.types'.

4. In src/features/checkout/components/CheckoutForm/CheckoutForm.types.ts, remove the local ShippingFormData interface declaration. Import ShippingFormData from '../../checkout.types'.

5. Run npx tsc --noEmit. Fix any resulting type errors (there should be none if the shapes are identical, which they are).

Do not change any runtime behavior. This is a pure type consolidation.
```

### Verification checklist

- [ ] `ShippingFormData` is declared exactly once (in `checkout.types.ts` via re-export)
- [ ] `useCheckout.ts` imports `ShippingFormData` rather than declaring it
- [ ] `CheckoutForm.types.ts` imports `ShippingFormData` rather than declaring it
- [ ] `npx tsc --noEmit` passes

---

## Issue 4 — `OrderSummary.hooks.ts` is Dead Code

### What is wrong

`src/features/checkout/components/OrderSummary/OrderSummary.hooks.ts` exports two hooks that are never imported anywhere:

- `usePromoCode` — wraps three callback props in event-handler adapters. Adds zero logic.
- `useOrderCalculations` — re-implements the math already done by `calculateOrderTotals` from `@/shared/lib/utils/orderCalculations`. The `useCheckout.ts` hook uses the shared utility directly. This hook duplicates it.

Neither hook appears in any import across the entire codebase.

### Files affected

- `src/features/checkout/components/OrderSummary/OrderSummary.hooks.ts` — delete
- `src/features/checkout/components/OrderSummary/index.ts` — remove any re-export if present

### AI Prompt

```
Delete src/features/checkout/components/OrderSummary/OrderSummary.hooks.ts.

Before deleting, run a codebase-wide search for any imports of this file:
  grep -r "OrderSummary.hooks" src/

If any imports are found, resolve them first by inlining the logic at the call site.
If no imports are found (expected), delete the file.

Also check src/features/checkout/components/OrderSummary/index.ts — if it re-exports anything from OrderSummary.hooks, remove that line.

Run npx tsc --noEmit after deletion to confirm nothing broke.
```

### Verification checklist

- [ ] `OrderSummary.hooks.ts` file no longer exists
- [ ] Zero grep matches for `OrderSummary.hooks` across the codebase
- [ ] `npx tsc --noEmit` passes

---

## Issue 5 — `isGuestOrder` Prop Typed But Never Rendered

### What is wrong

`OrderSuccess.types.ts` declares `isGuestOrder: boolean`. The prop is passed from `CheckoutPage` and from tests. But inside `OrderSuccess.tsx` the prop is accepted and immediately ignored — no conditional rendering, no account creation CTA, nothing.

This is an unfinished feature stub. It creates a misleading API: callers believe they are controlling behavior, but they are not.

The intent was clearly to show authenticated users a standard success screen and offer guest users a "Create an account to track your order" CTA. That feature must either be implemented or the prop must be removed.

### Files affected

- `src/features/checkout/components/OrderSuccess/OrderSuccess.tsx`
- `src/features/checkout/components/OrderSuccess/OrderSuccess.types.ts`
- `src/features/checkout/pages/CheckoutPage/CheckoutPage.tsx` (passes the prop)
- `src/features/checkout/components/OrderSuccess/OrderSuccess.test.tsx` (passes the prop)

### Option A — Implement the feature (recommended)

### AI Prompt (Option A)

```
Implement the isGuestOrder feature in OrderSuccess.tsx.

When isGuestOrder is true, render an additional section below the order confirmation
that contains:
- A heading: t('orderSuccess.createAccount')
- A short description: t('orderSuccess.createAccountDescription')
- A link/button to /register with text t('orderSuccess.signUp')

When isGuestOrder is false, render nothing extra.

Add i18n keys to the translation file at:
src/frontend/storefront/public/locales/en/translation.json
under the orderSuccess namespace:
  "createAccount": "Save your order history"
  "createAccountDescription": "Create a free account to track this order and future purchases."
  "signUp": "Create Account"

Add a test case to OrderSuccess.test.tsx:
  it('shows account creation CTA for guest orders', ...)
  it('does not show account creation CTA for authenticated orders', ...)

Run npx tsc --noEmit and npx vitest run src/features/checkout/components/OrderSuccess.
```

### Option B — Remove the prop

```
Remove the isGuestOrder prop from the checkout feature.

1. In OrderSuccess.types.ts: remove isGuestOrder from OrderSuccessProps.
2. In OrderSuccess.tsx: remove the destructured prop (it is already unused in JSX).
3. In CheckoutPage.tsx: remove isGuestOrder={isGuestOrder} from <OrderSuccess>.
4. In useCheckout.ts: remove isGuestOrder from UseCheckoutReturn interface and state.
5. In OrderSuccess.test.tsx: remove isGuestOrder from all render calls.

Run npx tsc --noEmit.
```

### Verification checklist

- [ ] `isGuestOrder` is either fully implemented (Option A) or fully removed (Option B) — no half-states
- [ ] All tests pass
- [ ] No TypeScript errors

---

## Issue 6 — `setFormData` Backward-Compat Adapter is Pointless

### What is wrong

In `useCheckout.ts`:

```ts
// Adapter for backward compatibility
const setFormData = useCallback(
  (data: Partial<ShippingFormData>) => {
    form.setValues({ ...form.values, ...data });
  },
  [form]
);
```

Problems:
1. The comment says "backward compatibility" but there is no external API to be backward compatible with. `useCheckout` is a feature-internal hook.
2. The manual spread `{ ...form.values, ...data }` defeats the `useForm` hook's own partial update support. If `useForm.setValues` accepts partials (it likely does), this double-merges.
3. `[form]` in the dependency array means this `useCallback` recreates on every render anyway, making `useCallback` pointless.

### AI Prompt

```
Remove the setFormData adapter from useCheckout.ts.

1. Delete the setFormData useCallback block from useCheckout.ts.
2. Remove setFormData from UseCheckoutReturn interface.
3. Remove setFormData from the return object of useCheckout.
4. In CheckoutPage.tsx, replace onFormDataChange={setFormData} with onFormDataChange={form.setValues}.
   (Adjust to match whatever the actual useForm API exposes for partial field updates.)
5. In CheckoutForm.types.ts, verify the onFormDataChange prop type still matches.
6. Run npx tsc --noEmit.
```

### Verification checklist

- [ ] No `setFormData` anywhere in the checkout feature
- [ ] `CheckoutPage.tsx` uses the form hook's native setter directly
- [ ] `npx tsc --noEmit` passes

---

## Issue 7 — Three `eslint-disable react-hooks/exhaustive-deps` Suppressions

### What is wrong

Three `useEffect` hooks in `useCheckout.ts` suppress the exhaustive-deps rule. Each is a potential stale closure bug:

**Effect 1 — Draft persistence:**
```ts
useEffect(() => {
  setShippingDraft(form.values);
}, [form.values]); // eslint-disable-line react-hooks/exhaustive-deps
```
`setShippingDraft` is missing from deps. If `useLocalStorage` returns a new function reference on each render, this will never re-run.

**Effect 2 — Telemetry on mount:**
```ts
useEffect(() => {
  telemetry.track('checkout.view', { isAuthenticated });
}, []); // eslint-disable-line react-hooks/exhaustive-deps
```
`isAuthenticated` is captured at mount time. If auth state resolves async (common with token refresh), the telemetry event fires with the wrong value. Use a `useRef` to capture the latest value at fire time, or pass it via a stable ref.

**Effect 3 — User pre-fill:**
```ts
useEffect(() => {
  if (isAuthenticated && user && !form.values.firstName) {
    form.setValues({ ...form.values, ... });
  }
}, [isAuthenticated, user]); // eslint-disable-line react-hooks/exhaustive-deps
```
`form` is in the closure but not in deps. This is the most dangerous: if `form.values` changes between renders, the spread `{ ...form.values, ...}` in the effect reads a stale snapshot.

### AI Prompt

```
Fix the three useEffect exhaustive-deps suppressions in useCheckout.ts (or its split sub-hooks after Issue 1 is resolved).

Effect 1 — Draft persistence:
  Add setShippingDraft to the dependency array. Confirm that useLocalStorage returns a stable setter (if not, wrap with useCallback in useLocalStorage, or use a useRef to hold the setter).

Effect 2 — Telemetry on mount:
  Use a ref to capture isAuthenticated at fire time:
    const isAuthenticatedRef = useRef(isAuthenticated);
    useEffect(() => { isAuthenticatedRef.current = isAuthenticated; });
    useEffect(() => {
      telemetry.track('checkout.view', { isAuthenticated: isAuthenticatedRef.current });
    }, []);
  Remove the eslint-disable comment.

Effect 3 — User pre-fill:
  Use a hasPrefilledRef to prevent re-running after the user has edited the form:
    const hasPrefilledRef = useRef(false);
    useEffect(() => {
      if (isAuthenticated && user && !hasPrefilledRef.current) {
        hasPrefilledRef.current = true;
        form.setValues((prev) => ({
          ...prev,
          firstName: prev.firstName || user.firstName || '',
          lastName: prev.lastName || user.lastName || '',
          email: prev.email || user.email || '',
          phone: prev.phone || user.phone || '',
        }));
      }
    }, [isAuthenticated, user, form.setValues]);
  This respects the user's edits, doesn't read stale form.values from a closure, and satisfies the deps rule.

After all three are fixed: run npx eslint src/features/checkout/hooks/ and confirm zero react-hooks/exhaustive-deps warnings.
```

### Verification checklist

- [ ] Zero `eslint-disable react-hooks/exhaustive-deps` comments in the checkout hooks folder
- [ ] `npx eslint src/features/checkout/hooks/` passes cleanly
- [ ] `npx vitest run src/features/checkout` passes

---

## Issue 8 — `PaymentMethodSelector` Missing `index.ts`

### What is wrong

Every other component in `src/features/checkout/components/` has an `index.ts` barrel:
- `CheckoutForm/index.ts` ✅
- `OrderSummary/index.ts` ✅
- `OrderSuccess/index.ts` ✅
- `PaymentMethodSelector/index.ts` ❌

This forces consumers to use the full internal path instead of the barrel.

### AI Prompt

```
Create src/features/checkout/components/PaymentMethodSelector/index.ts with content:
  export { default } from './PaymentMethodSelector';
```

### Verification checklist

- [ ] File exists at `PaymentMethodSelector/index.ts`
- [ ] `npx tsc --noEmit` passes

---

## Issue 9 — Hardcoded Country List in JSX

### What is wrong

`CheckoutForm.tsx` has a `<select>` with 5 hardcoded `<option>` values inline in JSX. This is business logic in a presentational component:

```tsx
<option value="US">United States</option>
<option value="CA">Canada</option>
<option value="UK">United Kingdom</option>
<option value="DE">Germany</option>
<option value="FR">France</option>
```

Problems:
- Adding a country requires editing a component file
- Not translatable (labels are hardcoded English)
- Inconsistent with how payment methods are handled (fetched from API via `useGetPaymentMethodsQuery`)

### AI Prompt

```
Extract the hardcoded country list from CheckoutForm.tsx into a constants file.

1. Create src/features/checkout/checkout.constants.ts with:
   export const SUPPORTED_COUNTRIES = [
     { code: 'US', label: 'United States' },
     { code: 'CA', label: 'Canada' },
     { code: 'GB', label: 'United Kingdom' },  // Note: ISO 3166-1 is GB, not UK
     { code: 'DE', label: 'Germany' },
     { code: 'FR', label: 'France' },
   ] as const;

2. In CheckoutForm.tsx, replace the hardcoded <option> elements with:
   {SUPPORTED_COUNTRIES.map(({ code, label }) => (
     <option key={code} value={code}>{label}</option>
   ))}

3. Fix the country code: 'UK' → 'GB' (UK is not a valid ISO 3166-1 alpha-2 code; GB is correct).

4. Update checkoutSchemas.ts if the country field has no specific validation — it is currently just z.string().min(1), which is fine.

Run npx tsc --noEmit.
```

### Verification checklist

- [ ] `SUPPORTED_COUNTRIES` constant exists in `checkout.constants.ts`
- [ ] `CheckoutForm.tsx` has no hardcoded `<option>` elements
- [ ] Country code is `GB` not `UK`
- [ ] `npx tsc --noEmit` passes

---

## Issue 10 — Fake `maxStock: 99` Misleads Users

### What is wrong

When mapping backend cart items for authenticated users, `useCheckout.ts` injects a fake stock value:

```ts
const DEFAULT_CART_ITEM_MAX_STOCK = 99;

return backendCart.items.map((item) => ({
  ...
  maxStock: DEFAULT_CART_ITEM_MAX_STOCK,  // ← the backend doesn't return stock
}));
```

The comment even admits: `// When backend cart items are mapped to CartItem, stock isn't provided by the API`.

If the `CartItem` type's `maxStock` field is used anywhere to display "X left in stock" or to cap a quantity input, users see a fabricated number. The inventory check mutation (`checkAvailabilityMutation`) is the correct place to gate on actual stock — the `maxStock` field on the UI model should be `null` or `undefined` when unknown.

### AI Prompt

```
Fix the fake maxStock value in the backend cart mapping in useCheckout.ts (or useCheckoutCart.ts after the split).

1. Check the CartItem type definition in src/features/cart/slices/cartSlice.ts.
   If maxStock is typed as `number`, change it to `number | null`.
   If it is already optional or nullable, skip this step.

2. In the backend cart mapping:
   Change: maxStock: DEFAULT_CART_ITEM_MAX_STOCK
   To:     maxStock: null

3. Delete the DEFAULT_CART_ITEM_MAX_STOCK constant (it will be unused).

4. Find all usages of maxStock across the codebase:
   grep -r "maxStock" src/
   For each usage that assumes it is a number, add a null check.
   Typical pattern: quantity input max = item.maxStock ?? 99 (keep the fallback for display only, not for business logic)

5. Run npx tsc --noEmit. Fix any resulting type errors.
```

### Verification checklist

- [ ] `DEFAULT_CART_ITEM_MAX_STOCK` constant no longer exists
- [ ] Backend cart mapping sets `maxStock: null`
- [ ] `CartItem.maxStock` is typed as `number | null` or `number | undefined`
- [ ] All UI usages of `maxStock` handle the null case
- [ ] `npx tsc --noEmit` passes

---

## Issue 11 — Stock Check Conflates Infrastructure Failure with Business Failure

### What is wrong

In `handleFormSubmit` in `useCheckout.ts`:

```ts
try {
  const stockCheckResult = await checkAvailabilityMutation(...).unwrap();
  if (!stockCheckResult.isAvailable) {
    setError(t('checkout.stockIssues', { issues: issueMessages }));
    return;
  }
  // ...order creation...
} catch (err) {
  const message = errorObj.data?.message || errorObj.message || t('checkout.orderFailed');
  setError(message);
}
```

If the stock check request fails with a network error (500, timeout), it falls into `catch` and displays `t('checkout.orderFailed')` — the same message shown when order creation fails. The user cannot tell if their items are out of stock, if the server is down, or if their order failed to submit.

### AI Prompt

```
Separate the stock check error handling from the order creation error handling in handleFormSubmit (in useCheckout.ts or useCheckoutOrder.ts after the split).

Replace the current single try/catch with two separate try/catch blocks:

async function handleFormSubmit(values: ShippingFormData) {
  setError(null);
  telemetry.track('checkout.submit_attempt', { itemCount: cartItems.length, subtotal });

  // Step 1: Stock availability check
  let stockCheckResult;
  try {
    stockCheckResult = await checkAvailabilityMutation({
      items: cartItems.map((item) => ({ productId: item.id, quantity: item.quantity })),
    }).unwrap();
  } catch {
    setError(t('checkout.stockCheckFailed'));  // infra failure — add this i18n key
    return;
  }

  if (!stockCheckResult.isAvailable) {
    const issueMessages = stockCheckResult.issues
      .map((issue) => `${issue.productName}: ${issue.message}`)
      .join(', ');
    setError(t('checkout.stockIssues', { issues: issueMessages }));
    return;
  }

  // Step 2: Order creation
  try {
    const result = await createOrder(orderData).unwrap();
    await clearCartApi().unwrap();
    dispatch(clearCart());
    setIsGuestOrder(!isAuthenticated);
    setOrderNumber(result.orderNumber);
    setShippingDraft({});
    telemetry.track('checkout.complete', { orderNumber: result.orderNumber, paymentMethod, isGuest: !isAuthenticated });
    setOrderComplete(true);
  } catch (err) {
    const errorObj = err as { data?: { message?: string }; message?: string };
    const message = errorObj.data?.message || errorObj.message || t('checkout.orderFailed');
    telemetry.track('checkout.error', { message });
    setError(message);
  }
}

Add the new i18n key to translation.json:
  "stockCheckFailed": "Unable to verify item availability. Please try again."

Also change telemetry.track('checkout.submit') to telemetry.track('checkout.submit_attempt') to distinguish intent from completion.
```

### Verification checklist

- [ ] Stock check infrastructure failure shows `t('checkout.stockCheckFailed')` not `t('checkout.orderFailed')`
- [ ] Stock business failure (items out of stock) shows `t('checkout.stockIssues')`
- [ ] Order creation failure shows `t('checkout.orderFailed')`
- [ ] `checkout.stockCheckFailed` key exists in `translation.json`
- [ ] `checkout.submit_attempt` telemetry fires, not `checkout.submit`
- [ ] `npx tsc --noEmit` passes

---

## Issue 12 — Telemetry Fires Before Knowing the Outcome

### What is wrong

```ts
telemetry.track('checkout.submit', { itemCount: cartItems.length, subtotal });
// ... stock check that might fail ...
// ... order creation that might fail ...
```

`checkout.submit` fires unconditionally at the top of `handleFormSubmit`. If the stock check or order creation fails, both `checkout.submit` and `checkout.error` are recorded — inflating the submit count in analytics dashboards. Conversion funnel calculations become inaccurate: submit rate ≠ order attempt rate.

This is addressed as part of Issue 11 (rename to `checkout.submit_attempt`). Mark complete when Issue 11 is done.

---

## Issue 13 — `useCheckout.test.tsx` All Mock Paths Are Wrong

### What is wrong

All 6 `vi.mock()` calls in `src/features/checkout/hooks/__tests__/useCheckout.test.tsx` use paths from a previous architecture that no longer exists:

| Mock path in test | Actual current path | Status |
|---|---|---|
| `'../../features/orders/api/ordersApi'` | `'@/features/orders/api'` | ❌ Wrong |
| `'../../store/api/cartApi'` | `'@/features/cart/api'` | ❌ Wrong |
| `'../../store/api/promoCodeApi'` | `'../api'` (relative) or `'@/features/checkout/api'` | ❌ Wrong |
| `'../../store/api/inventoryApi'` | `'../api'` (relative) or `'@/features/checkout/api'` | ❌ Wrong |
| `'../useCartSync'` | `'@/features/cart/hooks/useCartSync'` | ❌ Wrong |
| `'../../utils/constants'` | Does not exist | ❌ Wrong |
| `'../../utils/validation'` | Does not exist | ❌ Wrong |

Because none of the mocks intercept their targets, all 6 tests pass by testing only the initial/default state of the real hooks. They provide false coverage confidence.

### AI Prompt

```
Rewrite src/features/checkout/hooks/__tests__/useCheckout.test.tsx.

1. Fix all vi.mock() paths to match the current project structure:
   - '@/features/orders/api' for useCreateOrderMutation
   - '@/features/cart/api' for useGetCartQuery and useClearCartMutation
   - '../../api' for useValidatePromoCodeMutation and useCheckAvailabilityMutation (relative from __tests__ to api/)
   - '@/features/cart/hooks/useCartSync' for useCartSync
   - Remove the mocks for '../../utils/constants' and '../../utils/validation' — these files don't exist

2. Replace the 6 shallow tests with behavioral tests:

   Test: 'initializes with empty form and no order state'
   Test: 'uses local cart items for guest users'
   Test: 'uses backend cart items for authenticated users'
   Test: 'calculates subtotal from cart items'
   Test: 'applies validated promo code discount to totals'
   Test: 'shows stock issue error when availability check fails'
   Test: 'submits order and sets orderComplete on success'
   Test: 'sets error when order creation fails'
   Test: 'clears cart after successful order'
   Test: 'pre-fills form fields from authenticated user profile'
   Test: 'persists form draft to localStorage'

3. Use renderHookWithProviders from '@/shared/lib/test/test-utils'.
4. Use preloadedState to set auth and cart state per test.
5. Each test should set up only the mocks it needs, using mockReturnValue/mockResolvedValue.

Run npx vitest run src/features/checkout/hooks to confirm all tests pass.
```

### Verification checklist

- [ ] All `vi.mock()` paths resolve to actual files (verify with `npx tsc --noEmit` and a run)
- [ ] Zero tests pass vacuously — each test exercises a real behavior path
- [ ] Tests for success path, error path, and guest vs authenticated path all exist
- [ ] `npx vitest run src/features/checkout/hooks` passes

---

## Senior Developer Notes — Things Not in the Issue List

### Race Condition: Double Submit

The submit button in `CheckoutForm.tsx` does not disable during submission. A user with a slow connection can click "Place Order" twice, firing two `createOrder` mutations. The backend must handle idempotency, but the frontend should also guard:

- Add an `isSubmitting` boolean to `UseCheckoutReturn`
- Disable the submit button when `isSubmitting` is true
- Set `isSubmitting = true` at the start of `handleFormSubmit`, `false` in finally

### Browser Back Button and Draft Persistence

The draft is saved to localStorage under `checkout:shippingDraft`. If a user navigates away and returns, the draft is correctly restored. However, if a user completes an order and then hits the browser back button, the form will be empty (draft was cleared) but `orderComplete` will be `false` (it is local state). The user sees an empty checkout form instead of their order confirmation.

Consider: after a successful order, save the `orderNumber` in sessionStorage and redirect to `/order-confirmation/:orderNumber` instead of showing the success state inside the checkout page.

### Error Boundary

`CheckoutPage` has no error boundary. If `useCheckout` throws (e.g., a malformed API response reaching a `.map()` call), the entire page white-screens. Wrap `CheckoutPage` in a feature-level `ErrorBoundary` with a "Something went wrong — your cart is safe" fallback.

### Form Accessibility — Missing `aria-invalid`

`CheckoutForm.tsx` uses `aria-describedby` for error messages (good), but does not set `aria-invalid="true"` on fields with errors. Screen readers announce the error text but do not signal that the field itself is invalid. Add `aria-invalid={!!errors.fieldName}` to each input.

### `usePerformanceMonitor` in `CheckoutPage`

This hook is called but it is unclear what it does. If it uses a `useEffect`, confirm it does not interfere with the checkout form's `useEffect` for draft persistence (execution order matters).

### Missing `CheckoutPage.test.tsx`

There is no page-level test for `CheckoutPage`. The three conditional render paths (empty cart, order complete, checkout form) are completely untested at the integration level. Add a test file that mocks `useCheckout` and asserts each of the three states renders correctly.

### Payment Security Note

The `paymentMethod` string passed to `createOrder` is just a method identifier (`'credit_card'`, `'paypal'`, etc.) — no raw card data is handled in the frontend, which is correct. Ensure the `paymentsApi` integration test verifies this is never sent to the wrong endpoint. PCI DSS compliance means raw card details must never touch your server — they should go directly to the payment processor SDK (Stripe, etc.). If this is a training/portfolio project this is informational only.

---

## Execution Order

Do issues in this order to avoid merge conflicts and cascading type errors:

1. **Issue 3** (dedup types) — sets up `checkout.types.ts` which Issues 1 and 6 depend on
2. **Issue 4** (delete dead code) — removes a file before restructuring
3. **Issue 8** (add `index.ts`) — trivial, standalone
4. **Issue 9** (extract countries) — trivial, standalone
5. **Issue 1** (split God hook) — biggest change, do after all standalone cleanups
6. **Issue 6** (remove `setFormData`) — naturally done during Issue 1 split
7. **Issue 7** (fix `useEffect` deps) — naturally done during Issue 1 split
8. **Issue 2** (fix CSS) — independent, do any time
9. **Issue 10** (fix fake maxStock) — independent
10. **Issue 11 + 12** (error handling + telemetry) — do together
11. **Issue 5** (isGuestOrder) — do after Issue 1 so the hook return type is settled
12. **Issue 13** (fix tests) — do last, after all runtime code is correct

---

## Definition of Done

All of the following must be true before this refactor is considered complete:

- [ ] `npx tsc --noEmit` — zero errors
- [ ] `npx eslint src/features/checkout/` — zero warnings
- [ ] `npx vitest run src/features/checkout` — all tests pass, no skipped tests
- [ ] `npx vitest run` (full suite) — no regressions outside checkout
- [ ] `useCheckout.ts` is under 70 lines
- [ ] No `eslint-disable` comments in any checkout hook file
- [ ] `ShippingFormData` declared exactly once
- [ ] `OrderSummary.hooks.ts` deleted
- [ ] `CheckoutPage.module.css` defines all 10 referenced classes
- [ ] `isGuestOrder` is either implemented or removed — not half-present
- [ ] All `vi.mock()` paths in tests resolve to real files
- [ ] `PaymentMethodSelector/index.ts` exists
- [ ] Country list is in `checkout.constants.ts`
- [ ] `DEFAULT_CART_ITEM_MAX_STOCK` constant is deleted
