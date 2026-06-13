namespace ECommerce.Inventory.Application.Queries.CheckBulkStockAvailability;

public record StockCheckItem(Guid ProductId, int Quantity);

public record CheckBulkStockAvailabilityQuery(IReadOnlyList<StockCheckItem> Items)
    : IRequest<Result<BulkStockAvailabilityDto>>;
