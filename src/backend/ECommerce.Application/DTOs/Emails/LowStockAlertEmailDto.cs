namespace ECommerce.Application.DTOs.Emails;

/// <summary>
/// DTO for low stock alert email notification.
/// Consolidates 6 parameters into a single object for email service methods.
/// </summary>
public record LowStockAlertEmailDto
{
    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string Email { get; init; } = null!;

    /// <summary>
    /// Recipient first name for personalization.
    /// </summary>
    public string FirstName { get; init; } = null!;

    /// <summary>
    /// Product name that triggered the low stock alert.
    /// </summary>
    public string ProductName { get; init; } = null!;

    /// <summary>
    /// Current stock quantity of the product.
    /// </summary>
    public int CurrentStock { get; init; }

    /// <summary>
    /// Stock threshold that triggered the alert.
    /// </summary>
    public int Threshold { get; init; }

    /// <summary>
    /// Product SKU (optional, for reference in email).
    /// </summary>
    public string? Sku { get; init; }
}
