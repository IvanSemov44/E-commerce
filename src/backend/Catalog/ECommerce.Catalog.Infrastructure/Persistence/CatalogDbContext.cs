using ECommerce.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Infrastructure.Persistence;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductRatingReadModel> ProductRatings => Set<ProductRatingReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("catalog");

        // Restrict Catalog model to Catalog-owned persistence types only.
        // Shared entities contain navigation properties to other bounded contexts;
        // if left as-is EF will discover and scaffold unrelated tables.
        modelBuilder.Entity<Product>(builder =>
        {
            builder.Ignore(p => p.Reviews);
            builder.Ignore(p => p.CartItems);
            builder.Ignore(p => p.OrderItems);
            builder.Ignore(p => p.Wishlists);
            builder.Ignore(p => p.InventoryLogs);
        });

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
