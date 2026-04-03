using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Inventory.Application.DTOs;
using ECommerce.Inventory.Domain.Interfaces;

namespace ECommerce.Inventory.Application.Queries.GetInventory;

public class GetInventoryQueryHandler(IInventoryItemRepository _repo)
    : IRequestHandler<GetInventoryQuery, Result<List<InventoryItemDto>>>
{
    public async Task<Result<List<InventoryItemDto>>> Handle(
        GetInventoryQuery query, CancellationToken cancellationToken)
    {
        var items = query.LowStockOnly
            ? await _repo.GetLowStockAsync(cancellationToken: cancellationToken)
            : await _repo.GetAllAsync(cancellationToken);

        var dtos = items
            .Select(i => new InventoryItemDto(
                i.Id, i.ProductId, i.Stock.Quantity, i.LowStockThreshold,
                i.Stock.Quantity <= i.LowStockThreshold,
                i.Stock.Quantity <= 0))
            .ToList();

        return Result<List<InventoryItemDto>>.Ok(dtos);
    }
}