namespace ECommerce.Inventory.Application.Queries.GetInventory;

public record GetInventoryQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool LowStockOnly = false
) : IRequest<Result<PaginatedResult<InventoryItemDto>>>;