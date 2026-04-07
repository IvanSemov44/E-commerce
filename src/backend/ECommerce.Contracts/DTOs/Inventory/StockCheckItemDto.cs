namespace ECommerce.Contracts.DTOs.Inventory;

public class StockCheckItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

