namespace ECommerce.Payments.Application.DTOs;

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
