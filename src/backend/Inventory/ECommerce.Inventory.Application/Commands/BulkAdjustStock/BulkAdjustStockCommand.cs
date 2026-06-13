namespace ECommerce.Inventory.Application.Commands.BulkAdjustStock;

public record BulkAdjustStockItem(Guid ProductId, int NewQuantity);

public record BulkAdjustStockCommand(IReadOnlyList<BulkAdjustStockItem> Updates)
    : IRequest<Result<List<StockAdjustmentResultDto>>>, ITransactionalCommand;
