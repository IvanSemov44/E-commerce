using ECommerce.SharedKernel.Common;

namespace ECommerce.SharedKernel.Entities;

public class ProductImage : BaseEntity
{
    public Guid ProductId { get; set; }
    public string Url { get; set; } = null!;
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }

    // Navigation property
    public virtual Product Product { get; set; } = null!;
}
