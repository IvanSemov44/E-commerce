# Frontend Coding Guide - E-Commerce Storefront

**Last Updated**: March 2026 | **Status**: Production-Ready | **Stack**: React 19 + Redux Toolkit + RTK Query + Vitest

**Compiler**: React Compiler is enabled (Vite + `babel-plugin-react-compiler`)

---

## Quick Start: Rules by Priority

Every frontend contribution must follow these rules. They are tiered by severity.

### P0 - Blocking (Reject PR if missing)

#### **1. RTK Query for All API Calls**
Every API endpoint uses RTK Query via `baseApi.injectEndpoints()`. Never use `fetch`/`axios` directly.

```typescript
// src/features/products/api/productApi.ts
import type { Product, PaginatedResult, ApiResponse } from '@/shared/types';
import { baseApi } from '@/shared/lib/api/baseApi';

const productApiSlice = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getProducts: builder.query<PaginatedResult<Product>, { page?: number; pageSize?: number }>({
      query: ({ page = 1, pageSize = 20 }) => `/products?page=${page}&pageSize=${pageSize}`,
      transformResponse: (response: ApiResponse<PaginatedResult<Product>>) =>
        response.data || { items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0, hasNext: false, hasPrevious: false },
    }),
    createProduct: builder.mutation<Product, CreateProductDto>({
      query: (body) => ({ url: '/products', method: 'POST', body }),
      invalidatesTags: ['Products'],
    }),
  }),
});

export const { useGetProductsQuery, useCreateProductMutation } = productApiSlice;
```

Key points:
- ONE shared `baseApi` in `src/shared/lib/api/baseApi.ts` — features inject their endpoints
- Use `transformResponse` to unwrap the `ApiResponse<T>` envelope so components receive clean data
- Use `providesTags` / `invalidatesTags` for cache management
- Import API slices in `store.ts` so endpoints get registered

#### **2. TypeScript Types on All Components**
All components must have explicit TypeScript types. No `any`.

```typescript
// Interfaces define the contract
interface ProductCardProps {
  id: string;
  name: string;
  price: number;
  onSelect: (id: string) => void;
}

// Plain function declarations (preferred) or named function with memo
export default function ProductCard({ id, name, price, onSelect }: ProductCardProps) {
  return <button onClick={() => onSelect(id)}>{name} - ${price}</button>;
}
```

#### **3. Error Handling with useApiErrorHandler**
Use the centralized `useApiErrorHandler` hook from `src/shared/hooks/useApiErrorHandler.ts`. Never cast errors with `as any`.

```typescript
import { useApiErrorHandler } from '@/shared/hooks/useApiErrorHandler';

export default function CheckoutForm() {
  const { handleError, getErrorMessage } = useApiErrorHandler();
  const [createOrder, { isLoading }] = useCreateOrderMutation();

  const onSubmit = async (data: OrderDto) => {
    try {
      await createOrder(data).unwrap();
    } catch (error) {
      handleError(error, 'Failed to place order');  // Shows toast automatically
    }
  };

  // ...
}
```

#### **4. Redux for UI State Only**
Use Redux slices for UI state (filters, pagination, modals). RTK Query owns all server data.

```typescript
// src/features/cart/slices/cartSlice.ts — UI/local state only
const cartSlice = createSlice({
  name: 'cart',
  initialState: { items: [] as CartItem[] },
  reducers: {
    addItem: (state, action: PayloadAction<CartItem>) => { ... },
    removeItem: (state, action: PayloadAction<string>) => { ... },
  },
});
```

### P1 - Expected (Flag in code review)

#### **5. Import Path Conventions: Use `@` Alias**
Use the `@` alias for all imports instead of relative paths (`../../../`). This improves readability, makes refactoring easier, and is configured in `tsconfig.json`.

**Alias mappings:**
- `@/features/*` → `src/features/*` (feature modules)
- `@/shared/*` → `src/shared/*` (shared components, hooks, utils, types)
- `@/` → `src/` (root level)

**Examples:**

```typescript
// ✅ GOOD - Use @ alias (always preferred)
import Button from '@/shared/components/ui/Button';
import { useGetOrdersQuery } from '@/features/orders/api/ordersApi';
import { useForm } from '@/shared/hooks/useForm';
import type { Product } from '@/shared/types';
import { ProductCard } from '@/features/products/components';

// ❌ AVOID - Relative paths
import Button from '../../../../shared/components/ui/Button';
import { useGetOrdersQuery } from '../../api/ordersApi';
import { useForm } from '@/shared/hooks/useForm';  // Inconsistent mix!
```

**Rules:**
- Use `@` for ALL imports (features, shared, even same-feature imports when from other directories)
- Never mix relative and `@` paths in the same file
- Relative imports (like `./Button`) are acceptable ONLY for same-directory imports (e.g., `./sibling.tsx`)

#### **6. Data Flow Architecture**
Follow this sequence for every feature:
1. **API Layer** — `baseApi.injectEndpoints()` + exported hooks
2. **Redux Slice** — Only if non-API state needed (UI state, local cart)
3. **Components** — Consume hooks, handle loading/error/empty states
4. **Pages** — Orchestrate components, wrap in ErrorBoundary

#### **7. API Response Envelope**
All API calls return this envelope. Use `transformResponse` to unwrap it.

```typescript
interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  error: {
    message: string;
    code: string;
    errors?: Record<string, string[]>;
    traceId: string;
  } | null;
  traceId: string;
}
```

#### **8. Component Colocation Architecture**

Components follow a **colocation pattern** where a component and its related files are organized together in a dedicated folder structure. This improves code organization, reusability, and maintainability.

**Incremental adoption rule:**
- If a PR touches a component, migrate that component to the colocation structure in the same PR.
- Keep migration PRs small (1-3 components) and avoid behavior changes.
- Use the tracker (`COLOCATION_ADOPTION_TRACKER.md`) to prioritize high-traffic and high-churn components first.

**Basic colocation structure (1-2 hooks or utilities):**
```
ComponentName/
├── ComponentName.tsx         # Main component
├── ComponentName.types.ts    # TypeScript interfaces/types
├── ComponentName.module.css  # Scoped styles
├── ComponentName.hooks.ts    # Custom hooks (if ≤2 hooks)
├── ComponentName.utils.ts    # Utility functions
├── ComponentName.test.tsx    # Component tests
└── index.ts                  # Barrel export
```

**Advanced colocation structure (3+ hooks):**
```
ComponentName/
├── ComponentName.tsx         # Main component
├── ComponentName.types.ts    # TypeScript interfaces/types
├── ComponentName.module.css  # Scoped styles
├── ComponentName.test.tsx    # Component tests
├── hooks/                    # Separate folder for 3+ hooks
│   ├── useFirstHook.ts      # Individual hook file
│   ├── useSecondHook.ts     # Individual hook file
│   ├── useThirdHook.ts      # Individual hook file
│   └── index.ts             # Barrel export for hooks
├── utils/                    # Optional: separate folder if 5+ functions
│   ├── helper1.utils.ts
│   ├── helper2.utils.ts
│   └── index.ts
└── index.ts                  # Main barrel export
```

**Feature-level structure (reference example):**
```
src/features/products/
├── api/
│   ├── productApi.ts          ← RTK Query endpoints
│   ├── categoriesApi.ts
│   └── reviewsApi.ts
├── components/
│   ├── ProductCard/           ← Colocated component
│   │   ├── ProductCard.tsx
│   │   ├── ProductCard.types.ts
│   │   ├── ProductCard.module.css
│   │   ├── ProductCard.hooks.ts
│   │   ├── ProductCard.test.tsx
│   │   └── index.ts
│   ├── ProductGrid/
│   │   ├── ProductGrid.tsx
│   │   ├── ProductGrid.types.ts
│   │   ├── ProductGrid.test.tsx
│   │   └── index.ts
│   └── ProductFilters/        ← Component with multiple hooks (folder structure)
│       ├── ProductFilters.tsx
│       ├── ProductFilters.types.ts
│       ├── ProductFilters.module.css
│       ├── ProductFilters.test.tsx
│       ├── hooks/
│       │   ├── usePriceFilters.ts
│       │   ├── useRatingFilter.ts
│       │   ├── useFeaturedFilter.ts
│       │   └── index.ts
│       └── index.ts
├── hooks/
│   ├── useProductFilters.ts
│   └── useProductDetails.ts
├── pages/
│   ├── ProductsPage/
│   └── ProductDetailPage/
└── types/
    └── index.ts
```

**When to use each pattern:**

1. **Single `.hooks.ts` file** (1-2 tightly-coupled hooks):
   ```typescript
   // ProductCard.hooks.ts
   export function useProductCardHandlers(...) { ... }
   export function useProductValidation(...) { ... }
   ```

2. **`hooks/` folder with separate files** (3+ hooks OR hooks potentially reused elsewhere):
   ```typescript
   // hooks/usePriceFilters.ts
   export function usePriceFilters(...) { ... }

   // hooks/useRatingFilter.ts
   export function useRatingFilter(...) { ... }

   // hooks/useFeaturedFilter.ts
   export function useFeaturedFilter(...) { ... }

   // hooks/index.ts
   export { usePriceFilters } from './usePriceFilters';
   export { useRatingFilter } from './useRatingFilter';
   export { useFeaturedFilter } from './useFeaturedFilter';
   ```

3. **Separate `utils/` folder** (5+ utility functions):
   ```typescript
   // utils/calculations.utils.ts
   export function calculateTotal(...) { ... }
   export function calculateTax(...) { ... }

   // utils/formatting.utils.ts
   export function formatPrice(...) { ... }
   export function formatCurrency(...) { ... }

   // utils/index.ts
   export * from './calculations.utils';
   export * from './formatting.utils';
   ```

**Main component barrel export (`index.ts`):**
```typescript
export { default } from './ComponentName';
export type { ComponentNameProps, RelatedTypes } from './ComponentName.types';
export { useCustomHook, anotherHook } from './hooks'; // or './ComponentName.hooks'
export { utilFunction1, utilFunction2 } from './utils'; // or './ComponentName.utils'
```

**Benefits:**
- ✅ **Encapsulation**: All component-related code in one folder
- ✅ **Scalability**: Easy to add tests, hooks, utils without cluttering files
- ✅ **Reusability**: Clear barrel exports make it easy to import what you need
- ✅ **Maintainability**: Each file has a single responsibility
- ✅ **Type safety**: Centralized types in `.types.ts` files
- ✅ **Discoverability**: Test files live next to components, not in separate `/tests` folder

**Import examples:**
```typescript
// Import from colocated component with hooks
import ProductFilters from '@/features/products/components/ProductFilters';
import { usePriceFilters, useRatingFilter } from '@/features/products/components/ProductFilters';

// Or with explicit path when needed
import { ProductFilters } from '@/features/products/components';
import { usePriceFilters } from '@/features/products/components/ProductFilters/hooks';
```

**Reference templates:**
- Starter template with copy-paste file contents: `COMPONENT_COLOCATION_TEMPLATE.md`
- Rollout tracker and adoption status: `COLOCATION_ADOPTION_TRACKER.md`

#### **9. Form Validation with Zod**
Validate before sending to API.

```typescript
import { z } from 'zod';

const CheckoutSchema = z.object({
  email: z.string().email('Invalid email'),
  address: z.string().min(1, 'Address required'),
});

type CheckoutDto = z.infer<typeof CheckoutSchema>;
```

### P2 - Recommended (Nice to have)

#### **10. URL Persistence for Filters**
Sync filters to query params so users can share/bookmark URLs.

```typescript
export function useFilterSync() {
  const [filters, setFilters] = useState(() => parseURLParams());
  const navigate = useNavigate();

  useEffect(() => {
    const params = new URLSearchParams();
    if (filters.category) params.set('category', filters.category);
    if (filters.minPrice) params.set('minPrice', String(filters.minPrice));
    navigate(`?${params.toString()}`, { replace: true });
  }, [filters, navigate]);

  return [filters, setFilters] as const;
}
```

#### **11. Suspense for Route-Level Code Splitting**
Use `React.lazy()` + `<Suspense>` for non-critical routes. This is already configured in `App.tsx`.

```typescript
// App.tsx — actual pattern
const Products = lazy(() => import('./features/products/pages/ProductsPage/ProductsPage'));
const Cart = lazy(() => import('./features/cart/pages/CartPage/CartPage'));

function App() {
  return (
    <ErrorBoundary fallback={<ErrorPage />}>
      <Suspense fallback={<LoadingFallback />}>
        <Routes>
          <Route path="/" element={<Home />} />           {/* Eagerly loaded */}
          <Route path="/products" element={<Products />} /> {/* Lazy loaded */}
          <Route path="/cart" element={<Cart />} />         {/* Lazy loaded */}
        </Routes>
      </Suspense>
    </ErrorBoundary>
  );
}
```

**Note:** Suspense works for `React.lazy()` code splitting. It does NOT work with RTK Query hooks (`useGetProductQuery` returns `{ data, isLoading }` — it doesn't throw promises). For data fetching, use the `QueryRenderer` pattern or conditional `isLoading`/`isError` checks.

### ErrorBoundary vs RTK Query Errors

`ErrorBoundary` handles render/runtime errors, but RTK Query request failures are returned via hook state (`error`) and must be handled in component logic.

```typescript
// ErrorBoundary catches render errors in children
<ErrorBoundary fallback={<ErrorPage />}>
  <Suspense fallback={<LoadingFallback />}>
    <ProductsPage />
  </Suspense>
</ErrorBoundary>

// RTK Query errors must be handled explicitly
function ProductsPage() {
  const { data, isLoading, error } = useGetProductsQuery({ page: 1, pageSize: 20 });

  return (
    <QueryRenderer
      isLoading={isLoading}
      error={error}
      data={data}
      errorMessage="Failed to load products"
      emptyState={{ title: 'No products found' }}
    >
      {(result) => <ProductGrid products={result.items} />}
    </QueryRenderer>
  );
}
```

#### **12. Error Recovery for Mutations**
Implement retry and user-friendly error messages for mutations. Offline queue and conflict resolution are optional, add them only where the UX demands it (e.g., cart updates).

```typescript
const [updateCart] = useUpdateCartItemMutation();
const { handleError } = useApiErrorHandler();

const handleQuantityChange = async (itemId: string, qty: number) => {
  try {
    await updateCart({ cartItemId: itemId, quantity: qty }).unwrap();
  } catch (error) {
    handleError(error, 'Failed to update cart');
  }
};
```

---

## Architecture: How It Actually Works

### Shared `baseApi` with Endpoint Injection

All API communication flows through one shared `baseApi` instance. Features inject their endpoints at import time.

**`src/shared/lib/api/baseApi.ts`** — Single source of truth:
```typescript
import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import { config } from '../../../config';

const getCsrfToken = (): string | null => {
  if (typeof document === 'undefined') return null;
  const match = document.cookie.match(/XSRF-TOKEN=([^;]+)/);
  return match ? decodeURIComponent(match[1]) : null;
};

const baseQuery = fetchBaseQuery({
  baseUrl: config.api.baseUrl,       // From src/config.ts → VITE_API_URL env var
  credentials: 'include',            // httpOnly cookies sent automatically
  prepareHeaders: (headers) => {
    const csrfToken = getCsrfToken();
    if (csrfToken) {
      headers.set('X-XSRF-TOKEN', csrfToken);
    }
    return headers;
  },
});

// Auto-refresh on 401, auto-logout on refresh failure
const baseQueryWithReauth: BaseQueryFn<...> = async (args, api, extraOptions) => {
  const result = await baseQuery(args, api, extraOptions);
  if (result.error && result.error.status === 401) {
    const refreshResult = await baseQuery(
      { url: '/auth/refresh-token', method: 'POST' }, api, extraOptions
    );
    if (refreshResult.error) {
      api.dispatch(logout());
    } else {
      return baseQuery(args, api, extraOptions);  // Retry original request
    }
  }
  return result;
};

export const baseApi = createApi({
  reducerPath: 'api',
  baseQuery: baseQueryWithReauth,
  keepUnusedDataFor: 60,
  tagTypes: ['Cart', 'Order', 'Profile', 'Review', 'Wishlist', 'WishlistCheck', 'Categories'],
  endpoints: () => ({}),  // Features inject their own endpoints
});
```

**Authentication:** Uses CSRF tokens + httpOnly cookies. No Bearer tokens in headers. The `credentials: 'include'` flag ensures cookies are sent with every request. 401 responses trigger automatic token refresh via `baseQueryWithReauth`.

### Cache & Refetch Strategy

Configure endpoint behavior based on data volatility:

```typescript
const productApiSlice = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getProducts: builder.query<PaginatedResult<Product>, ProductQuery>({
      query: ({ page = 1, pageSize = 20 }) => `/products?page=${page}&pageSize=${pageSize}`,
      providesTags: ['Products'],
      keepUnusedDataFor: 120,
      refetchOnFocus: false,
      refetchOnReconnect: true,
      refetchOnMountOrArgChange: 60,
    }),
    getCart: builder.query<CartDto, void>({
      query: () => '/cart',
      providesTags: ['Cart'],
      keepUnusedDataFor: 30,
      refetchOnFocus: true,
      refetchOnReconnect: true,
      pollingInterval: 0,
    }),
  }),
});
```

Use this baseline:
- Catalog/listing data: higher cache TTL, no focus refetch
- User-session data (cart/profile): lower cache TTL, refetch on focus/reconnect
- Real-time-like data: short polling only when UX explicitly requires it

### Store Configuration

**`src/shared/lib/store/store.ts`:**
```typescript
import { configureStore } from '@reduxjs/toolkit';
import { baseApi } from '@/shared/lib/api/baseApi';

// Import API slices so endpoints get injected
import '@/features/auth/api/authApi';
import '@/features/cart/api/cartApi';
import '@/features/products/api/productApi';
// ... other API slices

export const store = configureStore({
  reducer: {
    auth: authReducer,
    cart: cartReducer,
    [baseApi.reducerPath]: baseApi.reducer,  // Single API reducer
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(baseApi.middleware),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
```

### Environment Configuration

**`src/config.ts`** centralizes all environment-specific settings:
```typescript
export const config = {
  api: {
    baseUrl: import.meta.env.VITE_API_URL || 'http://localhost:5000/api',
    timeout: 30000,
  },
  features: {
    guestCheckout: true,
    cartSync: true,
    wishlist: true,
  },
  pagination: {
    defaultPageSize: 12,
  },
  business: {
    freeShippingThreshold: 100,
    standardShippingCost: 10,
    defaultTaxRate: 0.08,
    maxCartQuantity: 99,
  },
} as const;
```

Set `VITE_API_URL` in `.env` per environment. The config is consumed by `baseApi.ts` and any component that needs business rules.

---

## AsyncData Pattern (Type-Safe Loading States)

Instead of separate `isLoading`, `error`, `data` flags, use a **discriminated union** for explicit state constraints:

```typescript
// Define the pattern
type AsyncState<T> =
  | { status: 'idle' }
  | { status: 'pending' }
  | { status: 'success'; data: T }
  | { status: 'error'; error: Error };

// Usage in component
interface ProductListProps {
  state: AsyncState<Product[]>;
}

export function ProductList({ state }: ProductListProps) {
  switch (state.status) {
    case 'idle':
      return null;  // No query started yet
    case 'pending':
      return <LoadingSkeleton count={6} />;
    case 'success':
      return (
        <div>
          {state.data.map(p => <ProductCard key={p.id} {...p} />)}
        </div>
      );
    case 'error':
      return <ErrorAlert error={state.error} />;
  }
}

// Helper to derive from RTK Query hooks
function useAsyncProductList(filters: Filters): AsyncState<Product[]> {
  const { data, isLoading, error } = useGetProductsQuery(filters);
  
  if (isLoading) return { status: 'pending' };
  if (error) return { status: 'error', error: new Error(error.message) };
  if (data) return { status: 'success', data };
  return { status: 'idle' };
}
```

**Benefits:**
- ✅ Impossible to have `data && error` simultaneously
- ✅ Type-safe: `state.data` only exists if `status === 'success'`
- ✅ Self-documenting state machine

---

## Optimistic Updates for Ecommerce

Update the UI immediately while the API request is in flight. Rollback on error:

```typescript
// src/features/cart/hooks/useOptimisticUpdate.ts
export function useItemQuantityUpdate() {
  const dispatch = useAppDispatch();
  const [updateCartItem] = useUpdateCartItemMutation();
  const { handleError } = useApiErrorHandler();

  const updateQuantity = async (itemId: string, newQuantity: number) => {
    // Step 1: Get current state for rollback
    const currentState = store.getState().cart;
    const previousItem = currentState.items.find(i => i.id === itemId);

    // Step 2: Optimistically update Redux immediately
    dispatch(cartSlice.actions.updateItemQuantity({ itemId, quantity: newQuantity }));

    try {
      // Step 3: Send to API
      const result = await updateCartItem({ cartItemId: itemId, quantity: newQuantity }).unwrap();

      // Step 4: Confirm with server data (in case of conflicts)
      dispatch(confirmCartUpdate(result));
    } catch (error) {
      // Step 5: Rollback on error
      if (previousItem) {
        dispatch(cartSlice.actions.updateItemQuantity({
          itemId,
          quantity: previousItem.quantity
        }));
      } else {
        dispatch(cartSlice.actions.removeItem(itemId));
      }
      handleError(error, 'Failed to update cart');
    }
  };

  return { updateQuantity };
}

// Usage in component
export function CartItem({ item }: { item: CartItem }) {
  const { updateQuantity } = useItemQuantityUpdate();

  return (
    <div>
      <input
        type="number"
        value={item.quantity}
        onChange={(e) => updateQuantity(item.id, parseInt(e.target.value))}
      />
      {/* UI updates immediately, API request in background */}
    </div>
  );
}
```

---

## Race Condition Prevention for Mutations

Prevent double-submission and handle concurrent requests:

```typescript
// Hook that prevents duplicate mutation submissions
type MutationTuple<Args, Result> = [
  (args: Args) => { unwrap: () => Promise<Result> },
  { isLoading: boolean }
];

export function useMutationSafe<Args, Result>(
  useMutationHook: () => MutationTuple<Args, Result>
): [
  submit: (args: Args) => Promise<Result>,
  { isLoading: boolean }
] {
  const [trigger, result] = useMutationHook();
  const [isSubmitting, setIsSubmitting] = useState(false);

  const safeMutate = useCallback(async (args: Args) => {
    if (isSubmitting) {
      console.warn('Mutation already in progress, ignoring duplicate submission');
      throw new Error('DUPLICATE_SUBMISSION');
    }

    setIsSubmitting(true);
    try {
      return await trigger(args).unwrap();
    } finally {
      setIsSubmitting(false);
    }
  }, [trigger, isSubmitting]);

  return [safeMutate, { isLoading: result.isLoading || isSubmitting }];
}

// Usage
export function CheckoutForm() {
  const [createOrder, { isLoading }] = useMutationSafe(useCreateOrderMutation);

  return (
    <form
      onSubmit={async (e) => {
        e.preventDefault();
        await createOrder(formData);  // Won't double-submit if clicked multiple times
      }}
    >
      <button disabled={isLoading}>Confirm Order</button>
    </form>
  );
}
```

---

## Field-Level Validation Error Mapping

Map Zod and API validation errors to form fields:

```typescript
// src/features/checkout/hooks/useCheckoutForm.ts
export function useCheckoutForm() {
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [createOrder] = useCreateOrderMutation();

  const handleSubmit = async (formData: CheckoutFormData) => {
    // Clear previous errors
    setErrors({});

    // Step 1: Client-side validation (Zod)
    try {
      CheckoutSchema.parse(formData);
    } catch (error) {
      if (error instanceof ZodError) {
        const fieldErrors = Object.fromEntries(
          error.errors.map(err => [
            err.path.join('.'),  // 'address.street' for nested fields
            err.message
          ])
        );
        setErrors(fieldErrors);
        return;  // Stop here, don't send to API
      }
    }

    // Step 2: API call (server validation)
    try {
      await createOrder(formData).unwrap();
    } catch (error) {
      // Handle API validation errors
      const apiError = error as ApiErrorResponse;
      if (apiError.error?.errors) {
        // Backend returned { email: ["Invalid"], address: ["Required"] }
        setErrors(apiError.error.errors);
      } else {
        // Generic error
        setErrors({ _form: apiError.error?.message || 'Unknown error' });
      }
    }
  };

  return { handleSubmit, errors };
}

// Usage in form component
export function CheckoutForm() {
  const { handleSubmit, errors } = useCheckoutForm();

  return (
    <form onSubmit={(e) => { e.preventDefault(); handleSubmit(formData); }}>
      <div>
        <input name="email" placeholder="Email" />
        {errors.email && <span className={styles.error}>{errors.email}</span>}
      </div>
      <div>
        <input name="address" placeholder="Address" />
        {errors.address && <span className={styles.error}>{errors.address}</span>}
      </div>
      {errors._form && <div className={styles.formError}>{errors._form}</div>}
    </form>
  );
}
```

---

## Offline-First with LocalStorage Sync

Queue mutations while offline, sync when connection returns:

```typescript
// src/features/cart/hooks/usePersistentCart.ts
export function usePersistentCart() {
  const dispatch = useAppDispatch();
  const [addToCart] = useAddToCartMutation();

  // Persist cart to localStorage
  useEffect(() => {
    const cart = store.getState().cart;
    localStorage.setItem('pending_cart_items', JSON.stringify(cart.items));
  }, []);

  // Listen for online event and flush pending operations
  useEffect(() => {
    const handleOnline = async () => {
      const pendingJson = localStorage.getItem('pending_cart_items');
      if (!pendingJson) return;

      const pendingItems = JSON.parse(pendingJson) as CartItem[];
      let syncedCount = 0;

      for (const item of pendingItems) {
        try {
          await addToCart({
            productId: item.productId,
            quantity: item.quantity
          }).unwrap();
          syncedCount++;
        } catch (error) {
          console.error('Sync failed for item:', item.productId);
          // On first failure, stop and retry later
          break;
        }
      }

      if (syncedCount === pendingItems.length) {
        localStorage.removeItem('pending_cart_items');
        toast.success('Cart synced!');
      } else {
        toast.warning('Cart partially synced. Will retry.');
      }
    };

    window.addEventListener('online', handleOnline);
    return () => window.removeEventListener('online', handleOnline);
  }, [addToCart]);
}

// Usage in App.tsx
<PersistentCartProvider>
  <Routes>{/* ... */}</Routes>
</PersistentCartProvider>
```

---

## State Domain Matrix

Clear guidance on WHERE to store each piece of state:

| Data Type | Redux | RTK Query | localStorage | Comment |
|-----------|-------|-----------|--------------|---------|
| Auth token | ✅ | ❌ | ✅ | Persisted for session reuse |
| User profile | ❌ | ✅ | ❌ | Server-owned, cache invalidated on logout |
| Filter category | ✅ | ❌ | ✅ | Remember user's preference |
| Product list | ❌ | ✅ | ❌ | Server-owned, paginated |
| Selected payment method | ✅ | ❌ | ❌ | UI choice, not persisted |
| Cart items | ❌️ | ✅ (cache) + ✅ (local state) | ✅ | Optimistic updates + offline sync |
| Modal open state | ✅ | ❌ | ❌ | Pure UI state |
| Form draft (unsaved) | ✅ (temp) | ❌ | ✅ | Auto-save to prevent loss |
| Search results | ❌ | ✅ | ❌ | Server-owned, ephemeral |
| API error toast | ❌ | ❌ | ❌ | Handled by error hook, not stored |

---

## Testing Pyramid: Unit vs Integration vs E2E

```
        E2E (5%)
       /      \
      /        \
     / Integration \     (20%)
    /              \
   / Unit Tests     \    (75%)
```

### Unit Tests (75% of effort)
Test components in isolation with mocked API hooks:

```typescript
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi } from 'vitest';

vi.mock('@/features/cart/api/cartApi', () => ({
  useAddToCartMutation: vi.fn(() => [
    vi.fn().mockResolvedValue({ id: '1' }),
    { isLoading: false }
  ]),
}));

describe('ProductCard', () => {
  it('shows add to cart button and handles clicks', async () => {
    const user = userEvent.setup();
    render(<ProductCard id="1" name="Laptop" price={999} slug="laptop" imageUrl="/img.jpg" />);

    const button = screen.getByRole('button', { name: /add to cart/i });
    await user.click(button);
    // Assert toast or callback
  });
});
```

### Integration Tests (20% of effort)
Test multiple components + Redux state together:

```typescript
import { render, screen, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { setupServer } from 'msw/node';
import { store } from '@/shared/lib/store';

const server = setupServer(
  rest.post('/api/v1/cart/items', (req, res, ctx) =>
    res(ctx.json({ success: true, data: { id: '1' } }))
  )
);

beforeAll(() => server.listen());
afterEach(() => server.resetHandlers());

it('user adds product and sees cart total update', async () => {
  render(
    <Provider store={store}>
      <ProductCard {...mockProduct} />
      <CartSummary /> {/* Shows total */}
    </Provider>
  );

  await userEvent.click(screen.getByRole('button', { name: /add/i }));

  await waitFor(() => {
    expect(screen.getByText(/total.*\$1,009/)).toBeInTheDocument();  // $999 + tax
  });
});
```

### E2E Tests (5% of effort)
Test full user journeys in real browser:

```typescript
import { test, expect } from '@playwright/test';

test('user can search, add to cart, and checkout', async ({ page }) => {
  await page.goto('http://localhost:5173');

  // Search for product
  await page.fill('[placeholder="Search"]', 'laptop');
  await page.click('button:has-text("Search")');
  await page.waitForSelector('[data-testid="product-card"]');

  // Add to cart
  await page.click('button:has-text("Add to Cart")');
  await expect(page).toHaveURL(/\/cart/);

  // Verify cart
  await expect(page.locator('[data-testid="cart-total"]')).toContainText('$');
});
```

---

## Component Patterns

### Hooks-Based Data Fetching (Primary Pattern)

Components directly call RTK Query hooks. No Container/Presentational separation needed.

```typescript
// src/pages/HomePage/HomePage.tsx — actual pattern
export default function HomePage() {
  const { data: featured, isLoading, error } = useGetFeaturedProductsQuery(10);
  const { data: categories } = useGetTopLevelCategoriesQuery();

  return (
    <div>
      <QueryRenderer
        isLoading={isLoading}
        error={error}
        data={featured}
        errorMessage="Failed to load products"
        emptyState={{ title: 'No products found' }}
      >
        {(products) => (
          <div className={styles.grid}>
            {products.map((product) => (
              <ProductCard key={product.id} {...product} />
            ))}
          </div>
        )}
      </QueryRenderer>
    </div>
  );
}
```

### QueryRenderer Component

Use `QueryRenderer` from `src/shared/components/QueryRenderer/` to handle loading/error/empty states consistently:

```typescript
<QueryRenderer
  isLoading={isLoading}
  error={error}
  data={data}
  errorMessage="Custom error message"
  loadingSkeleton={{ count: 6, type: 'card' }}
  emptyState={{
    title: 'No items found',
    description: 'Try adjusting your filters',
    action: <Button onClick={resetFilters}>Clear filters</Button>,
  }}
>
  {(data) => <YourContent data={data} />}
</QueryRenderer>
```

For simpler cases, conditional checks are also acceptable:

```typescript
const { data, isLoading, isFetching, error } = useGetProductsQuery(params);

if (isLoading) return <LoadingSkeleton />;     // First load — show skeleton
if (error) return <ErrorAlert message="..." />;  // Error — show message
if (isFetching) return <RefreshIndicator />;    // Background refetch — subtle indicator
```

### Render Optimization with React Compiler

React Compiler is enabled in this project, so default to plain components/functions first. Add manual `memo`/`useCallback` only when profiling shows a measurable benefit or when referential stability is required by integration boundaries.

```typescript
// Preferred default with React Compiler enabled
export default function ProductCard({
  id, name, slug, price, imageUrl,
}: ProductCardProps) {
  const { handleError } = useApiErrorHandler();
  const [addToCart] = useAddToCartMutation();

  const handleAddToCart = async (event: React.MouseEvent) => {
    event.preventDefault();
    try {
      await addToCart({ productId: id, quantity: 1 }).unwrap();
      toast.success('Added to cart!');
    } catch (error) {
      handleError(error, 'Failed to add to cart');
    }
  };

  return (
    <article className={styles.card}>
      <Link to={`/products/${slug}`}>
        <img src={imageUrl} alt={name} loading="lazy" />
        <h3>{name}</h3>
        <span>${price.toFixed(2)}</span>
      </Link>
      <button onClick={handleAddToCart}>Add to Cart</button>
    </article>
  );
}
```

Use manual `memo`/`useCallback` when:
- Profiling shows avoidable re-renders and a measurable win
- A child component or third-party API depends on stable callback/prop identity
- A hot path still regresses after compiler optimization

Avoid manual `memo`/`useCallback` when:
- The component is simple and renders infrequently
- There is no measured performance issue
- The wrapper makes code harder to read without clear benefit

If manual memoization is needed, this pattern is still acceptable:

```typescript
// src/features/products/components/ProductCard/ProductCard.tsx — actual pattern
const ProductCard = memo(function ProductCard({
  id, name, slug, price, imageUrl,
}: ProductCardProps) {
  const { handleError } = useApiErrorHandler();
  const [addToCart] = useAddToCartMutation();

  const handleAddToCart = async (event: React.MouseEvent) => {
    event.preventDefault();
    try {
      await addToCart({ productId: id, quantity: 1 }).unwrap();
      toast.success('Added to cart!');
    } catch (error) {
      handleError(error, 'Failed to add to cart');
    }
  };

  return (
    <article className={styles.card}>
      <Link to={`/products/${slug}`}>
        <img src={imageUrl} alt={name} loading="lazy" />
        <h3>{name}</h3>
        <span>${price.toFixed(2)}</span>
      </Link>
      <button onClick={handleAddToCart}>Add to Cart</button>
    </article>
  );
});

export default ProductCard;
```

### Custom Hooks

Keep hooks simple — plain functions first; add `useCallback` only when a stable function reference is actually required.

```typescript
// src/features/products/hooks/useProductFilters.ts
export function useProductFilters() {
  const dispatch = useAppDispatch();
  const filters = useAppSelector((state) => state.ui.selectedFilters);

  const updateCategory = (category: string | null) => {
    dispatch(setCategory(category));
  };

  const updatePage = (page: number) => {
    dispatch(setPageNumber(page));
  };

  const resetFilters = () => {
    dispatch(setCategory(null));
    dispatch(setPageNumber(1));
  };

  return { filters, updateCategory, updatePage, resetFilters };
}
```

### Selector Memoization

Select only what you need. Use `createSelector` for derived data.

```typescript
// Select specific properties — NOT entire slices
const selectedCategory = useAppSelector(state => state.ui.selectedFilters.category);
const pageNumber = useAppSelector(state => state.ui.selectedFilters.pageNumber);

// Use createSelector for computed/derived values
import { createSelector } from '@reduxjs/toolkit';

const selectUi = (state: RootState) => state.ui;

export const selectActiveFilters = createSelector(
  [selectUi],
  (ui) => Object.entries(ui.selectedFilters).filter(([, value]) => value !== null && value !== 0)
);
```

---

## Data Contracts

### API Response Envelope
Use the same `ApiResponse<T>` envelope defined earlier in **Quick Start → 6. API Response Envelope**.

### Pagination Response
```typescript
interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
}
```

### HTTP Status Codes Mapping
| Status | Meaning | UI Action |
|--------|---------|-----------|
| 200 | Success | Show data |
| 400 | Bad request | Show validation errors |
| 401 | Unauthorized | Auto-refresh token, then redirect to login |
| 403 | Forbidden | Show "Access Denied" |
| 404 | Not found | Show "Not found" message |
| 409 | Conflict | Show "Resource modified" message |
| 422 | Validation error | Show field-level errors |
| 500 | Server error | Show "Try again later" message |

---

## Testing

### Test Stack
- **Vitest** as test runner (configured in `vite.config.ts`)
- **@testing-library/react** for component tests
- **vi.mock()** for unit tests (component-level mocking)
- **MSW** for integration tests where realistic request/response behavior is needed
- **Playwright** for E2E tests

### Mocking RTK Query Hooks

Pick one approach per test file (`vi.mock()` for unit tests or MSW for integration tests) to keep tests deterministic.

```typescript
// src/features/products/components/ProductCard/ProductCard.test.tsx
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi, describe, it, expect, beforeEach } from 'vitest';

// Mock the API hooks
vi.mock('@/features/cart/api/cartApi', () => ({
  useAddToCartMutation: vi.fn(() => [vi.fn(), { isLoading: false }]),
}));

vi.mock('@/features/wishlist/api', () => ({
  useAddToWishlistMutation: vi.fn(() => [vi.fn(), { isLoading: false }]),
  useRemoveFromWishlistMutation: vi.fn(() => [vi.fn(), { isLoading: false }]),
  useCheckInWishlistQuery: vi.fn(() => ({ data: false, refetch: vi.fn() })),
}));

describe('ProductCard', () => {
  it('renders product info', () => {
    render(<ProductCard id="1" name="Test Product" price={29.99} slug="test" imageUrl="/img.jpg" />);
    expect(screen.getByText('Test Product')).toBeInTheDocument();
    expect(screen.getByText('$29.99')).toBeInTheDocument();
  });

  it('calls addToCart on button click', async () => {
    const mockAddToCart = vi.fn().mockReturnValue({ unwrap: () => Promise.resolve() });
    vi.mocked(useAddToCartMutation).mockReturnValue([mockAddToCart, { isLoading: false }]);

    const user = userEvent.setup();
    render(<ProductCard id="1" name="Test" price={10} slug="test" imageUrl="/img.jpg" />);

    await user.click(screen.getByLabelText(/add to cart/i));
    expect(mockAddToCart).toHaveBeenCalledWith({ productId: '1', quantity: 1 });
  });
});
```

### Testing Custom Hooks

```typescript
import { renderHook, act } from '@testing-library/react';
import { Provider } from 'react-redux';
import { store } from '@/shared/lib/store';

const wrapper = ({ children }: { children: React.ReactNode }) => (
  <Provider store={store}>{children}</Provider>
);

it('updates category and resets pagination', () => {
  const { result } = renderHook(() => useProductFilters(), { wrapper });

  act(() => {
    result.current.updateCategory('electronics');
  });

  expect(result.current.filters.category).toBe('electronics');
});
```

### Testing Best Practices

```typescript
// Test user behavior, not implementation
it('user can add product to cart and see confirmation', async () => {
  const user = userEvent.setup();
  render(<ProductCard product={mockProduct} />);

  await user.click(screen.getByRole('button', { name: /add to cart/i }));

  await screen.findByText(/added to cart/i);
});

// Reset API state between tests to prevent cache bleed
afterEach(() => {
  store.dispatch(baseApi.util.resetApiState());
});
```

---

## Performance Budgets (Non-Negotiable)

Define explicit budgets and enforce them in CI:

- Main entry JS chunk: <= 200 KB (gzip)
- Any lazy route chunk: <= 150 KB (gzip)
- LCP <= 2.5s (p75), INP <= 200ms (p75), CLS <= 0.1 (p75)

```typescript
// Example budget check workflow (conceptual)
// 1) Build with stats
// 2) Compare emitted chunk sizes to thresholds
// 3) Fail PR if thresholds exceeded unless approved

export const performanceBudget = {
  mainChunkKbGzip: 200,
  lazyChunkKbGzip: 150,
  webVitals: {
    lcpMsP75: 2500,
    inpMsP75: 200,
    clsP75: 0.1,
  },
} as const;
```

Rules:
- Add lazy loading before increasing budgets
- Budget increases require ADR + approval
- Measure in production-like environment, not dev mode

---

## Accessibility Gates (WCAG 2.1 AA Baseline)

Required for all new UI and major UI changes:

- Keyboard-only navigation works for all interactive controls
- Visible focus indicators on interactive elements
- Accessible names for icon-only buttons (`aria-label`)
- Form fields have associated labels and error text via `aria-describedby`
- Color contrast meets WCAG AA

```typescript
// Recommended test pattern
it('has no critical accessibility violations', async () => {
  const { container } = render(<CheckoutForm />);
  const results = await axe(container);
  expect(results).toHaveNoViolations();
});
```

---

## Client Observability & Trace Correlation

Frontend errors and key user actions must be correlated with backend traces.

```typescript
// Attach request correlation id from response headers when available
const baseQuery = fetchBaseQuery({
  baseUrl: config.api.baseUrl,
  credentials: 'include',
});

const baseQueryWithTelemetry: BaseQueryFn = async (args, api, extraOptions) => {
  const startedAt = performance.now();
  const result = await baseQuery(args, api, extraOptions);
  const durationMs = performance.now() - startedAt;

  // Record high-value client telemetry
  telemetry.track('api.request', {
    endpoint: typeof args === 'string' ? args : args.url,
    success: !result.error,
    durationMs,
  });

  return result;
};
```

Minimum telemetry events:
- Route change
- API request success/failure + duration
- Checkout funnel milestones
- Client error boundary events

---

## Feature Flag Lifecycle Standard

Every feature flag must include:

- Owner (`team` + `person`)
- Expiry date
- Rollout plan (0% -> 5% -> 25% -> 100%)
- Cleanup task after full rollout

```typescript
type FeatureFlagMeta = {
  key: string;
  owner: string;
  expiresOn: string; // ISO date
  description: string;
};

export const featureFlags: FeatureFlagMeta[] = [
  {
    key: 'checkout.express-payments',
    owner: 'storefront-team',
    expiresOn: '2026-06-30',
    description: 'Enable express payments flow',
  },
];
```

Rules:
- Expired flags block release unless removed or renewed with approval
- Never gate security-critical fixes behind flags

---

## PR Checklist

Use this for every feature PR:

- [ ] **Code Quality**
  - [ ] Build passes: `npm run build`
  - [ ] Linter passes: `npm run lint`
  - [ ] All imports use `@` alias (no relative `../../../` paths)
  - [ ] No `any` types (use `useApiErrorHandler` instead of `as any` casts)
  - [ ] No console errors/warnings in dev/prod
  - [ ] No N+1 selectors (memoize derived state with `createSelector`)
  - [ ] No infinite loops in `useEffect` (check dependencies)
  - [ ] Performance budgets respected (chunk sizes + Web Vitals targets)

- [ ] **RTK Query & State**
  - [ ] New endpoints use `baseApi.injectEndpoints()` (not separate `createApi`)
  - [ ] `providesTags` / `invalidatesTags` configured for cache invalidation
  - [ ] `transformResponse` unwraps `ApiResponse<T>` envelope
  - [ ] Redux slices used only for UI state (not API data)
  - [ ] Optimistic updates + rollback implemented for mutations (cart, wishlist)
  - [ ] Concurrency protected: no double-submit buttons or mutation guards

- [ ] **Components**
  - [ ] All components have typed props interfaces
  - [ ] Loading states shown (`QueryRenderer` or conditional `isLoading`)
  - [ ] Error states shown (via `useApiErrorHandler` or `QueryRenderer`)
  - [ ] Strings use i18n (`useTranslation()`)
  - [ ] Styles use CSS Modules (no inline styles for colors/sizing)
  - [ ] **All SVG icons imported from `@/shared/components/icons/` (never inline SVGs or icon components in feature files)**
  - [ ] Accessibility gates pass (keyboard, labels, focus, contrast)
  - [ ] With React Compiler enabled, manual `memo()`/`useCallback` is used only for measured wins or identity-sensitive integrations

- [ ] **Forms**
  - [ ] Validation with Zod before API call
  - [ ] Field-level error display (not just form-level alerts)
  - [ ] Submit button disabled while loading (`isLoading` or `isSubmitting`)
  - [ ] Success/error toast notifications
  - [ ] Unsaved form draft auto-saved to localStorage

- [ ] **Testing**
  - [ ] Components tested with React Testing Library (not Enzyme or mount)
  - [ ] RTK Query hooks mocked with `vi.mock()`
  - [ ] User interactions tested (clicks, form fills, not implementation)
  - [ ] API state reset between tests (`baseApi.util.resetApiState()`)
  - [ ] At least 70% coverage on components (excluding styles, types)

- [ ] **Operations & Governance**
  - [ ] Critical user flows emit telemetry (route, API, funnel milestones)
  - [ ] Frontend-backend correlation IDs are captured for error triage
  - [ ] New feature flags include owner, expiry, rollout plan, cleanup task

---

## Anti-Patterns to Avoid

#### Using Relative Paths Instead of `@` Alias
```typescript
// BAD — relative paths are hard to refactor
import Button from '../../../../shared/components/ui/Button';
import { useGetOrdersQuery } from '../../api/ordersApi';
import useForm from '../../../hooks/useForm';

// GOOD — use @ alias everywhere (consistent and easy to refactor)
import Button from '@/shared/components/ui/Button';
import { useGetOrdersQuery } from '@/features/orders/api/ordersApi';
import useForm from '@/shared/hooks/useForm';

// Exception: same-directory relative imports are acceptable
import { sibling } from './sibling.tsx';  // OK — same folder
```

#### Mixing Icons with Other Components
```typescript
// BAD — icons in component files
// src/features/products/components/ProductCard.tsx
const StarIcon = () => (
  <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
    {/* ... */}
  </svg>
);

export function ProductCard() {
  return <StarIcon />;  // Scattered, hard to reuse
}

// GOOD — icons in centralized library, imported as needed
// src/shared/components/icons/StarIcon.tsx
export default function StarIcon(props: SVGProps<SVGSVGElement>) { ... }

// src/features/products/components/ProductCard.tsx
import { StarIcon } from '@/shared/components/icons';

export function ProductCard() {
  return <StarIcon />;  // Centralized, reusable everywhere
}
```

**Rule:** Every icon goes in `src/shared/components/icons/` in its own file. Never embed SVG or icon components in feature files.

#### Selecting Entire State
```typescript
// BAD — re-renders on ANY state change
const allState = useAppSelector(state => state.ui);

// GOOD — select only what you need
const pageNumber = useAppSelector(state => state.ui.selectedFilters.pageNumber);
```

#### Storing API Data in Redux
```typescript
// BAD — RTK Query already manages this
const productsSlice = createSlice({
  name: 'products',
  initialState: { items: [], loading: false, error: null },
  reducers: { ... }
});

// GOOD — let RTK Query handle it
const { data, isLoading, error } = useGetProductsQuery(params);
```

#### Creating Separate `createApi` Instances
```typescript
// BAD — creates separate middleware, separate cache
export const productApi = createApi({ reducerPath: 'productApi', ... });

// GOOD — inject into shared baseApi
const productApiSlice = baseApi.injectEndpoints({ endpoints: (builder) => ({ ... }) });
```

#### Using `tags` Instead of `providesTags`
```typescript
// BAD — `tags` is not a valid RTK Query property
getProducts: builder.query({
  query: () => '/products',
  tags: ['Products'],  // This does nothing!
}),

// GOOD — use providesTags
getProducts: builder.query({
  query: () => '/products',
  providesTags: ['Products'],
}),
```

#### Casting Errors with `as any`
```typescript
// BAD — loses type safety
} catch (error) {
  const apiError = error as any;
  toast.error(apiError?.data?.error?.message || 'Unknown error');
}

// GOOD — use centralized handler
const { handleError } = useApiErrorHandler();
try {
  await mutation.unwrap();
} catch (error) {
  handleError(error, 'Operation failed');
}
```

#### Ignoring isLoading vs isFetching
```typescript
const { data, isLoading, isFetching } = useGetProductsQuery(params);

// isLoading: true only on FIRST load (no cached data)
// isFetching: true on ANY request (including background refetch)

if (isLoading) return <Skeleton />;       // First load — full skeleton
if (isFetching) return <RefreshIcon />;   // Background refetch — subtle indicator
```

#### Not Resetting Cache in Tests
```typescript
// BAD — cache bleeds between tests
describe('ProductList', () => {
  it('loads products', () => { ... });
  it('handles error', () => { ... });  // May see cached data from previous test
});

// GOOD — reset between tests
afterEach(() => {
  store.dispatch(baseApi.util.resetApiState());
});
```

---

## File Structure

```
src/
├── features/                 ← Feature modules (self-contained)
│   ├── auth/
│   │   ├── api/              (authApi.ts)
│   │   ├── pages/            (LoginPage, RegisterPage, ...)
│   │   ├── slices/           (authSlice.ts)
│   │   ├── components/
│   │   └── hooks/
│   ├── cart/
│   ├── products/
│   ├── orders/
│   ├── profile/
│   ├── checkout/
│   └── wishlist/
│
├── shared/                   ← Cross-feature infrastructure
│   ├── components/
│   │   ├── ui/               (Button, Input, Card, Skeleton, Pagination)
│   │   ├── layouts/          (Header, Footer)
│   │   ├── icons/
│   │   ├── ErrorBoundary/
│   │   ├── QueryRenderer/
│   │   ├── Toast/
│   │   └── ...
│   ├── hooks/
│   │   ├── useApiErrorHandler.ts
│   │   ├── useForm.ts
│   │   ├── useToast.ts
│   │   └── useOnlineStatus.ts
│   ├── lib/
│   │   ├── api/baseApi.ts    (Shared RTK Query instance)
│   │   ├── store/store.ts    (Redux store)
│   │   ├── store/hooks.ts    (useAppDispatch, useAppSelector)
│   │   ├── test/setup.ts     (Vitest setup)
│   │   └── utils/
│   ├── types/index.ts        (Shared TypeScript types)
│   └── i18n/                 (Internationalization)
│
├── pages/                    ← Top-level pages (HomePage, CheckoutPage, static pages)
├── config.ts                 ← Environment config
├── App.tsx                   ← Routes + code splitting
└── main.tsx                  ← Entry point
```

---

## Support & Feedback

- **Question?** Check the rules by priority (P0 → P1 → P2)
- **Implementing a feature?** Follow the **PR Checklist** (critical for passing review)
- **Design pattern?** See **AsyncData**, **Optimistic Updates**, **Race Conditions**, **Testing Pyramid** sections
- **Performance issue?** Check **Render Optimization with React Compiler**, **Selector Optimization**, **State Domain Matrix** sections
- **Architecture decision?** Consult **Data Flow Architecture** and **State Domain Matrix**
- **Inconsistency found?** Update this guide in the same PR — it's a living document

**Last Updated**: March 2026 | **Stack**: React 19 + Redux Toolkit + RTK Query | **Status**: Production-Ready
