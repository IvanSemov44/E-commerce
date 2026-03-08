# Backend Testing Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Guide unit/integration testing of services, repositories, and API endpoints.

## Core Rules
1. Test expected business outcomes for `Result<T>` services.
2. Cover failure paths (not found, invalid state, conflicts).
3. Keep integration tests focused on critical flows.
4. Use cancellation-aware async tests where relevant.

## Real Code References
- Test project: `src/backend/ECommerce.Tests/`
- Service patterns: `src/backend/ECommerce.Application/Services/`
- Workflow strategy: `.ai/workflows/testing-strategy.md`

## Common Mistakes
- Testing only happy paths.
- Asserting framework internals instead of business behavior.
- Over-coupling tests to private implementation details.
