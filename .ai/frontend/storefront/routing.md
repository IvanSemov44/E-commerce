# Frontend Routing Standard

Updated: 2026-04-08
Owner: @ivans

> **Status:** React Router v7 **Framework Mode** is live. File-based routes via `flatRoutes()` are the current state — not a future plan.

---

## Core Rules

1. Route path strings live in `ROUTE_PATHS` — never hardcode `/products` in JSX.
2. Authenticated routes are guarded at the router level, not inside page components.
3. Keep layout concerns (header/footer) separate from page business logic.
4. Code-split non-critical pages so startup bundle stays lean.
5. Route params and query parsing are typed — use `useParams<{ slug: string }>()`, never cast from `any`.

---

## Current Mode: Framework Mode

File name IS the route. No manual registration. `flatRoutes()` in `src/app/routes.ts` picks up every file in `src/app/routes/` automatically.

**Key files:**
- Route config: `src/app/routes.ts`
- App root (providers, layout, Outlet): `src/app/root.tsx`
- Auth guard layout: `src/app/routes/_protected.tsx`
- Path constants: `src/shared/constants/navigation.ts`

### File naming conventions

| Convention | Meaning | Example |
|---|---|---|
| `_index.tsx` | index route (exact match) | `products/_index.tsx` → `/products` |
| `$param` | dynamic segment | `products.$slug.tsx` → `/products/:slug` |
| `_layout` | pathless layout wrapper | `_protected.tsx` wraps all `_protected.*.tsx` |
| `.` separator | URL path segment | `orders.$orderId.tsx` → `/orders/:orderId` |

### Current route file map

```
src/app/routes/
├── _index.tsx                     →  /
├── products._index.tsx            →  /products
├── products.$slug.tsx             →  /products/:slug
├── cart.tsx                       →  /cart
├── checkout.tsx                   →  /checkout
├── _protected.tsx                 →  pathless layout: auth guard
├── _protected.wishlist.tsx        →  /wishlist
├── _protected.orders._index.tsx   →  /orders
├── _protected.orders.$orderId.tsx →  /orders/:orderId
├── _protected.profile.tsx         →  /profile
├── login.tsx                      →  /login
├── register.tsx                   →  /register
├── forgot-password.tsx            →  /forgot-password
├── reset-password.tsx             →  /reset-password
└── about.tsx / blog.tsx / …       →  static content pages
```

### Adding a new route

Create a file in `src/app/routes/`. That is the only step. No registration needed.

- Public page: `src/app/routes/feature-name.tsx`
- Protected page: `src/app/routes/_protected.feature-name.tsx`

> **Note on exports:** Route module components use `export default` — this is a React Router framework requirement for file-based routing (`flatRoutes()`). This is the **only** place in the frontend where `export default` is allowed. All other components must use named exports (`export function`).

---

## Auth Guard

`_protected.tsx` is the pathless layout that wraps all protected routes. All routes prefixed with `_protected.` are automatically children of this layout and inherit its auth check.

**Current implementation — component-style guard:**
```tsx
// src/app/routes/_protected.tsx (current)
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

> **Pending improvement:** The component-style guard using `<Navigate>` works but runs after render. The loader + `redirect()` pattern runs before any child renders and is the idiomatic Framework Mode approach. See `.ai/workflows/rr7-framework-migration.md` Phase 3 for the target implementation.

**Target implementation — loader-based guard (not yet implemented):**
```tsx
// src/app/routes/_protected.tsx (target)
import { redirect, Outlet } from 'react-router';
import { store } from '@/shared/lib/store/store';

export async function loader() {
  const { auth } = store.getState();
  if (!auth.user) throw redirect('/login?returnTo=' + window.location.pathname);
  return null;
}

export default function ProtectedLayout() {
  return <Outlet />;
}
```

---

## Navigation Constants

`ROUTE_PATHS` is used in both `<Link to>`, `useNavigate`, and `generatePath`. Keep all path strings there — never hardcode URL strings in components or hooks.

```ts
// src/shared/constants/navigation.ts
export const ROUTE_PATHS = {
  home: '/',
  products: '/products',
  productDetail: '/products/:slug',
  orders: '/orders',
  orderDetail: '/orders/:orderId',
  // ...
};
```

Derive concrete URLs with:
```ts
import { generatePath } from 'react-router';
generatePath(ROUTE_PATHS.productDetail, { slug: product.slug });
```

---

## Common Mistakes

- Hardcoding path strings in `<Link>`, `navigate()`, or `fetch` — always use `ROUTE_PATHS`.
- Auth logic inside page components — guards belong at router level in `_protected.tsx`.
- Registering routes manually — Framework Mode uses file names, no `AppRoutes.tsx` to edit.
- Replacing RTK Query with route loaders for data fetching — loaders do guards and redirects; RTK Query owns data.

## Read Next

- `.ai/frontend/route-loaders.md` — loader pattern, RTK Query coexistence
- `.ai/workflows/rr7-framework-migration.md` — migration record and what remains
