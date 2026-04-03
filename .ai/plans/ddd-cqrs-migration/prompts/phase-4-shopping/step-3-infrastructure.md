# Phase 4, Step 3: Shopping Infrastructure Project

**Prerequisite**: Step 2 (`ECommerce.Shopping.Application`) is complete and `dotnet build` passes.

---

## Important: No EF migration needed

The `Carts`, `CartItems`, and `Wishlists` tables already exist. This step re-maps the existing tables to the new DDD aggregates. The only concern is preserving the `RowVersion` concurrency column and the `Wishlist` table structure.

**Existing table structure (do not change):**
- `Carts`: `Id`, `UserId` (nullable), `SessionId` (nullable), `RowVersion` (`[Timestamp]`)
- `CartItems`: `Id`, `CartId`, `ProductId`, `Quantity`, `UnitPrice`, `Currency`
- `Wishlists`: `Id`, `UserId`, `ProductId` — **one row per user+product pair**, NOT one row per user

The `Wishlist` aggregate stores `List<Guid>` but the table is a join table. Infrastructure must bridge this.

---

## Task: Create ECommerce.Shopping.Infrastructure Project

### 1. Create the project

```bash
cd src/backend
dotnet new classlib -n ECommerce.Shopping.Infrastructure -f net10.0 -o Shopping/ECommerce.Shopping.Infrastructure
dotnet sln ../../ECommerce.sln add Shopping/ECommerce.Shopping.Infrastructure/ECommerce.Shopping.Infrastructure.csproj

dotnet add Shopping/ECommerce.Shopping.Infrastructure/ECommerce.Shopping.Infrastructure.csproj \
    reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
dotnet add Shopping/ECommerce.Shopping.Infrastructure/ECommerce.Shopping.Infrastructure.csproj \
    reference Shopping/ECommerce.Shopping.Domain/ECommerce.Shopping.Domain.csproj
dotnet add Shopping/ECommerce.Shopping.Infrastructure/ECommerce.Shopping.Infrastructure.csproj \
    reference Shopping/ECommerce.Shopping.Application/ECommerce.Shopping.Application.csproj
dotnet add Shopping/ECommerce.Shopping.Infrastructure/ECommerce.Shopping.Infrastructure.csproj \
    reference ECommerce.Infrastructure/ECommerce.Infrastructure.csproj  # for AppDbContext

dotnet add Shopping/ECommerce.Shopping.Infrastructure/ECommerce.Shopping.Infrastructure.csproj \
    package Microsoft.EntityFrameworkCore

rm Shopping/ECommerce.Shopping.Infrastructure/Class1.cs
```

### 2. EF Core configurations

**File: `Shopping/ECommerce.Shopping.Infrastructure/Persistence/Configurations/CartConfiguration.cs`**

```csharp
using ECommerce.Shopping.Domain.Aggregates.Cart;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Shopping.Infrastructure.Persistence.Configurations;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId).IsRequired();

        // Preserve the existing row version concurrency token
        builder.Property(c => c.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        // CartItem child collection via backing field
        builder.HasMany<CartItem>("_items")
               .WithOne()
               .HasForeignKey(i => i.CartId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
```

**File: `Shopping/ECommerce.Shopping.Infrastructure/Persistence/Configurations/CartItemConfiguration.cs`**

```csharp
using ECommerce.Shopping.Domain.Aggregates.Cart;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Shopping.Infrastructure.Persistence.Configurations;

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.CartId).IsRequired();
        builder.Property(i => i.ProductId).IsRequired();
        builder.Property(i => i.Quantity).IsRequired();
        builder.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(i => i.Currency).HasMaxLength(10).IsRequired();
    }
}
```

**File: `Shopping/ECommerce.Shopping.Infrastructure/Persistence/Configurations/WishlistConfiguration.cs`**

```csharp
using ECommerce.Shopping.Domain.Aggregates.Wishlist;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Shopping.Infrastructure.Persistence.Configurations;

// The existing Wishlists table stores one row per (UserId, ProductId).
// The Wishlist aggregate stores List<Guid> ProductIds.
// We cannot use OwnsMany directly because the existing table has its own Id column.
// The repository manually reconstitutes the aggregate from multiple rows.
// This configuration maps the Wishlist aggregate root only — no OwnsMany.
public class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        builder.ToTable("WishlistItems");  // maps to the join table
        builder.HasKey(w => w.Id);
        builder.Property(w => w.UserId).IsRequired();
        // ProductIds is a private List<Guid> — not mapped here.
        // WishlistRepository handles the multi-row ↔ aggregate translation.
        builder.Ignore(w => w.ProductIds);
    }
}
```

> **Note on Wishlist mapping**: The existing `Wishlists` table has one row per `(UserId, ProductId)` pair. The `Wishlist` aggregate needs one aggregate per `UserId` with a list of `ProductId` values. The `WishlistRepository` must manually group rows by `UserId` to reconstruct the aggregate. This is the cleanest approach without an EF migration.

### 3. Add configurations to AppDbContext

In `AppDbContext.cs` add DbSets and apply configurations:

```csharp
// Add DbSets:
public DbSet<Cart>     Carts     => Set<Cart>();
public DbSet<CartItem> CartItems => Set<CartItem>();
// No DbSet<Wishlist> — repository queries Wishlists table directly

// In OnModelCreating:
modelBuilder.ApplyConfiguration(new CartConfiguration());
modelBuilder.ApplyConfiguration(new CartItemConfiguration());
```

### 4. Implement ICartRepository

**File: `Shopping/ECommerce.Shopping.Infrastructure/Persistence/Repositories/CartRepository.cs`**

```csharp
using ECommerce.Shopping.Domain.Aggregates.Cart;
using ECommerce.Shopping.Domain.Interfaces;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Shopping.Infrastructure.Persistence.Repositories;

public class CartRepository(AppDbContext _db) : ICartRepository
{
    public async Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _db.Carts
            .Include("_items")
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

    public async Task<Cart?> GetByIdAsync(Guid cartId, CancellationToken ct = default)
        => await _db.Carts
            .Include("_items")
            .FirstOrDefaultAsync(c => c.Id == cartId, ct);

    public async Task UpsertAsync(Cart cart, CancellationToken ct = default)
    {
        var exists = await _db.Carts.AnyAsync(c => c.Id == cart.Id, ct);
        if (exists)
            _db.Carts.Update(cart);
        else
            await _db.Carts.AddAsync(cart, ct);
    }

    public Task DeleteAsync(Cart cart, CancellationToken ct = default)
    {
        _db.Carts.Remove(cart);
        return Task.CompletedTask;
    }
}
```

### 5. Implement IWishlistRepository

The repository must bridge the gap between the existing `(UserId, ProductId)` table rows and the `Wishlist` aggregate's `List<Guid>`.

**File: `Shopping/ECommerce.Shopping.Infrastructure/Persistence/Repositories/WishlistRepository.cs`**

```csharp
using ECommerce.Shopping.Domain.Aggregates.Wishlist;
using ECommerce.Shopping.Domain.Interfaces;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Shopping.Infrastructure.Persistence.Repositories;

public class WishlistRepository(AppDbContext _db) : IWishlistRepository
{
    // Existing Wishlists table: one row per (UserId, ProductId)
    // We query it directly as an anonymous projection and reconstitute the aggregate.

    public async Task<Wishlist?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var rows = await _db.Database
            .SqlQueryRaw<WishlistRow>(
                "SELECT \"Id\", \"UserId\", \"ProductId\" FROM \"Wishlists\" WHERE \"UserId\" = {0}",
                userId)
            .ToListAsync(ct);

        if (rows.Count == 0) return null;

        var wishlist = Wishlist.Create(userId);
        // Reconstitute: set the Id to the first row's Id (or generate a stable one)
        // and add all product IDs
        foreach (var row in rows)
            wishlist.AddProduct(row.ProductId);

        return wishlist;
    }

    public async Task UpsertAsync(Wishlist wishlist, CancellationToken ct = default)
    {
        // Delete all existing rows for this user and re-insert
        await _db.Database.ExecuteSqlRawAsync(
            "DELETE FROM \"Wishlists\" WHERE \"UserId\" = {0}", wishlist.UserId);

        foreach (var productId in wishlist.ProductIds)
        {
            await _db.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Wishlists\" (\"Id\", \"UserId\", \"ProductId\") VALUES ({0}, {1}, {2})",
                Guid.NewGuid(), wishlist.UserId, productId);
        }
    }

    private record WishlistRow(Guid Id, Guid UserId, Guid ProductId);
}
```

> **Why raw SQL for Wishlist?** The aggregate's `List<Guid>` model doesn't map cleanly to the existing `(Id, UserId, ProductId)` table without an EF migration. Raw SQL lets us preserve the table while using the new aggregate. If you prefer, add a migration to restructure the Wishlists table into a single-row-per-user model with a JSON column — that's also valid but requires a data migration.

### 6. Implement IShoppingDbReader

**File: `Shopping/ECommerce.Shopping.Infrastructure/Services/ShoppingDbReader.cs`**

```csharp
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shopping.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Shopping.Infrastructure.Services;

public class ShoppingDbReader(AppDbContext _db) : IShoppingDbReader
{
    public async Task<ProductPriceInfo?> GetProductPriceAsync(Guid productId, CancellationToken ct)
    {
        // TODO Phase 8: replace with HTTP call to Catalog service
        var product = await _db.Products
            .AsNoTracking()
            .Where(p => p.Id == productId && !p.IsDeleted)
            .Select(p => new { Price = p.Price.Amount, Currency = p.Price.Currency })
            .FirstOrDefaultAsync(ct);

        return product is null ? null : new ProductPriceInfo(product.Price, product.Currency);
    }

    public async Task<bool> ProductExistsAsync(Guid productId, CancellationToken ct)
        => await _db.Products
            .AsNoTracking()
            .AnyAsync(p => p.Id == productId && !p.IsDeleted, ct);

    public async Task<bool> IsInStockAsync(Guid productId, int quantity, CancellationToken ct)
        => await _db.InventoryItems
            .AsNoTracking()
            .AnyAsync(i => i.ProductId == productId && i.Stock.Quantity >= quantity, ct);
}
```

### 7. DI registration

**File: `Shopping/ECommerce.Shopping.Infrastructure/DependencyInjection.cs`**

```csharp
using ECommerce.Shopping.Application.Interfaces;
using ECommerce.Shopping.Domain.Interfaces;
using ECommerce.Shopping.Infrastructure.Persistence.Repositories;
using ECommerce.Shopping.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Shopping.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddShoppingInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        services.AddScoped<IShoppingDbReader, ShoppingDbReader>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(
                typeof(ECommerce.Shopping.Application.Commands.AddToCart.AddToCartCommand).Assembly));

        return services;
    }
}
```

### 8. Wire up in Program.cs

```bash
dotnet add ECommerce.API/ECommerce.API.csproj \
    reference Shopping/ECommerce.Shopping.Infrastructure/ECommerce.Shopping.Infrastructure.csproj
```

In `Program.cs`:
```csharp
builder.Services.AddShoppingInfrastructure();
```

### 9. Update TestWebApplicationFactory

Add Cart seeding for the seeded test user so integration tests don't get empty carts:

```csharp
// In TestWebApplicationFactory seed:
// (no specific cart seed needed — GetCartQuery creates on first access)
// If you need a seeded cart item for testing remove/update operations,
// add it here after seeding the product and the test user.
```

### 10. Verify

```bash
cd src/backend
dotnet build
dotnet test
```

---

## Acceptance Criteria

- [ ] `ECommerce.Shopping.Infrastructure` project created and added to solution
- [ ] `CartConfiguration` maps `Cart` to existing `Carts` table and preserves `RowVersion` as `IsRowVersion().IsConcurrencyToken()`
- [ ] `CartItemConfiguration` maps `CartItem` to existing `CartItems` table
- [ ] `Include("_items")` used in repository (backing-field string)
- [ ] `CartRepository.UpsertAsync` checks existence before deciding `Update` vs `Add`
- [ ] `WishlistRepository` handles the existing one-row-per-product table structure via raw SQL
- [ ] `ShoppingDbReader` queries Products and InventoryItems with `// TODO Phase 8` comment on product query
- [ ] `DependencyInjection.cs` registers all three services and MediatR handlers
- [ ] `dotnet build` and `dotnet test` pass
