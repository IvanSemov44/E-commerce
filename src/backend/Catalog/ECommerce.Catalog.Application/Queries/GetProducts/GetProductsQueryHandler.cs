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
    IProductRepository _products
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

        var dtos = items.Select(p => p.ToDto(string.Empty)).ToList();

        return Result<PaginatedResult<ProductDto>>.Ok(new PaginatedResult<ProductDto>
        {
            Items      = dtos,
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize
        });
    }
}
