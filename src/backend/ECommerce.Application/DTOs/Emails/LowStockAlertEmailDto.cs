namespace ECommerce.Application.DTOs.Emails;

/// <summary>
/// DTO for low stock alert email notification.
/// Consolidates 6 parameters into a single object for email service methods.
/// </summary>
public class LowStockAlertEmailDto
{
    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// Recipient first name for personalization.
    /// </summary>
    public string FirstName { get; set; } = null!;

    /// <summary>
    /// Product name that triggered the low stock alert.
    /// </summary>
    public string ProductName { get; set; } = null!;

    /// <summary>
    /// Current stock quantity of the product.
    /// </summary>
    public int CurrentStock { get; set; }

    /// <summary>
    /// Stock threshold that triggered the alert.
    /// </summary>
    public int Threshold { get; set; }

    /// <summary>
    /// Product SKU (optional, for reference in email).
    /// </summary>
    public string? Sku { get; set; }
}
