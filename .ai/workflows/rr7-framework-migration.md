# Migration Record: React Router v7 Framework Mode

Updated: 2026-04-08
Owner: @ivans

> **STATUS: Framework Mode is live as of Phase 9. Phases 1, 2, and 5 are complete.**
> This document is a record of what was done and what remains — not a future plan.

---

## What Changed

| Concern | Before | After |
|---|---|---|
| Route registration | Manual `<Routes>` in `AppRoutes.tsx` | Files in `src/app/routes/` |
| Code splitting | Manual `React.lazy()` per page | Automatic per route file |
| Auth guards | `<ProtectedRoute>` wrapper component | Component-style `<Navigate>` (loader pattern pending — see Phase 3) |
| Data fetching | RTK Query only | RTK Query + loader guards (loaders for guards only) |
| Path constants | `ROUTE_PATHS` constants | `ROUTE_PATHS` stays (navigation, `<Link>`) |
| RTK Query | All server state | Unchanged — loaders do not replace it |

---

## Phase 1 — Install & Configure Framework Mode ✅ DONE

### What was done
- Installed `@react-router/dev` and `@react-router/node`
- Updated `vite.config.ts` to use `reactRouter()` from `@react-router/dev/vite`
- Created `react-router.config.ts` at project root:
  ```ts
  import type { Config } from '@react-router/dev/config';
  export default {
    appDirectory: 'src/app',
    ssr: false,
  } satisfies Config;
  ```
- Created `src/app/root.tsx` as the framework entry point (replaces `App.tsx`)
- Updated `main.tsx` to use `HydratedRouter`

### Key files (current state)
- `src/app/root.tsx` — providers, layout shell, `<Outlet>`
- `src/app/routes.ts` — `export default flatRoutes() satisfies RouteConfig`
- `vite.config.ts` — `plugins: [reactRouter()]`
- `react-router.config.ts` — `appDirectory: 'src/app'`, `ssr: false`

---

## Phase 2 — Create Route Files ✅ DONE

All routes from the old `AppRoutes.tsx` are now file-based in `src/app/routes/`.

### Route file map (implemented)

| URL | File |
|---|---|
| `/` | `_index.tsx` |
| `/products` | `products._index.tsx` |
| `/products/:slug` | `products.$slug.tsx` |
| `/cart` | `cart.tsx` |
| `/checkout` | `checkout.tsx` |
| `/wishlist` | `_protected.wishlist.tsx` |
| `/orders` | `_protected.orders._index.tsx` |
| `/orders/:orderId` | `_protected.orders.$orderId.tsx` |
| `/profile` | `_protected.profile.tsx` |
| `/login` | `login.tsx` |
| `/register` | `register.tsx` |
| `/forgot-password` | `forgot-password.tsx` |
| `/reset-password` | `reset-password.tsx` |
| Static content pages | `about.tsx`, `blog.tsx`, `careers.tsx`, … |

### File naming conventions
- `.` separates URL segments (`products.cart` → `/products/cart`)
- `$` prefix on a segment = dynamic param (`$slug` → `:slug`)
- `_name` prefix = pathless layout (no URL segment, wraps child routes)
- `_index` = index route (exact match of parent)

---

## Phase 3 — Auth Guards via Loaders 🚧 PARTIAL

### Current state
`_protected.tsx` exists and wraps all `_protected.*` routes. However, it uses a **component-style guard** (`useAppSelector` + `<Navigate>`), not the idiomatic loader-based approach.

```tsx
// src/app/routes/_protected.tsx (current — component-style)
import { Outlet, Navigate } from 'react-router';
import { selectAuthStatus } from '@/features/auth/slices/authSlice';
import { useAppSelector } from '@/shared/lib/store';
import { ROUTE_PATHS } from '@/shared/constants/navigation';

export default function ProtectedLayout() {
  const { isAuthenticated, loading } = useAppSelector(selectAuthStatus);

  if (loading) return <LoadingSpinner />;
  if (!isAuthenticated) return <Navigate to={ROUTE_PATHS.login} replace />;

  return <Outlet />;
}
```

**What still needs to be done:**
Replace the component body with a `loader` that runs before any child renders:

```tsx
// src/app/routes/_protected.tsx (target — loader-based)
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

**Why the loader pattern is better:**
- Runs before child components render — no flash of protected content
- `throw redirect()` is handled by the framework before any React renders
- No need to check auth state inside page components

**When to implement:** When the loading spinner UX during auth bootstrap needs to be improved, or when SSR is added. The current component-style guard works correctly for the SPA use case.

---

## Phase 4 — RTK Query + Loaders Coexistence ✅ DONE (by design)

Loaders do NOT replace RTK Query. They serve different concerns:

| Concern | Tool |
|---|---|
| Auth guard / redirect | Route loader |
| All feature data fetching | RTK Query |
| Mutations (add to cart, place order) | RTK Query mutations |
| Cache invalidation | RTK Query tags |
| Loading / error states in UI | RTK Query `isLoading`, `isError` |

The current pattern: loaders validate/redirect, RTK Query owns data. This is correct and requires no further changes.

---

## Phase 5 — Clean Up Legacy ✅ DONE

- `AppRoutes.tsx` deleted
- `ProtectedRoute.tsx` deleted
- Manual `React.lazy()` wrappings removed
- `ROUTE_PATHS` constants retained for `<Link>`, `useNavigate`, `generatePath`

---

## Phase 6 — Update Tests

Route-level component tests: use `MemoryRouter` from `test-utils.tsx` (`renderWithProviders` with `withRouter: true`). `test-utils.tsx` already uses `MemoryRouter`.

Loader tests (when Phase 3 is implemented): test the `loader` function directly by calling it and asserting the redirect response.

---

## What Remains

| Item | Status |
|---|---|
| Loader-based auth guard in `_protected.tsx` | Pending — see Phase 3 above |
| Streaming / Suspense improvements | Not started |
| Coverage enforcement for route files | Pending (Phase T-5) |
