# Frontend API Integration Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Standardize how frontend features call backend APIs and handle auth/errors.

## Core Rules
1. Define endpoints via `baseApi.injectEndpoints(...)`.
2. Unwrap `ApiResponse<T>` in `transformResponse`.
3. Keep auth refresh/retry behavior in base API wrapper, not components.
4. Use tags for cache invalidation and re-fetch behavior.
5. Handle errors through shared hook/utilities, not ad-hoc per component.

## Storefront Baseline
- Base query includes credentials and CSRF token handling.
- 401 responses attempt refresh-token flow.
- Telemetry tracks API request duration and success.

## Real Code References
- Base API wrapper: `src/frontend/storefront/src/shared/lib/api/baseApi.ts`
- Error handler hook: `src/frontend/storefront/src/shared/hooks/useApiErrorHandler.ts`
- Product API: `src/frontend/storefront/src/features/products/api/productApi.ts`
- Cart API: `src/frontend/storefront/src/features/cart/api/cartApi.ts`
- Orders API: `src/frontend/storefront/src/features/orders/api/ordersApi.ts`

## Common Mistakes
- Calling `fetch` directly in components for feature APIs.
- Returning raw envelope into UI instead of clean `data` payload.
- Missing invalidation tags after write mutations.
- Duplicating auth retry logic inside individual features.

## Checklist
- [ ] Endpoint defined through `baseApi.injectEndpoints`.
- [ ] `transformResponse` returns usable data shape.
- [ ] Tags configured for coherent cache updates.
- [ ] Errors surface via shared error handling.
