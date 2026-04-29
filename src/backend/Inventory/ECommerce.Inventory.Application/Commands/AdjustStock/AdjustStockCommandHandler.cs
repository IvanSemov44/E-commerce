namespace ECommerce.Inventory.Application.Commands.AdjustStock;

public class AdjustStockCommandHandler(
    IInventoryItemRepository _repo,
    IInventoryProjectionEventPublisher _projectionPublisher)
    : IRequestHandler<AdjustStockCommand, Result<StockAdjustmentResultDto>>
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

        await _projectionPublisher.PublishStockProjectionUpdatedAsync(
            command.ProductId,
            item.Stock.Quantity,
            command.Reason,
            cancellationToken);

        return Result<StockAdjustmentResultDto>.Ok(new StockAdjustmentResultDto(
            command.ProductId,
            item.Stock.Quantity,
            command.NewQuantity - previousQty,
            DateTime.UtcNow));
    }
}
