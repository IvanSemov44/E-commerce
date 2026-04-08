namespace ECommerce.Payments.Application.DTOs;

public class RefundPaymentDto
{
    public Guid OrderId { get; set; }
    public string? PaymentIntentId { get; set; }
    public decimal? Amount { get; set; }
    public string? Reason { get; set; }
}
