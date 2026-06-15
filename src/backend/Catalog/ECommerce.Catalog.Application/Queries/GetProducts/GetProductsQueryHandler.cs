using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Pagination;
using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.Catalog.Application.Extensions;
using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.Catalog.Domain.Queries;

namespace ECommerce.Catalog.Application.Queries;

public class GetProductsQueryHandler(
    IProductRepository _products,
    ICategoryRepository _categories
) : IRequestHandler<GetProductsQuery, Result<PaginatedResult<ProductDto>>>
{
    public async Task<Result<PaginatedResult<ProductDto>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = PaginationRequestNormalizer.Normalize(request.Page, request.PageSize);

        var queryParams = new ProductQueryParams(
            page, pageSize,
            request.CategoryId,
            request.Search,
            request.MinPrice,
            request.MaxPrice,
            request.MinRating,
            request.IsFeatured,
            request.SortBy);

        var (items, total) = await _products.GetPagedAsync(queryParams, cancellationToken);

        var categoryNames = await ProductCategoryNameLookup.BuildAsync(items, _categories, cancellationToken);
        var ratings       = await _products.GetRatingsByProductIdsAsync(items.Select(p => p.Id), cancellationToken);

        var dtos = items.Select(p =>
        {
            ratings.TryGetValue(p.Id, out var r);
            return p.ToDto(categoryNames.TryGetValue(p.CategoryId, out var n) ? n : string.Empty, r.AverageRating, r.ReviewCount);
        }).ToList();

        return Result<PaginatedResult<ProductDto>>.Ok(new PaginatedResult<ProductDto>
        {
            Items      = dtos,
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize
        });
    }
}
