using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Inventory.Application.DTOs;
using ECommerce.Inventory.Application.Errors;
using ECommerce.Inventory.Domain.Interfaces;

namespace ECommerce.Inventory.Application.Commands.ReduceStock;

public class ReduceStockCommandHandler(
    IInventoryItemRepository _repo,
    IUnitOfWork _uow
) : IRequestHandler<ReduceStockCommand, Result<StockAdjustmentResultDto>>
{
    public async Task<Result<StockAdjustmentResultDto>> Handle(
        ReduceStockCommand command, CancellationToken cancellationToken)
    {
        var item = await _repo.GetByProductIdAsync(command.ProductId, cancellationToken);
        if (item is null)
            return Result<StockAdjustmentResultDto>.Fail(InventoryApplicationErrors.InventoryItemNotFound);

        var result = item.Reduce(command.Amount, command.Reason);
        if (!result.IsSuccess)
            return Result<StockAdjustmentResultDto>.Fail(result.GetErrorOrThrow());

        await _uow.SaveChangesAsync(cancellationToken);

        return Result<StockAdjustmentResultDto>.Ok(new StockAdjustmentResultDto(
            command.ProductId, item.Stock.Quantity, -command.Amount, DateTime.UtcNow));
    }
}