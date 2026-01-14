using ECommerce.Core.Common;

namespace ECommerce.Core.Entities;

public class Review : BaseEntity
{
    public Guid ProductId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? OrderId { get; set; }
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }
    public bool IsVerified { get; set; }
    public bool IsApproved { get; set; }

    // Navigation properties
    public virtual Product Product { get; set; } = null!;
    public virtual User? User { get; set; }
    public virtual Order? Order { get; set; }
}
