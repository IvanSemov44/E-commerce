namespace ECommerce.Inventory.Application.DTOs;

public class StockCheckItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

public class StockCheckRequest
{
    public List<StockCheckItemDto> Items { get; set; } = new();
}
