# Frontend Type Safety Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Keep frontend code safe and refactor-friendly through strict typing.

## Core Rules
1. Avoid `any` in production feature code.
2. Type API request/response contracts explicitly.
3. Type component props and hook returns.
4. Keep shared contracts in shared types when reused across features.
5. Prefer narrow, domain-specific types over generic object maps.

## Real Code References
- Shared types: `src/frontend/storefront/src/shared/types/index.ts`
- API base contracts: `src/frontend/storefront/src/shared/lib/api/baseApi.ts`
- Typed slices:
  - `src/frontend/storefront/src/features/auth/slices/authSlice.ts`
  - `src/frontend/storefront/src/features/cart/slices/cartSlice.ts`
- Typed hooks:
  - `src/frontend/storefront/src/shared/hooks/useApiErrorHandler.ts`
  - `src/frontend/storefront/src/shared/hooks/useForm.ts`

## Common Mistakes
- Leaking `any` from utility/helper layers into features.
- Treating API response envelopes as untyped objects.
- Overloading one DTO/type for unrelated UI and API concerns.

## Checklist
- [ ] Feature exports typed API hooks and props.
- [ ] Shared contracts extracted when reused.
- [ ] No `any` introduced in production app code.
- [ ] Error objects are handled through typed guards/helpers.
