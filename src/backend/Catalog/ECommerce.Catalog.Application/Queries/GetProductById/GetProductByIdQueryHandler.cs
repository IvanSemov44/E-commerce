
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.Catalog.Application.Extensions;
using ECommerce.Catalog.Domain.Interfaces;

namespace ECommerce.Catalog.Application.Queries;

public class GetProductByIdQueryHandler(
    IProductRepository _products
) : IRequestHandler<GetProductByIdQuery, Result<ProductDetailDto>>
{
    public async Task<Result<ProductDetailDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await _products.GetByIdWithCategoryAsync(request.Id, cancellationToken);
        if (result is null)
            return Result<ProductDetailDto>.Fail(CatalogApplicationErrors.ProductNotFound);

        if (string.IsNullOrWhiteSpace(result.Value.CategoryName))
            return Result<ProductDetailDto>.Fail(CatalogApplicationErrors.CategoryNotFound);

        var ratings = await _products.GetRatingsByProductIdsAsync([request.Id], cancellationToken);
        ratings.TryGetValue(request.Id, out var r);

        return Result<ProductDetailDto>.Ok(result.Value.Product.ToDetailDto(result.Value.CategoryName, r.AverageRating, r.ReviewCount));
    }
}
