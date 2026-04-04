# Phase 5, Step 3: Infrastructure Project

**Prerequisite**: Steps 1 and 2 complete and building.

Create `ECommerce.Promotions.Infrastructure` — EF Core configuration, repository implementation, and DI wiring. No EF migration is needed: the `PromoCodes` table already exists with all columns.

---

## Task 1: Create the project

```bash
cd src/backend
dotnet new classlib -n ECommerce.Promotions.Infrastructure -o ECommerce.Promotions.Infrastructure
dotnet sln ECommerce.sln add ECommerce.Promotions.Infrastructure/ECommerce.Promotions.Infrastructure.csproj
dotnet add ECommerce.Promotions.Infrastructure/ECommerce.Promotions.Infrastructure.csproj reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
dotnet add ECommerce.Promotions.Infrastructure/ECommerce.Promotions.Infrastructure.csproj reference ECommerce.Promotions.Domain/ECommerce.Promotions.Domain.csproj
dotnet add ECommerce.Promotions.Infrastructure/ECommerce.Promotions.Infrastructure.csproj reference ECommerce.Promotions.Application/ECommerce.Promotions.Application.csproj
dotnet add ECommerce.Promotions.Infrastructure/ECommerce.Promotions.Infrastructure.csproj reference ECommerce.Infrastructure/ECommerce.Infrastructure.csproj
dotnet add ECommerce.Promotions.Infrastructure/ECommerce.Promotions.Infrastructure.csproj package Microsoft.EntityFrameworkCore
rm ECommerce.Promotions.Infrastructure/Class1.cs
```

---

## Task 2: EF Configuration

**Mapping strategy**:
- `PromoCodeString` → value converter (stored as `string` in `Code` column)
- `DiscountValue` → owned entity: `Type` → `DiscountType` column, `Amount` → `DiscountValue` column
- `DateRange` → owned entity, nullable: `Start` → `StartDate` column, `End` → `EndDate` column. Both columns are already nullable in the DB.
- `MaxDiscountAmount` and `MinimumOrderAmount` → regular nullable decimal columns (`MinOrderAmount`, `MaxDiscountAmount`)
- `RowVersion` → concurrency token

`ECommerce.Promotions.Infrastructure/Configurations/PromoCodeConfiguration.cs`

```csharp
using ECommerce.Promotions.Domain.Aggregates.PromoCode;
using ECommerce.Promotions.Domain.Enums;
using ECommerce.Promotions.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Promotions.Infrastructure.Configurations;

public class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode>
{
    public void Configure(EntityTypeBuilder<PromoCode> builder)
    {
        builder.ToTable("PromoCodes");

        builder.HasKey(p => p.Id);

        // PromoCodeString — value converter
        builder.Property(p => p.Code)
            .HasColumnName("Code")
            .HasMaxLength(20)
            .HasConversion(
                v => v.Value,
                v => PromoCodeString.Reconstitute(v))
            .IsRequired();

        builder.HasIndex(p => p.Code).IsUnique();

        // DiscountValue — owned entity mapping to existing columns
        builder.OwnsOne(p => p.Discount, dv =>
        {
            dv.Property(x => x.Type)
              .HasColumnName("DiscountType")
              .HasConversion<string>()
              .IsRequired();

            dv.Property(x => x.Amount)
              .HasColumnName("DiscountValue")
              .HasColumnType("decimal(18,2)")
              .IsRequired();
        });

        // DateRange — nullable owned entity mapping to nullable StartDate / EndDate columns
        // Navigation is required(false) so EF allows the PromoCode row to have null Start/End
        builder.OwnsOne(p => p.ValidPeriod, vp =>
        {
            vp.Property(x => x.Start)
              .HasColumnName("StartDate")
              .IsRequired(false);

            vp.Property(x => x.End)
              .HasColumnName("EndDate")
              .IsRequired(false);
        });
        builder.Navigation(p => p.ValidPeriod).IsRequired(false);

        builder.Property(p => p.MaxUses)
            .HasColumnName("MaxUses")
            .IsRequired(false);

        builder.Property(p => p.UsedCount)
            .HasColumnName("UsedCount")
            .IsRequired();

        builder.Property(p => p.IsActive)
            .HasColumnName("IsActive")
            .IsRequired();

        builder.Property(p => p.MinimumOrderAmount)
            .HasColumnName("MinOrderAmount")
            .HasColumnType("decimal(18,2)")
            .IsRequired(false);

        builder.Property(p => p.MaxDiscountAmount)
            .HasColumnName("MaxDiscountAmount")
            .HasColumnType("decimal(18,2)")
            .IsRequired(false);

        // Concurrency token — existing [Timestamp] column
        builder.Property(p => p.RowVersion)
            .HasColumnName("RowVersion")
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.Property(p => p.CreatedAt).HasColumnName("CreatedAt").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("UpdatedAt").IsRequired();

        // Ignore navigation to Orders — that relationship is managed by the Ordering context
        builder.Ignore("Orders");
    }
}
```

**Important — nullable DateRange**: EF owned entities can be null but require careful configuration. If both `StartDate` and `EndDate` are NULL in the DB, EF will set `ValidPeriod` to `null` on the aggregate automatically when `IsRequired(false)` is set on the navigation.

---

## Task 3: DbSet registration

Open `ECommerce.Infrastructure/Data/AppDbContext.cs`. At this point the file has:
```csharp
public DbSet<ECommerce.Core.Entities.PromoCode> PromoCodes { get; set; }
```

You need to add the new `DbSet` **without** removing the old one yet (the old service still references it until cutover). Add a second registration with a distinct name:

```csharp
// Phase 5 — new DDD aggregate (temporary name; renamed to PromoCodes in step-4 after old entity removed)
public DbSet<ECommerce.Promotions.Domain.Aggregates.PromoCode.PromoCode> PromoCodes2 { get; set; }
```

Also register the EF configuration in `OnModelCreating`:
```csharp
modelBuilder.ApplyConfiguration(new ECommerce.Promotions.Infrastructure.Configurations.PromoCodeConfiguration());
```

Add the reference from `ECommerce.Infrastructure` to `ECommerce.Promotions.Infrastructure`:
```bash
dotnet add src/backend/ECommerce.Infrastructure/ECommerce.Infrastructure.csproj reference src/backend/ECommerce.Promotions.Infrastructure/ECommerce.Promotions.Infrastructure.csproj
```

---

## Task 4: Repository

`ECommerce.Promotions.Infrastructure/Repositories/PromoCodeRepository.cs`

```csharp
using ECommerce.Infrastructure.Data;
using ECommerce.Promotions.Domain.Aggregates.PromoCode;
using ECommerce.Promotions.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Promotions.Infrastructure.Repositories;

public class PromoCodeRepository : IPromoCodeRepository
{
    private readonly AppDbContext _db;
    public PromoCodeRepository(AppDbContext db) => _db = db;

    public Task<PromoCode?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.PromoCodes2.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<PromoCode?> GetByCodeAsync(string normalizedCode, CancellationToken ct = default)
        => _db.PromoCodes2.FirstOrDefaultAsync(p => p.Code == PromoCodeString.Reconstitute(normalizedCode), ct);
        // Note: EF will compare the converted string values correctly

    public async Task<(List<PromoCode> Items, int TotalCount)> GetActiveAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.PromoCodes2.Where(p => p.IsActive).OrderByDescending(p => p.CreatedAt);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<(List<PromoCode> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, string? search, bool? isActive, CancellationToken ct = default)
    {
        var query = _db.PromoCodes2.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = query.Where(p => EF.Functions.Like(p.Code.Value, pattern));
        }

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        query = query.OrderByDescending(p => p.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task UpsertAsync(PromoCode promoCode, CancellationToken ct = default)
    {
        var existing = await _db.PromoCodes2.FindAsync(new object[] { promoCode.Id }, ct);
        if (existing is null)
            await _db.PromoCodes2.AddAsync(promoCode, ct);
        // If tracked, EF change tracking handles the update automatically
    }

    public async Task DeleteAsync(PromoCode promoCode, CancellationToken ct = default)
    {
        var existing = await _db.PromoCodes2.FindAsync(new object[] { promoCode.Id }, ct);
        if (existing is not null)
            _db.PromoCodes2.Remove(existing);
    }
}
```

**Note on `GetByCodeAsync`**: EF value converters allow querying by the converted value. Since `PromoCodeString` converts to its `.Value` string, the LINQ expression `p.Code == PromoCodeString.Reconstitute(normalizedCode)` is translated to a SQL `WHERE Code = @normalizedCode`. Alternatively, use:
```csharp
_db.PromoCodes2.FirstOrDefaultAsync(p => EF.Property<string>(p, "Code") == normalizedCode, ct)
```
Use whichever your EF version handles cleanly.

---

## Task 5: DependencyInjection

`ECommerce.Promotions.Infrastructure/DependencyInjection.cs`

```csharp
using ECommerce.Promotions.Application;
using ECommerce.Promotions.Domain.Interfaces;
using ECommerce.Promotions.Domain.Services;
using ECommerce.Promotions.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Promotions.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPromotionsInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IPromoCodeRepository, PromoCodeRepository>();
        services.AddScoped<DiscountCalculator>();
        services.AddPromotionsApplication();
        return services;
    }
}
```

---

## Task 6: Wire up in API

1. Add project reference from API to Infrastructure:
```bash
dotnet add src/backend/ECommerce.API/ECommerce.API.csproj reference src/backend/ECommerce.Promotions.Infrastructure/ECommerce.Promotions.Infrastructure.csproj
```

2. In `src/backend/ECommerce.API/Program.cs`, add before `builder.Build()`:
```csharp
builder.Services.AddPromotionsInfrastructure();
```

---

## Task 7: Verify (no EF migration needed)

```bash
cd src/backend
dotnet build
# Confirm no errors
dotnet run --project ECommerce.API -- &
sleep 3
# Verify the app still boots and old endpoints work
curl -s http://localhost:5000/api/promo-codes/active | jq '.success'
# Expected: true
kill %1
```

---

## Acceptance Criteria

- [ ] Project builds with zero errors
- [ ] `AppDbContext` has `PromoCodes2` DbSet for the new aggregate (old `PromoCodes` DbSet for `Core.Entities.PromoCode` still present)
- [ ] `PromoCodeConfiguration` applied in `OnModelCreating`
- [ ] No EF migration required (existing `PromoCodes` table unchanged)
- [ ] App boots and existing `GET /api/promo-codes/active` still returns 200 (old service still active)
- [ ] `DiscountCalculator` registered as Scoped
- [ ] `IPromoCodeRepository` → `PromoCodeRepository` registered as Scoped
- [ ] `PromoCodeString.Reconstitute` is `internal` — only Infrastructure can call it
