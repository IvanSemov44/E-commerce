# Phase 6, Step 3: Infrastructure Project

**Prerequisite**: Steps 1 and 2 complete and building.

Create `ECommerce.Reviews.Infrastructure` — EF Core configuration, repository implementation, and DI wiring.

---

## Task 1: Create the project

```bash
cd src/backend
dotnet new classlib -n ECommerce.Reviews.Infrastructure -o ECommerce.Reviews.Infrastructure
dotnet sln ECommerce.sln add ECommerce.Reviews.Infrastructure/ECommerce.Reviews.Infrastructure.csproj
dotnet add ECommerce.Reviews.Infrastructure/ECommerce.Reviews.Infrastructure.csproj reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
dotnet add ECommerce.Reviews.Infrastructure/ECommerce.Reviews.Infrastructure.csproj reference ECommerce.Reviews.Domain/ECommerce.Reviews.Domain.csproj
dotnet add ECommerce.Reviews.Infrastructure/ECommerce.Reviews.Infrastructure.csproj reference ECommerce.Reviews.Application/ECommerce.Reviews.Application.csproj
dotnet add ECommerce.Reviews.Infrastructure/ECommerce.Reviews.Infrastructure.csproj reference ECommerce.Infrastructure/ECommerce.Infrastructure.csproj
dotnet add ECommerce.Reviews.Infrastructure/ECommerce.Reviews.Infrastructure.csproj package Microsoft.EntityFrameworkCore
rm ECommerce.Reviews.Infrastructure/Class1.cs
```

---

## Task 2: EF Configuration

**File: `ECommerce.Reviews.Infrastructure/Configurations/ReviewConfiguration.cs`**

```csharp
using ECommerce.Reviews.Domain.Aggregates.Review;
using ECommerce.Reviews.Domain.Enums;
using ECommerce.Reviews.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Reviews.Infrastructure.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");

        builder.HasKey(r => r.Id);

        // Rating — value converter
        builder.Property(r => r.Rating)
            .HasColumnName("Rating")
            .HasConversion(
                v => v.Value,
                v => Rating.Reconstitute(v))
            .IsRequired();

        // ReviewText — value converter
        builder.Property(r => r.Text)
            .HasColumnName("Text")
            .HasConversion(
                v => v.Value,
                v => ReviewText.Reconstitute(v))
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(r => r.ProductId)
            .HasColumnName("ProductId")
            .IsRequired();

        builder.Property(r => r.AuthorId)
            .HasColumnName("AuthorId")
            .IsRequired();

        builder.Property(r => r.AuthorName)
            .HasColumnName("AuthorName")
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(r => r.Status)
            .HasColumnName("Status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(r => r.HelpfulCount)
            .HasColumnName("HelpfulCount")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(r => r.FlagCount)
            .HasColumnName("FlagCount")
            .HasDefaultValue(0)
            .IsRequired();

        // Concurrency token
        builder.Property(r => r.RowVersion)
            .HasColumnName("RowVersion")
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.Property(r => r.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .IsRequired();

        // Indexes
        builder.HasIndex(r => new { r.ProductId, r.AuthorId }).IsUnique();
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.FlagCount);
    }
}
```

---

## Task 3: DbSet registration

Open `ECommerce.Infrastructure/Data/AppDbContext.cs` and add:

```csharp
// Phase 6 — Reviews bounded context (permanent name)
public DbSet<ECommerce.Reviews.Domain.Aggregates.Review.Review> Reviews { get; set; }
```

Also register the EF configuration in `OnModelCreating`:
```csharp
modelBuilder.ApplyConfiguration(new ECommerce.Reviews.Infrastructure.Configurations.ReviewConfiguration());
```

Add the reference from `ECommerce.Infrastructure` to `ECommerce.Reviews.Infrastructure`:
```bash
dotnet add src/backend/ECommerce.Infrastructure/ECommerce.Infrastructure.csproj reference src/backend/ECommerce.Reviews.Infrastructure/ECommerce.Reviews.Infrastructure.csproj
```

---

## Task 4: Repository

**File: `ECommerce.Reviews.Infrastructure/Repositories/ReviewRepository.cs`**

```csharp
using ECommerce.Infrastructure.Data;
using ECommerce.Reviews.Domain.Aggregates.Review;
using ECommerce.Reviews.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Reviews.Infrastructure.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly AppDbContext _db;
    public ReviewRepository(AppDbContext db) => _db = db;

    public Task<Review?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Reviews.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<Review?> GetByProductAndAuthorAsync(Guid productId, Guid authorId, CancellationToken ct = default)
        => _db.Reviews.FirstOrDefaultAsync(r => r.ProductId == productId && r.AuthorId == authorId, ct);

    public async Task<(List<Review> Items, int TotalCount)> GetByProductAsync(
        Guid productId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Reviews
            .Where(r => r.ProductId == productId && r.Status.ToString() == "Approved")
            .OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<(List<Review> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, string? search, string? status, CancellationToken ct = default)
    {
        var query = _db.Reviews.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = query.Where(r => EF.Functions.Like(r.Text.Value, pattern) ||
                                     EF.Functions.Like(r.AuthorName, pattern));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status.ToString() == status);
        }

        query = query.OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<(List<Review> Items, int TotalCount)> GetPendingAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Reviews
            .Where(r => r.Status.ToString() == "Pending")
            .OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<(List<Review> Items, int TotalCount)> GetFlaggedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Reviews
            .Where(r => r.Status.ToString() == "Flagged")
            .OrderByDescending(r => r.FlagCount)
            .ThenByDescending(r => r.UpdatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task UpsertAsync(Review review, CancellationToken ct = default)
    {
        var existing = await _db.Reviews.FindAsync(new object[] { review.Id }, ct);
        if (existing is null)
            await _db.Reviews.AddAsync(review, ct);
    }

    public async Task DeleteAsync(Review review, CancellationToken ct = default)
    {
        var existing = await _db.Reviews.FindAsync(new object[] { review.Id }, ct);
        if (existing is not null)
            _db.Reviews.Remove(existing);
    }
}
```

---

## Task 5: DependencyInjection

**File: `ECommerce.Reviews.Infrastructure/DependencyInjection.cs`**

```csharp
using ECommerce.Reviews.Application;
using ECommerce.Reviews.Domain.Interfaces;
using ECommerce.Reviews.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Reviews.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddReviewsInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddReviewsApplication();
        return services;
    }
}
```

---

## Task 6: Wire up in API

1. Add project reference from API to Infrastructure:
```bash
dotnet add src/backend/ECommerce.API/ECommerce.API.csproj reference src/backend/ECommerce.Reviews.Infrastructure/ECommerce.Reviews.Infrastructure.csproj
```

2. In `src/backend/ECommerce.API/Program.cs`, add before `builder.Build()`:
```csharp
builder.Services.AddReviewsInfrastructure();
```

---

## Task 7: EF Migration (create new table)

```bash
cd src/backend
dotnet ef migrations add "Add_Reviews_Table" -p ECommerce.Infrastructure -s ECommerce.API
dotnet ef database update -p ECommerce.Infrastructure -s ECommerce.API
```

Verify the `Reviews` table was created:
```sql
SELECT * FROM sys.tables WHERE name = 'Reviews';
-- Expected: one row
```

---

## Task 8: Verify

```bash
cd src/backend
dotnet build
# Confirm no errors

dotnet run --project ECommerce.API -- &
sleep 3

# Verify the endpoint works (empty list)
curl -s http://localhost:5000/api/products/11111111-1111-1111-1111-111111111111/reviews | jq '.success'
# Expected: true

kill %1
```

---

## Acceptance Criteria

- [ ] Project builds with zero errors
- [ ] `AppDbContext` has `Reviews` DbSet
- [ ] `ReviewConfiguration` applied in `OnModelCreating`
- [ ] EF migration creates `Reviews` table with all columns
- [ ] Table has indexes on (ProductId, AuthorId), Status, FlagCount
- [ ] `RowVersion` column added as concurrency token
- [ ] App boots and `GET /api/products/{id}/reviews` returns 200 with empty list
- [ ] `IReviewRepository` → `ReviewRepository` registered as Scoped
- [ ] `Rating.Reconstitute` and `ReviewText.Reconstitute` are `internal`
