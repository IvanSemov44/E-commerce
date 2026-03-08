namespace ECommerce.Application.DTOs.Inventory;

public class BulkStockUpdateItem
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
