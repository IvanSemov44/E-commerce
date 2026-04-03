using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Inventory.Application.DTOs;
using ECommerce.Inventory.Application.Errors;
using ECommerce.Inventory.Domain.Interfaces;

namespace ECommerce.Inventory.Application.Queries.GetInventoryByProductId;

public class GetInventoryByProductIdQueryHandler(IInventoryItemRepository _repo)
    : IRequestHandler<GetInventoryByProductIdQuery, Result<InventoryItemDto>>
{
    public async Task<Result<InventoryItemDto>> Handle(
        GetInventoryByProductIdQuery query, CancellationToken cancellationToken)
    {
        var item = await _repo.GetByProductIdAsync(query.ProductId, cancellationToken);
        if (item is null)
            return Result<InventoryItemDto>.Fail(InventoryApplicationErrors.InventoryItemNotFound);

        return Result<InventoryItemDto>.Ok(new InventoryItemDto(
            item.Id, item.ProductId, item.Stock.Quantity, item.LowStockThreshold,
            item.Stock.Quantity <= item.LowStockThreshold,
            item.Stock.Quantity <= 0));
    }
}