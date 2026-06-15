using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.Catalog.Application.Extensions;
using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.Catalog.Domain.ValueObjects;

namespace ECommerce.Catalog.Application.Queries;

public class GetProductBySlugQueryHandler(
    IProductRepository _products
) : IRequestHandler<GetProductBySlugQuery, Result<ProductDetailDto>>
{
    public async Task<Result<ProductDetailDto>> Handle(GetProductBySlugQuery request, CancellationToken cancellationToken)
    {
        var slugResult = Slug.Create(request.Slug);
        if (!slugResult.IsSuccess)
            return Result<ProductDetailDto>.Fail(CatalogApplicationErrors.ProductNotFound);

        var result = await _products.GetBySlugWithCategoryAsync(slugResult.GetDataOrThrow(), cancellationToken);
        if (result is null)
            return Result<ProductDetailDto>.Fail(CatalogApplicationErrors.ProductNotFound);

        if (string.IsNullOrWhiteSpace(result.Value.CategoryName))
            return Result<ProductDetailDto>.Fail(CatalogApplicationErrors.CategoryNotFound);

        var ratings = await _products.GetRatingsByProductIdsAsync([result.Value.Product.Id], cancellationToken);
        ratings.TryGetValue(result.Value.Product.Id, out var r);

        return Result<ProductDetailDto>.Ok(result.Value.Product.ToDetailDto(result.Value.CategoryName, r.AverageRating, r.ReviewCount));
    }
}
