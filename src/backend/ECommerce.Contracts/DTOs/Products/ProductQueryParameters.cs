using ECommerce.Contracts.DTOs.Common;

namespace ECommerce.Contracts.DTOs.Products;

/// <summary>
/// Query parameters for the product listing endpoint.
/// Inherits page, pageSize, search, sortBy, sortOrder from RequestParameters.
/// </summary>
public class ProductQueryParameters : RequestParameters
{
    public Guid? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? MinRating { get; set; }
    public bool? IsFeatured { get; set; }
}

