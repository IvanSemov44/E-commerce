namespace ECommerce.Inventory.Application.Commands.ReduceStock;

public class ReduceStockCommandHandler(IInventoryItemRepository _repo)
    : IRequestHandler<ReduceStockCommand, Result<StockAdjustmentResultDto>>
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

        return Result<StockAdjustmentResultDto>.Ok(new StockAdjustmentResultDto(
            command.ProductId, item.Stock.Quantity, -command.Amount, DateTime.UtcNow));
    }
}
