using System.ComponentModel.DataAnnotations;
using ECommerce.Core.Common;
using ECommerce.Core.Enums;
using ECommerce.Core.Interfaces;

namespace ECommerce.Core.Entities;

/// <summary>
/// Represents a promotional code in the e-commerce system.
/// Implements optimistic concurrency for usage count updates.
/// </summary>
public class PromoCode : BaseEntity, IConcurrencyToken
{
    public string Code { get; set; } = null!;
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Concurrency token for optimistic locking on usage count updates.
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // Navigation properties
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
