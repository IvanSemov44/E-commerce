namespace ECommerce.Inventory.Application.Queries.GetInventory;

public class GetInventoryQueryHandler(IInventoryItemRepository _repo)
    : IRequestHandler<GetInventoryQuery, Result<PaginatedResult<InventoryItemDto>>>
{
    public async Task<Result<PaginatedResult<InventoryItemDto>>> Handle(
        GetInventoryQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repo.GetPagedAsync(
            query.Page, query.PageSize, query.Search, query.LowStockOnly, cancellationToken);

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
