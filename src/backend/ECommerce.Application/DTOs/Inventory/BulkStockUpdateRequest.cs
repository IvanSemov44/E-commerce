namespace ECommerce.Application.DTOs.Inventory;

public class BulkStockUpdateRequest
{
    public List<BulkStockUpdateItem> Updates { get; set; } = new();
}
