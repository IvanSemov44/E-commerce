using ECommerce.Core.Common;

namespace ECommerce.Core.Entities;

public class Cart : BaseEntity
{
    public Guid? UserId { get; set; }
    public string? SessionId { get; set; }

    // Navigation properties
    public virtual User? User { get; set; }
    public virtual ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
