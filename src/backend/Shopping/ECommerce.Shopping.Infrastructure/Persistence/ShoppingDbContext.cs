using ECommerce.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Shopping.Infrastructure.Persistence;

public class ShoppingDbContext(DbContextOptions<ShoppingDbContext> options) : DbContext(options)
{
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<ProductReadModel> Products => Set<ProductReadModel>();
    public DbSet<InventoryItemReadModel> InventoryItems => Set<InventoryItemReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("shopping");
        modelBuilder.Entity<Cart>().ToTable("Carts");
        modelBuilder.Entity<CartItem>().ToTable("CartItems");
        modelBuilder.Entity<Wishlist>().ToTable("Wishlists");
        modelBuilder.Entity<ProductReadModel>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.ToTable("Products", "public");
            entity.Property(x => x.Id).HasColumnName("Id");
            entity.Property(x => x.IsActive).HasColumnName("IsActive");
            entity.Property(x => x.Price).HasColumnName("Price");
            entity.Property(x => x.Sku).HasColumnName("Sku");
        });
        modelBuilder.Entity<InventoryItemReadModel>(entity =>
        {
            entity.HasKey(x => x.ProductId);
            entity.ToTable("InventoryStockProjections");
            entity.Property(x => x.ProductId).HasColumnName("ProductId");
            entity.Property(x => x.Quantity).HasColumnName("Quantity");
            entity.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
        });
    }
}
