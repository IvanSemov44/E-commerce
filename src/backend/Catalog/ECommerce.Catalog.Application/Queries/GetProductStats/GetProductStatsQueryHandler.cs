using System.Threading;
using System.Threading.Tasks;
using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Catalog.Application.Queries.GetProductStats;

public class GetProductStatsQueryHandler(IProductRepository products)
    : IRequestHandler<GetProductStatsQuery, Result<ProductStatsDto>>
{
    public async Task<Result<ProductStatsDto>> Handle(GetProductStatsQuery query, CancellationToken ct)
    {
        var totalProducts = await products.GetActiveProductsCountAsync(ct);
        return Result<ProductStatsDto>.Ok(new ProductStatsDto { TotalProducts = totalProducts });
    }
}
