using ECommerce.Core.Entities;
using ECommerce.Infrastructure.Data.Configurations;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Data;

/// <summary>
/// Main database context for the E-Commerce application.
/// Uses EF Core with PostgreSQL for data persistence.
/// </summary>
public class AppDbContext : DbContext, IDataProtectionKeyContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Users and Authentication
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<Address> Addresses { get; set; } = null!;

    // Catalog
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<ProductImage> ProductImages { get; set; } = null!;
    public DbSet<Review> Reviews { get; set; } = null!;

    // Shopping
    public DbSet<Cart> Carts { get; set; } = null!;
    public DbSet<CartItem> CartItems { get; set; } = null!;
    public DbSet<Wishlist> Wishlists { get; set; } = null!;

    // Orders and Payments
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    public DbSet<PromoCode> PromoCodes { get; set; } = null!;

    // Inventory
    public DbSet<InventoryLog> InventoryLogs { get; set; } = null!;

    // Data Protection Keys for persistent key storage
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the Configurations namespace
        // Each configuration class handles its own entity's mapping and conventions
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserConfiguration).Assembly);
    }
}
