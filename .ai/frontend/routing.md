# Frontend Routing Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Keep navigation predictable, secure, and maintainable.

## Core Rules
1. Define routes centrally and keep path naming consistent.
2. Protect authenticated routes with shared guards.
3. Keep route-level layout concerns separate from page business logic.
4. Prefer lazy loading for large page modules when it improves startup performance.
5. Keep route params and query parsing typed/validated where possible.

## Real Code References
- Route setup and providers: `src/frontend/storefront/src/App.tsx`
- App bootstrap: `src/frontend/storefront/src/main.tsx`
- Auth state and route checks: `src/frontend/storefront/src/features/auth/`

## Practical Guidance
- Co-locate page components under feature/domain folders.
- Use shared route guard patterns instead of per-page ad-hoc checks.
- Keep redirect rules explicit for unauthenticated/forbidden flows.

## Common Mistakes
- Scattering route definitions across unrelated files.
- Duplicating auth guard logic per route.
- Coupling page data fetching tightly to route definition modules.

## Checklist
- [ ] Routes are centrally organized and named consistently.
- [ ] Auth/role checks use shared guard pattern.
- [ ] Route params/query handling is typed.
- [ ] Large routes are split when beneficial.
