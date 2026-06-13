namespace ECommerce.Inventory.Application.Queries.GetLowStockItems;

public class GetLowStockItemsQueryHandler(IInventoryItemRepository _repo)
    : IRequestHandler<GetLowStockItemsQuery, Result<PaginatedResult<InventoryItemDto>>>
{
    public async Task<Result<PaginatedResult<InventoryItemDto>>> Handle(
        GetLowStockItemsQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repo.GetLowStockPagedAsync(
            query.Page, query.PageSize, query.ThresholdOverride, cancellationToken);

        var dtos = items.Select(i => new InventoryItemDto(
            i.Id, i.ProductId, i.Stock.Quantity, i.LowStockThreshold,
            i.Stock.Quantity <= i.LowStockThreshold,
            i.Stock.Quantity <= 0)).ToList();

        return Result<PaginatedResult<InventoryItemDto>>.Ok(new PaginatedResult<InventoryItemDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }
}
