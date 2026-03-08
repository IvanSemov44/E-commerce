using ECommerce.Core.Common;

namespace ECommerce.Core.Entities;

public class InventoryLog : BaseEntity
{
    public Guid ProductId { get; set; }
    public int QuantityChange { get; set; }
    public string Reason { get; set; } = null!; // 'sale', 'restock', 'adjustment', 'return'
    public Guid? ReferenceId { get; set; } // order_id or other reference
    public string? Notes { get; set; }
    public Guid? CreatedByUserId { get; set; }

    // Navigation properties
    public virtual Product Product { get; set; } = null!;
    public virtual User? CreatedByUser { get; set; }
}
