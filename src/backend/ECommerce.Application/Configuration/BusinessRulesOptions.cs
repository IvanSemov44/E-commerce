namespace ECommerce.Application.Configuration;

/// <summary>
/// Configuration options for business rules and calculations.
/// Allows customization of tax rates, shipping costs, and other business logic.
/// </summary>
public class BusinessRulesOptions
{
    public const string SectionName = "BusinessRules";

    /// <summary>
    /// Minimum order subtotal for free shipping (default: 100.00).
    /// </summary>
    public decimal FreeShippingThreshold { get; set; } = 100.00m;

    /// <summary>
    /// Standard shipping cost when order doesn't qualify for free shipping (default: 10.00).
    /// </summary>
    public decimal StandardShippingCost { get; set; } = 10.00m;

    /// <summary>
    /// Tax rate applied to order subtotal (default: 0.08 = 8%).
    /// </summary>
    public decimal TaxRate { get; set; } = 0.08m;
}
