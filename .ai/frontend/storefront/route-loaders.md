# Route Loaders Standard

Updated: 2026-03-15
Owner: @ivans

> **SPA Mode constraint:** This project runs with `ssr: false` in `react-router.config.ts`.
> Route `loader` exports are **not supported in SPA mode** — the framework will error at build time.
> Loaders only become available if/when SSR is enabled (`ssr: true`).
> Until then, auth guards and redirects stay as component-level logic in layout files (see `_protected.tsx`).

---

## Purpose

Route loaders run **before** a route renders. They are the correct place for:
- Auth guards and redirects
- Permission checks
- Prefetching data that must be ready before paint (future SSR)

They are **not** a replacement for RTK Query.

---

## Responsibility Split

| Concern | Owner |
|---|---|
| Auth guard / redirect | Route `loader` |
| Role / permission check | Route `loader` |
| Feature data (products, cart, orders…) | RTK Query |
| Mutations (add to cart, checkout…) | RTK Query mutations |
| UI loading / error states | RTK Query `isLoading`, `isError` |
| Cache invalidation | RTK Query tags |

The rule: if it involves fetching and caching domain data, it belongs in RTK Query. If it is a gate that decides whether the route renders at all, it belongs in a loader.

---

## Auth Guard Loader

```tsx
// src/app/routes/_protected.tsx
import { redirect, Outlet } from 'react-router';
import { store } from '@/shared/lib/store/store';

export async function loader({ request }: { request: Request }) {
  const { auth } = store.getState();
  if (!auth.user) {
    const returnTo = new URL(request.url).pathname;
    throw redirect(`/login?returnTo=${encodeURIComponent(returnTo)}`);
  }
  return null;
}

export default function ProtectedLayout() {
  return <Outlet />;
}
```

All routes named `_protected.*` inherit this loader automatically. No wrapper component needed.

---

## Loader + RTK Query Coexistence

The loader guards the route. RTK Query fetches the data. They do not share state.

```tsx
// src/app/routes/_protected.orders.$orderId.tsx
import { useParams } from 'react-router';
import { useGetOrderQuery } from '@/features/orders/api/ordersApi';

// No loader here — auth guard is inherited from _protected.tsx
export default function OrderDetailPage() {
  const { orderId } = useParams<{ orderId: string }>();
  const { data: order, isLoading, isError } = useGetOrderQuery(orderId!);

  if (isLoading) return <OrderDetailSkeleton />;
  if (isError || !order) return <ErrorState />;
  return <OrderDetail order={order} />;
}
```

---

## Accessing Route Params in Loaders

```tsx
export async function loader({ params }: { params: { slug: string } }) {
  // params.slug is the dynamic segment value
  if (!params.slug) throw redirect('/products');
  return null;
}
```

---

## Error Boundaries

Each route can export an `ErrorBoundary` component — it renders when the loader throws a non-redirect error:

```tsx
export function ErrorBoundary() {
  return <div>Something went wrong loading this page.</div>;
}
```

---

## What NOT to Do

```tsx
// ❌ Wrong: fetching data in a loader when RTK Query handles it
export async function loader() {
  const res = await fetch('/api/products');   // bypasses RTK Query cache
  return res.json();
}

// ✅ Correct: loader only guards, RTK Query fetches
export async function loader() {
  const { auth } = store.getState();
  if (!auth.user) throw redirect('/login');
  return null;
}
```

```tsx
// ❌ Wrong: auth guard still in component after migration
export default function ProfilePage() {
  const { user } = useAppSelector(selectUser);
  if (!user) return <Navigate to="/login" />;   // too late — page already rendered
  return <Profile />;
}

// ✅ Correct: redirect happens in loader before any render
```

---

## Testing Loaders

Test loaders as plain async functions — no React needed:

```ts
import { loader } from '@/app/routes/_protected';

test('redirects unauthenticated users', async () => {
  mockStore({ auth: { user: null } });
  await expect(loader({ request: new Request('http://localhost/orders') }))
    .rejects.toMatchObject({ status: 302 });
});
```
