using System.Linq;
using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.Catalog.Domain.Aggregates.Product;

namespace ECommerce.Catalog.Application.Extensions;

public static class ProductMappingExtensions
{
    public static ProductDto ToDto(this Product product, string categoryName)
    {
        var primary = product.Images.FirstOrDefault(i => i.IsPrimary) ?? product.Images.FirstOrDefault();
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name.Value,
            Slug = product.Slug.Value,
            Price = product.Price.Amount,
            Currency = product.Price.Currency,
            PrimaryImageUrl = primary?.Url,
            CategoryName = categoryName,
            IsActive = product.Status == ProductStatus.Active,
        };
    }

    public static ProductDetailDto ToDetailDto(this Product product, string categoryName)
    {
        return new ProductDetailDto
        {
            Id = product.Id,
            Name = product.Name.Value,
            Slug = product.Slug.Value,
            Price = product.Price.Amount,
            Currency = product.Price.Currency,
            CompareAtPrice = product.CompareAtPrice?.Amount,
            Sku = product.Sku.Value,
            Description = product.Description,
            Status = product.Status.ToString(),
            IsFeatured = product.IsFeatured,
            CategoryId = product.CategoryId,
            CategoryName = categoryName,
            Images = product.Images.Select(i => new ProductImageDto
            {
                Id = i.Id,
                Url = i.Url,
                AltText = i.AltText,
                IsPrimary = i.IsPrimary,
                DisplayOrder = i.DisplayOrder,
            }).ToList(),
        };
    }
}
