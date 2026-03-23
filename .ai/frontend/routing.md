# Frontend Routing Standard

Updated: 2026-03-15
Owner: @ivans

> **Status:** Currently running React Router v7 in **Library Mode** (manual `<Routes>` tree).
> Migration to **Framework Mode** (file-based routing) is planned. See `.ai/workflows/rr7-framework-migration.md`.

---

## Core Rules (apply to both modes)

1. Route path strings live in `ROUTE_PATHS` — never hardcode `/products` in JSX.
2. Authenticated routes are guarded at the router level, not inside page components.
3. Keep layout concerns (header/footer) separate from page business logic.
4. Code-split non-critical pages so startup bundle stays lean.
5. Route params and query parsing are typed — use `useParams<{ slug: string }>()`, never cast from `any`.

---

## Current Mode: Library Mode

Route registration is manual in `AppRoutes.tsx`. Auth guarding uses `<ProtectedRoute>`.

```tsx
// src/app/AppRoutes.tsx
<Routes>
  <Route path={ROUTE_PATHS.home} element={<Home />} />
  <Route path={ROUTE_PATHS.productDetail} element={<ProductDetail />} />
  <Route
    path={ROUTE_PATHS.orders}
    element={<ProtectedRoute><OrderHistory /></ProtectedRoute>}
  />
</Routes>
```

**Key files (current):**
- Route tree: `src/app/AppRoutes.tsx`
- Auth guard: `src/app/ProtectedRoute/ProtectedRoute.tsx`
- Path constants: `src/shared/constants/navigation.ts`
- App bootstrap: `src/App.tsx`, `src/main.tsx`

---

## Target Mode: Framework Mode (post-migration)

File name IS the route. No manual registration. Auth guards move to route loaders.

### File naming conventions

| Convention | Meaning | Example |
|---|---|---|
| `_index.tsx` | index route (exact match) | `products/_index.tsx` → `/products` |
| `$param` | dynamic segment | `products.$slug.tsx` → `/products/:slug` |
| `_layout` | pathless layout wrapper | `_protected.tsx` wraps all `_protected.*.tsx` |
| `.` separator | URL path segment | `orders.$orderId.tsx` → `/orders/:orderId` |

### Folder structure (post-migration)
```
src/app/routes/
├── _index.tsx                     →  /
├── products._index.tsx            →  /products
├── products.$slug.tsx             →  /products/:slug
├── cart.tsx                       →  /cart
├── checkout.tsx                   →  /checkout
├── _protected.tsx                 →  pathless layout: auth guard loader
├── _protected.wishlist.tsx        →  /wishlist
├── _protected.orders._index.tsx   →  /orders
├── _protected.orders.$orderId.tsx →  /orders/:orderId
├── _protected.profile.tsx         →  /profile
├── login.tsx                      →  /login
├── register.tsx                   →  /register
└── ...
```

### Auth guard pattern (post-migration)
```tsx
// src/app/routes/_protected.tsx
import { redirect, Outlet } from 'react-router';
import { store } from '@/shared/lib/store/store';

export async function loader() {
  const { auth } = store.getState();
  if (!auth.user) throw redirect('/login');
  return null;
}

export default function ProtectedLayout() {
  return <Outlet />;
}
```

> **Note on exports:** Route module components use `export default` — this is a React Router framework requirement for file-based routing (`flatRoutes()`). This is the **only** place in the frontend where `export default` is allowed. All other components must use named exports (`export function`).

### Adding a new route (post-migration)
Create a file. That is the only step. No `AppRoutes.tsx` to touch.

---

## Navigation Constants (both modes)

`ROUTE_PATHS` stays in both modes. Use it for `<Link to>`, `useNavigate`, and `generatePath`.

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
- Auth logic inside page components — guards belong at router level.
- Manually wrapping lazy components post-migration — Framework Mode splits automatically.
- Replacing RTK Query with route loaders — loaders do guards and redirects; RTK Query owns data.

## Read Next

- `.ai/frontend/route-loaders.md` — loader pattern, RTK Query coexistence
- `.ai/workflows/rr7-framework-migration.md` — step-by-step migration plan
