using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ECommerce.SharedKernel.Common;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.SharedKernel.Entities;

/// <summary>
/// Represents a product in the e-commerce catalog.
/// Implements optimistic concurrency for inventory and pricing updates.
/// </summary>
public class Product : BaseEntity, IConcurrencyToken
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public decimal? CostPrice { get; set; }
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public Guid? CategoryId { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 10;
    public decimal? Weight { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }

    /// <summary>
    /// Concurrency token for optimistic locking on inventory and pricing updates.
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // Navigation properties
    public virtual Category? Category { get; set; }
    public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
    public virtual ICollection<InventoryLog> InventoryLogs { get; set; } = new List<InventoryLog>();
}
