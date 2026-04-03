using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Inventory.Application.DTOs;
using ECommerce.Inventory.Application.Errors;
using ECommerce.Inventory.Domain.Interfaces;

namespace ECommerce.Inventory.Application.Commands.AdjustStock;

public class AdjustStockCommandHandler(
    IInventoryItemRepository _repo,
    IUnitOfWork _uow
) : IRequestHandler<AdjustStockCommand, Result<StockAdjustmentResultDto>>
{
    public async Task<Result<StockAdjustmentResultDto>> Handle(
        AdjustStockCommand command, CancellationToken cancellationToken)
    {
        var item = await _repo.GetByProductIdAsync(command.ProductId, cancellationToken);
        if (item is null)
            return Result<StockAdjustmentResultDto>.Fail(InventoryApplicationErrors.InventoryItemNotFound);

        var previousQty = item.Stock.Quantity;
        var result = item.Adjust(command.NewQuantity, command.Reason);
        if (!result.IsSuccess)
            return Result<StockAdjustmentResultDto>.Fail(result.GetErrorOrThrow());

        await _uow.SaveChangesAsync(cancellationToken);

        return Result<StockAdjustmentResultDto>.Ok(new StockAdjustmentResultDto(
            command.ProductId,
            item.Stock.Quantity,
            command.NewQuantity - previousQty,
            DateTime.UtcNow));
    }
}