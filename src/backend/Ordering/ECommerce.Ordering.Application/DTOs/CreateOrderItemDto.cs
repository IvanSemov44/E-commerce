namespace ECommerce.Ordering.Application.DTOs;

public class CreateOrderItemDto
{
    public string ProductId { get; set; } = null!;
    public int Quantity { get; set; }
}
