using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Pagination;
using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.Catalog.Application.Extensions;
using ECommerce.Catalog.Domain.Interfaces;

namespace ECommerce.Catalog.Application.Queries;

public class GetFeaturedProductsQueryHandler(
    IProductRepository _products
) : IRequestHandler<GetFeaturedProductsQuery, Result<PaginatedResult<ProductDto>>>
{
    public async Task<Result<PaginatedResult<ProductDto>>> Handle(GetFeaturedProductsQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = PaginationRequestNormalizer.Normalize(request.Page, request.PageSize);

        var (items, total) = await _products.GetFeaturedPagedAsync(page, pageSize, cancellationToken);

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
