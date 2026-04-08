namespace ECommerce.Contracts.DTOs.Inventory;

public class AdjustStockRequest
{
    public int Quantity { get; set; }
    public string Reason { get; set; } = null!;
    public string? Notes { get; set; }
}

