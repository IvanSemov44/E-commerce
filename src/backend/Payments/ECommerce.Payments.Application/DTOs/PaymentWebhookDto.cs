namespace ECommerce.Payments.Application.DTOs;

public class PaymentWebhookDto
{
    public string? EventType { get; set; }
    public string? PaymentIntentId { get; set; }
    public decimal? Amount { get; set; }
    public string? Status { get; set; }
    public string? Currency { get; set; }
    public long? Timestamp { get; set; }
}
