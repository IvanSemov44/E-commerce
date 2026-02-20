# Database Migration and Schema Validation

## Overview

This document describes the database migration and schema validation system implemented to prevent runtime errors caused by migration history being out of sync with the actual database schema.

## Problem Statement

In production, the `__EFMigrationsHistory` table may show that migrations have been applied, but the actual database schema might be different. This can happen due to:

1. Manual database modifications
2. Failed migrations that were partially applied
3. Database restores from backups taken at different migration states
4. Manual migration history manipulation

## Solution

### 1. Schema Validation on Startup

The `ValidateDatabaseSchemaAsync` method in [`ApplicationBuilderExtensions.cs`](../../src/backend/ECommerce.API/Extensions/ApplicationBuilderExtensions.cs) performs the following checks:

#### Required Tables Check
Verifies that critical tables exist:
- `Users`
- `Products`
- `Orders`
- `RefreshTokens`
- `Categories`

#### Column Validation
Checks for specific columns that have caused issues:

| Table | Column | Should Exist | Reason |
|-------|--------|--------------|--------|
| `RefreshTokens` | `RowVersion` | ❌ No | Entity ignores RowVersion from BaseEntity |
| `Products` | `RowVersion` | ✅ Yes | Optimistic concurrency |
| `Orders` | `RowVersion` | ✅ Yes | Optimistic concurrency |
| `PromoCodes` | `RowVersion` | ✅ Yes | Optimistic concurrency |
| `RefreshTokens` | `Token` | ✅ Yes | Required for authentication |
| `RefreshTokens` | `UserId` | ✅ Yes | Required for user association |
| `RefreshTokens` | `ExpiresAt` | ✅ Yes | Required for validity |

### 2. Idempotent Migrations

The `IgnoreRefreshTokenRowVersion` migration uses PostgreSQL PL/pgSQL to handle multiple scenarios:

```sql
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns 
               WHERE table_schema = 'public'
               AND table_name = 'RefreshTokens' 
               AND column_name = 'RowVersion') THEN
        ALTER TABLE "RefreshTokens" DROP COLUMN "RowVersion";
    END IF;
END $$;
```

This ensures the migration can be applied regardless of the current database state.

## Edge Cases and Failure Scenarios

### Scenario 1: Fresh Database
- **State**: No tables exist
- **Behavior**: All migrations apply from scratch
- **Schema Validation**: Passes after migrations

### Scenario 2: Database with Out-of-Sync Migration History
- **State**: `__EFMigrationsHistory` shows migrations applied, but columns missing
- **Behavior**: Schema validation fails with clear error message
- **Resolution**: Manual intervention required or re-run migrations

### Scenario 3: Column Exists When It Shouldn't
- **State**: `RefreshTokens.RowVersion` exists (from previous migration)
- **Behavior**: New migration drops the column
- **Schema Validation**: Passes after migration

### Scenario 4: Column Missing When It Should Exist
- **State**: `Products.RowVersion` missing
- **Behavior**: Schema validation fails at startup
- **Resolution**: Investigate migration history, may need to re-apply migrations

### Scenario 5: Partial Migration Failure
- **State**: Migration fails midway
- **Behavior**: Transaction rollback, startup fails with error
- **Resolution**: Fix the underlying issue, restart application

## Seeding Idempotency

All seeders check for existing data before inserting:

```csharp
// UserSeeder.cs
if (await context.Users.AnyAsync())
{
    return; // Database already seeded
}
```

This ensures:
- No duplicate data on restart
- Safe to run multiple times
- Production data is preserved

## Production Considerations

### Environment Variable
Production seeding is disabled by default. Enable with:
```
ENABLE_PRODUCTION_SEEDING=true
```

### Logging
All migration and validation steps are logged:
- `Applying pending migrations...`
- `No pending migrations found.`
- `Database schema validation passed.`
- `Database schema validation failed. Issues found:...`

### Health Checks
The `/health/ready` endpoint verifies database connectivity as part of readiness checks.

## Troubleshooting

### Error: "column 'RowVersion' does not exist"
This error indicates the migration history is out of sync. The fix is included in the `IgnoreRefreshTokenRowVersion` migration.

### Error: "Database schema validation failed"
1. Check the specific error message for which table/column is affected
2. Verify the database migration history: `SELECT * FROM "__EFMigrationsHistory"`
3. Compare with expected schema
4. Apply missing migrations or manually fix schema

### Error: "Missing required tables"
1. Verify database connection string
2. Check if migrations have been applied
3. Consider starting fresh if data loss is acceptable

## Rollback Procedure

To rollback the `IgnoreRefreshTokenRowVersion` migration:

```bash
dotnet ef migrations rollback --project ECommerce.Infrastructure --startup-project ECommerce.API
```

The rollback is also idempotent - it only adds the column if it doesn't exist.

## Related Files

- [`ApplicationBuilderExtensions.cs`](../../src/backend/ECommerce.API/Extensions/ApplicationBuilderExtensions.cs) - Schema validation logic
- [`AppDbContext.cs`](../../src/backend/ECommerce.Infrastructure/Data/AppDbContext.cs) - Entity configuration
- [`20260220062135_IgnoreRefreshTokenRowVersion.cs`](../../src/backend/ECommerce.Infrastructure/Migrations/20260220062135_IgnoreRefreshTokenRowVersion.cs) - Idempotent migration
- [`DatabaseSeeder.cs`](../../src/backend/ECommerce.Infrastructure/Data/DatabaseSeeder.cs) - Idempotent seeding
