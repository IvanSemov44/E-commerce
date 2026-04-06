using ECommerce.Core.Entities;
using ECommerce.Promotions.Domain.Aggregates.PromoCode;
using ECommerce.Promotions.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

/// <summary>
/// Configuration for User entity.
/// Users don't need optimistic concurrency - updates are typically single-user operations.
/// Note: User does not implement IConcurrencyToken, so no RowVersion property exists.
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
    }
}

/// <summary>
/// Configuration for Category entity.
/// Categories don't need optimistic concurrency - admin operations are typically sequential.
/// Note: Category does not implement IConcurrencyToken, so no RowVersion property exists.
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
    }
}

/// <summary>
/// Configuration for Product entity.
/// Products NEED optimistic concurrency for inventory and pricing updates.
/// Product implements IConcurrencyToken for RowVersion support.
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
/// Note: ProductImage does not implement IConcurrencyToken, so no RowVersion property exists.
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
    }
}

/// <summary>
/// Configuration for Address entity.
/// Addresses don't need optimistic concurrency - single user operations.
/// Note: Address does not implement IConcurrencyToken, so no RowVersion property exists.
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
    }
}

/// <summary>
/// Configuration for Cart entity.
/// Carts NEED optimistic concurrency for concurrent item updates.
/// Cart implements IConcurrencyToken for RowVersion support.
/// </summary>
public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.SessionId);

        // Enable RowVersion for optimistic concurrency - critical for cart item mutation race conditions
        entity.Property(e => e.RowVersion).IsRowVersion();

        entity.HasOne(e => e.User)
            .WithOne(e => e.Cart)
            .HasForeignKey<Cart>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configuration for CartItem entity.
/// CartItems don't need optimistic concurrency - managed through Cart aggregate.
/// Note: CartItem does not implement IConcurrencyToken, so no RowVersion property exists.
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
    }
}

/// <summary>
/// Configuration for Order entity.
/// Orders NEED optimistic concurrency for status and payment updates.
/// Order implements IConcurrencyToken for RowVersion support.
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

        // PromoCodeId is a foreign key to the new Promotions bounded context
        // The relationship is not configured here as PromoCode is now a DDD aggregate
        entity.Property(e => e.PromoCodeId)
            .IsRequired(false);
    }
}

/// <summary>
/// Configuration for OrderItem entity.
/// OrderItems don't need optimistic concurrency - managed through Order aggregate.
/// Note: OrderItem does not implement IConcurrencyToken, so no RowVersion property exists.
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
    }
}

/// <summary>
/// Configuration for Promotions PromoCode aggregate in shared AppDbContext.
/// Explicit value-object mappings are required for design-time model creation.
/// </summary>
public class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode>
{
    public void Configure(EntityTypeBuilder<PromoCode> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.Code).IsUnique();

        entity.Property(e => e.Code)
            .HasColumnName("Code")
            .HasMaxLength(50)
            .HasConversion(
                value => value.Value,
                value => PromoCodeString.Create(value).GetDataOrThrow())
            .IsRequired();

        entity.ComplexProperty(e => e.Discount, discountBuilder =>
        {
            discountBuilder.Property(d => d.Type)
                .HasColumnName("DiscountType")
                .HasConversion<string>()
                .IsRequired();

            discountBuilder.Property(d => d.Amount)
                .HasColumnName("DiscountValue")
                .HasPrecision(18, 2)
                .IsRequired();
        });

        entity.ComplexProperty(e => e.ValidPeriod, rangeBuilder =>
        {
            rangeBuilder.Property(v => v.Start)
                .HasColumnName("StartDate");

            rangeBuilder.Property(v => v.End)
                .HasColumnName("EndDate");
        });

        entity.Property(e => e.MinimumOrderAmount)
            .HasColumnName("MinOrderAmount")
            .HasPrecision(18, 2);

        entity.Property(e => e.MaxDiscountAmount)
            .HasColumnName("MaxDiscountAmount")
            .HasPrecision(18, 2);

        entity.Property(e => e.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();
    }
}

/// <summary>
/// Configuration for Review entity.
/// Reviews don't need optimistic concurrency - single user operations.
/// Note: Review does not implement IConcurrencyToken, so no RowVersion property exists.
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
    }
}

/// <summary>
/// Configuration for Wishlist entity.
/// Wishlists don't need optimistic concurrency - single user operations.
/// Note: Wishlist does not implement IConcurrencyToken, so no RowVersion property exists.
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
    }
}

/// <summary>
/// Configuration for InventoryLog entity.
/// InventoryLogs don't need optimistic concurrency - append-only audit records.
/// Note: InventoryLog does not implement IConcurrencyToken, so no RowVersion property exists.
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
    }
}

/// <summary>
/// Configuration for Inventory bounded-context aggregate in shared AppDbContext.
/// </summary>
public class InventoryItemAggregateConfiguration : IEntityTypeConfiguration<ECommerce.Inventory.Domain.Aggregates.InventoryItem.InventoryItem>
{
    public void Configure(EntityTypeBuilder<ECommerce.Inventory.Domain.Aggregates.InventoryItem.InventoryItem> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.ProductId).IsUnique();

        entity.OwnsOne(e => e.Stock, stockBuilder =>
        {
            stockBuilder.Property(s => s.Quantity)
                .HasColumnName("Quantity")
                .IsRequired();
        });

        entity.Property(e => e.ProductId).IsRequired();
        entity.Property(e => e.LowStockThreshold).IsRequired();
        entity.Property(e => e.TrackInventory).IsRequired();

        entity.Ignore(e => e.Log);

        entity.OwnsMany<ECommerce.Inventory.Domain.Aggregates.InventoryItem.InventoryLog>("_logEntries", logBuilder =>
        {
            logBuilder.ToTable("InventoryItemLogs");
            logBuilder.HasKey(l => l.Id);
            logBuilder.WithOwner().HasForeignKey(l => l.InventoryItemId);

            logBuilder.Property(l => l.InventoryItemId).IsRequired();
            logBuilder.Property(l => l.Delta).IsRequired();
            logBuilder.Property(l => l.Reason).IsRequired().HasMaxLength(500);
            logBuilder.Property(l => l.StockAfter).IsRequired();
            logBuilder.Property(l => l.OccurredAt).IsRequired();
        });
    }
}

/// <summary>
/// Configuration for RefreshToken entity.
/// RefreshTokens don't need optimistic concurrency - simple create/revoke operations.
/// Note: RefreshToken does not implement IConcurrencyToken, so no RowVersion property exists.
/// </summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.Token).IsUnique();

        entity.Property(e => e.Token).IsRequired().HasMaxLength(256);

        entity.HasOne(e => e.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configuration for OutboxMessage entity used by the integration outbox pattern.
/// </summary>
public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> entity)
    {
        entity.ToTable("outbox_messages", schema: "integration");

        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.IdempotencyKey).IsUnique();
        entity.HasIndex(e => e.ProcessedAt);
        entity.HasIndex(e => e.CreatedAt);

        entity.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(512);

        entity.Property(e => e.EventData)
            .IsRequired();

        entity.Property(e => e.LastError)
            .HasMaxLength(2000);
    }
}
