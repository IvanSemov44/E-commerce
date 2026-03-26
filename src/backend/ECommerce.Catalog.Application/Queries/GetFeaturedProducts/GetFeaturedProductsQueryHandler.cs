using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.Catalog.Application.Extensions;
using ECommerce.Catalog.Domain.Interfaces;

namespace ECommerce.Catalog.Application.Queries.GetFeaturedProducts;

public class GetFeaturedProductsQueryHandler(
    IProductRepository _products
) : IRequestHandler<GetFeaturedProductsQuery, Result<List<ProductDto>>>
{
    public async Task<Result<List<ProductDto>>> Handle(GetFeaturedProductsQuery request, CancellationToken cancellationToken)
    {
        var items = await _products.GetFeaturedAsync(request.Limit, cancellationToken);
        var dtos = items.Select(p => p.ToDto(string.Empty)).ToList();
        return Result<List<ProductDto>>.Ok(dtos);
    }
}
