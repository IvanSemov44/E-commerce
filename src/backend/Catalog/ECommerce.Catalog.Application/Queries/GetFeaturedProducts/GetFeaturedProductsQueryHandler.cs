using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.Catalog.Application.DTOs.Common;
using ECommerce.Catalog.Application.Extensions;
using ECommerce.Catalog.Domain.Interfaces;

namespace ECommerce.Catalog.Application.Queries.GetFeaturedProducts;

public class GetFeaturedProductsQueryHandler(
    IProductRepository _products
) : IRequestHandler<GetFeaturedProductsQuery, Result<PaginatedResult<ProductDto>>>
{
    public async Task<Result<PaginatedResult<ProductDto>>> Handle(GetFeaturedProductsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _products.GetFeaturedPagedAsync(request.Page, request.PageSize, cancellationToken);
        var dtos = items.Select(p => p.ToDto(string.Empty)).ToList();

        var page = new PaginatedResult<ProductDto>
        {
            Items = dtos,
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Result<PaginatedResult<ProductDto>>.Ok(page);
    }
}
