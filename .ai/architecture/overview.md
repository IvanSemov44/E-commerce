# Architecture Overview

Updated: 2026-03-08
Owner: @ivans

## Purpose
Summarize system architecture and where to find implementation rules.

## System Shape
- Backend: ASP.NET Core API + Application/Core/Infrastructure split.
- Frontend: React + TypeScript + Redux Toolkit + RTK Query.
- Database: PostgreSQL via EF Core.

## Layer Boundaries
- API -> Application -> Core
- Infrastructure -> Core/Application
- Core should not depend on Application/Infrastructure.

## Real Code References
- API startup composition: `src/backend/ECommerce.API/Program.cs`
- Application services: `src/backend/ECommerce.Application/Services/`
- Core contracts/entities: `src/backend/ECommerce.Core/`
- Infrastructure persistence: `src/backend/ECommerce.Infrastructure/`

## Read Next
1. `.ai/architecture/clean-architecture.md`
2. `.ai/architecture/patterns.md`
3. `.ai/workflows/adding-feature.md`
