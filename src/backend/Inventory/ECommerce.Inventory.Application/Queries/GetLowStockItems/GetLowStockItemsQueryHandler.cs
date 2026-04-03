using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Inventory.Application.DTOs;
using ECommerce.Inventory.Domain.Interfaces;

namespace ECommerce.Inventory.Application.Queries.GetLowStockItems;

public class GetLowStockItemsQueryHandler(IInventoryItemRepository _repo)
    : IRequestHandler<GetLowStockItemsQuery, Result<List<InventoryItemDto>>>
{
    public async Task<Result<List<InventoryItemDto>>> Handle(
        GetLowStockItemsQuery query, CancellationToken cancellationToken)
    {
        var items = await _repo.GetLowStockAsync(query.ThresholdOverride, cancellationToken);

        return Result<List<InventoryItemDto>>.Ok(items.Select(i => new InventoryItemDto(
            i.Id, i.ProductId, i.Stock.Quantity, i.LowStockThreshold,
            i.Stock.Quantity <= i.LowStockThreshold,
            i.Stock.Quantity <= 0)).ToList());
    }
}