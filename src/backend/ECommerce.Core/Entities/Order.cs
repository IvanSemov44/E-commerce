using System.ComponentModel.DataAnnotations;
using ECommerce.Core.Common;
using ECommerce.Core.Enums;
using ECommerce.Core.Interfaces;

namespace ECommerce.Core.Entities;

/// <summary>
/// Represents an order in the e-commerce system.
/// Implements optimistic concurrency for status and payment updates.
/// </summary>
public class Order : BaseEntity, IConcurrencyToken
{
    public string OrderNumber { get; set; } = null!;
    public Guid? UserId { get; set; }
    public string? GuestEmail { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public string? PaymentMethod { get; set; }
    public string? PaymentIntentId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public Guid? ShippingAddressId { get; set; }
    public Guid? BillingAddressId { get; set; }
    public Guid? PromoCodeId { get; set; }
    public string? Notes { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Concurrency token for optimistic locking on status and payment updates.
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // Navigation properties
    public virtual User? User { get; set; }
    public virtual Address? ShippingAddress { get; set; }
    public virtual Address? BillingAddress { get; set; }
    public virtual PromoCode? PromoCode { get; set; }
    public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
