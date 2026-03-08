# ADR-001: Keep 4-Layer Clean Architecture

Date: 2026-03-08
Status: accepted
Deciders: @ivans

## Context
The codebase spans API, business logic, domain model, and data infrastructure. Without clear boundaries, dependency direction drifts and maintainability drops.

## Decision
Keep a strict 4-layer architecture:
- API
- Application
- Core
- Infrastructure

with dependency direction enforced as:
- API -> Application -> Core
- Infrastructure -> Core/Application

## Consequences
- Positive: predictable boundaries and testability.
- Negative: some boilerplate around interfaces/mapping.
- Neutral: initial feature work may be slightly slower but safer long term.

## References
- `src/backend/ECommerce.API/`
- `src/backend/ECommerce.Application/`
- `src/backend/ECommerce.Core/`
- `src/backend/ECommerce.Infrastructure/`
