# Phase 5, Step 3: Promotions Infrastructure Project

**Prerequisite**: Step 2 (`ECommerce.Promotions.Application`) is complete and `dotnet build` passes.

---

## Important: No EF migration needed

The `PromoCodes` table already exists with all required columns. This step re-maps it to the new DDD aggregate using EF Core Owned Entities and a value converter for `PromoCodeString`.

**Existing table columns (do not change):**
- `Id` (uniqueidentifier), `Code` (nvarchar), `DiscountType` (nvarchar), `DiscountValue` (decimal)
- `MinOrderAmount` (decimal, nullable), `MaxDiscountAmount` (decimal, nullable)
- `MaxUses` (int, nullable), `UsedCount` (int)
- `StartDate` (datetime2, nullable), `EndDate` (datetime2, nullable)
- `IsActive` (bit), `RowVersion` ([Timestamp] / rowversion)
- `CreatedAt`, `UpdatedAt` (from BaseEntity)

**DbSet naming conflict**: The old entity `ECommerce.Core.Entities.PromoCode` likely has a DbSet named `PromoCodes` in `AppDbContext`. The new entity is `ECommerce.Promotions.Domain.Aggregates.PromoCode.PromoCode`. Handle this by:
1. Adding the new configuration in this step with a temporary DbSet name `PromoCodes2`.
2. Removing the old entity and renaming back to `PromoCodes` in step 4 (cutover).

---

## Task: Create ECommerce.Promotions.Infrastructure Project

### 1. Create the project

```bash
cd src/backend
dotnet new classlib -n ECommerce.Promotions.Infrastructure -f net10.0 \
    -o Promotions/ECommerce.Promotions.Infrastructure
dotnet sln ../../ECommerce.sln add \
    Promotions/ECommerce.Promotions.Infrastructure/ECommerce.Promotions.Infrastructure.csproj

dotnet add Promotions/ECommerce.Promotions.Infrastructure/ECommerce.Promotions.Infrastructure.csproj \
    reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
dotnet add Promotions/ECommerce.Promotions.Infrastructure/ECommerce.Promotions.Infrastructure.csproj \
    reference Promotions/ECommerce.Promotions.Domain/ECommerce.Promotions.Domain.csproj
dotnet add Promotions/ECommerce.Promotions.Infrastructure/ECommerce.Promotions.Infrastructure.csproj \
    reference Promotions/ECommerce.Promotions.Application/ECommerce.Promotions.Application.csproj
dotnet add Promotions/ECommerce.Promotions.Infrastructure/ECommerce.Promotions.Infrastructure.csproj \
    reference ECommerce.Infrastructure/ECommerce.Infrastructure.csproj  # for AppDbContext

dotnet add Promotions/ECommerce.Promotions.Infrastructure/ECommerce.Promotions.Infrastructure.csproj \
    package Microsoft.EntityFrameworkCore

rm Promotions/ECommerce.Promotions.Infrastructure/Class1.cs
```

### 2. EF Core configuration

**File: `Promotions/ECommerce.Promotions.Infrastructure/Persistence/Configurations/PromoCodeConfiguration.cs`**

```csharp
using ECommerce.Promotions.Domain.Aggregates.PromoCode;
using ECommerce.Promotions.Domain.Enums;
using ECommerce.Promotions.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Promotions.Infrastructure.Persistence.Configurations;

public class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode>
{
    public void Configure(EntityTypeBuilder<PromoCode> builder)
    {
        builder.ToTable("PromoCodes");
        builder.HasKey(p => p.Id);

        // ── PromoCodeString via value converter ───────────────────────────────
        // Reconstitute bypasses validation — EF re-reads an already-stored value.
        builder.Property(p => p.Code)
               .HasColumnName("Code")
               .HasMaxLength(20)
               .IsRequired()
               .HasConversion(
                   v => v.Value,
                   v => PromoCodeString.Reconstitute(v));

        // ── DiscountValue owned entity ─────────────────────────────────────────
        // Maps to existing columns: DiscountType, DiscountValue
        builder.OwnsOne(p => p.Discount, discount =>
        {
            discount.Property(d => d.Type)
                    .HasColumnName("DiscountType")
                    .IsRequired()
                    .HasConversion<string>(); // stores enum name as string

            discount.Property(d => d.Amount)
                    .HasColumnName("DiscountValue")
                    .HasColumnType("decimal(18,4)")
                    .IsRequired();
        });

        // ── ValidPeriod owned entity (nullable) ────────────────────────────────
        // Maps to existing columns: StartDate, EndDate
        // ValidPeriod itself can be null (no date restriction).
        // Use Navigation().IsRequired(false) to tell EF the owned type is optional.
        builder.OwnsOne(p => p.ValidPeriod, vp =>
        {
            vp.Property(d => d.Start)
              .HasColumnName("StartDate")
              .IsRequired(false);

            vp.Property(d => d.End)
              .HasColumnName("EndDate")
              .IsRequired(false);
        });

        builder.Navigation(p => p.ValidPeriod).IsRequired(false);

        // ── Scalar columns ─────────────────────────────────────────────────────
        builder.Property(p => p.MinimumOrderAmount)
               .HasColumnName("MinOrderAmount")
               .HasColumnType("decimal(18,2)")
               .IsRequired(false);

        builder.Property(p => p.MaxDiscountAmount)
               .HasColumnType("decimal(18,2)")
               .IsRequired(false);

        builder.Property(p => p.MaxUses).IsRequired(false);
        builder.Property(p => p.UsedCount).IsRequired();
        builder.Property(p => p.IsActive).IsRequired();

        // ── RowVersion concurrency token ───────────────────────────────────────
        builder.Property(p => p.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        // ── Unique index on Code ───────────────────────────────────────────────
        builder.HasIndex(p => p.Code).IsUnique();

        // ── Ignore Orders navigation (belongs to Ordering context) ─────────────
        builder.Ignore("Orders");
    }
}
```

> **Note on ValidPeriod null reconstitution**: When both `StartDate` and `EndDate` are null in the database, EF Core will set `ValidPeriod` to `null` on the aggregate (because of `Navigation().IsRequired(false)`). This is the correct behaviour — null ValidPeriod = no date restriction.

### 3. Implement IPromoCodeRepository

**File: `Promotions/ECommerce.Promotions.Infrastructure/Persistence/Repositories/PromoCodeRepository.cs`**

```csharp
using ECommerce.Infrastructure.Persistence;
using ECommerce.Promotions.Domain.Aggregates.PromoCode;
using ECommerce.Promotions.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Promotions.Infrastructure.Persistence.Repositories;

public class PromoCodeRepository(AppDbContext _db) : IPromoCodeRepository
{
    // Use the temporary DbSet name until old entity is removed in step 4
    private IQueryable<PromoCode> PromoCodes => _db.Set<PromoCode>();

    public async Task<PromoCode?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await PromoCodes.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<PromoCode?> GetByCodeAsync(string code, CancellationToken ct = default)
        => await PromoCodes
            .FirstOrDefaultAsync(p => EF.Property<string>(p, "Code") == code, ct);

    public async Task<(IReadOnlyList<PromoCode> Items, int TotalCount)> GetActiveAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = PromoCodes
            .Where(p => p.IsActive)
            .OrderBy(p => EF.Property<string>(p, "Code"));

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<(IReadOnlyList<PromoCode> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, string? search, bool? isActive, CancellationToken ct = default)
    {
        var query = PromoCodes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var upper = search.Trim().ToUpperInvariant();
            query = query.Where(p =>
                EF.Property<string>(p, "Code").Contains(upper));
        }

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        query = query.OrderBy(p => EF.Property<string>(p, "Code"));

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task UpsertAsync(PromoCode promoCode, CancellationToken ct = default)
    {
        var exists = await PromoCodes.AnyAsync(p => p.Id == promoCode.Id, ct);
        if (exists)
            _db.Update(promoCode);
        else
            await _db.AddAsync(promoCode, ct);
    }

    public Task DeleteAsync(PromoCode promoCode, CancellationToken ct = default)
    {
        _db.Remove(promoCode);
        return Task.CompletedTask;
    }
}
```

### 4. Update AppDbContext

In `ECommerce.Infrastructure/Data/AppDbContext.cs`, add the following.

**Add a temporary DbSet** (named `PromoCodes2` to avoid a name clash with the old `ECommerce.Core.Entities.PromoCode` DbSet — this will be renamed back in step 4 after the old entity is removed):

```csharp
// TEMPORARY — rename to PromoCodes in step 4 after removing old Core entity
public DbSet<ECommerce.Promotions.Domain.Aggregates.PromoCode.PromoCode> PromoCodes2
    => Set<ECommerce.Promotions.Domain.Aggregates.PromoCode.PromoCode>();
```

**Apply the configuration in `OnModelCreating`**:

```csharp
modelBuilder.ApplyConfiguration(
    new ECommerce.Promotions.Infrastructure.Persistence.Configurations.PromoCodeConfiguration());
```

### 5. DI registration

**File: `Promotions/ECommerce.Promotions.Infrastructure/DependencyInjection.cs`**

```csharp
using ECommerce.Promotions.Domain.Interfaces;
using ECommerce.Promotions.Domain.Services;
using ECommerce.Promotions.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Promotions.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPromotionsInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IPromoCodeRepository, PromoCodeRepository>();

        // DiscountCalculator is a domain service — concrete class, no interface
        services.AddScoped<DiscountCalculator>();

        // Register MediatR handlers from the Application assembly
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(
                typeof(ECommerce.Promotions.Application.Commands.CreatePromoCode.CreatePromoCodeCommand).Assembly));

        return services;
    }
}
```

### 6. Wire up in ECommerce.API

```bash
dotnet add ECommerce.API/ECommerce.API.csproj \
    reference Promotions/ECommerce.Promotions.Infrastructure/ECommerce.Promotions.Infrastructure.csproj
```

In `Program.cs` (or `Extensions/ServiceCollectionExtensions.cs`), add:

```csharp
builder.Services.AddPromotionsInfrastructure();
```

### 7. Verify build (do NOT run tests yet — old controller still uses IPromoCodeService)

```bash
cd src/backend
dotnet build
```

The solution must compile. If there are type conflicts between the old `ECommerce.Core.Entities.PromoCode` and the new `ECommerce.Promotions.Domain.Aggregates.PromoCode.PromoCode`, use fully-qualified type names in `AppDbContext` (as shown in step 4 above).

---

## Acceptance Criteria

- [ ] `ECommerce.Promotions.Infrastructure` project created and added to solution
- [ ] `PromoCodeConfiguration` maps to existing `PromoCodes` table with no schema changes
- [ ] `OwnsOne` for `Discount` → maps `Type` → `DiscountType` column (string), `Amount` → `DiscountValue` column
- [ ] `OwnsOne` for `ValidPeriod` with `Navigation().IsRequired(false)` — null when both dates are null in DB
- [ ] `PromoCodeString` mapped via `HasConversion` using `Reconstitute` (internal factory)
- [ ] `RowVersion` configured with `IsRowVersion().IsConcurrencyToken()`
- [ ] `HasIndex(p => p.Code).IsUnique()` applied
- [ ] `PromoCodeRepository` implements all 6 `IPromoCodeRepository` methods
- [ ] `GetByCodeAsync` compares the stored string value (case-sensitive — codes are always stored in UPPER-CASE)
- [ ] `DiscountCalculator` registered as `Scoped`
- [ ] `DependencyInjection.cs` registers repo, DiscountCalculator, and MediatR handlers from Application assembly
- [ ] `AppDbContext` has `PromoCodes2` DbSet (temporary) and applies `PromoCodeConfiguration`
- [ ] `ECommerce.API` project references Infrastructure and calls `AddPromotionsInfrastructure()`
- [ ] `dotnet build` passes (full solution)
