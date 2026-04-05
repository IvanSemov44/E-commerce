using ECommerce.Core.Entities;
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
        modelBuilder.Entity<Category>().ToTable("Categories");
        modelBuilder.Entity<Product>().ToTable("Products");
        modelBuilder.Entity<ProductImage>().ToTable("ProductImages");
        modelBuilder.Entity<ProductRatingReadModel>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("Reviews", "public");
            entity.Property(x => x.ProductId).HasColumnName("ProductId");
            entity.Property(x => x.Rating).HasColumnName("Rating");
        });
    }
}
