# Frontend Overview

Updated: 2026-03-08
Owner: @ivans

## Purpose
Quick orientation for storefront/admin frontend architecture.

## Architecture Snapshot
- React 19 + TypeScript + Vite 7.
- React Router v7 Framework Mode (file-based routes via `flatRoutes()`). Auth guards use component-style redirect pending migration to loader-based redirects.
- Server state via RTK Query (`baseApi.injectEndpoints`).
- Redux slices for UI/local state only.
- Shared API base with auth refresh and telemetry in storefront.
- Auth guard: `_protected.tsx` pathless layout wraps all protected routes.

## Non-Negotiable Rules
- Do not use manual `fetch` in feature components for API workflows.
- Unwrap `ApiResponse<T>` in API layer (`transformResponse`) so UI gets clean data.
- Use typed hooks/selectors and avoid `any` in production code.
- Keep cross-cutting utilities in shared modules.

## Real Code References
- Storefront API base: `src/frontend/storefront/src/shared/lib/api/baseApi.ts`
- Storefront store: `src/frontend/storefront/src/shared/lib/store/store.ts`
- Example API feature: `src/frontend/storefront/src/features/products/api/productApi.ts`
- Example slices: `src/frontend/storefront/src/features/auth/slices/authSlice.ts`, `src/frontend/storefront/src/features/cart/slices/cartSlice.ts`
- Error handling hook: `src/frontend/storefront/src/shared/hooks/useApiErrorHandler.ts`

## Read Next
- `.ai/frontend/storefront/redux.md`
- `.ai/frontend/storefront/routing.md`
- `.ai/frontend/storefront/route-loaders.md`
- `.ai/workflows/adding-feature.md`
- `.ai/reference/common-mistakes.md`
