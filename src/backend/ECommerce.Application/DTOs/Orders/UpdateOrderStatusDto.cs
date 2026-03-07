namespace ECommerce.Application.DTOs.Orders;

/// <summary>
/// Request DTO for updating order status.
/// </summary>
public class UpdateOrderStatusDto
{
    public string Status { get; set; } = null!;
    public string? TrackingNumber { get; set; }
}