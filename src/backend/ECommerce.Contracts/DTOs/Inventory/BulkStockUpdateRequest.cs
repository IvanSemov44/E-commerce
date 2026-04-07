namespace ECommerce.Contracts.DTOs.Inventory;

public class BulkStockUpdateRequest
{
    public List<BulkStockUpdateItem> Updates { get; set; } = new();
}

