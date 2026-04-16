# File Structure Reference

Updated: 2026-03-08
Owner: @ivans

## Purpose
Provide a practical map of where to implement and where to document changes.

## Backend
- API layer: `src/backend/ECommerce.API/`
- Application layer: `src/backend/ECommerce.Application/`
- Core domain: `src/backend/ECommerce.Core/`
- Infrastructure/data: `src/backend/ECommerce.Infrastructure/`
- Tests: `src/backend/ECommerce.Tests/`

## Frontend
- Storefront app: `src/frontend/storefront/src/`
- App root (providers, layout shell): `src/frontend/storefront/src/app/root.tsx`
- Route config: `src/frontend/storefront/src/app/routes.ts`
- Route files (Framework Mode): `src/frontend/storefront/src/app/routes/`
- Feature modules: `src/frontend/storefront/src/features/`
- Static/content pages (About, Blog, Legal etc. — no feature logic or API calls): `src/frontend/storefront/src/pages/`
- Shared UI/lib/hooks: `src/frontend/storefront/src/shared/`
- Path constants: `src/frontend/storefront/src/shared/constants/navigation.ts`

## AI Documentation
- Canonical docs hub: `.ai/README.md`
- Workflows: `.ai/workflows/`
- Domain rules: `.ai/backend/` and `.ai/frontend/`
