using System.ComponentModel.DataAnnotations;
using ECommerce.Core.Common;
using ECommerce.Core.Interfaces;

namespace ECommerce.Core.Entities;

public class Cart : BaseEntity, IConcurrencyToken
{
    public Guid? UserId { get; set; }
    public string? SessionId { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // Navigation properties
    public virtual User? User { get; set; }
    public virtual ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
