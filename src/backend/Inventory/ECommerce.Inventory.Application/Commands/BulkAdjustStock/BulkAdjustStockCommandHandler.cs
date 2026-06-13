namespace ECommerce.Inventory.Application.Commands.BulkAdjustStock;

public class BulkAdjustStockCommandHandler(IInventoryItemRepository _repo)
    : IRequestHandler<BulkAdjustStockCommand, Result<List<StockAdjustmentResultDto>>>
{
    public async Task<Result<List<StockAdjustmentResultDto>>> Handle(
        BulkAdjustStockCommand command, CancellationToken cancellationToken)
    {
        var productIds = command.Updates.Select(x => x.ProductId).Distinct().ToList();
        var inventoryItems = await _repo.GetByProductIdsAsync(productIds, cancellationToken);
        var inventoryMap = inventoryItems.ToDictionary(i => i.ProductId);

        var results = new List<StockAdjustmentResultDto>();

        foreach (var update in command.Updates)
        {
            if (!inventoryMap.TryGetValue(update.ProductId, out var item))
                return Result<List<StockAdjustmentResultDto>>.Fail(InventoryApplicationErrors.InventoryItemNotFound);

            var previousQty = item.Stock.Quantity;
            var adjustResult = item.Adjust(update.NewQuantity, "bulk_update");
            if (!adjustResult.IsSuccess)
                return Result<List<StockAdjustmentResultDto>>.Fail(adjustResult.GetErrorOrThrow());

            results.Add(new StockAdjustmentResultDto(
                update.ProductId,
                item.Stock.Quantity,
                update.NewQuantity - previousQty,
                DateTime.UtcNow));
        }

        return Result<List<StockAdjustmentResultDto>>.Ok(results);
    }
}
