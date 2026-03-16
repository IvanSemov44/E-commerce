# Products Feature — Level Up to 10/10

Current score: **7.2 / 10**

Three groups, commit after each. No new dependencies, no new patterns — fix what's already there.

---

## Group A — Quick fixes (naming · types · dead code)

Seven isolated, low-risk changes. All mechanical.

### A1 — Rename `debouncedSearch` → `search` in `useProductFilters`

**File:** `hooks/useProductFilters.ts`

The hook does zero debouncing — it's a plain `searchParams.get('search')`. The name misleads
every reader. Rename the variable and the `ProductFiltersState` interface field.

**Also update consumer:**
- `pages/ProductsPage/ProductsPage.tsx` line 32: `search: filters.debouncedSearch` → `search: filters.search`

---

### A2 — Rename `parseFloat_` → `parseOptionalFloat`

**Files:** `hooks/useProductFilters.ts`, `components/ProductsToolbar/ProductsToolbar.tsx`

Trailing underscore is not a TypeScript convention — it's a Python habit. Both files define the
same function independently (duplication fixed in Group B). Rename in both.

---

### A3 — Remove magic number `stockQuantity = 99` in `ProductCard`

**File:** `components/ProductCard/ProductCard.tsx` line 33

```ts
// Before
stockQuantity = 99,

// After — prop is always provided; remove the default entirely
stockQuantity,
```

`ProductCardProps` should already have `stockQuantity` as required, or give it a semantically
correct fallback. `99` is not documented, not in constants, and wrong on products with stock > 99.
Check `ProductCard.types.ts` — if the field is already required, just drop the default.

---

### A4 — Type `sortBy` properly in `GetProductsQueryParams`

**File:** `types/index.ts` line 17

```ts
// Before
sortBy?: string;

// After
sortBy?: SortBy;
```

Import `SortBy` from `../constants`. The type already exists — it's just not wired here.
This gives compile-time safety on every call site.

---

### A5 — Remove local `Review` interface from `ReviewList`

**File:** `components/ReviewList/ReviewList.tsx` lines 10–17

The file defines its own `interface Review` instead of importing the shared `ProductReview` from
`@/shared/types`. If the shared type gains a field, `ReviewList` silently falls behind.

```ts
// Remove the local interface
// Import instead:
import type { ProductReview } from '@/shared/types';

// Update props:
interface ReviewListProps {
  reviews: ProductReview[];
  ...
}
```

---

### A6 — Remove redundant `refetchReviews()` call in `ProductDetailPage`

**File:** `pages/ProductDetailPage/ProductDetailPage.tsx` line 71

```tsx
// Before
<ReviewForm productId={product.id} onSuccess={() => refetchReviews()} />

// After
<ReviewForm productId={product.id} />
```

`reviewsApi.createReview` already uses `invalidatesTags: [{ type: 'Review', id: 'LIST' }]`.
RTK Query auto-refetches the list after a successful mutation. The manual call fires a second
redundant network request. Remove `refetchReviews` from `useProductData` return too if nothing
else uses it, or keep it for external use but stop passing it here.

---

### A7 — Standardize error handling in `useWishlistActions`

**File:** `components/ProductActions/ProductActions.hooks.ts` lines 41–43

```ts
// Before — silent, no log, no trace
} catch {
  // mutation error state handled by isAdding / isRemoving
}

// After — consistent with useCartActions in the same file
} catch (err) {
  logger.error('useWishlistActions', 'Failed to toggle wishlist', err);
}
```

`useCartActions` in the same file uses `logger.error`. `useWishlistToggle` in `ProductCard.hooks.ts`
uses `handleError`. The three wishlist/cart hooks have three different error patterns. At minimum
the two hooks in the same file must be consistent.

---

## Group B — Architecture (duplication · constants · dead code)

Three structural improvements.

### B1 — Extract shared parsing utils

**Create:** `utils/parsing.ts`

Both `hooks/useProductFilters.ts` and `components/ProductsToolbar/ProductsToolbar.tsx` define
identical `parseSortBy` and `parseOptionalFloat` (formerly `parseFloat_`) functions. Neither imports
from the other.

```ts
// utils/parsing.ts
import { VALID_SORT_BY, type SortBy } from '../constants';

export function parseOptionalFloat(value: string | null): number | undefined {
  return value ? parseFloat(value) : undefined;
}

export function parseSortBy(value: string | null): SortBy {
  return value && (VALID_SORT_BY as readonly string[]).includes(value)
    ? (value as SortBy)
    : 'newest';
}
```

Delete the local copies in both files, import from `../utils/parsing`.

**Export from `utils/index.ts`** (create if not exists) so future utils follow the same pattern.

---

### B2 — Add missing constants for magic numbers

**File:** `constants.ts`

Two hardcoded timing values exist in component files with no documentation:

```ts
// ProductsToolbar.tsx line 41
const debouncedSearch = useDebounce(searchInput, 500);

// ProductCard.hooks.ts line 136
setTimeout(() => setIsAddingToCart(false), 300);
```

Add to `constants.ts`:

```ts
/** Delay (ms) before search input writes to URL */
export const SEARCH_DEBOUNCE_MS = 500;

/** Delay (ms) before "adding..." spinner resets on ProductCard quick-add */
export const QUICK_ADD_RESET_MS = 300;
```

Update the two files to import and use the named constants.

---

### B3 — Remove `sortOrder` from `GetProductsQueryParams`

**File:** `types/index.ts` line 18

```ts
sortOrder?: string;  // ← never passed by any caller, never used by the API
```

Grep the entire frontend for `sortOrder` — no component or hook sets it. It's dead surface area
on the public interface. Remove it.

---

## Group C — Test coverage (pages)

The single biggest quality gap. Two page components, zero tests.

### C1 — `ProductsPage.test.tsx`

**Create:** `pages/ProductsPage/ProductsPage.test.tsx`

Mock `useGetProductsQuery` and `useGetCategoriesQuery`. Use `MemoryRouter` with `initialEntries`
to drive URL state.

Required test cases:

| # | Test | Setup |
|---|------|-------|
| 1 | renders page header | default |
| 2 | shows skeleton while loading | `isLoading: true` |
| 3 | shows error state | `error: { status: 500 }` |
| 4 | shows empty state when no products and no filters | `data: { items: [] }`, no URL params |
| 5 | shows "no matches" empty state when filters are active | `data: { items: [] }`, `?search=xyz` |
| 6 | renders product grid when data is returned | `data: { items: [...] }` |
| 7 | renders sidebar filters | default |
| 8 | shows refetch indicator during background fetch | `isFetching: true, isLoading: false` |

Pattern — use `MemoryRouter initialEntries` + `renderWithProviders`:

```tsx
renderWithProviders(
  <MemoryRouter initialEntries={['/?search=shoes']}>
    <ProductsPage />
  </MemoryRouter>,
  { preloadedState }
);
```

---

### C2 — `ProductDetailPage.test.tsx`

**Create:** `pages/ProductDetailPage/ProductDetailPage.test.tsx`

Mock `useGetProductBySlugQuery` and `useGetProductReviewsQuery`. Use `MemoryRouter` with route
`/products/:slug` matching.

Required test cases:

| # | Test | Setup |
|---|------|-------|
| 1 | shows skeleton while product is loading | `isLoading: true` |
| 2 | shows error state when product fails | `error: { status: 404 }` |
| 3 | shows empty state when product not found | `data: null` |
| 4 | renders product info when loaded | full mock product |
| 5 | renders image gallery | product with images |
| 6 | shows review form when authenticated | `isAuthenticated: true` |
| 7 | hides review form when not authenticated | `isAuthenticated: false` |
| 8 | renders review list | reviews data |
| 9 | shows reviews loading skeleton | `reviewsLoading: true` |

---

## Execution order

```
Group A → commit "fix(products): naming, types, dead code (Group A)"
Group B → commit "refactor(products): extract parsing utils, add missing constants (Group B)"
Group C → commit "test(products): add page-level test coverage (Group C)"
```

After Group C: run `vitest run src/features/products` — all tests must pass before push.

---

## What this gets you

| Area | Before | After |
|---|---|---|
| TypeScript types | 7/10 | 10/10 |
| Naming clarity | 7/10 | 10/10 |
| Code duplication | 7/10 | 10/10 |
| Error handling consistency | 6/10 | 9/10 |
| Test coverage | 6/10 | 9.5/10 |
| **Overall** | **7.2/10** | **~9.5/10** |

The remaining 0.5 gap (9.5 not 10) is `usePerformanceMonitor` being ceremonial without a real
dashboard behind it, and `memo()` on `ProductCard` being partially defeated by `useAppSelector`
reads inside it. Both require infrastructure changes outside this feature.
