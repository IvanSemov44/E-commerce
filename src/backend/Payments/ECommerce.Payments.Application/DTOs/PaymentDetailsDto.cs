namespace ECommerce.Payments.Application.DTOs;

public record PaymentDetailsDto
{
    public Guid OrderId { get; init; }
    public string PaymentIntentId { get; init; } = null!;
    public string Status { get; init; } = null!;
    public string PaymentMethod { get; init; } = null!;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public string? ErrorMessage { get; init; }
}
