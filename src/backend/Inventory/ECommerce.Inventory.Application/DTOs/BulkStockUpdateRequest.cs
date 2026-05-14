namespace ECommerce.Inventory.Application.DTOs;

public class BulkStockUpdateItem
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

public class BulkStockUpdateRequest
{
    public List<BulkStockUpdateItem> Updates { get; set; } = new();
}
