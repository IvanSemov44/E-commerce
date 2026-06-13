namespace ECommerce.Inventory.Application.Queries.CheckBulkStockAvailability;

public class CheckBulkStockAvailabilityQueryHandler(IInventoryItemRepository _repo)
    : IRequestHandler<CheckBulkStockAvailabilityQuery, Result<BulkStockAvailabilityDto>>
{
    public async Task<Result<BulkStockAvailabilityDto>> Handle(
        CheckBulkStockAvailabilityQuery query, CancellationToken cancellationToken)
    {
        var productIds = query.Items.Select(x => x.ProductId).Distinct().ToList();
        var inventoryItems = await _repo.GetByProductIdsAsync(productIds, cancellationToken);
        var inventoryMap = inventoryItems.ToDictionary(i => i.ProductId);

        var issues = new List<StockAvailabilityIssueDto>();

        foreach (var item in query.Items)
        {
            if (!inventoryMap.TryGetValue(item.ProductId, out var inv))
            {
                issues.Add(new StockAvailabilityIssueDto(item.ProductId, 0, item.Quantity, "Product not found"));
                continue;
            }

            if (inv.Stock.Quantity < item.Quantity)
                issues.Add(new StockAvailabilityIssueDto(item.ProductId, inv.Stock.Quantity, item.Quantity, "Insufficient stock"));
        }

        return Result<BulkStockAvailabilityDto>.Ok(new BulkStockAvailabilityDto(issues.Count == 0, issues));
    }
}
