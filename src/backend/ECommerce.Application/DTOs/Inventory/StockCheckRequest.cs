namespace ECommerce.Application.DTOs.Inventory;

public class StockCheckRequest
{
    public List<StockCheckItemDto> Items { get; set; } = new();
}
