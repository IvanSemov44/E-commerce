using ECommerce.Core.Common;

namespace ECommerce.Core.Entities;

public class Wishlist : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
