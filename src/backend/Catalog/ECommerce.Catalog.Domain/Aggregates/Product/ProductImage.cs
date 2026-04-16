using System;
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Catalog.Domain.Aggregates.Product;

public sealed class ProductImage : Entity
{
    public Guid ProductId { get; private set; }
    public string Url { get; private set; } = null!;
    public string? AltText { get; private set; }
    public bool IsPrimary { get; private set; }
    public int DisplayOrder { get; private set; }

    private ProductImage() { }

    internal ProductImage(Guid id, Guid productId, string url, string? altText, bool isPrimary, int displayOrder)
    {
        Id = id;
        ProductId = productId;
        Url = url;
        AltText = altText;
        IsPrimary = isPrimary;
        DisplayOrder = displayOrder;
    }

    internal void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;
}
