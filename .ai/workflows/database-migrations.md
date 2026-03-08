# Workflow: Database Migrations

Updated: 2026-03-08
Owner: @ivans

## Purpose
Create, review, and apply EF Core migrations safely for this codebase.

## Rules
- Migrations are immutable after push. Do not edit old migration files; add a new migration to correct issues.
- Always specify both project and startup project flags.
- Review both `Up()` and `Down()` before committing.
- Keep migration names explicit and action-oriented.

## Commands
Run from `src/backend`:

```powershell
# Create migration
dotnet ef migrations add MigrationName -p ECommerce.Infrastructure -s ECommerce.API

# Apply migration
dotnet ef database update -p ECommerce.Infrastructure -s ECommerce.API
```

## Naming Examples
- `AddProductTable`
- `AlterProductAddBarcodeColumn`
- `CreateProductSlugUniqueIndex`

## Verification Steps
1. Confirm migration file appears under `src/backend/ECommerce.Infrastructure/Migrations/`.
2. Open migration and verify both `Up()` and `Down()` are present.
3. Run `dotnet ef database update -p ECommerce.Infrastructure -s ECommerce.API`.
4. Run backend build/tests.

## Common Failure Modes
- Missing `-p`/`-s` flags and generating migration in wrong project.
- Editing an already-pushed migration instead of adding a follow-up migration.
- Forgetting rollback path (`Down()`) logic.
- Committing migration without validating SQL intent.

## Real Code References
- Migration folder: `src/backend/ECommerce.Infrastructure/Migrations/`
- Example migrations:
  - `src/backend/ECommerce.Infrastructure/Migrations/20260223084551_InitialCreate.cs`
  - `src/backend/ECommerce.Infrastructure/Migrations/20260307094912_AddUserRowVersionAndDiscountTypeEnum.cs`

## Canonical Source
This file is the canonical migration workflow for AI assistants and contributors.
Do not rely on legacy guides for migration rules if those guides are archived or removed.
