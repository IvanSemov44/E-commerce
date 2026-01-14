using ECommerce.Core.Common;

namespace ECommerce.Core.Entities;

public class PromoCode : BaseEntity
{
    public string Code { get; set; } = null!;
    public string DiscountType { get; set; } = null!; // 'percentage' or 'fixed'
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
