# Migration Reset & Codebase Cleanup Plan

## Overview

This plan outlines a complete cleanup of the migration history and RowVersion property duplication to establish a clean, best-practices codebase before going to production.

---

## Part 1: Current Issues to Address

### 1. RowVersion Property Duplication

**Problem:** `RowVersion` is declared in both `BaseEntity` and derived entity classes, creating property shadowing.

| File | Line | Issue |
|------|------|-------|
| [`BaseEntity.cs`](src/backend/ECommerce.Core/Common/BaseEntity.cs:11) | 11-12 | Declares `RowVersion` with `[Timestamp]` |
| [`Product.cs`](src/backend/ECommerce.Core/Entities/Product.cs:28) | 28-29 | Re-declares `RowVersion` (shadowing) |
| [`Order.cs`](src/backend/ECommerce.Core/Entities/Order.cs:31) | 31-32 | Re-declares `RowVersion` (shadowing) |
| [`PromoCode.cs`](src/backend/ECommerce.Core/Entities/PromoCode.cs:20) | 20-21 | Re-declares `RowVersion` (shadowing) |

**Best Practice:** Define concurrency tokens once in the base class or use explicit configuration in `IEntityTypeConfiguration`. Never shadow properties.

### 2. Migration History Complexity

**Current Migrations:**
```
20260114130946_InitialCreate
20260205074918_MakeAddressUserIdNullable
20260206103736_AddRefreshTokens
20260218134000_AddRowVersionToAllTables    ← Added RowVersion to ALL tables
20260219084103_AddDataProtectionKeys
20260220081155_OptimizeRowVersionColumns   ← Removed RowVersion from some tables
```

**Problem:** The migration history shows iterative fixes rather than a clean design. This is fine for development but not ideal for production.

---

## Part 2: Recommended Cleanup Strategy

### Option A: Complete Migration Reset (Recommended for Staging)

This approach consolidates all migrations into a single clean initial migration.

#### Steps:

1. **Delete all existing migrations**
   ```bash
   # Delete all migration files
   rm -rf src/backend/ECommerce.Infrastructure/Migrations/*
   ```

2. **Clean up entity classes** (remove duplicate RowVersion)
   - Remove `RowVersion` property from `Product.cs`
   - Remove `RowVersion` property from `Order.cs`
   - Remove `RowVersion` property from `PromoCode.cs`
   - Keep `RowVersion` in `BaseEntity.cs`

3. **Create new initial migration**
   ```bash
   cd src/backend
   dotnet ef migrations add InitialCreate --project ECommerce.Infrastructure --startup-project ECommerce.API
   ```

4. **Drop and recreate database on Render.com**
   - Delete the PostgreSQL database on Render
   - Create a new PostgreSQL database
   - Deploy the application - migrations will run automatically

#### Pros:
- ✅ Clean migration history
- ✅ Single source of truth for schema
- ✅ No legacy workarounds
- ✅ Easier to maintain

#### Cons:
- ❌ All data will be lost (acceptable for staging)
- ❌ Need to re-seed data

---

### Option B: Keep History, Fix Code Only

Keep migration history but clean up the entity classes.

#### Steps:

1. **Clean up entity classes** (remove duplicate RowVersion)
2. **Create a new migration** to reflect the code cleanup
3. **Apply migration to Render.com**

#### Pros:
- ✅ Preserves migration history
- ✅ Data preserved

#### Cons:
- ❌ Migration history still shows the iterative fixes
- ❌ More complex history to understand

---

## Part 3: Detailed Implementation Plan (Option A)

### Phase 1: Code Cleanup

#### Step 1.1: Remove Duplicate RowVersion from Entity Classes

**Files to modify:**

1. **[`Product.cs`](src/backend/ECommerce.Core/Entities/Product.cs)** - Remove lines 27-29:
   ```csharp
   // REMOVE THIS:
   // Concurrency token for optimistic locking
   [Timestamp]
   public byte[]? RowVersion { get; set; }
   ```

2. **[`Order.cs`](src/backend/ECommerce.Core/Entities/Order.cs)** - Remove lines 30-32:
   ```csharp
   // REMOVE THIS:
   // Concurrency token for optimistic locking
   [Timestamp]
   public byte[]? RowVersion { get; set; }
   ```

3. **[`PromoCode.cs`](src/backend/ECommerce.Core/Entities/PromoCode.cs)** - Remove lines 19-21:
   ```csharp
   // REMOVE THIS:
   // Concurrency token for optimistic locking
   [Timestamp]
   public byte[]? RowVersion { get; set; }
   ```

#### Step 1.2: Verify Entity Configurations

The configurations in [`EntityConfigurations.cs`](src/backend/ECommerce.Infrastructure/Data/Configurations/EntityConfigurations.cs) are correct:
- `ProductConfiguration`, `OrderConfiguration`, `PromoCodeConfiguration` use `entity.Property(e => e.RowVersion).IsRowVersion()`
- Other configurations use `entity.Ignore(e => e.RowVersion)`

These work correctly because EF Core will find the `RowVersion` property from the base class.

### Phase 2: Migration Reset

#### Step 2.1: Delete All Migrations

```bash
# From the project root
Remove-Item -Recurse -Force src/backend/ECommerce.Infrastructure/Migrations/*
```

#### Step 2.2: Create New Initial Migration

```bash
cd src/backend
dotnet ef migrations add InitialCreate --project ECommerce.Infrastructure --startup-project ECommerce.API
```

#### Step 2.3: Review Generated Migration

Verify the generated migration includes:
- All tables with correct columns
- RowVersion only on `Products`, `Orders`, `PromoCodes` tables
- All indexes and constraints

### Phase 3: Database Reset on Render.com

#### Step 3.1: Delete Existing Database

1. Go to Render.com Dashboard
2. Navigate to the PostgreSQL database
3. Delete the database

#### Step 3.2: Create New Database

1. Create a new PostgreSQL database on Render.com
2. Update the `DATABASE_URL` environment variable in the web service

#### Step 3.3: Deploy

1. Push the changes to trigger a new deployment
2. The application will create the schema from the new initial migration
3. Enable production seeding if needed: `ENABLE_PRODUCTION_SEEDING=true`

---

## Part 4: Best Practices for RowVersion in EF Core

### Recommended Pattern

```csharp
// BaseEntity.cs - Define RowVersion once
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // RowVersion for optimistic concurrency - inherited by all entities
    // Individual configurations decide whether to use or ignore it
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}

// EntityConfigurations.cs - Explicit configuration
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> entity)
    {
        // Enable RowVersion for entities with concurrent modifications
        entity.Property(e => e.RowVersion).IsRowVersion();
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
        // Disable RowVersion for single-user entities
        entity.Ignore(e => e.RowVersion);
    }
}
```

### Why This Works

1. **Single Definition:** `RowVersion` is defined once in `BaseEntity`
2. **Explicit Configuration:** Each entity configuration explicitly enables or disables it
3. **No Shadowing:** Derived classes don't redeclare the property
4. **Clear Intent:** Comments explain the purpose

---

## Part 5: Verification Checklist

After completing the cleanup:

- [ ] All entity classes compile without errors
- [ ] No duplicate `RowVersion` properties in any entity class
- [ ] Single `InitialCreate` migration exists
- [ ] Migration includes RowVersion only on `Products`, `Orders`, `PromoCodes`
- [ ] Application starts without errors
- [ ] Database schema validation passes
- [ ] All API endpoints work correctly

---

## Part 6: Timeline

| Phase | Tasks | 
|-------|-------|
| Phase 1 | Code cleanup (remove duplicate RowVersion) |
| Phase 2 | Migration reset (delete and recreate) |
| Phase 3 | Database reset on Render.com |
| Verification | Test all functionality |

---

## Decision Required

Please confirm which approach you prefer:

1. **Option A: Complete Migration Reset** (Recommended for staging)
   - Clean migration history
   - Data loss acceptable
   
2. **Option B: Keep History, Fix Code Only**
   - Preserve migration history
   - Data preserved

Once confirmed, I can switch to Code mode to implement the changes.
