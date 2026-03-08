# Backend Database Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Document EF Core persistence patterns and migration discipline.

## Core Rules
1. Schema changes go through EF migrations only.
2. Never edit applied migration history in shared branches.
3. Repositories do not commit; UnitOfWork commits.
4. Use `CancellationToken` in async data operations.

## Real Code References
- DbContext and configurations: `src/backend/ECommerce.Infrastructure/Data/`
- Migrations: `src/backend/ECommerce.Infrastructure/Migrations/`
- UnitOfWork commit boundary: `src/backend/ECommerce.Infrastructure/UnitOfWork.cs`
- Migration startup apply/seed: `src/backend/ECommerce.API/Program.cs`
- Migration workflow doc: `.ai/workflows/database-migrations.md`

## Common Mistakes
- Running manual schema changes outside migrations.
- Calling `SaveChangesAsync` in repository methods.
- Mixing migration concerns inside feature/service logic.
