namespace ECommerce.Application.DTOs.Payments;

/// <summary>
/// DTO for initiating a payment for an order.
/// </summary>
public class ProcessPaymentDto
{
    public Guid OrderId { get; set; }
    public string PaymentMethod { get; set; } = null!; // "stripe", "paypal", "credit_card", etc.
    public string? CardToken { get; set; } // Stripe token or payment token
    public string? PayPalEmail { get; set; } // For PayPal payments
    public decimal Amount { get; set; }
    public string? IdempotencyKey { get; set; } // For idempotent requests
}

/// <summary>
/// DTO for payment response after processing.
/// </summary>
public class PaymentResponseDto
{
    public bool Success { get; set; }
    public string? PaymentIntentId { get; set; }
    public string Message { get; set; } = null!;
    public string? TransactionId { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Status { get; set; } = null!; // "pending", "processing", "completed", "failed"
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// DTO for retrieving payment details.
/// </summary>
public class PaymentDetailsDto
{
    public Guid OrderId { get; set; }
    public string PaymentIntentId { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string PaymentMethod { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
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
public class RefundResponseDto
{
    public bool Success { get; set; }
    public string? RefundId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = null!;
    public string Message { get; set; } = null!;
    public DateTime ProcessedAt { get; set; }
}
