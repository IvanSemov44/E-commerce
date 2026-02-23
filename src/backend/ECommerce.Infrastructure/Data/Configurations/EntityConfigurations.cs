using ECommerce.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

/// <summary>
/// Configuration for User entity.
/// Users don't need optimistic concurrency - updates are typically single-user operations.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.Email).IsUnique();
        
        entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
        entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
        entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        
        // Ignore RowVersion - User updates don't need optimistic concurrency
        entity.Ignore(e => e.RowVersion);
    }
}

/// <summary>
/// Configuration for Category entity.
/// Categories don't need optimistic concurrency - admin operations are typically sequential.
/// </summary>
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.Slug).IsUnique();
        
        entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
        
        entity.HasOne(e => e.Parent)
            .WithMany(e => e.Children)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Ignore RowVersion - Category updates don't need optimistic concurrency
        entity.Ignore(e => e.RowVersion);
    }
}

/// <summary>
/// Configuration for Product entity.
/// Products NEED optimistic concurrency for inventory and pricing updates.
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.Slug).IsUnique();
        entity.HasIndex(e => e.IsActive);
        entity.HasIndex(e => e.IsFeatured);
        entity.HasIndex(e => new { e.IsActive, e.Price });
        
        entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
        entity.Property(e => e.Slug).IsRequired().HasMaxLength(255);
        entity.Property(e => e.Price).HasPrecision(10, 2);
        entity.Property(e => e.CompareAtPrice).HasPrecision(10, 2);
        entity.Property(e => e.CostPrice).HasPrecision(10, 2);
        
        // Enable RowVersion for optimistic concurrency - critical for inventory management
        entity.Property(e => e.RowVersion).IsRowVersion();
        
        entity.HasOne(e => e.Category)
            .WithMany(e => e.Products)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>
/// Configuration for ProductImage entity.
/// ProductImages don't need optimistic concurrency - managed through Product aggregate.
/// </summary>
public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> entity)
    {
        entity.HasKey(e => e.Id);
        
        entity.HasOne(e => e.Product)
            .WithMany(e => e.Images)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Ignore RowVersion - images are managed through Product aggregate
        entity.Ignore(e => e.RowVersion);
    }
}

/// <summary>
/// Configuration for Address entity.
/// Addresses don't need optimistic concurrency - single user operations.
/// </summary>
public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> entity)
    {
        entity.HasKey(e => e.Id);
        
        entity.Property(e => e.StreetLine1).IsRequired().HasMaxLength(255);
        entity.Property(e => e.City).IsRequired().HasMaxLength(100);
        entity.Property(e => e.State).IsRequired().HasMaxLength(100);
        entity.Property(e => e.PostalCode).IsRequired().HasMaxLength(20);
        entity.Property(e => e.Country).IsRequired().HasMaxLength(2);
        
        entity.HasOne(e => e.User)
            .WithMany(e => e.Addresses)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Ignore RowVersion - address updates are single-user operations
        entity.Ignore(e => e.RowVersion);
    }
}

/// <summary>
/// Configuration for Cart entity.
/// Carts don't need optimistic concurrency - session-based operations.
/// </summary>
public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.SessionId);
        
        entity.HasOne(e => e.User)
            .WithOne(e => e.Cart)
            .HasForeignKey<Cart>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Ignore RowVersion - cart operations are session-based
        entity.Ignore(e => e.RowVersion);
    }
}

/// <summary>
/// Configuration for CartItem entity.
/// CartItems don't need optimistic concurrency - managed through Cart aggregate.
/// </summary>
public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.CartId, e.ProductId }).IsUnique();
        
        entity.HasOne(e => e.Cart)
            .WithMany(e => e.Items)
            .HasForeignKey(e => e.CartId)
            .OnDelete(DeleteBehavior.Cascade);
        
        entity.HasOne(e => e.Product)
            .WithMany(e => e.CartItems)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Ignore RowVersion - cart items are managed through Cart aggregate
        entity.Ignore(e => e.RowVersion);
    }
}

/// <summary>
/// Configuration for PromoCode entity.
/// PromoCodes NEED optimistic concurrency for usage count updates.
/// </summary>
public class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode>
{
    public void Configure(EntityTypeBuilder<PromoCode> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.Code).IsUnique();
        
        entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
        entity.Property(e => e.DiscountValue).HasPrecision(10, 2);
        entity.Property(e => e.MinOrderAmount).HasPrecision(10, 2);
        entity.Property(e => e.MaxDiscountAmount).HasPrecision(10, 2);
        
        // Enable RowVersion for optimistic concurrency - critical for usage limits
        entity.Property(e => e.RowVersion).IsRowVersion();
    }
}

/// <summary>
/// Configuration for Order entity.
/// Orders NEED optimistic concurrency for status and payment updates.
/// </summary>
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.OrderNumber).IsUnique();
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => e.CreatedAt);
        entity.HasIndex(e => e.Status);
        
        entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(20);
        entity.Property(e => e.Subtotal).HasPrecision(10, 2);
        entity.Property(e => e.DiscountAmount).HasPrecision(10, 2);
        entity.Property(e => e.ShippingAmount).HasPrecision(10, 2);
        entity.Property(e => e.TaxAmount).HasPrecision(10, 2);
        entity.Property(e => e.TotalAmount).HasPrecision(10, 2);
        
        // Enable RowVersion for optimistic concurrency - critical for order processing
        entity.Property(e => e.RowVersion).IsRowVersion();
        
        entity.HasOne(e => e.User)
            .WithMany(e => e.Orders)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        
        entity.HasOne(e => e.PromoCode)
            .WithMany(e => e.Orders)
            .HasForeignKey(e => e.PromoCodeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>
/// Configuration for OrderItem entity.
/// OrderItems don't need optimistic concurrency - managed through Order aggregate.
/// </summary>
public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> entity)
    {
        entity.HasKey(e => e.Id);
        
        entity.Property(e => e.UnitPrice).HasPrecision(10, 2);
        entity.Property(e => e.TotalPrice).HasPrecision(10, 2);
        
        entity.HasOne(e => e.Order)
            .WithMany(e => e.Items)
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        
        entity.HasOne(e => e.Product)
            .WithMany(e => e.OrderItems)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Ignore RowVersion - order items are managed through Order aggregate
        entity.Ignore(e => e.RowVersion);
    }
}

/// <summary>
/// Configuration for Review entity.
/// Reviews don't need optimistic concurrency - single user operations.
/// </summary>
public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> entity)
    {
        entity.HasKey(e => e.Id);
        
        entity.HasOne(e => e.Product)
            .WithMany(e => e.Reviews)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        
        entity.HasOne(e => e.User)
            .WithMany(e => e.Reviews)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Ignore RowVersion - reviews are single-user operations
        entity.Ignore(e => e.RowVersion);
    }
}

/// <summary>
/// Configuration for Wishlist entity.
/// Wishlists don't need optimistic concurrency - single user operations.
/// </summary>
public class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
        
        entity.HasOne(e => e.User)
            .WithMany(e => e.Wishlists)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        entity.HasOne(e => e.Product)
            .WithMany(e => e.Wishlists)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Ignore RowVersion - wishlist operations are single-user
        entity.Ignore(e => e.RowVersion);
    }
}

/// <summary>
/// Configuration for InventoryLog entity.
/// InventoryLogs don't need optimistic concurrency - append-only audit records.
/// </summary>
public class InventoryLogConfiguration : IEntityTypeConfiguration<InventoryLog>
{
    public void Configure(EntityTypeBuilder<InventoryLog> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.ProductId);
        entity.HasIndex(e => new { e.ProductId, e.CreatedAt });
        
        entity.HasOne(e => e.Product)
            .WithMany(e => e.InventoryLogs)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Ignore RowVersion - inventory logs are append-only audit records
        entity.Ignore(e => e.RowVersion);
    }
}

/// <summary>
/// Configuration for RefreshToken entity.
/// RefreshTokens don't need optimistic concurrency - simple create/revoke operations.
/// </summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.Token).IsUnique();
        
        entity.Property(e => e.Token).IsRequired().HasMaxLength(256);
        
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Ignore RowVersion - tokens are simple create/revoke operations
        entity.Ignore(e => e.RowVersion);
    }
}
