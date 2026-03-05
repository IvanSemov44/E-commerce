namespace ECommerce.Application.DTOs.Orders;

public class CreateOrderItemDto
{
    public string ProductId { get; set; } = null!;
    public int Quantity { get; set; }
}