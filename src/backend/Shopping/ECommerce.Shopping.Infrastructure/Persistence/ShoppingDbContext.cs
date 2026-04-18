using ECommerce.Shopping.Domain.Aggregates.Cart;
using ECommerce.Shopping.Domain.Aggregates.Wishlist;
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShoppingDbContext).Assembly);
    }
}
