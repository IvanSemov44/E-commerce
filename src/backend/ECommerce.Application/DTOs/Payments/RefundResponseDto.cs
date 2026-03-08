namespace ECommerce.Application.DTOs.Payments;

/// <summary>
/// DTO for refund response.
/// </summary>
public record RefundResponseDto
{
    public bool Success { get; init; }
    public string? RefundId { get; init; }
    public decimal Amount { get; init; }
    public string Status { get; init; } = null!;
    public string Message { get; init; } = null!;
    public DateTime ProcessedAt { get; init; }
}
