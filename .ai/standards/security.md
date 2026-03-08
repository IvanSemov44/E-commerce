# Security Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Capture baseline security controls and guardrails used across API and frontend.

## Core Rules
1. Authentication/authorization config remains centralized.
2. Keep CORS and CSRF configuration environment-aware.
3. Never hardcode secrets in code or docs.
4. Validate external input at DTO and business boundaries.

## Real Code References
- Startup security wiring: `src/backend/ECommerce.API/Program.cs`
- Security-related extensions: `src/backend/ECommerce.API/Extensions/`
- Validation layer: `src/backend/ECommerce.Application/Validators/`

## Common Mistakes
- Relaxed CORS in production-like environments.
- Treating client-side checks as security boundaries.
- Logging sensitive token/credential data.
