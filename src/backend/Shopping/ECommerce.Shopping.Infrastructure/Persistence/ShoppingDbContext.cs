using ECommerce.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Shopping.Infrastructure.Persistence;

public class ShoppingDbContext(DbContextOptions<ShoppingDbContext> options) : DbContext(options)
{
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("shopping");
        modelBuilder.Entity<Cart>().ToTable("Carts");
        modelBuilder.Entity<CartItem>().ToTable("CartItems");
        modelBuilder.Entity<Wishlist>().ToTable("Wishlists");
    }
}
