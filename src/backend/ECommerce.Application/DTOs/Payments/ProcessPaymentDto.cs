namespace ECommerce.Application.DTOs.Payments;

/// <summary>
/// DTO for initiating a payment for an order.
/// </summary>
public class ProcessPaymentDto
{
    public Guid OrderId { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public string? CardToken { get; set; }
    public string? PayPalEmail { get; set; }
    public string? PaypalToken { get; set; }
    public decimal Amount { get; set; }
    public string? IdempotencyKey { get; set; }
}
