namespace ECommerce.Inventory.Application.Queries.GetLowStockItems;

public record GetLowStockItemsQuery(
    int? ThresholdOverride = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PaginatedResult<InventoryItemDto>>>;