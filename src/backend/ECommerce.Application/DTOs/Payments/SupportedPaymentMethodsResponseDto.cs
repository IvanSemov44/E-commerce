namespace ECommerce.Application.DTOs.Payments;

/// <summary>
/// DTO for supported payment methods response.
/// Provides a structured response instead of a plain list for better extensibility.
/// </summary>
public class SupportedPaymentMethodsResponseDto
{
    /// <summary>
    /// List of supported payment method identifiers.
    /// Examples: "stripe", "paypal", "credit_card", "debit_card", "apple_pay", "google_pay"
    /// </summary>
    public List<string> Methods { get; set; } = new();
}
