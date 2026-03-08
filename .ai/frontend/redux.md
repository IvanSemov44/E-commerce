# Frontend Redux + RTK Query Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Define state-management boundaries and API integration conventions.

## Core Rules
1. RTK Query owns server state.
2. Slices own UI/local state.
3. Feature APIs are created with `baseApi.injectEndpoints(...)`.
4. API responses are unwrapped with `transformResponse`.
5. Use tags (`providesTags` / `invalidatesTags`) to control cache refresh.

## Storefront Pattern
- Single `baseApi` with wrappers for re-auth and telemetry.
- Features inject their own endpoints.
- 401 handling retries via refresh-token flow before logout.

## Real Code References
- Base API: `src/frontend/storefront/src/shared/lib/api/baseApi.ts`
- Product API: `src/frontend/storefront/src/features/products/api/productApi.ts`
- Cart API: `src/frontend/storefront/src/features/cart/api/cartApi.ts`
- Orders API: `src/frontend/storefront/src/features/orders/api/ordersApi.ts`
- Auth slice: `src/frontend/storefront/src/features/auth/slices/authSlice.ts`
- Cart slice: `src/frontend/storefront/src/features/cart/slices/cartSlice.ts`

## Common Mistakes
- Storing API collections in slices instead of RTK Query cache.
- Skipping `transformResponse` and forcing components to parse envelopes.
- Omitting cache invalidation tags after mutations.
- Implementing auth retry logic in components instead of base API.

## Checklist
- [ ] Endpoint defined via `baseApi.injectEndpoints`.
- [ ] `transformResponse` unwraps `ApiResponse<T>`.
- [ ] Cache tags are configured for query/mutation coherence.
- [ ] Slice only stores UI/local concerns.
