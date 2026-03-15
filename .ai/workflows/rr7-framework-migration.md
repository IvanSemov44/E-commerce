# Migration Plan: React Router v7 Framework Mode

Updated: 2026-03-15
Owner: @ivans

## Current State

`react-router-dom ^7.12.0` is installed but running in **Library Mode** (classic `<Routes>/<Route>` tree in `AppRoutes.tsx`). Framework Mode unlocks file-based routing, automatic code splitting, and route loaders — the modern React Router v7 pattern.

No breaking changes to the backend. This is a pure frontend refactor.

---

## What Changes, What Stays

| Concern | Before | After |
|---|---|---|
| Route registration | Manual `<Routes>` in `AppRoutes.tsx` | Files in `app/routes/` |
| Code splitting | Manual `React.lazy()` per page | Automatic per route file |
| Auth guards | `<ProtectedRoute>` wrapper component | `loader` with `redirect()` |
| Data fetching | RTK Query only | RTK Query + loader guards |
| Path constants | `ROUTE_PATHS` constants | `ROUTE_PATHS` stays (navigation, `<Link>`) |
| RTK Query | All server state | Unchanged — loaders do not replace it |

---

## Phase 1 — Install & Configure Framework Mode

### 1.1 Install packages
```bash
npm install @react-router/dev @react-router/node
npm uninstall @vitejs/plugin-react
```

> `@react-router/dev/vite` includes its own React plugin — `@vitejs/plugin-react` is redundant after this.

### 1.2 Update `vite.config.ts`
```ts
// Before
import react from '@vitejs/plugin-react';
plugins: [react()]

// After
import { reactRouter } from '@react-router/dev/vite';
plugins: [reactRouter()]
```

### 1.3 Create `react-router.config.ts` (project root)
```ts
import type { Config } from '@react-router/dev/config';

export default {
  appDirectory: 'src/app',       // where routes/ folder lives
  ssr: false,                    // SPA mode — no server rendering yet
} satisfies Config;
```

### 1.4 Create `app/root.tsx`
This replaces `App.tsx` as the framework entry point. Move providers here:
```tsx
import { Outlet, Scripts, ScrollRestoration } from 'react-router';
import { Provider } from 'react-redux';
import { store } from '@/shared/lib/store/store';
import Layout from '@/app/layouts/Layout';

export default function Root() {
  return (
    <Provider store={store}>
      <Layout>
        <Outlet />
      </Layout>
      <ScrollRestoration />
      <Scripts />
    </Provider>
  );
}
```

### 1.5 Update `main.tsx`
```tsx
// Before: ReactDOM.createRoot(…).render(<App />)
// After:
import { HydratedRouter } from 'react-router/dom';
ReactDOM.hydrateRoot(document, <HydratedRouter />);
```

---

## Phase 2 — Create Route Files

Create `src/app/routes/` and map each current `<Route>` to a file. File naming conventions:
- `.` separates URL segments (`products.cart` → `/products/cart`)
- `$` prefix on a segment = dynamic param (`$slug` → `:slug`)
- `_name` prefix = pathless layout (no URL segment, wraps child routes)
- `_index` = index route (exact match of parent)

### Route file map

| Current `ROUTE_PATHS` | New file | URL |
|---|---|---|
| `home: '/'` | `_index.tsx` | `/` |
| `products: '/products'` | `products._index.tsx` | `/products` |
| `productDetail: '/products/:slug'` | `products.$slug.tsx` | `/products/:slug` |
| `cart: '/cart'` | `cart.tsx` | `/cart` |
| `checkout: '/checkout'` | `checkout.tsx` | `/checkout` |
| `wishlist: '/wishlist'` | `_protected.wishlist.tsx` | `/wishlist` |
| `orders: '/orders'` | `_protected.orders._index.tsx` | `/orders` |
| `orderDetail: '/orders/:orderId'` | `_protected.orders.$orderId.tsx` | `/orders/:orderId` |
| `profile: '/profile'` | `_protected.profile.tsx` | `/profile` |
| `login: '/login'` | `login.tsx` | `/login` |
| `register: '/register'` | `register.tsx` | `/register` |
| `forgotPassword: '/forgot-password'` | `forgot-password.tsx` | `/forgot-password` |
| `resetPassword: '/reset-password'` | `reset-password.tsx` | `/reset-password` |
| Content pages (`/privacy`, `/about`, …) | `privacy.tsx`, `about.tsx`, … | as-is |

### Example route file — public page
```tsx
// src/app/routes/products._index.tsx
export default function ProductsPage() {
  // exact same component as before — just re-exported
  return <ProductsPageComponent />;
}
```

### Example route file — dynamic param
```tsx
// src/app/routes/products.$slug.tsx
import { useParams } from 'react-router';

export default function ProductDetailPage() {
  const { slug } = useParams<{ slug: string }>();
  // use slug with RTK Query as before
}
```

---

## Phase 3 — Auth Guards via Loaders

Replace `<ProtectedRoute>` with a pathless layout that uses a `loader`.

### 3.1 Create `_protected.tsx`
```tsx
// src/app/routes/_protected.tsx
import { redirect, Outlet } from 'react-router';
import { store } from '@/shared/lib/store/store';

export async function loader() {
  const { auth } = store.getState();
  if (!auth.user) {
    throw redirect('/login?returnTo=' + window.location.pathname);
  }
  return null;
}

export default function ProtectedLayout() {
  return <Outlet />;
}
```

All routes named `_protected.*` are automatically children of this layout. The `loader` runs before any child renders — if not authenticated, it redirects. No component-level guard needed.

### 3.2 Delete `ProtectedRoute.tsx`
```bash
rm src/app/ProtectedRoute/ProtectedRoute.tsx
rm src/app/ProtectedRoute/ProtectedRoute.test.tsx
```

---

## Phase 4 — RTK Query + Loaders Coexistence

**Loaders do NOT replace RTK Query.** They serve different concerns:

| Concern | Tool |
|---|---|
| Auth guard / redirect | Route loader |
| Data prefetch for SSR (future) | Route loader |
| All feature data fetching | RTK Query |
| Mutations (add to cart, place order) | RTK Query mutations |
| Cache invalidation | RTK Query tags |
| Loading / error states in UI | RTK Query `isLoading`, `isError` |

The coexistence pattern: loaders validate/redirect, RTK Query owns data.

```tsx
// src/app/routes/products.$slug.tsx
import { useParams } from 'react-router';
import { useGetProductBySlugQuery } from '@/features/products/api/productApi';

// No loader needed for data — RTK Query handles it
export default function ProductDetailPage() {
  const { slug } = useParams<{ slug: string }>();
  const { data, isLoading, isError } = useGetProductBySlugQuery(slug!);
  // ...
}
```

---

## Phase 5 — Clean Up Legacy

After all routes are migrated and tested:

```bash
# Delete
rm src/app/AppRoutes.tsx
rm src/app/ProtectedRoute/ProtectedRoute.tsx
rm src/app/ProtectedRoute/ProtectedRoute.test.tsx
rm src/app/skeletons/RouteLoadingFallback/RouteLoadingFallback.tsx   # handled by Suspense in root.tsx
```

Update `ROUTE_PATHS` to remove any `:param` strings that are now unused in route registration (they remain valid for `generatePath` / `<Link to>` usage).

Remove all `React.lazy()` wrapping from what used to be in `AppRoutes.tsx`. The framework splits automatically.

---

## Phase 6 — Update Tests

- `ProtectedRoute.test.tsx` → delete (logic is now in loader, test the loader directly)
- Route-level tests: use `createMemoryRouter` from `react-router` in test setup, or use `@testing-library/react` with the framework's test utilities
- Update `useRouteTelemetry.test.tsx` if it uses old router setup

---

## Rollout Order

1. Phase 1 (install + config) — isolated, no user-facing change
2. Phase 2 (one route at a time) — migrate leaf routes first, verify each
3. Phase 3 (auth guards) — after all protected routes exist as files
4. Phase 5 (cleanup) — after full regression pass
5. Phase 6 (tests) — parallel with Phase 2–3

---

## Risks & Mitigations

| Risk | Mitigation |
|---|---|
| `ROUTE_PATHS` mismatches | Keep constants unchanged; file names mirror them |
| Redux store not available in loader | Access via `store.getState()` directly (store is a singleton) |
| Telemetry hook breaks | Update `useRouteTelemetry` to use `useLocation` from `react-router` (API unchanged) |
| Docker HMR during migration | No change — Vite HMR works the same with `@react-router/dev` |
