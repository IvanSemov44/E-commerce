using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.Catalog.Application.Extensions;
using ECommerce.Catalog.Domain.Interfaces;

namespace ECommerce.Catalog.Application.Queries.GetLowStockProducts;

public class GetLowStockProductsQueryHandler(
    IProductRepository _products
) : IRequestHandler<GetLowStockProductsQuery, Result<List<ProductDto>>>
{
    public async Task<Result<List<ProductDto>>> Handle(GetLowStockProductsQuery request, CancellationToken cancellationToken)
    {
        // TODO Phase 3: move to Inventory context once InventoryItem aggregate exists
        var items = await _products.GetLowStockAsync(request.Threshold, cancellationToken);
        var dtos = items.Select(p => p.ToDto(string.Empty)).ToList();
        return Result<List<ProductDto>>.Ok(dtos);
    }
}
