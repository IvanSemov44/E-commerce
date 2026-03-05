namespace ECommerce.Application.DTOs.Payments;

/// <summary>
/// DTO for payment response after processing.
/// </summary>
public record PaymentResponseDto
{
    public bool Success { get; init; }
    public string? PaymentIntentId { get; init; }
    public string Message { get; init; } = null!;
    public string? TransactionId { get; init; }
    public DateTime ProcessedAt { get; init; }
    public string PaymentMethod { get; init; } = null!;
    public decimal Amount { get; init; }
    public string Status { get; init; } = null!;
    public Dictionary<string, string>? Metadata { get; init; }
}