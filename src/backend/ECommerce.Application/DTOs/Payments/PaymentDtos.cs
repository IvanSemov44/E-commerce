namespace ECommerce.Application.DTOs.Payments;

/// <summary>
/// DTO for initiating a payment for an order.
/// </summary>
public class ProcessPaymentDto
{
    public Guid OrderId { get; set; }
    public string PaymentMethod { get; set; } = null!; // "stripe", "paypal", "credit_card", etc.
    public string? CardToken { get; set; } // Stripe token or payment token
    public string? PayPalEmail { get; set; } // For PayPal payments via email
    public string? PaypalToken { get; set; } // For PayPal payments via token (alternative to PayPalEmail)
    public decimal Amount { get; set; }
    public string? IdempotencyKey { get; set; } // For idempotent requests
}

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

/// <summary>
/// DTO for retrieving payment details.
/// </summary>
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

/// <summary>
/// DTO for refund requests.
/// </summary>
public class RefundPaymentDto
{
    public Guid OrderId { get; set; }
    public string? PaymentIntentId { get; set; }
    public decimal? Amount { get; set; } // null = full refund
    public string? Reason { get; set; }
}

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

/// <summary>
/// DTO for payment webhook events from payment providers.
/// </summary>
public class PaymentWebhookDto
{
    public string? EventType { get; set; }
    public string? PaymentIntentId { get; set; }
    public decimal? Amount { get; set; }
    public string? Status { get; set; }
    public string? Currency { get; set; }
    public long? Timestamp { get; set; }
}
