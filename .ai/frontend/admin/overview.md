# Admin Panel — Architecture Overview

Updated: 2026-04-09
Owner: @ivans

## Purpose

Internal backoffice for managing the e-commerce platform. Not publicly accessible — all routes require admin authentication.

---

## Tech Stack

- React 18 + TypeScript + Vite
- React Router v6 (Library Mode — `<BrowserRouter>` + manual `<Routes>` tree in `App.tsx`)
- RTK Query for all API calls — flat `store/api/` folder, one `createApi` per domain
- Redux for auth and toast state only (`authSlice`, `toastSlice`)
- CSS Modules (`*.module.css`) — same rule as storefront, no Tailwind utility classes in source
- Vitest + Playwright for tests

**Entry point:** `src/App.tsx`
**Dev server:** `npm run dev` (port configured in `vite.config.ts`)
**Build:** `npm run build`

---

## Key architectural differences from storefront

| Concern | Admin | Storefront |
|---|---|---|
| Router | RR6 `<BrowserRouter>` + `<Routes>` in `App.tsx` | RR7 Framework Mode (`flatRoutes()`) |
| Auth guard | `<ProtectedRoute>` wrapper component | `_protected.tsx` pathless layout |
| API setup | One `createApi` per domain in `store/api/` | `baseApi.injectEndpoints` per feature |
| API base query | `csrfBaseQuery` from `utils/apiFactory.ts` | `baseQueryWithReauth` (handles 401 + token refresh) |
| Token refresh | Not implemented — session expires and redirects to login | Automatic silent refresh |
| i18n | None — English only | `react-i18next`, en + bg |
| State | 2 slices: `auth`, `toast` | Feature slices + RTK Query cache per feature |
| Code splitting | None — all pages in one bundle | Automatic per route (RR7 Framework Mode) |
| SSR | No | No (`ssr: false` in `react-router.config.ts`) |

---

## Project structure

```
src/
├── App.tsx                   ← Provider tree + Router + full route tree
├── main.tsx                  ← ReactDOM.createRoot entry
├── config.ts                 ← Runtime config (API URL, env flags)
├── components/               ← Shared components used across pages
│   ├── AuthInitializer.tsx   ← Bootstraps auth state from cookie/session on mount
│   ├── ErrorBoundary.tsx
│   ├── Header.tsx
│   ├── ProductForm.tsx       ← Reusable create/edit product form
│   ├── PromoCodeForm.tsx     ← Reusable create/edit promo code form
│   ├── ProtectedRoute.tsx    ← Auth guard: checks authSlice, redirects to /login
│   ├── QueryRenderer.tsx     ← Wraps RTK Query result: loading/error/children
│   ├── Sidebar.tsx
│   ├── Toast/
│   └── ui/                   ← Primitive UI: Button, Input, Card, Badge, Modal, Table, Pagination, ConfirmationDialog
├── hooks/                    ← Custom hooks
│   ├── useConfirmation.ts    ← Controls ConfirmationDialog open/close + callback
│   ├── useCrudModal.ts       ← Controls create/edit modal with selected item state
│   ├── useForm.ts            ← Simple controlled form state (not the storefront Pattern A)
│   └── useToast.ts
├── layouts/
│   └── AdminLayout.tsx       ← Header + Sidebar + <Outlet> shell for all protected pages
├── pages/                    ← One file per admin section
│   ├── Dashboard.tsx
│   ├── Products.tsx
│   ├── Inventory.tsx
│   ├── Orders.tsx
│   ├── PromoCodes.tsx
│   ├── Reviews.tsx
│   ├── Customers.tsx
│   ├── Settings.tsx
│   └── Login.tsx
├── store/
│   ├── store.ts              ← configureStore: all reducers registered here
│   ├── hooks.ts              ← useAppDispatch, useAppSelector
│   ├── api/                  ← One createApi per domain (see API section below)
│   └── slices/
│       ├── authSlice.ts      ← Auth state: user, token, isAuthenticated, loading
│       └── toastSlice.ts
├── test/setup.ts             ← Vitest setup
├── types/index.ts            ← Shared TypeScript types
└── utils/
    ├── apiFactory.ts         ← csrfBaseQuery shared by all API slices
    ├── constants.ts
    ├── csrf.ts               ← getCsrfToken() reads cookie
    ├── formatters.ts         ← Date, currency, number formatters
    ├── logger.ts
    └── validation.ts         ← Form validation helpers
```

---

## Routing

All routes are registered manually in `src/App.tsx`. There is no file-based routing.

```tsx
// App.tsx — complete route tree
<Routes>
  <Route path="/login" element={<Login />} />
  <Route element={<ProtectedRoute><AdminLayout /></ProtectedRoute>}>
    <Route path="/"             element={<Dashboard />} />
    <Route path="/products"     element={<Products />} />
    <Route path="/orders"       element={<Orders />} />
    <Route path="/reviews"      element={<Reviews />} />
    <Route path="/customers"    element={<Customers />} />
    <Route path="/promo-codes"  element={<PromoCodes />} />
    <Route path="/inventory"    element={<Inventory />} />
    <Route path="/settings"     element={<Settings />} />
  </Route>
  <Route path="*" element={<Navigate to="/" replace />} />
</Routes>
```

**Adding a new admin page:**
1. Create `src/pages/MyPage.tsx`
2. Add `<Route path="/my-page" element={<MyPage />} />` inside the protected block in `App.tsx`
3. Add a sidebar link in `src/components/Sidebar.tsx`

**Auth guard — `ProtectedRoute`:** Reads `isAuthenticated` from `authSlice`. If false, redirects to `/login`. No route loader involved.

---

## API layer

Each domain has its own `createApi` in `store/api/`. All use the shared `csrfBaseQuery` from `utils/apiFactory.ts`.

```ts
// utils/apiFactory.ts
export const csrfBaseQuery = fetchBaseQuery({
  baseUrl: import.meta.env.VITE_API_URL || 'http://localhost:5000/api',
  credentials: 'include',          // sends cookies for session auth
  prepareHeaders: (headers) => {
    const csrfToken = getCsrfToken();
    if (csrfToken) headers.set('X-XSRF-TOKEN', csrfToken);
    return headers;
  },
});
```

| API slice | File | Domains |
|---|---|---|
| `productsApi` | `store/api/productsApi.ts` | CRUD products, stock updates |
| `ordersApi` | `store/api/ordersApi.ts` | List orders, update status |
| `inventoryApi` | `store/api/inventoryApi.ts` | Stock levels |
| `reviewsApi` | `store/api/reviewsApi.ts` | List/approve/delete reviews |
| `customersApi` | `store/api/customersApi.ts` | List customers |
| `promoCodesApi` | `store/api/promoCodesApi.ts` | CRUD promo codes |
| `dashboardApi` | `store/api/dashboardApi.ts` | Stats and aggregates |
| `authApi` | `store/api/authApi.ts` | Login, logout, get current user |

**Adding a new endpoint:**
1. Add the endpoint to the existing `createApi` for that domain — do not create a new `createApi`
2. Add `transformResponse` to unwrap `ApiResponse<T>` to clean data (same pattern as storefront)
3. Use `providesTags`/`invalidatesTags` for cache coherence

**Why separate `createApi` per domain (not `injectEndpoints` like storefront):**
The admin app predates the storefront's shared `baseApi` pattern. All admin API state is simpler (no auth refresh, no telemetry), so the added complexity of a shared base was not warranted.

---

## State management

Only two Redux slices exist in the admin:

| Slice | State | Used for |
|---|---|---|
| `authSlice` | `user`, `token`, `isAuthenticated`, `loading` | Auth guard, header user display, logout |
| `toastSlice` | `toasts[]` | Notification queue |

All other data lives in RTK Query cache (the API slices above). Do not create additional slices — if you need derived data, use a selector.

---

## Component patterns

### QueryRenderer
Wraps RTK Query results to handle loading/error states without repeating guard clauses in every page:

```tsx
<QueryRenderer isLoading={isLoading} error={error}>
  <ProductList products={data} />
</QueryRenderer>
```

### useCrudModal
Controls a create/edit modal that operates on a typed item:

```tsx
const { isOpen, selectedItem, openCreate, openEdit, close } = useCrudModal<Product>();
// openCreate() → isOpen=true, selectedItem=null (new item)
// openEdit(product) → isOpen=true, selectedItem=product (existing item)
```

### useConfirmation
Controls a delete confirmation dialog:

```tsx
const { isOpen, confirm, cancel, pending } = useConfirmation();
// confirm(callback) → opens dialog; if user confirms, runs callback
```

### ProductForm / PromoCodeForm
Reusable forms that work for both create and edit — the `item` prop is `null` for create, a populated object for edit.

---

## Common mistakes

- **Adding a route without updating the Sidebar** — pages become unreachable via navigation even if the URL works directly
- **Creating a new `createApi` for a new endpoint** — add to the existing domain slice instead
- **Using `fetch` directly** — always use RTK Query hooks
- **Dispatching slice actions for server data** — RTK Query cache is the source of truth; slices are for auth + toast only
- **Expecting token refresh** — the admin has no refresh logic; a 401 response redirects to `/login`; users must log in again

---

## Tests

Unit/component tests: `npm run test`
E2E tests: `npm run test:e2e` (requires app running on port configured in `playwright.config.ts`)

E2E suites in `e2e/`:
- `admin-auth.spec.ts` — login, logout, protected redirect
- `admin-products.spec.ts` — CRUD product flow
- `admin-inventory.spec.ts` — stock management
- `admin-orders.spec.ts` — order status updates
- `admin-promocodes.spec.ts` — promo code CRUD
- `api-catalog-admin.spec.ts` — API contract tests for admin endpoints

---

## Read Next

- `.ai/frontend/README.md` — comparison with storefront
- `.ai/frontend/storefront/api-integration.md` — storefront API patterns (for contrast)
