# ADR-003: JWT Access + Refresh Token Authentication

Date: 2026-03-08
Status: accepted
Deciders: @ivans

## Context
Storefront and API require stateless authenticated access with practical session continuity.

## Decision
Use JWT-based authentication with refresh-token flow and backend-side validation policies.

## Consequences
- Positive: scalable stateless API authentication.
- Positive: better UX with refresh flow instead of frequent hard logins.
- Negative: additional complexity in token lifecycle/security controls.

## References
- `src/backend/ECommerce.API/Extensions/`
- `src/backend/ECommerce.API/Program.cs`
- `src/frontend/storefront/src/shared/lib/api/baseApi.ts`
