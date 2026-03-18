# ADR 003 — RTK Query for Frontend Server State

**Status:** Accepted
**Date:** 2025
**Deciders:** Frontend team

---

## Context

The storefront and admin panel need to fetch, cache, and mutate data from the .NET API. We needed a consistent approach that:
- Avoids manual loading/error state boilerplate in every component
- Provides automatic caching and cache invalidation
- Integrates with existing Redux store (auth slice, cart slice)
- Keeps components clean — no `fetch()` calls directly in JSX

## Decision

Use **RTK Query** (`@reduxjs/toolkit`) as the single mechanism for all server state.

All API calls are defined via `baseApi.injectEndpoints()` in feature-specific files:

```
src/features/auth/api/authApi.ts
src/features/products/api/productApi.ts
src/features/cart/api/cartApi.ts
...
```

Components consume generated hooks (`useGetProductsQuery`, `useCreateOrderMutation`), never calling `fetch` or `axios` directly.

**Redux slices** (`authSlice`, `cartSlice`, `toastSlice`) manage UI-only state — they never hold server data.

## Alternatives considered

| Option | Why rejected |
|--------|-------------|
| TanStack Query (React Query) | Excellent library, but we're already using Redux for auth/cart UI state. Using both Redux + React Query means two separate caching layers and two devtools to debug |
| SWR | Simpler API but less control over cache invalidation and optimistic updates; no mutations pattern |
| Manual fetch in components | No caching, duplicated loading/error state, untestable, hard to invalidate |
| Apollo Client | GraphQL only; our backend is REST |

## Consequences

**Good:**
- Automatic cache deduplication — multiple components requesting the same product don't fire multiple requests
- `providesTags` / `invalidatesTags` makes cache invalidation explicit and declarative
- Optimistic updates built in (used in cart mutations)
- One devtools (Redux DevTools) covers both UI state and server state
- Generated hooks are strongly typed from the DTO types

**Watch out for:**
- Never put server data into a Redux slice — RTK Query is the single source of truth for it
- Tag invalidation is manual — if you add a mutation, add the `invalidatesTags` or the UI won't refresh
- `transformResponse` should be used to normalize/flatten API responses, not in components
- For paginated queries, use `serializeQueryArgs` + `merge` to append pages correctly

## Rules enforced

From `CLAUDE.md`:
> Frontend API calls use RTK Query (`baseApi.injectEndpoints`), not manual fetch in components.
> Frontend server state stays in RTK Query; slices manage UI state.
